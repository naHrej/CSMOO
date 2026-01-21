using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
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
            var (completeScript, lineOffset) = BuildScriptWithVariables(collectionFixedCode, variables);

            // Create script without executing
            var script = CSharpScript.Create(completeScript, _scriptOptions, typeof(ScriptGlobals));

            // Get compilation diagnostics
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();

            // Convert diagnostics to DiagnosticInfo and adjust line numbers
            foreach (var diagnostic in diagnostics)
            {
                var diagnosticInfo = ConvertDiagnostic(diagnostic, lineOffset);
                if (diagnosticInfo.IsError)
                {
                    result.Errors.Add(diagnosticInfo);
                }
                else if (diagnosticInfo.IsWarning)
                {
                    // Filter out expected nullable warnings:
                    // CS8625: Cannot convert null literal to non-nullable reference type (for initial null assignments)
                    // CS8601/CS8602: Possible null reference for dynamic types (compiler can't track null state for dynamic)
                    if (diagnosticInfo.ErrorCode != "CS8625" && 
                        diagnosticInfo.ErrorCode != "CS8601" && 
                        diagnosticInfo.ErrorCode != "CS8602")
                    {
                        result.Warnings.Add(diagnosticInfo);
                    }
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

    public CompilationResult PrecompileFunction(string code, string? objectId = null, string[]? parameterTypes = null, string[]? parameterNames = null, string returnType = "void")
    {
        var result = new CompilationResult
        {
            CodeHash = ComputeCodeHash(code)
        };

        try
        {
            // Build function script similar to ScriptEngine.ExecuteFunction
            var scriptCode = new System.Text.StringBuilder();
            int parameterLineCount = 0;

            // Declare parameters if provided
            // IMPORTANT: Use actual parameter names instead of param0, param1, etc.
            // This ensures cached scripts work correctly with runtime parameter names
            if (parameterTypes != null && parameterTypes.Length > 0)
            {
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    // Use actual parameter name if provided, otherwise fall back to param{i}
                    var paramName = (parameterNames != null && i < parameterNames.Length && !string.IsNullOrEmpty(parameterNames[i]))
                        ? parameterNames[i]
                        : $"param{i}";
                    scriptCode.AppendLine($"{parameterTypes[i]} {paramName} = ({parameterTypes[i]})GetParameter(\"{paramName}\");");
                    parameterLineCount++;
                }
            }

            // Apply preprocessing
            var preprocessedFunctionCode = PreprocessObjectReferenceSyntax(code);
            var collectionFixedFunctionCode = PreprocessCollectionExpressions(preprocessedFunctionCode);
            scriptCode.AppendLine(collectionFixedFunctionCode);

            var finalCode = scriptCode.ToString();
            int functionLineOffset = parameterLineCount;

            // Create script without executing
            var script = CSharpScript.Create(finalCode, _scriptOptions, typeof(ScriptGlobals));

            // Get compilation diagnostics
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();

            // Convert diagnostics to DiagnosticInfo and adjust line numbers
            foreach (var diagnostic in diagnostics)
            {
                var diagnosticInfo = ConvertDiagnostic(diagnostic, functionLineOffset);
                if (diagnosticInfo.IsError)
                {
                    result.Errors.Add(diagnosticInfo);
                }
                else if (diagnosticInfo.IsWarning)
                {
                    // Filter out expected nullable warnings:
                    // CS8625: Cannot convert null literal to non-nullable reference type (for initial null assignments)
                    // CS8601/CS8602: Possible null reference for dynamic types (compiler can't track null state for dynamic)
                    if (diagnosticInfo.ErrorCode != "CS8625" && 
                        diagnosticInfo.ErrorCode != "CS8601" && 
                        diagnosticInfo.ErrorCode != "CS8602")
                    {
                        result.Warnings.Add(diagnosticInfo);
                    }
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

    private DiagnosticInfo ConvertDiagnostic(Diagnostic diagnostic, int lineOffset = 0)
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
            var originalLine = lineSpan.StartLinePosition.Line + 1; // Convert to 1-based
            
            // Adjust line number to reference original script
            // Only adjust if the line is after the offset (i.e., in the original code section)
            if (originalLine > lineOffset)
            {
                info.Line = originalLine - lineOffset;
            }
            else
            {
                // Line is in injected code, keep original line number but mark as 0 or negative?
                // For now, keep as-is but could be improved
                info.Line = originalLine;
            }
            
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

        // Handle method calls on GameObject variables: variableName.MethodName(args)
        // This rewrites typed method calls to use CallFunctionOnObject to avoid dynamic casting
        // Pattern matches: identifier.MethodName(args) where MethodName starts with capital letter
        // Excludes common C# methods like ToString(), GetType(), Equals(), etc.
        var methodCallPattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\\.([A-Z][a-zA-Z0-9_]*)\\s*\\(([^)]*)\\)";
        var knownBuiltInMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ToString", "GetType", "Equals", "GetHashCode", "ReferenceEquals",
            "GetType", "MemberwiseClone", "CompareTo", "Clone"
        };
        
        result = Regex.Replace(result, methodCallPattern, match =>
        {
            var variableName = match.Groups[1].Value;
            var methodName = match.Groups[2].Value;
            var args = match.Groups[3].Value;
            
            // Skip if it's a known built-in C# method
            if (knownBuiltInMethods.Contains(methodName))
                return match.Value;
            
            // Skip if it's a known ScriptGlobals method call (like ObjectResolver.ResolveObject)
            if (IsKnownStaticType(variableName))
                return match.Value;
            
            // Rewrite to CallFunctionOnObject(variableName, "MethodName", args)
            if (string.IsNullOrEmpty(args))
            {
                return $"CallFunctionOnObject({variableName}, \"{methodName}\")";
            }
            else
            {
                return $"CallFunctionOnObject({variableName}, \"{methodName}\", {args})";
            }
        });

        return result;
    }

    private string PreprocessCollectionExpressions(string originalCode)
    {
        if (string.IsNullOrEmpty(originalCode))
            return originalCode;

        var result = originalCode;

        // Collection expression preprocessing removed - scripts should use proper types
        // (List<GameObject>, List<string>, etc.) instead of dynamic
        // Property collection expression preprocessing removed - use proper types
        return result;
    }

    private (string script, int lineOffset) BuildScriptWithVariables(string originalCode, Dictionary<string, string>? variables)
    {
        var scriptBuilder = new System.Text.StringBuilder();
        int lineCount = 0;

        // Enable nullable reference types for the script
        scriptBuilder.AppendLine("#nullable enable");
        lineCount++; // Line 1

        scriptBuilder.AppendLine();
        lineCount++; // Line 2 (blank)

        // Track which variables are already declared
        var declaredVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add variable declarations from pattern matching
        // IMPORTANT: Read from Variables dictionary at runtime instead of hardcoding values
        // This allows cached scripts to work correctly with different runtime variable values
        if (variables != null && variables.Count > 0)
        {
            scriptBuilder.AppendLine("// Auto-generated variable declarations from pattern matching");
            lineCount++; // Comment line 1
            scriptBuilder.AppendLine("// Variables are read from Variables dictionary at runtime to support caching");
            lineCount++; // Comment line 2
            foreach (var kvp in variables)
            {
                // Read from Variables dictionary instead of hardcoding the value
                // This ensures cached scripts work with actual runtime values
                scriptBuilder.AppendLine($"string {kvp.Key} = Variables.TryGetValue(\"{kvp.Key}\", out var _{kvp.Key}_val) ? _{kvp.Key}_val : \"\";");
                lineCount++; // Variable declaration line
                declaredVariables.Add(kvp.Key);
            }
            scriptBuilder.AppendLine();
            lineCount++; // Blank line
        }

        // Find variables already declared in the code
        var codeDeclaredVariables = FindDeclaredVariables(originalCode);
        foreach (var varName in codeDeclaredVariables)
        {
            declaredVariables.Add(varName);
        }

        // Add automatic object resolution variables (only if not already declared)
        // Pass declared variables to ExtractPotentialObjectReferences so it can skip them
        var objectVariables = ExtractPotentialObjectReferences(originalCode, codeDeclaredVariables);
        
        if (objectVariables.Count > 0)
        {
            scriptBuilder.AppendLine("// Auto-resolved object variables (typed)");
            lineCount++; // Comment line
            foreach (var objectVar in objectVariables)
            {
                // Skip if already declared from pattern or in code
                var isDeclared = declaredVariables.Contains(objectVar);
                
                if (!isDeclared)
                {
                    // Infer type based on variable name
                    var inferredType = InferObjectType(objectVar);
                    // Keep nullable type since ObjectResolver.ResolveObject returns GameObject?
                    // The ?? throw ensures non-null at runtime, but we keep the nullable type for compiler compatibility
                    var injectedLine = $"{inferredType} {objectVar} = ObjectResolver.ResolveObject(\"{objectVar}\", This) ?? throw new ScriptExecutionException(\"Object '{objectVar}' not found\");";
                    
                    scriptBuilder.AppendLine(injectedLine);
                    lineCount++; // Variable declaration line
                    declaredVariables.Add(objectVar);
                }
            }
            scriptBuilder.AppendLine();
            lineCount++; // Blank line
        }

        // Add the original code
        scriptBuilder.AppendLine("// Original script code:");
        lineCount++; // Comment line
        scriptBuilder.AppendLine(originalCode);

        // lineCount now represents the number of lines before the original code starts
        // This includes the "// Original script code:" comment line
        // The original code starts at lineCount + 1 in the compiled script
        // So the offset is lineCount (the number of lines to subtract)
        return (scriptBuilder.ToString(), lineCount);
    }

    private HashSet<string> ExtractPotentialObjectReferences(string code, HashSet<string>? declaredVariables = null)
    {
        var potentialObjects = new HashSet<string>();
        
        if (string.IsNullOrEmpty(code))
            return potentialObjects;
        
        // Use case-insensitive comparison for declared variables check
        var declaredVars = declaredVariables ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
        // IMPORTANT: Exclude namespace-qualified identifiers like System.Text, System.Char, etc.
        // These should not be auto-resolved as they're namespace references, not object instances
        var objectAccessPattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\.\s*[a-zA-Z_]";
        var matches = System.Text.RegularExpressions.Regex.Matches(code, objectAccessPattern);

        // Common lambda parameter names to exclude (single/double letter variables commonly used in LINQ)
        var commonLambdaParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "v", "f", "ip", "p", "k", "x", "y", "z", "i", "j", "n", "m", "t", "s", "e", "a", "b", "c", "d"
        };

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var identifier = match.Groups[1].Value;
            var fullMatch = match.Value;
            
            // Skip namespace-qualified identifiers (System.Text, System.Char, etc.)
            // These are namespace references, not object instances that should be auto-resolved
            if (identifier == "System" && (fullMatch.Contains("System.Text") || fullMatch.Contains("System.Char")))
            {
                continue; // Skip System when it's part of System.Text or System.Char
            }
            
            // Skip very short identifiers that are likely lambda parameters (1-2 characters)
            // These are commonly used in LINQ expressions like .OrderBy(v => v.Name)
            if (identifier.Length <= 2 && commonLambdaParams.Contains(identifier))
            {
                // Check if this identifier appears in a lambda expression context
                // Look backwards from the match position to find if it's part of a lambda
                var beforeMatch = code.Substring(0, match.Index);
                var searchStart = Math.Max(0, beforeMatch.Length - 200); // Look back up to 200 chars
                var searchText = beforeMatch.Substring(searchStart);
                
                // Check for lambda patterns: .Method(identifier => or .Method((identifier, ...) =>
                // Also check for tuple patterns: (identifier, ...) =>
                var lambdaPatterns = new[]
                {
                    new System.Text.RegularExpressions.Regex(@$"\.(OrderBy|Where|Select|Any|All|FirstOrDefault|SingleOrDefault|GroupBy|Join|Zip)\s*\(\s*{System.Text.RegularExpressions.Regex.Escape(identifier)}\s*=>"),
                    new System.Text.RegularExpressions.Regex(@$"\(\s*{System.Text.RegularExpressions.Regex.Escape(identifier)}\s*,"),
                    new System.Text.RegularExpressions.Regex(@$",\s*{System.Text.RegularExpressions.Regex.Escape(identifier)}\s*\)\s*=>")
                };
                
                bool isLambdaParam = false;
                foreach (var pattern in lambdaPatterns)
                {
                    if (pattern.IsMatch(searchText))
                    {
                        isLambdaParam = true;
                        break;
                    }
                }
                
                if (isLambdaParam)
                {
                    continue; // This is a lambda parameter, not an object reference
                }
                // If it's a common lambda param but NOT in a lambda context, still skip it
                // These short names are almost always lambda params, not object references
                continue;
            }
            
            // Skip common keywords, built-in types, known static types, known ScriptGlobals identifiers,
            // and variables that are already declared in the script
            if (!IsReservedKeyword(identifier) && !IsBuiltInType(identifier) && 
                !knownStaticTypes.Contains(identifier) && !knownIdentifiers.Contains(identifier) &&
                !declaredVars.Contains(identifier))
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

        // Pattern to match pattern match variables: is TypeName varName or is not TypeName varName
        // These are variables declared in pattern matching expressions like: if (obj is GameObject playerObj)
        var patternMatchPattern = @"is\s+(?:not\s+)?([a-zA-Z_][a-zA-Z0-9_<>\[\]]*)\s+([a-zA-Z_][a-zA-Z0-9_]*)";
        var patternMatchMatches = System.Text.RegularExpressions.Regex.Matches(code, patternMatchPattern);
        
        foreach (System.Text.RegularExpressions.Match match in patternMatchMatches)
        {
            if (match.Groups.Count > 2)
            {
                var varName = match.Groups[2].Value;
                // Add pattern match variables to declared set so they won't be auto-injected
                declared.Add(varName);
            }
        }

        // Pattern to match variable declarations: var name = ... or string name = ... or dynamic name = ... or TypeName name = ...
        // This pattern matches any identifier (type name, including nullable types like GameObject?) followed by an identifier (variable name) and equals sign
        // Updated to handle nullable types: TypeName? variableName = ...
        // Match nullable types explicitly first: TypeName? varName =
        // Pattern: Match TypeName? varName = where TypeName is an identifier
        // Use a pattern that works with or without word boundaries
        var nullablePattern = @"([a-zA-Z_][a-zA-Z0-9_<>\[\]]*)\?\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=";
        var nullableMatches = System.Text.RegularExpressions.Regex.Matches(code, nullablePattern);
        
        foreach (System.Text.RegularExpressions.Match match in nullableMatches)
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
        
        // Then match non-nullable types: TypeName varName =
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
                
                // Skip if already added from nullable pattern
                if (declared.Contains(varName))
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
