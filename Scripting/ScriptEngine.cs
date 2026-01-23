using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using CSMOO.Logging;
using CSMOO.Commands;
using CSMOO.Configuration;
using CSMOO.Database;
using LiteDB;
using CSMOO.Object;
using CSMOO.Functions;
using CSMOO.Verbs;
using System.Runtime.CompilerServices;
using CSMOO.Exceptions;
using CSMOO.Core;
using CSMOO.Scripting;
using System.Text;
using System.Text.RegularExpressions;

namespace CSMOO.Scripting;

/// <summary>
/// Unified script engine for executing both verbs and functions with consistent behavior
/// </summary>
public class ScriptEngine
{
    private readonly ScriptOptions _scriptOptions;
    private readonly IObjectManager _objectManager;
    private readonly ILogger _logger;
    private readonly IConfig _config;
    private readonly IObjectResolver _objectResolver;
    private readonly IVerbResolver _verbResolver;
    private readonly IFunctionResolver _functionResolver;
    private readonly IDbProvider _dbProvider;
    private readonly IPlayerManager _playerManager;
    private readonly IVerbManager _verbManager;
    private readonly IRoomManager _roomManager;
    private readonly ICompilationCache _compilationCache;
    private readonly Lazy<HashSet<string>> _knownTypeNames;

    // Primary constructor with DI dependencies
    public ScriptEngine(
        IObjectManager objectManager,
        ILogger logger,
        IConfig config,
        IObjectResolver objectResolver,
        IVerbResolver verbResolver,
        IFunctionResolver functionResolver,
        IDbProvider dbProvider,
        IPlayerManager playerManager,
        IVerbManager verbManager,
        IRoomManager roomManager,
        ICompilationCache compilationCache)
    {
        _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _objectResolver = objectResolver ?? throw new ArgumentNullException(nameof(objectResolver));
        _verbResolver = verbResolver ?? throw new ArgumentNullException(nameof(verbResolver));
        _functionResolver = functionResolver ?? throw new ArgumentNullException(nameof(functionResolver));
        _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
        _verbManager = verbManager ?? throw new ArgumentNullException(nameof(verbManager));
        _roomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
        _compilationCache = compilationCache ?? throw new ArgumentNullException(nameof(compilationCache));

        // Ensure circular dependency is wired for PlayerManagerInstance.
        // Many code paths construct PlayerManagerInstance directly; it requires SetObjectManager()
        // or calls like GetOnlinePlayers() will throw "ObjectManager not set".
        if (_playerManager is CSMOO.Object.PlayerManagerInstance pmi)
        {
            pmi.SetObjectManager(_objectManager);
        }
        
        _scriptOptions = ScriptOptions.Default
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest)
            .WithReferences(
                typeof(object).Assembly,                    // System.Object
                typeof(Console).Assembly,                   // System.Console
                typeof(Enumerable).Assembly,                // System.Linq
                typeof(GameObject).Assembly,                // Our game objects
                typeof(ObjectManager).Assembly,             // Our managers
                typeof(ScriptGlobals).Assembly,             // CSMOO.Scripting namespace
                typeof(HtmlAgilityPack.HtmlDocument).Assembly, // HtmlAgilityPack
                Assembly.GetExecutingAssembly()             // Current assembly
            )
            .WithImports(
                "System",
                "CSMOO.Core",
                "System.Dynamic",
                "System.Linq",
                "System.Collections.Generic",
                "CSMOO.Object",
                "CSMOO.Exceptions",
                "CSMOO.Verbs",
                "CSMOO.Functions",
                "HtmlAgilityPack"
            );
        
        // Build type name cache from referenced assemblies
        _knownTypeNames = new Lazy<HashSet<string>>(() => BuildTypeNameCache(_scriptOptions));
    }
    
    /// <summary>
    /// Builds a cache of all type names from the referenced assemblies
    /// </summary>
    private HashSet<string> BuildTypeNameCache(ScriptOptions scriptOptions)
    {
        var typeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Add C# keywords and built-in types
        var builtInTypes = new[]
        {
            "string", "int", "bool", "float", "double", "decimal", "char", "byte", "sbyte",
            "short", "ushort", "uint", "long", "ulong", "object", "void", "var", "dynamic"
        };
        foreach (var type in builtInTypes)
        {
            typeNames.Add(type);
        }
        
        // Get types from assemblies we know are referenced
        var assemblies = new[]
        {
            typeof(object).Assembly,                    // System.Object
            typeof(Console).Assembly,                   // System.Console
            typeof(Enumerable).Assembly,                // System.Linq
            typeof(GameObject).Assembly,                // Our game objects
            typeof(ObjectManager).Assembly,             // Our managers
            typeof(ScriptGlobals).Assembly,             // CSMOO.Scripting
            typeof(HtmlAgilityPack.HtmlDocument).Assembly, // HtmlAgilityPack
            Assembly.GetExecutingAssembly()            // Current assembly
        };
        
        foreach (var assembly in assemblies)
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    // Add the simple type name (e.g., "StringBuilder")
                    typeNames.Add(type.Name);
                    
                    // Add the full name without namespace for common types (e.g., "System.StringBuilder" -> "StringBuilder")
                    if (type.Namespace != null)
                    {
                        var shortName = type.FullName?.Substring(type.Namespace.Length + 1);
                        if (!string.IsNullOrEmpty(shortName))
                        {
                            typeNames.Add(shortName);
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Some types might not be loadable, that's okay
                foreach (var type in ex.Types.Where(t => t != null))
                {
                    try
                    {
                        if (type != null)
                        {
                            typeNames.Add(type.Name);
                            if (type.Namespace != null && type.FullName != null)
                            {
                                var shortName = type.FullName.Substring(type.Namespace.Length + 1);
                                if (!string.IsNullOrEmpty(shortName))
                                {
                                    typeNames.Add(shortName);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Skip unloadable types
                    }
                }
            }
            catch
            {
                // Skip assemblies we can't load
                continue;
            }
        }
        
        return typeNames;
    }
    
    /// <summary>
    /// Checks if an identifier is definitively a GameObject type (or subclass)
    /// Only returns true if we can definitively determine it's a GameObject type, not based on naming patterns
    /// </summary>
    private bool IsGameObjectType(string identifier, string code, int identifierStart)
    {
        // Known GameObject variables from ScriptGlobals - these are always GameObject types
        var knownGameObjectVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Player", "This", "ThisGameObject", "ThisPlayer", "ThisRoom", "ThisExit",
            "ThisObject", "Caller", "CallerGameObject", "CallerPlayer", "Location"
        };
        
        // Check if it's a known GameObject variable from ScriptGlobals
        if (knownGameObjectVars.Contains(identifier))
            return true;
        
        // Check if the identifier is declared as a GameObject type in the code
        // Look backwards from the identifier to find its declaration
        var searchStart = Math.Max(0, identifierStart - 1000); // Look back up to 1000 chars
        var searchText = code.Substring(searchStart, identifierStart - searchStart);
        
        // GameObject-derived types: GameObject, Room, Player, Exit, Item, Container
        // Pattern: GameObject? identifier = or Room? identifier = etc.
        var gameObjectTypePattern = new System.Text.RegularExpressions.Regex(@$"\b(GameObject\??|Room\??|Player\??|Exit\??|Item\??|Container\??)\s+{System.Text.RegularExpressions.Regex.Escape(identifier)}\s*=", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (gameObjectTypePattern.IsMatch(searchText))
            return true;
        
        // Pattern: var identifier = ObjectResolver.ResolveObject(...) or GetObject(...) or GetObjectByDbRef(...)
        var objectResolverPattern = new System.Text.RegularExpressions.Regex(@$"\bvar\s+{System.Text.RegularExpressions.Regex.Escape(identifier)}\s*=\s*(ObjectResolver\.ResolveObject|GetObject|GetObjectBy|GetObjectById|GetObjectByDbRef|GetGameObjectById|GetGameObjectByDbRef)\s*\(", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (objectResolverPattern.IsMatch(searchText))
            return true;
        
        // Pattern: var identifier = new (Room|Player|Exit|Item|Container|GameObject)(...)
        var newObjectPattern = new System.Text.RegularExpressions.Regex(@$"\bvar\s+{System.Text.RegularExpressions.Regex.Escape(identifier)}\s*=\s*new\s+(Room|Player|Exit|Item|Container|GameObject)\s*\(", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (newObjectPattern.IsMatch(searchText))
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Checks if the method call result needs a cast based on the assignment context
    /// </summary>
    private bool CheckIfNeedsCast(string code, int callEnd)
    {
        // Look ahead to see if the result is assigned to a typed variable
        var lookAhead = callEnd;
        while (lookAhead < code.Length && char.IsWhiteSpace(code[lookAhead]))
            lookAhead++;
        
        if (lookAhead < code.Length && code[lookAhead] == '=')
        {
            // Found assignment, look backwards for type declaration
            var searchStart = Math.Max(0, callEnd - 100);
            var searchText = code.Substring(searchStart, callEnd - searchStart);
            
            // Check for typed variable declarations: List<GameObject> var = or string var =
            var typedVarPattern = new System.Text.RegularExpressions.Regex(@"\b(List<[^>]+>|string|int|bool|GameObject\??|Room\??|Player\??)\s+\w+\s*=");
            return typedVarPattern.IsMatch(searchText);
        }
        
        return false;
    }
    
    /// <summary>
    /// Determines the cast type needed based on function definition or assignment context
    /// Tries to get actual return type from function definition, falls back to assignment context
    /// </summary>
    private string? DetermineCastType(string code, int callEnd, string methodName, string objectIdentifier)
    {
        // Try to infer from assignment context
        var lookAhead = callEnd;
        while (lookAhead < code.Length && char.IsWhiteSpace(code[lookAhead]))
            lookAhead++;
        
        if (lookAhead < code.Length && code[lookAhead] == '=')
        {
            // Found assignment, look backwards for type declaration
            var searchStart = Math.Max(0, callEnd - 300);
            var searchText = code.Substring(searchStart, callEnd - searchStart);
            
            // Try to extract type from variable declaration: string description = or List<GameObject> exits =
            var typePattern = new System.Text.RegularExpressions.Regex(@"\b(List<[^>]+>|string\??|int\??|bool\??|GameObject\??|Room\??|Player\??|Exit\??|Item\??|Container\??)\s+\w+\s*=");
            var match = typePattern.Match(searchText);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        
        // If we can't infer from context, return null (no cast) - let the compiler handle it
        // The function might return object? which is fine
        return null;
    }

    // Backward compatibility constructor
    public ScriptEngine()
        : this(CreateDefaultObjectManager(), CreateDefaultLogger(), CreateDefaultConfig(), CreateDefaultObjectResolver(),
               CreateDefaultVerbResolver(), CreateDefaultFunctionResolver(), CreateDefaultDbProvider(),
               CreateDefaultPlayerManager(), CreateDefaultVerbManager(), CreateDefaultRoomManager(), CreateDefaultCompilationCache())
    {
    }

    private static IDbProvider CreateDefaultDbProvider()
    {
        return DbProvider.Instance;
    }

    private static IPlayerManager CreateDefaultPlayerManager()
    {
        return new PlayerManagerInstance(DbProvider.Instance);
    }

    private static IVerbManager CreateDefaultVerbManager()
    {
        return new VerbManagerInstance(DbProvider.Instance);
    }

    private static IRoomManager CreateDefaultRoomManager()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        return new RoomManagerInstance(dbProvider, logger, objectManager);
    }

    private static ICompilationCache CreateDefaultCompilationCache()
    {
        return new CompilationCache();
    }

    // Helper methods for backward compatibility
    private static IObjectManager CreateDefaultObjectManager()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        return new ObjectManagerInstance(dbProvider, classManager);
    }

    private static ILogger CreateDefaultLogger()
    {
        return new LoggerInstance(Config.Instance);
    }

    private static IConfig CreateDefaultConfig()
    {
        return Config.Instance;
    }

    private static IObjectResolver CreateDefaultObjectResolver()
    {
        var dbProvider = DbProvider.Instance;
        var config = Config.Instance;
        var logger = new LoggerInstance(config);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        var coreClassFactory = new CoreClassFactoryInstance(dbProvider, logger);
        return new ObjectResolverInstance(objectManager, coreClassFactory);
    }

    private static IVerbResolver CreateDefaultVerbResolver()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        return new VerbResolverInstance(dbProvider, objectManager, logger);
    }

    private static IFunctionResolver CreateDefaultFunctionResolver()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        return new FunctionResolverInstance(dbProvider, objectManager);
    }

    /// <summary>
    /// Execute a verb with unified script globals
    /// </summary>
    public string ExecuteVerb(Verb verb, string input, Player player,
        CommandProcessor commandProcessor, string? thisObjectId = null, Dictionary<string, string>? variables = null)
    {
        var result = ExecuteVerbWithResult(verb, input, player, commandProcessor, thisObjectId, variables);
        return result.result;
    }

    /// <summary>
    /// Execute a verb with unified script globals and return both success status and result
    /// </summary>
    public (bool success, string result) ExecuteVerbWithResult(Verb verb, string input, Player player,
        CommandProcessor commandProcessor, string? thisObjectId = null, Dictionary<string, string>? variables = null)
    {
        var previousContext = Builtins.UnifiedContext; // Store previous context
        try
        {
            var actualThisObjectId = thisObjectId ?? verb.ObjectId;
            var thisObject = _objectManager.GetObject(verb.ObjectId);
            var playerObject = _objectManager.GetObject(player.Id);

            // Debug logging to identify null objects
            if (thisObject == null)
            {
                _logger.Warning($"ExecuteVerb: thisObject is null for ID '{actualThisObjectId}' (verb: {verb.Name})");
            }
            if (playerObject == null)
            {
                _logger.Warning($"ExecuteVerb: playerObject is null for ID '{player.Id}' (verb: {verb.Name})");
            }
            // check if thisObject has an admin flag
            var isAdmin = (thisObject?.Permissions.Contains("admin") == true);

            var globals = isAdmin 
                ? new AdminScriptGlobals(_objectManager, _objectResolver, _verbResolver, _functionResolver, _dbProvider)
                : new ScriptGlobals(_objectManager, _verbResolver, _functionResolver, _dbProvider);


            globals.Player = player; // Always the Database.Player
            globals.This = thisObject;
            globals.ThisGameObject = thisObject; // Set typed version
            globals.Caller = previousContext?.This ?? playerObject; // The object that called this verb (or Player if no previous context)
            globals.CallerGameObject = previousContext?.ThisGameObject ?? playerObject; // Set typed version
            globals.CallDepth = (previousContext?.CallDepth ?? 0) + 1; // Track call depth
            globals.CommandProcessor = commandProcessor;
            globals.Input = input;
            globals.Args = ParseArguments(input);
            globals.Verb = verb.Name;
            globals.Variables = variables ?? new Dictionary<string, string>();


            // Check for maximum call depth
            if (globals.CallDepth > _config.Scripting.MaxCallDepth)
            {
                throw new InvalidOperationException($"Maximum script call depth exceeded: {globals.CallDepth} > {_config.Scripting.MaxCallDepth}");
            }

            // If we have a previous context, inherit the Helpers to maintain consistency
            if (previousContext != null)
            {
                globals.Helpers = previousContext.Helpers;
            }
            else
            {
                globals.Helpers = new ScriptHelpers(player, commandProcessor, _objectManager, _playerManager, _dbProvider, _logger, _verbManager, _roomManager);
            }

            // Set ThisObject to the same value as This (since it's an alias)
            globals.ThisObject = globals.This;

            // Initialize the object factory for enhanced script support
            globals.InitializeObjectFactory();

            // Check cache first
            var cachedScript = _compilationCache.GetVerb(verb.Id);
            var currentHash = ComputeCodeHash(verb.Code);
            var cachedHash = _compilationCache.GetVerbCodeHash(verb.Id);

            Script<object> script;
            bool useCache = cachedScript != null && cachedHash != null && currentHash == cachedHash;

            if (useCache && cachedScript != null)
            {
                // Use cached compiled script (fast!)
                script = cachedScript;
                _logger.Debug($"Using cached compiled script for verb '{verb.Name}' (ID: {verb.Id})");
            }
            else
            {
                // Need to compile (first time or code changed)
                // Build the complete script with automatic variable declarations and DBref/ID preprocessing
                var preprocessedCode = PreprocessObjectReferenceSyntax(verb.Code);
                var collectionFixedCode = PreprocessCollectionExpressions(preprocessedCode);
                var completeScript = BuildScriptWithVariables(collectionFixedCode, variables);

                // Create script
                script = CSharpScript.Create(completeScript, _scriptOptions, typeof(ScriptGlobals));
                
                // Cache for next time (only if compilation succeeded)
                // We'll cache after successful execution to ensure it works
            }

            // Set Builtins context for script execution
            Builtins.UnifiedContext = globals;

            // Push this verb onto the script call stack
            ScriptStackTrace.PushVerbFrame(verb, thisObject);

            // Execute with timeout
            using var cts = new CancellationTokenSource(_config.Scripting.MaxExecutionTimeMs);
            try
            {
                if (script == null)
                {
                    throw new InvalidOperationException("Script compilation failed - script is null");
                }
                var scriptResult = script.RunAsync(globals, cts.Token).Result;
                var returnValue = scriptResult.ReturnValue;

                // Check if the verb returns a boolean to indicate success/failure
                if (returnValue is bool boolResult)
                {
                    return (boolResult, "");
                }

                // If not a boolean, assume success and return the string representation
                var stringResult = returnValue?.ToString() ?? "";

                // Cache the compiled script if not already cached (successful execution)
                if (!useCache)
                {
                    var codeHash = ComputeCodeHash(verb.Code);
                    _compilationCache.SetVerb(verb.Id, script, codeHash);
                    _logger.Debug($"Cached compiled script for verb '{verb.Name}' (ID: {verb.Id})");
                }

                // Don't clear the stack trace here - let the top-level caller handle it
                // ScriptStackTrace.Clear();

                return (true, stringResult);
            }
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
            {
                ScriptStackTrace.UpdateCurrentFrame(ex, verb.Code);
                throw new ScriptExecutionException($"Script execution timed out after {_config.Scripting.MaxExecutionTimeMs}ms", ex);
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                // Unwrap AggregateException to get the real exception
                ScriptStackTrace.UpdateCurrentFrame(ex.InnerException, verb.Code);

                // If the inner exception is already a ScriptExecutionException, just re-throw it
                if (ex.InnerException is ScriptExecutionException)
                {
                    throw ex.InnerException;
                }
                else
                {
                    throw new ScriptExecutionException(ex.InnerException.Message, ex.InnerException);
                }
            }
            catch (OperationCanceledException ex)
            {
                ScriptStackTrace.UpdateCurrentFrame(ex, verb.Code);
                throw new ScriptExecutionException($"Script execution timed out after {_config.Scripting.MaxExecutionTimeMs}ms", ex);
            }
            catch (Exception ex)
            {
                ScriptStackTrace.UpdateCurrentFrame(ex, verb.Code);

                // If it's already a ScriptExecutionException, preserve the original stack frames
                if (ex is ScriptExecutionException scriptEx)
                {
                    throw; // Just re-throw to preserve the original exception with its frames
                }
                else
                {
                    throw new ScriptExecutionException(ex.Message, ex);
                }
            }
        }
        catch (Exception ex)
        {
            // Just update the current frame and re-throw - let the caller handle messaging
            ScriptStackTrace.UpdateCurrentFrame(ex, verb.Code);
            throw;
        }
        finally
        {
            // Pop the verb frame from the script call stack
            ScriptStackTrace.PopFrame();

            // Restore previous Builtins context to support nested function calls
            Builtins.UnifiedContext = previousContext;
        }
    }

    /// <summary>
    /// Execute a function with unified script globals and type checking
    /// </summary>
    public object? ExecuteFunction(Function function, object?[] parameters, Player player,
        CommandProcessor? commandProcessor = null, string? thisObjectId = null)
    {

        var actualThisObjectId = thisObjectId ?? function.ObjectId;
        var previousContext = Builtins.UnifiedContext; // Store previous context
        var thisObject = _objectManager.GetObject(actualThisObjectId);
        var playerObject = _objectManager.GetObject(player.Id);

        // Context validation
        if (thisObject == null)
            throw new ScriptExecutionException($"{function.Name} cannot be invoked due to a ScriptEngine context error");

        // Keyword logic - check access modifiers
        // Note: Owner check is only required for Internal functions, not for Public functions
        if (function.AccessModifiers.Contains(Keyword.Private) && actualThisObjectId != (previousContext?.This?.Id ?? playerObject?.Id))
            throw new ScriptExecutionException($"Function '{function.Name}' is private to {thisObject?.Name}({thisObject?.Id}).");
        
        // Internal functions require an owner to check ownership
        if (function.AccessModifiers.Contains(Keyword.Internal))
        {
            if (thisObject?.Owner == null)
                throw new ScriptExecutionException($"Function '{function.Name}' cannot be executed because its object ({thisObject?.Name}(#{thisObject?.DbRef}) has no owner (required for Internal access).");
            if (thisObject.Owner.Id != (previousContext?.This?.Owner?.Id ?? playerObject?.Id))
                throw new ScriptExecutionException($"Function '{function.Name}' is internal to {thisObject.Owner.Name}({thisObject.Owner.Id}).");
        }
        
        if (function.AccessModifiers.Contains(Keyword.Protected) && thisObject?.ClassId != previousContext?.This?.ClassId)
            throw new ScriptExecutionException($"Function '{function.Name}' is protected and {previousContext?.This?.Name}(#{previousContext?.This?.DbRef}) has a different class to {thisObject?.Name}(#{thisObject?.DbRef})");
        

        try
        {
            // Validate parameter count
            if (parameters.Length != function.ParameterTypes.Length)
            {
                throw new ArgumentException($"Function '{function.Name}' expects {function.ParameterTypes.Length} parameters, but {parameters.Length} were provided.");
            }

            // Validate parameter types
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!ValidateParameterType(parameters[i], function.ParameterTypes[i]))
                {
                    throw new ArgumentException($"Parameter {i + 1} of function '{function.Name}' expected type '{function.ParameterTypes[i]}', but got '{parameters[i]?.GetType().Name ?? "null"}'.");
                }
            }




            // Debug logging to identify null objects
            if (thisObject == null)
            {
                _logger.Warning($"ExecuteFunction: thisObject is null for ID '{actualThisObjectId}' (function: {function.Name})");
            }
            if (playerObject == null)
            {
                _logger.Warning($"ExecuteFunction: playerObject is null for ID '{player.Id}' (function: {function.Name})");
            }

            // Create globals for function execution
            var globals = new ScriptGlobals(_objectManager, _verbResolver, _functionResolver, _dbProvider)
            {
                Player = player, // Always the Database.Player
                This = thisObject ?? CreateNullGameObject(actualThisObjectId),
                ThisGameObject = thisObject ?? CreateNullGameObject(actualThisObjectId), // Set typed version
                Caller = previousContext?.This ?? playerObject, // The object that called this function (or Player if no previous context)
                CallerGameObject = previousContext?.ThisGameObject ?? playerObject, // Set typed version
                CallDepth = (previousContext?.CallDepth ?? 0) + 1, // Track call depth
                CommandProcessor = commandProcessor!, // May be null for some function calls, but Helpers will handle it
                CallingObjectId = actualThisObjectId,
                Parameters = parameters
            };

            // Check for maximum call depth
            if (globals.CallDepth > _config.Scripting.MaxCallDepth)
            {
                throw new InvalidOperationException($"Maximum script call depth exceeded: {globals.CallDepth} > {_config.Scripting.MaxCallDepth}");
            }

            // If we have a previous context, inherit the Helpers to maintain consistency
            if (previousContext != null)
            {
                globals.Helpers = previousContext.Helpers;
            }
            else if (commandProcessor != null)
            {
                globals.Helpers = new ScriptHelpers(player, commandProcessor, _objectManager, _playerManager, _dbProvider, _logger, _verbManager, _roomManager);
            }

            // Set ThisObject to the same value as This (since it's an alias)
            globals.ThisObject = globals.This;

            // Add parameters as named variables to the globals
            for (int i = 0; i < function.ParameterNames.Length && i < parameters.Length; i++)
            {
                var paramName = function.ParameterNames[i];
                if (!string.IsNullOrEmpty(paramName))
                {
                    globals.SetParameter(paramName, parameters[i]);
                }
            }

            // Initialize the object factory for enhanced script support
            globals.InitializeObjectFactory();

            // Check cache first
            var cachedScript = _compilationCache.GetFunction(function.Id);
            var currentHash = ComputeCodeHash(function.Code);
            var cachedHash = _compilationCache.GetFunctionCodeHash(function.Id);

            Script<object> script;
            bool useCache = cachedScript != null && cachedHash != null && currentHash == cachedHash;

            if (useCache && cachedScript != null)
            {
                // Use cached compiled script (fast!)
                script = cachedScript;
                _logger.Debug($"Using cached compiled script for function '{function.Name}' (ID: {function.Id})");
            }
            else
            {
                // Need to compile (first time or code changed)
                // Build script code that declares the parameters as variables
                var scriptCode = new System.Text.StringBuilder();

                // Declare each parameter as a local variable
                for (int i = 0; i < function.ParameterNames.Length && i < parameters.Length; i++)
                {
                    var paramName = function.ParameterNames[i];
                    var paramType = function.ParameterTypes[i];
                    if (!string.IsNullOrEmpty(paramName))
                    {
                        scriptCode.AppendLine($"{paramType} {paramName} = ({paramType})GetParameter(\"{paramName}\");");
                    }
                }

                // Add the actual function code with DBref/ID preprocessing
                var preprocessedFunctionCode = PreprocessObjectReferenceSyntax(function.Code);
                var collectionFixedFunctionCode = PreprocessCollectionExpressions(preprocessedFunctionCode);
                scriptCode.AppendLine(collectionFixedFunctionCode);

                var finalCode = scriptCode.ToString();

                // Create script
                script = CSharpScript.Create(finalCode, _scriptOptions, typeof(ScriptGlobals));
            }

            // Set Builtins context for script execution
            Builtins.UnifiedContext = globals;

            // Push this function onto the script call stack
            ScriptStackTrace.PushFunctionFrame(function, thisObject);

            // Execute with timeout
            using var cts = new CancellationTokenSource(_config.Scripting.MaxExecutionTimeMs);
            ScriptState<object> result;
            try
            {
                if (script == null)
                {
                    throw new InvalidOperationException("Script compilation failed - script is null");
                }
                result = script.RunAsync(globals, cts.Token).Result;
            }
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
            {
                ScriptStackTrace.UpdateCurrentFrame(ex, function.Code);
                throw new ScriptExecutionException($"Function '{function.Name}' execution timed out after {_config.Scripting.MaxExecutionTimeMs}ms", ex);
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                // Unwrap AggregateException to get the real exception
                ScriptStackTrace.UpdateCurrentFrame(ex.InnerException, function.Code);

                // If the inner exception is already a ScriptExecutionException, just re-throw it
                if (ex.InnerException is ScriptExecutionException)
                {
                    throw ex.InnerException;
                }
                else
                {
                    throw new ScriptExecutionException(ex.InnerException.Message, ex.InnerException);
                }
            }
            catch (OperationCanceledException ex)
            {
                ScriptStackTrace.UpdateCurrentFrame(ex, function.Code);
                throw new ScriptExecutionException($"Function '{function.Name}' execution timed out after {_config.Scripting.MaxExecutionTimeMs}ms", ex);
            }
            catch (Exception ex)
            {
                ScriptStackTrace.UpdateCurrentFrame(ex, function.Code);

                // If it's already a ScriptExecutionException, preserve the original stack frames
                if (ex is ScriptExecutionException scriptEx)
                {
                    throw; // Just re-throw to preserve the original exception with its frames
                }
                else
                {
                    throw new ScriptExecutionException(ex.Message, ex);
                }
            }

            // Validate return type
            var returnValue = result.ReturnValue;
            if (!ValidateReturnType(returnValue, function.ReturnType))
            {
                _logger.Warning($"Function '{function.Name}' returned unexpected type. Expected '{function.ReturnType}', got '{returnValue?.GetType().Name ?? "null"}'.");
            }

            // Cache the compiled script if not already cached (successful execution)
            if (!useCache)
            {
                var codeHash = ComputeCodeHash(function.Code);
                _compilationCache.SetFunction(function.Id, script, codeHash);
                _logger.Debug($"Cached compiled script for function '{function.Name}' (ID: {function.Id})");
            }

            // Don't clear the stack trace here - let the top-level caller handle it
            // ScriptStackTrace.Clear();

            return returnValue;
        }
        catch (Exception ex)
        {
            // Just update the current frame and re-throw - let the caller handle messaging
            ScriptStackTrace.UpdateCurrentFrame(ex, function.Code);
            throw;
        }
        finally
        {
            // Pop the function frame from the script call stack
            ScriptStackTrace.PopFrame();

            // Restore previous Builtins context to support nested function calls
            Builtins.UnifiedContext = previousContext;
        }
    }

    private List<string> ParseArguments(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Skip(1).ToList(); // Skip the verb itself
    }

    /// <summary>
    /// Creates a placeholder GameObject for null object references
    /// This ensures This and ThisObject are never null, preventing runtime errors
    /// </summary>
    private static GameObject CreateNullGameObject(string objectId)
    {
        // Create a minimal GameObject that represents a missing/null object
        var nullGameObject = new GameObject
        {
            Id = objectId,
            DbRef = 0,
            Properties = new LiteDB.BsonDocument(),
            Location = null,
            Contents = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        // Add a special property to indicate this is a null object
        nullGameObject.Properties["_isNullObject"] = true;
        nullGameObject.Properties["name"] = $"<missing object {objectId}>";

        return nullGameObject;
    }

    /// <summary>
    /// Preprocesses script code (collection expression preprocessing removed - scripts should use proper types)
    /// </summary>
    private string PreprocessCollectionExpressions(string originalCode)
    {
        if (string.IsNullOrEmpty(originalCode))
            return originalCode;

        // Collection expression preprocessing removed - scripts should use proper types
        // (List<GameObject>, List<string>, etc.) instead of dynamic
        return originalCode;
    }

    /// <summary>
    /// Preprocesses script code to transform DBref syntax (#4.property, #4.function(), etc.) and ID syntax ($objectId.property) into valid C# code
    /// </summary>
    private string PreprocessObjectReferenceSyntax(string originalCode)
    {
        if (string.IsNullOrEmpty(originalCode))
            return originalCode;

        var result = originalCode;

        // Handle DBref patterns: #<number>.<identifier> or #<number>.<identifier>(...)
        var dbrefPattern = @"#(\d+)\.(\w+)(\(.*?\))?";
        result = System.Text.RegularExpressions.Regex.Replace(result, dbrefPattern, match =>
        {
            var dbref = match.Groups[1].Value;
            var member = match.Groups[2].Value;
            var methodCall = match.Groups[3].Value; // Will be empty for properties, contains (...) for methods

            if (!string.IsNullOrEmpty(methodCall))
            {
                // This is a method call: #4.methodName(args) -> GetObjectByDbRef(4).methodName(args)
                return $"GetObjectByDbRef({dbref}).{member}{methodCall}";
            }
            else
            {
                // This is a property access: #4.propertyName -> GetObjectByDbRef(4).propertyName
                return $"GetObjectByDbRef({dbref}).{member}";
            }
        });

        // Handle DBref assignment patterns: #4.property = value
        var dbrefAssignmentPattern = @"#(\d+)\.(\w+)\s*=";
        result = System.Text.RegularExpressions.Regex.Replace(result, dbrefAssignmentPattern, match =>
        {
            var dbref = match.Groups[1].Value;
            var member = match.Groups[2].Value;

            return $"GetObjectByDbRef({dbref}).{member} =";
        });

        // Handle ID patterns: $<identifier>.<identifier> or $<identifier>.<identifier>(...)
        var idPattern = @"\$([a-zA-Z0-9\-_]+)\.(\w+)(\(.*?\))?";
        result = System.Text.RegularExpressions.Regex.Replace(result, idPattern, match =>
        {
            var objectId = match.Groups[1].Value;
            var member = match.Groups[2].Value;
            var methodCall = match.Groups[3].Value; // Will be empty for properties, contains (...) for methods

            if (!string.IsNullOrEmpty(methodCall))
            {
                // This is a method call: $objectId.methodName(args) -> GetObjectById("objectId").methodName(args)
                return $"GetObjectById(\"{objectId}\").{member}{methodCall}";
            }
            else
            {
                // This is a property access: $objectId.propertyName -> GetObjectById("objectId").propertyName
                return $"GetObjectById(\"{objectId}\").{member}";
            }
        });

        // Handle ID assignment patterns: $objectId.property = value
        var idAssignmentPattern = @"\$([a-zA-Z0-9\-_]+)\.(\w+)\s*=";
        result = System.Text.RegularExpressions.Regex.Replace(result, idAssignmentPattern, match =>
        {
            var objectId = match.Groups[1].Value;
            var member = match.Groups[2].Value;

            return $"GetObjectById(\"{objectId}\").{member} =";
        });

        // Handle method calls on GameObject variables: variableName.MethodName(args)
        // This rewrites typed method calls to use CallFunctionOnObject to avoid dynamic casting
        // Process the string manually to handle nested parentheses correctly
        result = RewriteMethodCalls(result);

        return result;
    }

    /// <summary>
    /// Builds a complete script by injecting variable declarations before the main code
    /// </summary>
    private string BuildScriptWithVariables(string originalCode, Dictionary<string, string>? variables)
    {
        var scriptBuilder = new System.Text.StringBuilder();

        // Enable nullable reference types for the script
        scriptBuilder.AppendLine("#nullable enable");
        scriptBuilder.AppendLine();

        // Add variable declarations from pattern matching
        // IMPORTANT: Read from Variables dictionary at runtime instead of hardcoding values
        // This allows cached scripts to work correctly with different runtime variable values
        if (variables != null && variables.Count > 0)
        {
            scriptBuilder.AppendLine("// Auto-generated variable declarations from pattern matching");
            scriptBuilder.AppendLine("// Variables are read from Variables dictionary at runtime to support caching");
            foreach (var kvp in variables)
            {
                // Read from Variables dictionary instead of hardcoding the value
                // This ensures cached scripts work with actual runtime values
                scriptBuilder.AppendLine($"string {kvp.Key} = Variables.TryGetValue(\"{kvp.Key}\", out var _{kvp.Key}_val) ? _{kvp.Key}_val : \"\";");
            }
            scriptBuilder.AppendLine();
        }

        // Add automatic object resolution variables
        var objectVariables = ExtractPotentialObjectReferences(originalCode);
        if (objectVariables.Count > 0)
        {
            scriptBuilder.AppendLine("// Auto-resolved object variables (typed)");
            foreach (var objectVar in objectVariables)
            {
                // Infer type based on variable name
                var inferredType = InferObjectType(objectVar);
                scriptBuilder.AppendLine($"{inferredType} {objectVar} = ObjectResolver.ResolveObject(\"{objectVar}\", This) ?? throw new ScriptExecutionException(\"Object '{objectVar}' not found\");");
            }
            scriptBuilder.AppendLine();
        }

        // Add the original verb code
        scriptBuilder.AppendLine("// Original script code:");
        scriptBuilder.AppendLine(originalCode);

        return scriptBuilder.ToString();
    }

    /// <summary>
    /// Extracts potential object references from script code that could be resolved by ObjectResolver
    /// </summary>
    private HashSet<string> ExtractPotentialObjectReferences(string code)
    {
        var potentialObjects = new HashSet<string>();
        
        if (string.IsNullOrEmpty(code))
            return potentialObjects;

        // Remove comments from the code before processing
        var codeWithoutComments = RemoveComments(code);
        
        // Remove string literals to avoid matching content inside strings
        var codeWithoutStrings = RemoveStringLiterals(codeWithoutComments);

        // Known type/class names that should not be auto-resolved (they're static types, not object instances)
        var knownStaticTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "StringComparer", "StringComparison", "System", "Console", "Math", "DateTime",
            "TimeSpan", "Guid", "Regex", "Encoding", "Convert", "Environment", "AppDomain",
            "Type", "Assembly", "Activator", "Task", "Thread", "ThreadPool", "Linq",
            "Enumerable", "List", "Dictionary", "HashSet", "Array", "StringBuilder",
            "Object", "Char", "Int32", "Int64", "Double", "Single", "Boolean", "Decimal"
        };

        // Look for patterns like: identifier.something (property access or method call)
        // But not string endings like "word." or punctuation
        // IMPORTANT: Exclude namespace-qualified identifiers like System.Text, System.Char, etc.
        var objectAccessPattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\.\s*[a-zA-Z_]";
        var matches = Regex.Matches(codeWithoutStrings, objectAccessPattern);

        // Collect all unique identifiers first
        var candidateIdentifiers = new HashSet<string>();
        foreach (Match match in matches)
        {
            var identifier = match.Groups[1].Value;
            var fullMatch = match.Value;
            
            // Skip namespace-qualified identifiers (System.Text, System.Char, etc.)
            // These are namespace references, not object instances that should be auto-resolved
            if (identifier == "System" && (fullMatch.Contains("System.Text") || fullMatch.Contains("System.Char")))
            {
                continue; // Skip System when it's part of System.Text or System.Char
            }
            
            // Skip if it's a known static type (like StringComparer, StringComparison, etc.)
            if (!knownStaticTypes.Contains(identifier))
            {
                candidateIdentifiers.Add(identifier);
            }
        }

        // Also check for variable declarations in the user's code to avoid conflicts
        var declaredInUserCode = FindVariableDeclaredInCode(codeWithoutStrings);
        
        // Then check each unique identifier only once
        foreach (var identifier in candidateIdentifiers)
        {
            // Skip if already declared in user's code
            if (declaredInUserCode.Contains(identifier))
                continue;
                
            // Check if this identifier is already defined/available
            if (!IsIdentifierAlreadyDefined(identifier))
            {
                potentialObjects.Add(identifier);
            }
        }

        return potentialObjects;
    }

    /// <summary>
    /// Infers the C# type for an object variable based on its name
    /// </summary>
    private string InferObjectType(string variableName)
    {
        var lowerName = variableName.ToLower();
        
        // Common patterns for player references
        if (lowerName == "player" || lowerName == "me" || lowerName == "caller" && variableName.ToLower() == "caller")
        {
            return "Player?";
        }
        
        // Common patterns for room references
        if (lowerName == "room" || lowerName == "here" || lowerName == "location")
        {
            return "Room?";
        }
        
        // Common patterns for exit references
        if (lowerName == "exit" || lowerName == "door")
        {
            return "Exit?";
        }
        
        // System object
        if (lowerName == "system")
        {
            return "GameObject?";
        }
        
        // Default to GameObject? for unknown types
        return "GameObject?";
    }

    /// <summary>
    /// Checks if an identifier is already defined in the script context
    /// </summary>
    private bool IsIdentifierAlreadyDefined(string identifier)
    {
        // Check against known ScriptGlobals properties and methods
        var knownIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // ScriptGlobals properties
            "Player", "This", "ThisGameObject", "ThisPlayer", "ThisRoom", "ThisExit",
            "ThisObject", "Caller", "CallerGameObject", "CallerPlayer", "CallDepth",
            "ThisObjectId", "CommandProcessor", "ObjectManager", "WorldManager",
            "PlayerManager", "Helpers", "Location", "Input", "Args", "Verb", "Variables",
            "Parameters", "CallingObjectId", "me", "player", "here",
            // ScriptGlobals methods
            "obj", "objById", "GetObject", "GetObjectById", "GetGameObjectById",
            "GetObjectByDbRef", "GetGameObjectByDbRef", "Say", "notify", "SayToRoom",
            "GetThisGameObject", "GetPlayerGameObject", "GetPlayerLocation",
            "GetThisProperty", "SetThisProperty", "GetPlayer", "FindObjectInRoom",
            "CallVerb", "CallFunction", "ThisVerb", "Me", "Here", "System", "Object", "Class",
            // Builtins static methods (commonly used)
            "Builtins", "ObjectResolver",
            // Common types
            "string", "int", "bool", "var", "dynamic", "object", "void",
            // Known static types and their members
            "StringComparer", "StringComparison", "Console", "Math", "DateTime",
            "TimeSpan", "Guid", "Regex", "Encoding", "Convert", "Environment", "AppDomain",
            "Type", "Assembly", "Activator", "Task", "Thread", "ThreadPool", "Linq",
            "Enumerable", "List", "Dictionary", "HashSet", "Array", "StringBuilder",
            "Char", "Int32", "Int64", "Double", "Single", "Boolean", "Decimal",
            // Common enum values
            "OrdinalIgnoreCase", "CurrentCulture", "InvariantCulture", "Ordinal",
            // Common parameter names that might be used in local functions
            // These are added as a safety net in case parameter detection misses them
            "usage", "summary", "category", "topic", "help", "description",
            // Namespace-qualified types (to avoid matching partial names)
            "ObjectManager", "ObjectResolver", "Builtins"
        };
        
        if (knownIdentifiers.Contains(identifier))
            return true;
        
        // Also check with a test script compilation
        var testScript = $"var test = {identifier};";
        
        try
        {
            // Try to create a script with the identifier - if it compiles, it's already defined
            var script = CSharpScript.Create(testScript, _scriptOptions, typeof(ScriptGlobals));
            // We don't need to run it, just check if it compiles successfully
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();
            
            // If there are no errors about undefined variables, it's already defined
            return !diagnostics.Any(d => d.Id == "CS0103"); // CS0103 = "The name 'X' does not exist in the current context"
        }
        catch
        {
            // If compilation fails for any reason, assume it's not defined
            return false;
        }
    }

    private bool IsKnownStaticType(string identifier)
    {
        var staticTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "StringComparer", "StringComparison", "System", "Console", "Math", "DateTime",
            "TimeSpan", "Guid", "Regex", "Encoding", "Convert", "Environment", "AppDomain",
            "Type", "Assembly", "Activator", "Task", "Thread", "ThreadPool", "Linq",
            "Enumerable", "List", "Dictionary", "HashSet", "Array", "StringBuilder",
            "Object", "Char", "Int32", "Int64", "Double", "Single", "Boolean", "Decimal",
            "ObjectManager", "ObjectResolver", "Builtins"
        };
        return staticTypes.Contains(identifier);
    }

    /// <summary>
    /// Rewrites method calls on GameObject variables to use CallFunctionOnObject
    /// Only rewrites method calls on identifiers that are likely variables, not types
    /// </summary>
    private string RewriteMethodCalls(string code)
    {
        if (string.IsNullOrEmpty(code))
            return code;

        var knownBuiltInMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ToString", "GetType", "Equals", "GetHashCode", "ReferenceEquals",
            "MemberwiseClone", "CompareTo", "Clone"
        };

        // Use reflection-based type cache instead of hardcoded list
        var knownTypes = _knownTypeNames.Value;

        var result = new System.Text.StringBuilder();
        var i = 0;
        var inString = false;
        var inCharLiteral = false;
        var stringChar = '\0';
        
        while (i < code.Length)
        {
            // Handle string and character literals - don't rewrite inside them
            if (!inString && !inCharLiteral && (code[i] == '"' || code[i] == '\''))
            {
                if (code[i] == '"')
                {
                    inString = true;
                    stringChar = '"';
                }
                else
                {
                    inCharLiteral = true;
                    stringChar = '\'';
                }
                result.Append(code[i]);
                i++;
                continue;
            }
            
            if (inString || inCharLiteral)
            {
                result.Append(code[i]);
                // Check for end of string/char literal
                if (code[i] == stringChar)
                {
                    // Check if it's escaped
                    var isEscaped = false;
                    if (i > 0 && code[i - 1] == '\\' && stringChar == '"')
                    {
                        var backslashCount = 0;
                        var j = i - 1;
                        while (j >= 0 && code[j] == '\\')
                        {
                            backslashCount++;
                            j--;
                        }
                        isEscaped = (backslashCount % 2 == 1);
                    }
                    
                    if (!isEscaped)
                    {
                        inString = false;
                        inCharLiteral = false;
                        stringChar = '\0';
                    }
                }
                i++;
                continue;
            }
            
            // Look for pattern: identifier.MethodName(
            // where identifier starts with letter/underscore and MethodName starts with capital letter
            if (i < code.Length - 1 && (char.IsLetter(code[i]) || code[i] == '_'))
            {
                // Try to match identifier
                var identifierStart = i;
                while (i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] == '_'))
                    i++;
                
                var identifier = code.Substring(identifierStart, i - identifierStart);
                
                // Skip if it's a known type or keyword
                if (knownTypes.Contains(identifier) || IsKnownStaticType(identifier))
                {
                    result.Append(identifier);
                    continue;
                }
                
                // Check if this is a function call (identifier followed by '(') - don't rewrite those
                var checkPos = i;
                while (checkPos < code.Length && char.IsWhiteSpace(code[checkPos]))
                    checkPos++;
                if (checkPos < code.Length && code[checkPos] == '(')
                {
                    // This is a function call, not a variable - skip rewriting
                    result.Append(identifier);
                    continue;
                }
                
                // Check if this looks like a type declaration (e.g., "string message" or "List<string>")
                // Look ahead to see if this is followed by a space and then a variable name or generic type
                var lookAhead = i;
                while (lookAhead < code.Length && char.IsWhiteSpace(code[lookAhead]))
                    lookAhead++;
                
                if (lookAhead < code.Length)
                {
                    // Check for generic type parameter: List<string>
                    if (code[lookAhead] == '<')
                    {
                        result.Append(identifier);
                        continue;
                    }
                    
                    // Check if followed by a lowercase letter (likely a variable name in a parameter)
                    // This catches: "string message", "int count", etc.
                    if (char.IsLower(code[lookAhead]) || code[lookAhead] == '_')
                    {
                        result.Append(identifier);
                        continue;
                    }
                }
                
                if (i < code.Length && code[i] == '.')
                {
                    i++; // skip '.'
                    
                    // Check if next token is a capital letter (method name)
                    if (i < code.Length && char.IsUpper(code[i]))
                    {
                        var methodStart = i;
                        while (i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] == '_'))
                            i++;
                        
                        var methodName = code.Substring(methodStart, i - methodStart);
                        
                        // Skip whitespace before opening paren
                        while (i < code.Length && char.IsWhiteSpace(code[i]))
                            i++;
                        
                        if (i < code.Length && code[i] == '(')
                        {
                            // Found a method call pattern
                            // Skip if it's a known built-in C# method
                            if (!knownBuiltInMethods.Contains(methodName))
                            {
                                // Only rewrite if we can definitively determine this is a GameObject type
                                // Check if identifier is declared as GameObject or a subclass
                                if (IsGameObjectType(identifier, code, identifierStart))
                                {
                                    // Find matching closing parenthesis
                                    var parenStart = i;
                                    i++; // skip '('
                                    var depth = 1;
                                    
                                    while (i < code.Length && depth > 0)
                                    {
                                        if (code[i] == '(')
                                            depth++;
                                        else if (code[i] == ')')
                                            depth--;
                                        i++;
                                    }
                                    
                                    if (depth == 0)
                                    {
                                        // Found matching paren
                                        var argsStart = parenStart + 1;
                                        var argsEnd = i - 1;
                                        var args = code.Substring(argsStart, argsEnd - argsStart).Trim();
                                        
                                        // Determine cast type from function definition or assignment context
                                        var castType = DetermineCastType(code, i, methodName, identifier);
                                        
                                        // Rewrite to CallFunctionOnObject
                                        var callExpr = string.IsNullOrEmpty(args)
                                            ? $"CallFunctionOnObject({identifier}, \"{methodName}\")"
                                            : $"CallFunctionOnObject({identifier}, \"{methodName}\", {args})";
                                        
                                        if (!string.IsNullOrEmpty(castType))
                                        {
                                            result.Append($"({castType}){callExpr}");
                                        }
                                        else
                                        {
                                            result.Append(callExpr);
                                        }
                                        continue; // Already advanced i past the closing paren
                                    }
                                }
                            }
                            
                            // Not a method we should rewrite, or couldn't find matching paren
                            // Write back what we've consumed so far
                            result.Append(code.Substring(identifierStart, i - identifierStart));
                            continue;
                        }
                    }
                    
                    // Not a method call, write back what we consumed
                    result.Append(code.Substring(identifierStart, i - identifierStart));
                    continue;
                }
                
                // Not a method call pattern, write the identifier
                result.Append(identifier);
                continue;
            }
            
            // Regular character, just append
            result.Append(code[i]);
            i++;
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Removes single-line (//) and multi-line (/* */) comments from C# code
    /// </summary>
    private string RemoveComments(string code)
    {
        if (string.IsNullOrEmpty(code))
            return code;

        var result = new System.Text.StringBuilder();
        var lines = code.Split('\n');
        bool inMultiLineComment = false;

        foreach (var line in lines)
        {
            var processedLine = new System.Text.StringBuilder();
            bool inString = false;
            char? stringChar = null;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                char? next = i + 1 < line.Length ? line[i + 1] : null;

                // Handle string literals - don't remove comments inside strings
                if (!inMultiLineComment && (c == '"' || c == '\''))
                {
                    if (!inString)
                    {
                        inString = true;
                        stringChar = c;
                        processedLine.Append(c);
                    }
                    else if (c == stringChar)
                    {
                        // Check if it's escaped
                        int backslashCount = 0;
                        int j = i - 1;
                        while (j >= 0 && line[j] == '\\')
                        {
                            backslashCount++;
                            j--;
                        }
                        if (backslashCount % 2 == 0) // Even number of backslashes means not escaped
                        {
                            inString = false;
                            stringChar = null;
                        }
                        processedLine.Append(c);
                    }
                    else
                    {
                        processedLine.Append(c);
                    }
                    continue;
                }

                if (inString)
                {
                    processedLine.Append(c);
                    continue;
                }

                // Handle multi-line comments
                if (inMultiLineComment)
                {
                    if (c == '*' && next == '/')
                    {
                        inMultiLineComment = false;
                        i++; // Skip the '/'
                    }
                    continue;
                }

                // Check for start of multi-line comment
                if (c == '/' && next == '*')
                {
                    inMultiLineComment = true;
                    i++; // Skip the '*'
                    continue;
                }

                // Check for single-line comment
                if (c == '/' && next == '/')
                {
                    break; // Rest of line is a comment
                }

                processedLine.Append(c);
            }

            if (!inMultiLineComment)
            {
                result.AppendLine(processedLine.ToString());
            }
            else
            {
                result.AppendLine(); // Preserve line structure for error reporting
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Removes string literals from C# code to avoid matching content inside strings
    /// </summary>
    private string RemoveStringLiterals(string code)
    {
        if (string.IsNullOrEmpty(code))
            return code;

        var result = new System.Text.StringBuilder();
        bool inString = false;
        char? stringChar = null;
        
        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];

            // Handle string literals - replace content with spaces to preserve positions
            if (c == '"' || c == '\'')
            {
                if (!inString)
                {
                    inString = true;
                    stringChar = c;
                    result.Append(c); // Keep the opening quote
                }
                else if (c == stringChar)
                {
                    // Check if it's escaped
                    int backslashCount = 0;
                    int j = i - 1;
                    while (j >= 0 && code[j] == '\\')
                    {
                        backslashCount++;
                        j--;
                    }
                    if (backslashCount % 2 == 0) // Even number of backslashes means not escaped
                    {
                        inString = false;
                        stringChar = null;
                        result.Append(c); // Keep the closing quote
                    }
                    else
                    {
                        result.Append(' '); // Replace escaped quote with space
                    }
                }
                else
                {
                    result.Append(' '); // Replace string content with space
                }
            }
            else if (inString)
            {
                result.Append(' '); // Replace string content with spaces
            }
            else
            {
                result.Append(c); // Keep non-string content
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Finds variable names that are declared or assigned in the user's code
    /// </summary>
    private HashSet<string> FindVariableDeclaredInCode(string code)
    {
        var declaredVariables = new HashSet<string>();
        
        if (string.IsNullOrEmpty(code))
            return declaredVariables;

        // Look for variable declarations: TYPE name; or TYPE name = ...; or TYPE? name = ...; (nullable types)
        // Updated to handle nullable types like GameObject? resolved = ...
        // Match nullable types explicitly first: TypeName? varName =
        var nullablePattern = @"\b([a-zA-Z_][a-zA-Z0-9_<>\[\]]*)\?\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=";
        var nullableMatches = Regex.Matches(code, nullablePattern);
        foreach (Match match in nullableMatches)
        {
            if (match.Groups.Count > 2)
            {
                var variableName = match.Groups[2].Value;
                declaredVariables.Add(variableName);
            }
        }
        
        // Then match non-nullable types: TypeName varName = (including var, dynamic, string, etc.)
        var variableDeclarationPattern = @"\b(var|dynamic|string|int|bool|float|double|decimal|object|Player|GameObject|Room|Exit|ObjectClass|[a-zA-Z_][a-zA-Z0-9_<>\[\]]*)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=";
        var matches = Regex.Matches(code, variableDeclarationPattern);
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 2)
            {
                var variableName = match.Groups[2].Value;
                // Skip if already added from nullable pattern
                if (!declaredVariables.Contains(variableName))
                {
                    declaredVariables.Add(variableName);
                }
            }
        }

        // Look for assignments: name = ...;
        var assignmentPattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*=(?!=)";
        var assignmentMatches = Regex.Matches(code, assignmentPattern);

        foreach (Match match in assignmentMatches)
        {
            var variableName = match.Groups[1].Value;
            declaredVariables.Add(variableName);
        }

        // Also look for foreach variable declarations: foreach (var item in ...)
        var foreachPattern = @"\bforeach\s*\(\s*(var|dynamic|string|int|bool|float|double|decimal|object|Player|GameObject)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+in\b";
        var foreachMatches = Regex.Matches(code, foreachPattern);
        
        foreach (Match match in foreachMatches)
        {
            var variableName = match.Groups[2].Value;
            declaredVariables.Add(variableName);
        }

        // Check for function/method parameters: TypeName MethodName(TypeName? paramName) or TypeName MethodName(TypeName paramName, ...)
        // This pattern matches: returnType methodName(paramType? paramName) or returnType methodName(paramType paramName)
        // It handles nullable types, multiple parameters, and local functions
        var functionParamPattern = @"(?:public\s+|private\s+|static\s+|verb\s+)?(?:[a-zA-Z_][a-zA-Z0-9_<>\[\]]*\??\s+)?([a-zA-Z_][a-zA-Z0-9_]*)\s*\([^)]*?([a-zA-Z_][a-zA-Z0-9_<>\[\]]*\??)\s+([a-zA-Z_][a-zA-Z0-9_]*)";
        var functionParamMatches = Regex.Matches(code, functionParamPattern);
        foreach (Match match in functionParamMatches)
        {
            // Group 1 is method name, Group 2 is param type, Group 3 is param name
            if (match.Groups.Count > 3)
            {
                var paramType = match.Groups[2].Value;
                var paramName = match.Groups[3].Value;
                
                // Skip if already declared
                if (!declaredVariables.Contains(paramName))
                {
                    // Only add if param type looks like a type (starts with uppercase, is known type, or is nullable)
                    var baseType = paramType.TrimEnd('?');
                    if (baseType.Length > 0 && (char.IsUpper(baseType[0]) || 
                        baseType.Equals("string", StringComparison.OrdinalIgnoreCase) || 
                        baseType.Equals("int", StringComparison.OrdinalIgnoreCase) ||
                        baseType.Equals("bool", StringComparison.OrdinalIgnoreCase) ||
                        baseType.Equals("var", StringComparison.OrdinalIgnoreCase) ||
                        baseType.Equals("dynamic", StringComparison.OrdinalIgnoreCase)))
                    {
                        declaredVariables.Add(paramName);
                    }
                }
            }
        }
        
        // Also match simpler patterns for single parameters: (TypeName? paramName) or (TypeName paramName)
        // This catches cases where the method name pattern didn't match
        var simpleParamPattern = @"\(([a-zA-Z_][a-zA-Z0-9_<>\[\]]*\??)\s+([a-zA-Z_][a-zA-Z0-9_]*)\)";
        var simpleParamMatches = Regex.Matches(code, simpleParamPattern);
        foreach (Match match in simpleParamMatches)
        {
            if (match.Groups.Count > 2)
            {
                var paramType = match.Groups[1].Value;
                var paramName = match.Groups[2].Value;
                
                // Skip if already declared
                if (!declaredVariables.Contains(paramName))
                {
                    // Only add if param type looks like a type
                    var baseType = paramType.TrimEnd('?');
                    if (baseType.Length > 0 && (char.IsUpper(baseType[0]) || 
                        baseType.Equals("string", StringComparison.OrdinalIgnoreCase) || 
                        baseType.Equals("int", StringComparison.OrdinalIgnoreCase) ||
                        baseType.Equals("bool", StringComparison.OrdinalIgnoreCase) ||
                        baseType.Equals("var", StringComparison.OrdinalIgnoreCase) ||
                        baseType.Equals("dynamic", StringComparison.OrdinalIgnoreCase)))
                    {
                        declaredVariables.Add(paramName);
                    }
                }
            }
        }

        return declaredVariables;
    }

 
    /// <summary>
    /// Validates that a parameter matches the expected type
    /// </summary>
    private static bool ValidateParameterType(object? parameter, string expectedType)
    {
        if (expectedType == "object")
            return true; // object accepts anything

        if (parameter == null)
            return expectedType.EndsWith("?"); // null only allowed for nullable types

        var actualType = parameter.GetType();
        return expectedType.ToLower() switch
        {
            "string" => actualType == typeof(string),
            "int" => actualType == typeof(int),
            "bool" => actualType == typeof(bool),
            "float" => actualType == typeof(float),
            "double" => actualType == typeof(double),
            "decimal" => actualType == typeof(decimal),
            "player" => actualType == typeof(Player),
            "gameobject" => actualType == typeof(GameObject),
            "objectclass" => actualType == typeof(ObjectClass),
            _ => true // For unknown types, allow anything
        };
    }

    /// <summary>
    /// Validates that a return value matches the expected return type
    /// </summary>
    private static bool ValidateReturnType(object? returnValue, string expectedType)
    {
        if (expectedType == "void")
            return returnValue == null;

        if (expectedType == "object")
            return true; // object accepts anything

        if (returnValue == null)
            return expectedType.EndsWith("?"); // null only allowed for nullable types

        var actualType = returnValue.GetType();
        return expectedType.ToLower() switch
        {
            "string" => actualType == typeof(string),
            "int" => actualType == typeof(int),
            "bool" => actualType == typeof(bool),
            "float" => actualType == typeof(float),
            "double" => actualType == typeof(double),
            "decimal" => actualType == typeof(decimal),
            "player" => actualType == typeof(Player),
            "gameobject" => actualType == typeof(GameObject),
            "objectclass" => actualType == typeof(ObjectClass),
            _ => true // For unknown types, allow anything
        };
    }

    /// <summary>
    /// Computes SHA256 hash of code for cache invalidation
    /// </summary>
    private string ComputeCodeHash(string code)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}



