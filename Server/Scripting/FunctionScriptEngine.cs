using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using CSMOO.Server.Database;
using CSMOO.Server.Database.Models;
using CSMOO.Server.Logging;
using CSMOO.Server.Commands;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Executes function code with proper type checking and parameter validation
/// </summary>
public class FunctionScriptEngine
{
    private readonly ScriptOptions _scriptOptions;

    public FunctionScriptEngine()
    {
        _scriptOptions = ScriptOptions.Default
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
                "CSMOO.Server.Database",
                "CSMOO.Server.Commands",
                "CSMOO.Server.Scripting",
                "HtmlAgilityPack"
            );
    }

    /// <summary>
    /// Executes a function with type-checked parameters
    /// </summary>
    public object? ExecuteFunction(Function function, object?[] parameters, Player callingPlayer, CommandProcessor? commandProcessor = null, string callingObjectId = "")
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

            // Create globals for function execution
            var globals = new FunctionScriptGlobals
            {
                Player = callingPlayer,
                CommandProcessor = commandProcessor,
                CallingObjectId = callingObjectId,
                Parameters = parameters
            };

            // Add parameters as named variables to the globals
            for (int i = 0; i < function.ParameterNames.Length && i < parameters.Length; i++)
            {
                var paramName = function.ParameterNames[i];
                if (!string.IsNullOrEmpty(paramName))
                {
                    globals.SetParameter(paramName, parameters[i]);
                }
            }

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

            // Create and execute script
            var script = CSharpScript.Create(finalCode, _scriptOptions, typeof(FunctionScriptGlobals));
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
}

/// <summary>
/// Global variables and functions available in function scripts
/// </summary>
public class FunctionScriptGlobals : ScriptGlobals
{
    /// <summary>
    /// Parameters passed to the function
    /// </summary>
    public object?[] Parameters { get; set; } = Array.Empty<object?>();

    /// <summary>
    /// The object ID that called this function
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
    /// Call a function on an object (e.g., player:getName(), system:display_login())
    /// </summary>
    public new object? CallFunction(string objectRef, string functionName, params object?[] parameters)
    {
        if (Player == null)
            throw new InvalidOperationException("No player context available.");

        var objectId = FunctionResolver.ResolveObjectReference(objectRef, Player.Id, Player.Location ?? "");
        if (objectId == null)
        {
            throw new ArgumentException($"Object '{objectRef}' not found.");
        }

        var function = FunctionResolver.FindFunction(objectId, functionName);
        if (function == null)
        {
            throw new ArgumentException($"Function '{functionName}' not found on object '{objectRef}'.");
        }

        var engine = new FunctionScriptEngine();
        return engine.ExecuteFunction(function, parameters, Player!, CommandProcessor, CallingObjectId);
    }

    /// <summary>
    /// Convenience method to call functions on the system object
    /// </summary>
    public new object? System(string functionName, params object?[] parameters)
    {
        return CallFunction("system", functionName, parameters);
    }

    /// <summary>
    /// Convenience method to call functions on the calling player
    /// </summary>
    public new object? Me(string functionName, params object?[] parameters)
    {
        return CallFunction("player", functionName, parameters);
    }

    /// <summary>
    /// Convenience method to call functions on the current room
    /// </summary>
    public new object? Here(string functionName, params object?[] parameters)
    {
        return CallFunction("here", functionName, parameters);
    }

    /// <summary>
    /// Convenience method to call functions on the calling object
    /// </summary>
    public object? This(string functionName, params object?[] parameters)
    {
        if (string.IsNullOrEmpty(CallingObjectId))
        {
            throw new InvalidOperationException("No calling object context available.");
        }
        
        var function = FunctionResolver.FindFunction(CallingObjectId, functionName);
        if (function == null)
        {
            throw new ArgumentException($"Function '{functionName}' not found on calling object.");
        }

        if (Player == null)
        {
            throw new InvalidOperationException("No player context available for function calls.");
        }

        var engine = new FunctionScriptEngine();
        return engine.ExecuteFunction(function, parameters, Player, CommandProcessor, CallingObjectId);
    }

    /// <summary>
    /// Call a function on a specific object by DBREF
    /// </summary>
    public new object? Object(int dbref, string functionName, params object?[] parameters)
    {
        return CallFunction($"#{dbref}", functionName, parameters);
    }

    /// <summary>
    /// Call a function on a class
    /// </summary>
    public new object? Class(string className, string functionName, params object?[] parameters)
    {
        return CallFunction($"class:{className}", functionName, parameters);
    }
}
