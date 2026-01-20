using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using CSMOO.Logging;
using CSMOO.Configuration;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Core;
using CSMOO.Verbs;
using CSMOO.Functions;
using System.Reflection;

namespace CSMOO.Scripting;

/// <summary>
/// Precompiles scripts to check for errors and cache compiled scripts
/// </summary>
public class ScriptPrecompiler : IScriptPrecompiler
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

    public ScriptPrecompiler(
        IObjectManager objectManager,
        ILogger logger,
        IConfig config,
        IObjectResolver objectResolver,
        IVerbResolver verbResolver,
        IFunctionResolver functionResolver,
        IDbProvider dbProvider,
        IPlayerManager playerManager,
        IVerbManager verbManager,
        IRoomManager roomManager)
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
        
        _scriptOptions = ScriptOptions.Default
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest)
            .WithReferences(
                typeof(object).Assembly,
                typeof(Console).Assembly,
                typeof(Enumerable).Assembly,
                typeof(GameObject).Assembly,
                typeof(ObjectManager).Assembly,
                typeof(ScriptGlobals).Assembly,
                typeof(HtmlAgilityPack.HtmlDocument).Assembly,
                Assembly.GetExecutingAssembly()
            )
            .WithImports(
                "System",
                "CSMOO.Core",
                "System.Dynamic",
                "System.Linq",
                "System.Collections.Generic",
                "CSMOO.Object",
                "CSMOO.Exceptions",
                "HtmlAgilityPack"
            );
    }

    public CompilationResult PrecompileVerb(string code, string? objectId = null, string? pattern = null, Dictionary<string, string>? variables = null)
    {
        var result = new CompilationResult
        {
            CodeHash = ComputeCodeHash(code)
        };

        try
        {
            // Extract variables from pattern if provided and variables not already set
            if (variables == null && !string.IsNullOrEmpty(pattern))
            {
                variables = ExtractVariablesFromPattern(pattern);
            }

            // Apply same preprocessing as ScriptEngine
            var preprocessedCode = PreprocessObjectReferenceSyntax(code);
            var collectionFixedCode = PreprocessCollectionExpressions(preprocessedCode);
            var completeScript = BuildScriptWithVariables(collectionFixedCode, variables);

            // Create script without executing
            var script = CSharpScript.Create(completeScript, _scriptOptions, typeof(ScriptGlobals));

            // Get compilation diagnostics
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();

            // Convert diagnostics to DiagnosticInfo
            foreach (var diagnostic in diagnostics)
            {
                var diagnosticInfo = ConvertDiagnostic(diagnostic);
                if (diagnosticInfo.IsError)
                {
                    result.Errors.Add(diagnosticInfo);
                }
                else if (diagnosticInfo.IsWarning)
                {
                    result.Warnings.Add(diagnosticInfo);
                }
            }

            // Success if no errors and no warnings (warnings treated as errors)
            result.Success = result.Errors.Count == 0 && result.Warnings.Count == 0;
            
            if (result.Success)
            {
                result.CompiledScript = script;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error precompiling verb: {ex.Message}", ex);
            result.Errors.Add(new DiagnosticInfo
            {
                Line = 1,
                Column = 1,
                ErrorCode = "CS0000",
                Message = $"Compilation failed: {ex.Message}",
                Severity = DiagnosticSeverity.Error
            });
            result.Success = false;
        }

        return result;
    }

    public CompilationResult PrecompileFunction(string code, string? objectId = null, string[]? parameterTypes = null, string returnType = "void")
    {
        var result = new CompilationResult
        {
            CodeHash = ComputeCodeHash(code)
        };

        try
        {
            // Build function script similar to ScriptEngine.ExecuteFunction
            var scriptCode = new System.Text.StringBuilder();

            // Declare parameters if provided
            if (parameterTypes != null && parameterTypes.Length > 0)
            {
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    var paramName = $"param{i}";
                    scriptCode.AppendLine($"{parameterTypes[i]} {paramName} = ({parameterTypes[i]})GetParameter(\"{paramName}\");");
                }
            }

            // Apply preprocessing
            var preprocessedFunctionCode = PreprocessObjectReferenceSyntax(code);
            var collectionFixedFunctionCode = PreprocessCollectionExpressions(preprocessedFunctionCode);
            scriptCode.AppendLine(collectionFixedFunctionCode);

            var finalCode = scriptCode.ToString();

            // Create script without executing
            var script = CSharpScript.Create(finalCode, _scriptOptions, typeof(ScriptGlobals));

            // Get compilation diagnostics
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();

            // Convert diagnostics to DiagnosticInfo
            foreach (var diagnostic in diagnostics)
            {
                var diagnosticInfo = ConvertDiagnostic(diagnostic);
                if (diagnosticInfo.IsError)
                {
                    result.Errors.Add(diagnosticInfo);
                }
                else if (diagnosticInfo.IsWarning)
                {
                    result.Warnings.Add(diagnosticInfo);
                }
            }

            // Success if no errors and no warnings (warnings treated as errors)
            result.Success = result.Errors.Count == 0 && result.Warnings.Count == 0;
            
            if (result.Success)
            {
                result.CompiledScript = script;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error precompiling function: {ex.Message}", ex);
            result.Errors.Add(new DiagnosticInfo
            {
                Line = 1,
                Column = 1,
                ErrorCode = "CS0000",
                Message = $"Compilation failed: {ex.Message}",
                Severity = DiagnosticSeverity.Error
            });
            result.Success = false;
        }

        return result;
    }

    public CompilationResult PrecompileScript(string code)
    {
        var result = new CompilationResult
        {
            CodeHash = ComputeCodeHash(code)
        };

        try
        {
            // Apply preprocessing
            var preprocessedCode = PreprocessObjectReferenceSyntax(code);
            var collectionFixedCode = PreprocessCollectionExpressions(preprocessedCode);

            // Create script without executing
            var script = CSharpScript.Create(collectionFixedCode, _scriptOptions, typeof(ScriptGlobals));

            // Get compilation diagnostics
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();

            // Convert diagnostics to DiagnosticInfo
            foreach (var diagnostic in diagnostics)
            {
                var diagnosticInfo = ConvertDiagnostic(diagnostic);
                if (diagnosticInfo.IsError)
                {
                    result.Errors.Add(diagnosticInfo);
                }
                else if (diagnosticInfo.IsWarning)
                {
                    result.Warnings.Add(diagnosticInfo);
                }
            }

            // Success if no errors and no warnings (warnings treated as errors)
            result.Success = result.Errors.Count == 0 && result.Warnings.Count == 0;
            
            if (result.Success)
            {
                result.CompiledScript = script;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error precompiling script: {ex.Message}", ex);
            result.Errors.Add(new DiagnosticInfo
            {
                Line = 1,
                Column = 1,
                ErrorCode = "CS0000",
                Message = $"Compilation failed: {ex.Message}",
                Severity = DiagnosticSeverity.Error
            });
            result.Success = false;
        }

        return result;
    }

    public string ComputeCodeHash(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private DiagnosticInfo ConvertDiagnostic(Diagnostic diagnostic)
    {
        var info = new DiagnosticInfo
        {
            ErrorCode = diagnostic.Id,
            Message = diagnostic.GetMessage(),
            Severity = diagnostic.Severity
        };

        // Extract location information
        if (diagnostic.Location != Location.None)
        {
            var lineSpan = diagnostic.Location.GetLineSpan();
            info.Line = lineSpan.StartLinePosition.Line + 1; // Convert to 1-based
            info.Column = lineSpan.StartLinePosition.Character + 1; // Convert to 1-based
            info.FilePath = lineSpan.Path;
        }

        return info;
    }

    // Reuse preprocessing methods from ScriptEngine (same logic)
    private string PreprocessObjectReferenceSyntax(string originalCode)
    {
        if (string.IsNullOrEmpty(originalCode))
            return originalCode;

        var result = originalCode;

        // Handle DBref patterns: #<number>.<identifier> or #<number>.<identifier>(...)
        var dbrefPattern = @"#(\d+)\.(\w+)(\(.*?\))?";
        result = Regex.Replace(result, dbrefPattern, match =>
        {
            var dbref = match.Groups[1].Value;
            var member = match.Groups[2].Value;
            var methodCall = match.Groups[3].Value;

            if (!string.IsNullOrEmpty(methodCall))
            {
                return $"GetObjectByDbRef({dbref}).{member}{methodCall}";
            }
            else
            {
                return $"GetObjectByDbRef({dbref}).{member}";
            }
        });

        // Handle DBref assignment patterns: #4.property = value
        var dbrefAssignmentPattern = @"#(\d+)\.(\w+)\s*=";
        result = Regex.Replace(result, dbrefAssignmentPattern, match =>
        {
            var dbref = match.Groups[1].Value;
            var member = match.Groups[2].Value;
            return $"GetObjectByDbRef({dbref}).{member} =";
        });

        // Handle ID patterns: $<identifier>.<identifier> or $<identifier>.<identifier>(...)
        var idPattern = @"\$([a-zA-Z0-9\-_]+)\.(\w+)(\(.*?\))?";
        result = Regex.Replace(result, idPattern, match =>
        {
            var objectId = match.Groups[1].Value;
            var member = match.Groups[2].Value;
            var methodCall = match.Groups[3].Value;

            if (!string.IsNullOrEmpty(methodCall))
            {
                return $"GetObjectById(\"{objectId}\").{member}{methodCall}";
            }
            else
            {
                return $"GetObjectById(\"{objectId}\").{member}";
            }
        });

        // Handle ID assignment patterns: $objectId.property = value
        var idAssignmentPattern = @"\$([a-zA-Z0-9\-_]+)\.(\w+)\s*=";
        result = Regex.Replace(result, idAssignmentPattern, match =>
        {
            var objectId = match.Groups[1].Value;
            var member = match.Groups[2].Value;
            return $"GetObjectById(\"{objectId}\").{member} =";
        });

        return result;
    }

    private string PreprocessCollectionExpressions(string originalCode)
    {
        if (string.IsNullOrEmpty(originalCode))
            return originalCode;

        var result = originalCode;

        // Fix dynamic variable assignments with collection expressions
        var dynamicArrayPattern = @"(dynamic\s+\w+\s*=\s*)\[\s*\]";
        result = Regex.Replace(result, dynamicArrayPattern, 
            match => match.Groups[1].Value + "new List<object>()");

        var dynamicDictPattern = @"(dynamic\s+\w+\s*=\s*)\{\s*\}";
        result = Regex.Replace(result, dynamicDictPattern, 
            match => match.Groups[1].Value + "new Dictionary<string, object>()");

        var dynamicArrayWithItemsPattern = @"(dynamic\s+\w+\s*=\s*)\[([^\]]+)\]";
        result = Regex.Replace(result, dynamicArrayWithItemsPattern, 
            match => match.Groups[1].Value + "new List<object> { " + match.Groups[2].Value + " }");

        // Fix dynamic property assignments with collection expressions
        var propertyArrayPattern = @"(\w+\.\w+\s*=\s*)\[\s*\]";
        result = Regex.Replace(result, propertyArrayPattern, 
            match => match.Groups[1].Value + "new List<object>()");

        var propertyDictPattern = @"(\w+\.\w+\s*=\s*)\{\s*\}";
        result = Regex.Replace(result, propertyDictPattern, 
            match => match.Groups[1].Value + "new Dictionary<string, object>()");

        var propertyArrayWithItemsPattern = @"(\w+\.\w+\s*=\s*)\[([^\]]+)\]";
        result = Regex.Replace(result, propertyArrayWithItemsPattern, 
            match => match.Groups[1].Value + "new List<object> { " + match.Groups[2].Value + " }");

        return result;
    }

    private string BuildScriptWithVariables(string originalCode, Dictionary<string, string>? variables)
    {
        var scriptBuilder = new System.Text.StringBuilder();

        // Track which variables are already declared
        var declaredVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add variable declarations from pattern matching
        if (variables != null && variables.Count > 0)
        {
            scriptBuilder.AppendLine("// Auto-generated variable declarations from pattern matching");
            foreach (var kvp in variables)
            {
                var escapedValue = kvp.Value
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");

                scriptBuilder.AppendLine($"string {kvp.Key} = \"{escapedValue}\";");
                declaredVariables.Add(kvp.Key);
            }
            scriptBuilder.AppendLine();
        }

        // Find variables already declared in the code
        var codeDeclaredVariables = FindDeclaredVariables(originalCode);
        foreach (var varName in codeDeclaredVariables)
        {
            declaredVariables.Add(varName);
        }

        // Add automatic object resolution variables (only if not already declared)
        var objectVariables = ExtractPotentialObjectReferences(originalCode);
        if (objectVariables.Count > 0)
        {
            scriptBuilder.AppendLine("// Auto-resolved object variables (typed)");
            foreach (var objectVar in objectVariables)
            {
                // Skip if already declared from pattern or in code
                if (!declaredVariables.Contains(objectVar))
                {
                    // Infer type based on variable name
                    var inferredType = InferObjectType(objectVar);
                    scriptBuilder.AppendLine($"{inferredType} {objectVar} = ObjectResolver.ResolveObject(\"{objectVar}\", This) ?? throw new ScriptExecutionException(\"Object '{objectVar}' not found\");");
                    declaredVariables.Add(objectVar);
                }
            }
            scriptBuilder.AppendLine();
        }

        // Add the original code
        scriptBuilder.AppendLine("// Original script code:");
        scriptBuilder.AppendLine(originalCode);

        return scriptBuilder.ToString();
    }

    private HashSet<string> ExtractPotentialObjectReferences(string code)
    {
        var potentialObjects = new HashSet<string>();
        
        if (string.IsNullOrEmpty(code))
            return potentialObjects;

        // Known type/class names that should not be auto-resolved (they're static types, not object instances)
        var knownStaticTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "StringComparer", "StringComparison", "System", "Console", "Math", "DateTime",
            "TimeSpan", "Guid", "Regex", "Encoding", "Convert", "Environment", "AppDomain",
            "Type", "Assembly", "Activator", "Task", "Thread", "ThreadPool", "Linq",
            "Enumerable", "List", "Dictionary", "HashSet", "Array", "StringBuilder",
            "Object", "Char", "Int32", "Int64", "Double", "Single", "Boolean", "Decimal"
        };

            // Known ScriptGlobals identifiers that should not be auto-resolved
            var knownIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Player", "This", "ThisGameObject", "ThisPlayer", "ThisRoom", "ThisExit",
                "ThisObject", "Caller", "CallerGameObject", "CallerPlayer", "CallDepth",
                "ThisObjectId", "CommandProcessor", "ObjectManager", "WorldManager",
                "PlayerManager", "Helpers", "Location", "Input", "Args", "Verb", "Variables",
                "Parameters", "CallingObjectId", "me", "player", "here",
                "obj", "objById", "GetObject", "GetObjectById", "GetGameObjectById",
                "GetObjectByDbRef", "GetGameObjectByDbRef", "Say", "notify", "SayToRoom",
                "GetThisGameObject", "GetPlayerGameObject", "GetPlayerLocation",
                "GetThisProperty", "SetThisProperty", "GetPlayer", "FindObjectInRoom",
                "CallVerb", "CallFunction", "ThisVerb", "Me", "Here", "System", "Object", "Class",
                "Builtins", "ObjectResolver",
                "string", "int", "bool", "var", "dynamic", "object", "void",
                // Namespace-qualified types (to avoid matching partial names)
                "ObjectManager", "ObjectResolver", "Builtins"
            };

        // Simplified extraction - just find identifiers followed by dot
        var objectAccessPattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\.\s*[a-zA-Z_]";
        var matches = System.Text.RegularExpressions.Regex.Matches(code, objectAccessPattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var identifier = match.Groups[1].Value;
            // Skip common keywords, built-in types, known static types, and known ScriptGlobals identifiers
            if (!IsReservedKeyword(identifier) && !IsBuiltInType(identifier) && 
                !knownStaticTypes.Contains(identifier) && !knownIdentifiers.Contains(identifier))
            {
                potentialObjects.Add(identifier);
            }
        }

        return potentialObjects;
    }

    /// <summary>
    /// Finds variables that are already declared in the code (var, string, dynamic, etc.)
    /// </summary>
    private HashSet<string> FindDeclaredVariables(string code)
    {
        var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        if (string.IsNullOrEmpty(code))
            return declared;

        // Pattern to match variable declarations: var name = ... or string name = ... or dynamic name = ... or TypeName name = ...
        // This pattern matches any identifier (type name) followed by an identifier (variable name) and equals sign
        var declarationPattern = @"\b([a-zA-Z_][a-zA-Z0-9_<>\[\]]*)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=";
        var matches = System.Text.RegularExpressions.Regex.Matches(code, declarationPattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Groups.Count > 2)
            {
                var typeName = match.Groups[1].Value;
                var varName = match.Groups[2].Value;
                
                // Skip if variable name is a reserved keyword
                if (IsReservedKeyword(varName))
                    continue;
                
                // Skip if it's clearly a method call (check if there's a parenthesis right after =)
                var matchEnd = match.Index + match.Length;
                if (matchEnd < code.Length)
                {
                    var afterMatch = code.Substring(matchEnd, Math.Min(10, code.Length - matchEnd)).TrimStart();
                    if (afterMatch.StartsWith("("))
                        continue; // This is likely a method call, not a variable declaration
                }
                
                // Accept if type name:
                // 1. Starts with uppercase (like ObjectClass, StringBuilder, List<string>)
                // 2. Is a known type
                // 3. Is a known type keyword (var, string, int, etc.)
                if (typeName.Length > 0 && (char.IsUpper(typeName[0]) || IsKnownType(typeName) || IsKnownTypeKeyword(typeName)))
                {
                    declared.Add(varName);
                }
            }
        }

        // Also check for foreach declarations: foreach (var item in ...) or foreach (Type item in ...)
        var foreachPattern = @"foreach\s*\(\s*([a-zA-Z_][a-zA-Z0-9_<>\[\]]*)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+in";
        var foreachMatches = System.Text.RegularExpressions.Regex.Matches(code, foreachPattern);
        foreach (System.Text.RegularExpressions.Match match in foreachMatches)
        {
            if (match.Groups.Count > 2)
            {
                var varName = match.Groups[2].Value;
                declared.Add(varName);
            }
        }

        // Check for for loop declarations: for (int i = ...) or for (var i = ...)
        var forPattern = @"for\s*\(\s*([a-zA-Z_][a-zA-Z0-9_<>\[\]]*)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=";
        var forMatches = System.Text.RegularExpressions.Regex.Matches(code, forPattern);
        foreach (System.Text.RegularExpressions.Match match in forMatches)
        {
            if (match.Groups.Count > 2)
            {
                var typeName = match.Groups[1].Value;
                var varName = match.Groups[2].Value;
                // Only add if it looks like a type (uppercase or known keyword)
                if (char.IsUpper(typeName[0]) || IsKnownTypeKeyword(typeName))
                {
                    declared.Add(varName);
                }
            }
        }

        return declared;
    }

    private bool IsKnownType(string typeName)
    {
        var knownTypes = new HashSet<string>
        {
            "ObjectClass", "GameObject", "Player", "Room", "Exit", "Verb", "Function",
            "StringBuilder", "List", "Dictionary", "HashSet", "KeyValuePair"
        };
        return knownTypes.Contains(typeName);
    }

    private bool IsKnownTypeKeyword(string typeName)
    {
        var keywords = new HashSet<string>
        {
            "var", "string", "int", "bool", "float", "double", "decimal", "dynamic", "object",
            "List", "Dictionary", "HashSet", "IEnumerable", "IList", "IDictionary"
        };
        return keywords.Contains(typeName);
    }

    private bool IsReservedKeyword(string identifier)
    {
        var keywords = new HashSet<string>
        {
            "if", "else", "for", "foreach", "while", "do", "switch", "case", "default",
            "return", "break", "continue", "goto", "try", "catch", "finally", "throw",
            "var", "dynamic", "string", "int", "bool", "float", "double", "object", "char",
            "null", "true", "false", "this", "base", "new", "using", "namespace",
            "class", "struct", "interface", "enum", "void", "public", "private", "protected"
        };
        return keywords.Contains(identifier.ToLower());
    }

    private bool IsBuiltInType(string identifier)
    {
        var types = new HashSet<string>
        {
            "Player", "GameObject", "Room", "Item", "Container", "Exit",
            "This", "Caller", "Player", "Args", "Input", "Verb", "Variables"
        };
        return types.Contains(identifier);
    }

    /// <summary>
    /// Infers the C# type for an object variable based on its name
    /// </summary>
    private string InferObjectType(string variableName)
    {
        var lowerName = variableName.ToLower();
        
        // Common patterns for player references
        if (lowerName == "player" || lowerName == "me" || (lowerName == "caller" && variableName.ToLower() == "caller"))
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
    /// Extracts variable names from a pattern like "examine {targetName}" and returns placeholder values
    /// </summary>
    private Dictionary<string, string> ExtractVariablesFromPattern(string pattern)
    {
        var variables = new Dictionary<string, string>();
        
        if (string.IsNullOrEmpty(pattern))
            return variables;

        // Extract variable names from pattern like "examine {targetName}" or "give {item} to {person}"
        var variablePattern = @"\{(\w+)\}";
        var matches = System.Text.RegularExpressions.Regex.Matches(pattern, variablePattern);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var varName = match.Groups[1].Value;
            // Use placeholder value so code compiles - real values provided at runtime
            variables[varName] = "placeholder";
        }
        
        return variables;
    }
}
