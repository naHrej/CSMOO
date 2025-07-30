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
                "System.Dynamic",
                "System.Linq",
                "System.Collections.Generic",
                "System.Text",
                "System.Threading.Tasks",
                "CSMOO.Server.Database",
                "CSMOO.Server.Commands",
                "CSMOO.Server.Scripting",
                "CSMOO.Server.Core",
                "HtmlAgilityPack"
            );
    }

    /// <summary>
    /// Execute a verb with unified script globals
    /// </summary>
    public string ExecuteVerb(Verb verb, string input, Database.Player player, 
        CommandProcessor commandProcessor, string? thisObjectId = null, Dictionary<string, string>? variables = null)
    {
        var previousContext = Builtins.UnifiedContext; // Store previous context
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
                Player = player, // Always the Database.Player
                This = thisObject ?? CreateNullGameObject(actualThisObjectId),
                Caller = previousContext?.This ?? playerObject, // The object that called this verb (or Player if no previous context)
                CommandProcessor = commandProcessor,
                Input = input,
                Args = ParseArguments(input),
                Verb = verb.Name,
                Variables = variables ?? new Dictionary<string, string>()
            };

            // If we have a previous context, inherit the Helpers to maintain consistency
            if (previousContext != null)
            {
                globals.Helpers = previousContext.Helpers;
            }
            else
            {
                globals.Helpers = new ScriptHelpers(player, commandProcessor);
            }

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
            // Restore previous Builtins context to support nested function calls
            Builtins.UnifiedContext = previousContext;
        }
    }

    /// <summary>
    /// Execute a function with unified script globals and type checking
    /// </summary>
    public object? ExecuteFunction(Function function, object?[] parameters, Database.Player player, 
        CommandProcessor? commandProcessor = null, string? thisObjectId = null)
    {
        var previousContext = Builtins.UnifiedContext; // Store previous context
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
                Player = player, // Always the Database.Player
                This = thisObject ?? CreateNullGameObject(actualThisObjectId),
                Caller = previousContext?.This ?? playerObject, // The object that called this function (or Player if no previous context)
                CommandProcessor = commandProcessor,
                CallingObjectId = actualThisObjectId,
                Parameters = parameters
            };

            // If we have a previous context, inherit the Helpers to maintain consistency
            if (previousContext != null)
            {
                globals.Helpers = previousContext.Helpers;
            }
            else if (commandProcessor != null)
            {
                globals.Helpers = new ScriptHelpers(player, commandProcessor);
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
