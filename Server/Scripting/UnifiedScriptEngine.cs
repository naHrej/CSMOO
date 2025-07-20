using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using CSMOO.Server.Database;
using CSMOO.Server.Database.Models;
using CSMOO.Server.Logging;
using CSMOO.Server.Commands;
using LiteDB;
using Database = CSMOO.Server.Database;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Unified script engine for executing both verbs and functions with consistent behavior
/// </summary>
public class UnifiedScriptEngine
{
    private readonly ScriptOptions _scriptOptions;

    public UnifiedScriptEngine()
    {
        _scriptOptions = ScriptOptions.Default
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest)
            .WithReferences(
                typeof(object).Assembly,                    // System.Object
                typeof(Console).Assembly,                   // System.Console
                typeof(Enumerable).Assembly,                // System.Linq
                typeof(GameObject).Assembly,                // Our game objects
                typeof(ObjectManager).Assembly,             // Our managers
                typeof(HtmlAgilityPack.HtmlDocument).Assembly, // HtmlAgilityPack
                Assembly.GetExecutingAssembly()             // Current assembly
            )
            .WithImports(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "System.Text",
                "System.Threading.Tasks",
                "CSMOO.Server.Database",
                "CSMOO.Server.Commands",
                "CSMOO.Server.Scripting",
                "HtmlAgilityPack"
            );
    }

    /// <summary>
    /// Execute a verb with unified script globals
    /// </summary>
    public string ExecuteVerb(Verb verb, string input, Database.Player player, 
        CommandProcessor commandProcessor, string? thisObjectId = null, Dictionary<string, string>? variables = null)
    {
        try
        {
            var actualThisObjectId = thisObjectId ?? verb.ObjectId;
            var thisObject = Database.ObjectManager.GetObject(actualThisObjectId);
            var playerObject = Database.ObjectManager.GetObject(player.Id);

            // Debug logging to identify null objects
            if (thisObject == null)
            {
                Logger.Warning($"ExecuteVerb: thisObject is null for ID '{actualThisObjectId}' (verb: {verb.Name})");
            }
            if (playerObject == null)
            {
                Logger.Warning($"ExecuteVerb: playerObject is null for ID '{player.Id}' (verb: {verb.Name})");
            }

            var globals = new UnifiedScriptGlobals
            {
                Player = playerObject,
                This = thisObject ?? CreateNullGameObject(actualThisObjectId),
                CommandProcessor = commandProcessor,
                Helpers = new ScriptHelpers(player, commandProcessor),
                Input = input,
                Args = ParseArguments(input),
                Verb = verb.Name,
                Variables = variables ?? new Dictionary<string, string>()
            };

            // Set ThisObject to the same value as This (since it's an alias)
            globals.ThisObject = globals.This;

            // Initialize the object factory for enhanced script support
            globals.InitializeObjectFactory();

            // Build the complete script with automatic variable declarations
            var completeScript = BuildScriptWithVariables(verb.Code, variables);

            // Set Builtins context for script execution
            Builtins.UnifiedContext = globals;
            
            var script = CSharpScript.Create(completeScript, _scriptOptions, typeof(UnifiedScriptGlobals));
            var result = script.RunAsync(globals).Result;
            
            return result.ReturnValue?.ToString() ?? "";
        }
        catch (Exception ex)
        {
            Logger.Error($"Script execution error in verb '{verb.Name}': {ex.Message}");
            if (ex.InnerException != null)
            {
                Logger.Error($"Inner exception: {ex.InnerException.Message}");
            }
            throw;
        }
        finally
        {
            // Clear Builtins context to avoid memory leaks
            Builtins.UnifiedContext = null;
        }
    }

    /// <summary>
    /// Execute a function with unified script globals and type checking
    /// </summary>
    public object? ExecuteFunction(Function function, object?[] parameters, Database.Player player, 
        CommandProcessor? commandProcessor = null, string? thisObjectId = null)
    {
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

            var actualThisObjectId = thisObjectId ?? function.ObjectId;
            var thisObject = Database.ObjectManager.GetObject(actualThisObjectId);
            var playerObject = Database.ObjectManager.GetObject(player.Id);

            // Debug logging to identify null objects
            if (thisObject == null)
            {
                Logger.Warning($"ExecuteFunction: thisObject is null for ID '{actualThisObjectId}' (function: {function.Name})");
            }
            if (playerObject == null)
            {
                Logger.Warning($"ExecuteFunction: playerObject is null for ID '{player.Id}' (function: {function.Name})");
            }

            // Create globals for function execution
            var globals = new UnifiedScriptGlobals
            {
                Player = playerObject,
                This = thisObject ?? CreateNullGameObject(actualThisObjectId),
                CommandProcessor = commandProcessor,
                CallingObjectId = actualThisObjectId,
                Parameters = parameters
            };

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

            // Build script code that declares the parameters as variables
            var scriptCode = new StringBuilder();
            
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
            
            // Add the actual function code
            scriptCode.AppendLine(function.Code);
            
            var finalCode = scriptCode.ToString();

            // Set Builtins context for script execution
            Builtins.UnifiedContext = globals;

            // Create and execute script
            var script = CSharpScript.Create(finalCode, _scriptOptions, typeof(UnifiedScriptGlobals));
            var result = script.RunAsync(globals).Result;

            // Validate return type
            var returnValue = result.ReturnValue;
            if (!ValidateReturnType(returnValue, function.ReturnType))
            {
                Logger.Warning($"Function '{function.Name}' returned unexpected type. Expected '{function.ReturnType}', got '{returnValue?.GetType().Name ?? "null"}'.");
            }

            return returnValue;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error executing function '{function.Name}': {ex.Message}");
            throw;
        }
        finally
        {
            // Clear Builtins context to avoid memory leaks
            Builtins.UnifiedContext = null;
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
            ClassId = "",
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
    /// Builds a complete script by injecting variable declarations before the main code
    /// </summary>
    private string BuildScriptWithVariables(string originalCode, Dictionary<string, string>? variables)
    {
        if (variables == null || variables.Count == 0)
        {
            return originalCode;
        }

        var scriptBuilder = new StringBuilder();
        
        // Add variable declarations at the beginning
        scriptBuilder.AppendLine("// Auto-generated variable declarations from pattern matching");
        foreach (var kvp in variables)
        {
            // Use simple string escaping for C# string literals
            var escapedValue = kvp.Value
                .Replace("\\", "\\\\")   // Escape backslashes
                .Replace("\"", "\\\"")   // Escape double quotes
                .Replace("\r", "\\r")    // Escape carriage returns
                .Replace("\n", "\\n")    // Escape newlines
                .Replace("\t", "\\t");   // Escape tabs
                
            scriptBuilder.AppendLine($"string {kvp.Key} = \"{escapedValue}\";");
            Logger.Debug($"Auto-declared variable: {kvp.Key} = \"{escapedValue}\"");
        }
        scriptBuilder.AppendLine();
        
        // Add the original verb code
        scriptBuilder.AppendLine("// Original script code:");
        scriptBuilder.AppendLine(originalCode);
        
        var completeScript = scriptBuilder.ToString();
        Logger.Debug($"Complete generated script:\n{completeScript}");
        
        return completeScript;
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
            "player" => actualType == typeof(Database.Player),
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
            "player" => actualType == typeof(Database.Player),
            "gameobject" => actualType == typeof(GameObject),
            "objectclass" => actualType == typeof(ObjectClass),
            _ => true // For unknown types, allow anything
        };
    }
}

/// <summary>
/// Unified script globals for both verb and function execution
/// </summary>
public class UnifiedScriptGlobals : EnhancedScriptGlobals
{
    /// <summary>
    /// Script helpers for advanced functionality
    /// </summary>
    public new ScriptHelpers? Helpers { get; set; }
    
    /// <summary>
    /// The object this script is running on (now directly GameObject with dynamic support)
    /// </summary>
    public GameObject? This { get; set; }
    
    /// <summary>
    /// Alias for This - the object this script is running on (now directly GameObject with dynamic support)
    /// </summary>
    public GameObject? ThisObject 
    { 
        get => This; 
        set => This = value; 
    }
    
    /// <summary>
    /// The current player as GameObject (now with dynamic support)
    /// </summary>
    public new GameObject? Player { get; set; }
    
    /// <summary>
    /// Get the underlying GameObject for This (for internal use)
    /// </summary>
    public GameObject? GetThisGameObject()
    {
        return This;
    }
    
    /// <summary>
    /// Get the underlying GameObject for Player (for internal use)
    /// </summary>
    public GameObject? GetPlayerGameObject()
    {
        return Player;
    }

    /// <summary>
    /// The complete input string that triggered this verb (null for functions)
    /// </summary>
    public string? Input { get; set; }
    
    /// <summary>
    /// Parsed arguments from the input (empty for functions)
    /// </summary>
    public List<string> Args { get; set; } = new List<string>();
    
    /// <summary>
    /// The name of the verb being executed (null for functions)
    /// </summary>
    public string? Verb { get; set; }

    /// <summary>
    /// Named variables extracted from the verb pattern (e.g., {item}, {person}) (empty for functions)
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Parameters passed to the function (empty for verbs)
    /// </summary>
    public object?[] Parameters { get; set; } = Array.Empty<object?>();

    /// <summary>
    /// The object ID that called this function (same as This.Id)
    /// </summary>
    public string CallingObjectId { get; set; } = "";

    private readonly Dictionary<string, object?> _namedParameters = new();

    /// <summary>
    /// Sets a named parameter that can be accessed in the script
    /// </summary>
    public void SetParameter(string name, object? value)
    {
        _namedParameters[name] = value;
    }

    /// <summary>
    /// Gets a named parameter
    /// </summary>
    public object? GetParameter(string name)
    {
        return _namedParameters.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// Get a property from the current object (this)
    /// </summary>
    public object? GetThisProperty(string propertyName)
    {
        var thisGameObject = GetThisGameObject();
        if (thisGameObject == null) return null;
        return Database.ObjectManager.GetProperty(thisGameObject, propertyName)?.RawValue;
    }

    /// <summary>
    /// Set a property on the current object (this)
    /// </summary>
    public void SetThisProperty(string propertyName, object? value)
    {
        var thisGameObject = GetThisGameObject();
        if (thisGameObject == null) return;
        
        // Convert value to BsonValue
        BsonValue bsonValue = value switch
        {
            null => BsonValue.Null,
            string s => new BsonValue(s),
            int i => new BsonValue(i),
            long l => new BsonValue(l),
            double d => new BsonValue(d),
            float f => new BsonValue((double)f),
            bool b => new BsonValue(b),
            DateTime dt => new BsonValue(dt),
            BsonValue bv => bv,
            _ => new BsonValue(value.ToString() ?? "")
        };
        
        Database.ObjectManager.SetProperty(thisGameObject, propertyName, bsonValue);
    }

    /// <summary>
    /// Send a message to a specific GameObject player
    /// </summary>
    public void notify(GameObject targetPlayer, string message)
    {
        var dbPlayer = targetPlayer as Database.Player ?? 
                      GameDatabase.Instance.Players.FindById(targetPlayer.Id);
        
        if (dbPlayer != null)
        {
            CommandProcessor?.SendToPlayer(message, dbPlayer.SessionGuid);
        }
    }

    /// <summary>
    /// Get a player by name or ID for use with notify()
    /// </summary>
    public GameObject? GetPlayer(string nameOrId)
    {
        // Try by name first
        var player = GameDatabase.Instance.Players.FindOne(p => 
            p.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase));
        
        if (player != null) return Database.ObjectManager.GetObject(player.Id);
        
        // Try by ID
        var playerById = GameDatabase.Instance.Players.FindById(nameOrId);
        return playerById != null ? Database.ObjectManager.GetObject(playerById.Id) : null;
    }

    /// <summary>
    /// Get the current player for use with notify() - returns dynamic GameObject
    /// </summary>
    public new dynamic? me => Player;

    /// <summary>
    /// Get the current player for use with notify() - returns dynamic GameObject
    /// </summary>
    public new dynamic? player => Player;

    /// <summary>
    /// Send a message to all players in the same room as the current player
    /// </summary>
    public new void SayToRoom(string message, bool includePlayer = false)
    {
        var playerGameObject = GetPlayerGameObject();
        if (playerGameObject?.Location == null) return;

        var playersInRoom = Database.PlayerManager.GetOnlinePlayers()
            .Where(p => p.Location == playerGameObject.Location)
            .ToList();

        foreach (var otherPlayer in playersInRoom)
        {
            if (!includePlayer && otherPlayer.Id == playerGameObject.Id)
                continue;

            CommandProcessor?.SendToPlayer(message, otherPlayer.SessionGuid);
        }
    }

    /// <summary>
    /// Find an object in the current room by name
    /// </summary>
    public string? FindObjectInRoom(string name)
    {
        var playerGameObject = GetPlayerGameObject();
        if (playerGameObject?.Location == null) return null;

        var objects = Database.ObjectManager.GetObjectsInLocation(playerGameObject.Location);
        var targetObject = objects.FirstOrDefault(obj =>
        {
            var objName = Database.ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = Database.ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            name = name.ToLower();
            return objName?.Contains(name) == true || shortDesc?.Contains(name) == true;
        });

        return targetObject?.Id;
    }

    /// <summary>
    /// Call a verb on an object from within another script
    /// </summary>
    public new object? CallVerb(string objectRef, string verbName, params object[] args)
    {
        try
        {
            // Prevent calling system programming commands from scripts
            if (objectRef.Equals("system", StringComparison.OrdinalIgnoreCase) && verbName.StartsWith("@"))
            {
                throw new InvalidOperationException($"Cannot call system programming command '{verbName}' from within a script. Programming commands must be executed directly from the command line.");
            }

            // Resolve the object reference
            var objectId = ResolveObjectFromScript(objectRef);
            if (objectId == null)
            {
                throw new ArgumentException($"Object '{objectRef}' not found");
            }

            // Find the verb on the object (with inheritance)
            var allVerbsOnObject = VerbResolver.GetAllVerbsOnObject(objectId);
            var verbMatch = allVerbsOnObject.FirstOrDefault(v => 
                v.verb.Name.Equals(verbName, StringComparison.OrdinalIgnoreCase));

            if (verbMatch.verb == null)
            {
                throw new ArgumentException($"Verb '{verbName}' not found on object '{objectRef}'");
            }

            // Execute the verb with the provided arguments
            var scriptEngine = new UnifiedScriptEngine();
            
            // Build input string from arguments
            var inputArgs = args.Select(a => a?.ToString() ?? "").ToArray();
            var input = verbName + (inputArgs.Length > 0 ? " " + string.Join(" ", inputArgs) : "");
            
            // Get Database.Player from GameObject
            var playerGameObject = GetPlayerGameObject();
            var dbPlayer = playerGameObject as Database.Player ?? 
                          GameDatabase.Instance.Players.FindById(playerGameObject?.Id ?? "");
            
            if (dbPlayer == null || CommandProcessor == null)
            {
                throw new InvalidOperationException("Cannot call verb without valid player and command processor context");
            }
            
            var result = scriptEngine.ExecuteVerb(verbMatch.verb, input, dbPlayer, CommandProcessor, objectId);
            
            // Try to parse result as different types
            if (string.IsNullOrEmpty(result)) return null;
            if (bool.TryParse(result, out bool boolVal)) return boolVal;
            if (int.TryParse(result, out int intVal)) return intVal;
            if (double.TryParse(result, out double doubleVal)) return doubleVal;
            return result; // Return as string if no other type matches
        }
        catch (Exception ex)
        {
            var playerGameObject = GetPlayerGameObject();
            if (playerGameObject != null) notify(playerGameObject, $"Error calling {objectRef}:{verbName}() - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Call a function on an object from within another script
    /// </summary>
    public new object? CallFunction(string objectRef, string functionName, params object?[] parameters)
    {
        // Get Database.Player from GameObject
        var playerGameObject = GetPlayerGameObject();
        var dbPlayer = playerGameObject as Database.Player ?? 
                      GameDatabase.Instance.Players.FindById(playerGameObject?.Id ?? "");
        
        if (dbPlayer == null)
            throw new InvalidOperationException("No player context available.");

        var objectId = FunctionResolver.ResolveObjectReference(objectRef, dbPlayer.Id, dbPlayer.Location ?? "");
        if (objectId == null)
        {
            throw new ArgumentException($"Object '{objectRef}' not found.");
        }

        var function = FunctionResolver.FindFunction(objectId, functionName);
        if (function == null)
        {
            throw new ArgumentException($"Function '{functionName}' not found on object '{objectRef}'.");
        }

        var engine = new UnifiedScriptEngine();
        return engine.ExecuteFunction(function, parameters, dbPlayer, CommandProcessor, CallingObjectId);
    }

    /// <summary>
    /// Resolve object reference from script context
    /// </summary>
    private string? ResolveObjectFromScript(string objectRef)
    {
        // Handle special keywords
        switch (objectRef.ToLower())
        {
            case "this":
                return GetThisGameObject()?.Id;
            case "me":
                return GetPlayerGameObject()?.Id;
            case "here":
                return GetPlayerGameObject()?.Location;
            case "system":
                return GetSystemObjectId();
        }
        
        // Check if it's a DBREF (starts with # followed by digits)
        if (objectRef.StartsWith("#") && int.TryParse(objectRef.Substring(1), out int dbref))
        {
            var obj = GameDatabase.Instance.GameObjects.FindOne(o => o.DbRef == dbref);
            return obj?.Id;
        }
        
        // Check if it's a class reference
        if (objectRef.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectRef.Substring(6);
            var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return objectClass?.Id;
        }
        
        if (objectRef.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectRef.Substring(0, objectRef.Length - 6);
            var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return objectClass?.Id;
        }

        // Check if it's a direct class ID
        var classById = GameDatabase.Instance.ObjectClasses.FindById(objectRef);
        if (classById != null)
        {
            return classById.Id;
        }
        
        // Try to find by name
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        var match = allObjects.FirstOrDefault(obj =>
        {
            var objName = (Database.ObjectManager.GetProperty(obj.Id, "name") as BsonValue)?.AsString;
            return objName?.Equals(objectRef, StringComparison.OrdinalIgnoreCase) == true;
        });
        
        if (match != null) return match.Id;
        
        // Try as class name
        var objectClass2 = GameDatabase.Instance.ObjectClasses.FindOne(c => 
            c.Name.Equals(objectRef, StringComparison.OrdinalIgnoreCase));
        return objectClass2?.Id;
    }

    /// <summary>
    /// Get system object ID (helper method)
    /// </summary>
    private string GetSystemObjectId()
    {
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        var systemObj = allObjects.FirstOrDefault(obj => 
            (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
            (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
        return systemObj?.Id ?? "";
    }

    /// <summary>
    /// Call a verb on the current object (this)
    /// </summary>
    public object? ThisVerb(string verbName, params object[] args)
    {
        return CallVerb("this", verbName, args);
    }

    /// <summary>
    /// Call a verb on the player object (me)
    /// </summary>
    public new object? Me(string verbName, params object[] args)
    {
        return CallVerb("me", verbName, args);
    }

    /// <summary>
    /// Call a verb on the current room (here)
    /// </summary>
    public new object? Here(string verbName, params object[] args)
    {
        return CallVerb("here", verbName, args);
    }

    /// <summary>
    /// Call a verb on the system object
    /// </summary>
    public new object? System(string verbName, params object[] args)
    {
        return CallVerb("system", verbName, args);
    }

    /// <summary>
    /// Call a verb on an object by DBREF
    /// </summary>
    public new object? Object(int dbref, string verbName, params object[] args)
    {
        return CallVerb($"#{dbref}", verbName, args);
    }

    /// <summary>
    /// Call a verb on a class
    /// </summary>
    public new object? Class(string className, string verbName, params object[] args)
    {
        return CallVerb($"class:{className}", verbName, args);
    }
}
