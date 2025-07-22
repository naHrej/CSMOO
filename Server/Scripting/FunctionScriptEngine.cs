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
                "CSMOO.Server.Core", // Ensure we can access core functionality
                "HtmlAgilityPack"
            );
    }

    /// <summary>
    /// Executes a function with type-checked parameters
    /// </summary>
    public object? ExecuteFunction(Function function, object?[] parameters, Player callingPlayer, CommandProcessor? commandProcessor = null, string callingObjectId = "")
    {
        // Use the new unified script engine for consistent behavior
        var unifiedEngine = new UnifiedScriptEngine();
        return unifiedEngine.ExecuteFunction(function, parameters, callingPlayer, commandProcessor, callingObjectId);
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

    /// <summary>
    /// Dynamic object that provides "this.propertyName" syntax
    /// </summary>
    public ThisObjectAccessor @this { get; private set; }

    /// <summary>
    /// Get the calling object ID - alias for CallingObjectId for easier access
    /// </summary>
    public string ThisObject => CallingObjectId;

    private readonly Dictionary<string, object?> _namedParameters = new();

    /// <summary>
    /// Constructor that initializes the dynamic object accessor
    /// </summary>
    public FunctionScriptGlobals()
    {
        @this = new ThisObjectAccessor(this);
    }

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
    /// Get a property from the calling object
    /// </summary>
    public object? GetThisProperty(string propertyName)
    {
        if (string.IsNullOrEmpty(CallingObjectId))
        {
            throw new InvalidOperationException("No calling object context available.");
        }
        
        var property = Database.ObjectManager.GetProperty(CallingObjectId, propertyName);
        return property?.RawValue;
    }

    /// <summary>
    /// Set a property on the calling object
    /// </summary>
    public void SetThisProperty(string propertyName, object? value)
    {
        if (string.IsNullOrEmpty(CallingObjectId))
        {
            throw new InvalidOperationException("No calling object context available.");
        }

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

        Database.ObjectManager.SetProperty(CallingObjectId, propertyName, bsonValue);
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

/// <summary>
/// Dynamic object that provides natural property access syntax for the calling object
/// Enables "this.propertyName" and "this.propertyName = value" syntax
/// </summary>
public class ThisObjectAccessor : DynamicObject
{
    private readonly FunctionScriptGlobals _globals;

    public ThisObjectAccessor(FunctionScriptGlobals globals)
    {
        _globals = globals;
    }

    /// <summary>
    /// Handles property getting: this.propertyName
    /// </summary>
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        try
        {
            result = _globals.GetThisProperty(binder.Name);
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error accessing property '{binder.Name}' on calling object: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Handles property setting: this.propertyName = value
    /// </summary>
    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        try
        {
            _globals.SetThisProperty(binder.Name, value);
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error setting property '{binder.Name}' on calling object: {ex.Message}", ex);
        }
    }
}
