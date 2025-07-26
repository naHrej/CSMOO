using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CSMOO.Server.Database;
using CSMOO.Server.Database.Models;
using CSMOO.Server.Logging;
using CSMOO.Server.Commands;
using LiteDB;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Dynamic object wrapper that provides natural syntax for object property access and verb calls
/// Supports syntax like: player.Name, player.Name = "value", player:verbname(args)
/// </summary>
public class ScriptObject : DynamicObject
{
    private readonly string _objectId;
    private readonly Player _currentPlayer;
    private readonly CommandProcessor _commandProcessor;
    private readonly ScriptHelpers _helpers;

    public ScriptObject(string objectId, Player currentPlayer, CommandProcessor commandProcessor, ScriptHelpers helpers)
    {
        _objectId = objectId;
        _currentPlayer = currentPlayer;
        _commandProcessor = commandProcessor;
        _helpers = helpers;
    }

    /// <summary>
    /// The object ID this wrapper represents
    /// </summary>
    public string ObjectId => _objectId;

    /// <summary>
    /// Get the actual GameObject
    /// </summary>
    public GameObject? GetGameObject()
    {
        // Use DbProvider for all DB access
        return ObjectManager.GetObject(_objectId);
    }

    /// <summary>
    /// Handles property getting: player.Name
    /// </summary>
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        var propertyName = binder.Name;
        
        try
        {
            var obj = GetGameObject();
            if (obj == null)
            {
                throw new ArgumentException($"Object {_objectId} not found");
            }

            // Direct database lookup for the property
            var propertyValue = ObjectManager.GetProperty(obj, propertyName);
            
            if (propertyValue == null)
            {
                throw new ArgumentException($"Property '{propertyName}' not found on object {_objectId}");
            }
            
            // Convert BsonValue to appropriate C# type
            result = propertyValue.RawValue;
            return true;
        }
        catch (Exception ex)
        {
            // Let the error bubble up to the script engine
            throw new InvalidOperationException($"Error accessing property '{propertyName}' on object {_objectId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Handles property setting: player.Name = "value"
    /// </summary>
    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        var propertyName = binder.Name;
        
        try
        {
            var obj = GetGameObject();
            if (obj == null)
            {
                throw new ArgumentException($"Object {_objectId} not found");
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

            ObjectManager.SetProperty(obj, propertyName, bsonValue);
            return true;
        }
        catch (Exception ex)
        {
            // Let the error bubble up to the script engine
            throw new InvalidOperationException($"Error setting property '{propertyName}' on object {_objectId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Handles method calls (verbs and functions): player.getName() or dynamic property access
    /// </summary>
    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        var methodName = binder.Name;
        
        try
        {
            // First try to find a verb
            var verb = FindVerb(methodName);
            if (verb != null)
            {
                // Try to call the verb on this object
                var verbResult = CallVerb(methodName, args);
                result = verbResult;
                return true;
            }

            // If no verb found, try to find a function
            var function = FindFunction(methodName);
            if (function != null)
            {
                // Try to call the function on this object
                var functionResult = CallFunction(methodName, args);
                result = functionResult;
                return true;
            }

            throw new ArgumentException($"Verb or function '{methodName}' not found on object {_objectId}");
        }
        catch (Exception ex)
        {
            // Let the error bubble up to the script engine
            throw new InvalidOperationException($"Error calling '{methodName}' on object {_objectId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Call a verb on this object
    /// </summary>
    public string CallVerb(string verbName, params object?[]? args)
    {
        // Find the verb on this object or its class hierarchy
        var verb = FindVerb(verbName);
        if (verb == null)
        {
            throw new ArgumentException($"Verb '{verbName}' not found on object {_objectId}");
        }

        // Convert arguments to strings
        var stringArgs = args?.Select(arg => arg?.ToString() ?? "").ToList() ?? new List<string>();
        
        // Create input string for the verb
        var input = verbName;
        if (stringArgs.Any())
        {
            input += " " + string.Join(" ", stringArgs);
        }

        // Execute the verb using the unified script engine
        var scriptEngine = new UnifiedScriptEngine();
        return scriptEngine.ExecuteVerb(verb, input, _currentPlayer, _commandProcessor, _objectId);
    }

    /// <summary>
    /// Call a function on this object
    /// </summary>
    public object? CallFunction(string functionName, params object?[]? args)
    {
        // Find the function on this object or its class hierarchy
        var function = FindFunction(functionName);
        if (function == null)
        {
            throw new ArgumentException($"Function '{functionName}' not found on object {_objectId}");
        }

        // Execute the function using the unified script engine
        var functionEngine = new UnifiedScriptEngine();
        return functionEngine.ExecuteFunction(function, args ?? new object[0], _currentPlayer, _commandProcessor, _objectId);
    }

    /// <summary>
    /// Find a verb on this object or its class hierarchy
    /// </summary>
    private Verb? FindVerb(string verbName)
    {
        var verbs = DbProvider.Instance.Find<Verb>("verbs", v => v.ObjectId == _objectId).ToList();
        var verb = verbs.FirstOrDefault(v =>
            v.Name.Equals(verbName, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(v.Aliases) &&
             v.Aliases.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(alias => alias.Equals(verbName, StringComparison.OrdinalIgnoreCase))));

        if (verb != null) return verb;

        // If not found, try the object's class hierarchy
        var obj = GetGameObject();
        if (obj != null)
        {
            var objectClass = DbProvider.Instance.FindById<ObjectClass>("objectclasses", obj.ClassId);
            while (objectClass != null)
            {
                var classVerbs = DbProvider.Instance.Find<Verb>("verbs", v => v.ObjectId == objectClass.Id).ToList();
                verb = classVerbs.FirstOrDefault(v =>
                    v.Name.Equals(verbName, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(v.Aliases) &&
                     v.Aliases.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Any(alias => alias.Equals(verbName, StringComparison.OrdinalIgnoreCase))));

                if (verb != null) return verb;

                // Move up the class hierarchy
                if (!string.IsNullOrEmpty(objectClass.ParentClassId))
                {
                    objectClass = DbProvider.Instance.FindById<ObjectClass>("objectclasses", objectClass.ParentClassId);
                }
                else
                {
                    break;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Find a function on this object or its class hierarchy
    /// </summary>
    private Function? FindFunction(string functionName)
    {
        // Use the existing FunctionResolver to find the function
        return FunctionResolver.FindFunction(_objectId, functionName);
    }

    /// <summary>
    /// String representation
    /// </summary>
    public override string ToString()
    {
        var obj = GetGameObject();
        if (obj == null) return $"ScriptObject({_objectId}) [INVALID]";
        
        var nameProperty = ObjectManager.GetProperty(obj, "name");
        var shortDescProperty = ObjectManager.GetProperty(obj, "shortDescription");
        
        var name = nameProperty?.AsString;
        var shortDesc = shortDescProperty?.AsString;
        
        return name ?? shortDesc ?? $"Object #{obj.DbRef}";
    }
}

/// <summary>
/// Factory for creating ScriptObject instances with natural syntax support
/// </summary>
public class ScriptObjectFactory
{
    private readonly Player _currentPlayer;
    private readonly CommandProcessor _commandProcessor;
    private readonly ScriptHelpers _helpers;

    public ScriptObjectFactory(Player currentPlayer, CommandProcessor commandProcessor, ScriptHelpers helpers)
    {
        _currentPlayer = currentPlayer;
        _commandProcessor = commandProcessor;
        _helpers = helpers;
    }

    /// <summary>
    /// Create a ScriptObject for the given object reference
    /// Supports: "me", "here", "system", "#123", object names, etc.
    /// </summary>
    public dynamic? GetObject(string objectReference)
    {
        var objectId = _helpers.ResolveObject(objectReference);
        if (objectId == null) return null;
        
        return new ScriptObject(objectId, _currentPlayer, _commandProcessor, _helpers);
    }

    /// <summary>
    /// Create a ScriptObject for a direct object ID
    /// </summary>
    public dynamic? GetObjectById(string objectId)
    {
        var obj = ObjectManager.GetObject(objectId);
        if (obj == null) return null;
        return new ScriptObject(objectId, _currentPlayer, _commandProcessor, _helpers);
    }
}

/// <summary>
/// Enhanced script globals with natural object syntax support
/// </summary>
public class EnhancedScriptGlobals : ScriptGlobals
{
    private ScriptObjectFactory? _objectFactory;

    /// <summary>
    /// Initialize the object factory
    /// </summary>
    public void InitializeObjectFactory()
    {
        // Check for Helpers in base, derived class, or UnifiedScriptGlobals
        ScriptHelpers? helpers = null;
        Database.Player? dbPlayer = null;
        
        if (this is UnifiedScriptGlobals unifiedGlobals)
        {
            helpers = unifiedGlobals.Helpers;
            // Convert GameObject back to Database.Player if needed
            dbPlayer = (Database.Player?)unifiedGlobals.Player ?? 
                      DbProvider.Instance.FindById<Database.Player>("players", ((Database.Player?)unifiedGlobals.Player)?.Id ?? "");
        }
        // VerbScriptGlobals merged: handle legacy ThisObjectId
        else if (this is UnifiedScriptGlobals legacyVerbGlobals && !string.IsNullOrEmpty(legacyVerbGlobals.ThisObjectId))
        {
            helpers = legacyVerbGlobals.Helpers;
            dbPlayer = (Database.Player?)legacyVerbGlobals.Player;
        }
        else
        {
            helpers = Helpers;
            dbPlayer = Player as Database.Player;
        }
        
        if (dbPlayer != null && CommandProcessor != null && helpers != null)
        {
            _objectFactory = new ScriptObjectFactory(dbPlayer, CommandProcessor, helpers);
        }
    }

    /// <summary>
    /// Get object with natural syntax: GetObject("player") or GetObject("#123")
    /// Usage: var player = GetObject("me"); player.Name = "NewName"; player:sayHello();
    /// </summary>
    public new dynamic? GetObject(string objectReference)
    {
        return _objectFactory?.GetObject(objectReference);
    }

    /// <summary>
    /// The player executing the verb
    /// Usage: player.Name = "NewName"; player.Location = "room_id";
    /// </summary>
    public dynamic? player
    {
        get
        {
            string? playerId = null;
            if (this is UnifiedScriptGlobals unifiedGlobals)
            {
                playerId = ((Database.Player?)unifiedGlobals.Player)?.Id;
            }
            else
            {
                playerId = Player?.Id;
            }
            return _objectFactory?.GetObjectById(playerId ?? "");
        }
    }

    /// <summary>
    /// The object this verb is running on (same as 'this')
    /// Usage: me.Name = "New Name"; me:someVerb();
    /// </summary>
    public dynamic? me
    {
        get
        {
            if (this is UnifiedScriptGlobals unifiedGlobals)
            {
                return _objectFactory?.GetObjectById((unifiedGlobals.This as GameObject)?.Id ?? "");
            }
            else if (this is UnifiedScriptGlobals legacyVerbGlobals && !string.IsNullOrEmpty(legacyVerbGlobals.ThisObjectId))
            {
                return _objectFactory?.GetObjectById(legacyVerbGlobals.ThisObjectId);
            }
            return null;
        }
    }

    /// <summary>
    /// The current room (where the player is)
    /// Usage: here.Name = "New Room Name"; here:look();
    /// </summary>
    public dynamic? here
    {
        get
        {
            string? location = null;
            if (this is UnifiedScriptGlobals unifiedGlobals)
            {
                location = ((Database.Player?)unifiedGlobals.Player)?.Location?.Id;
            }
            else
            {
                location = Player?.Location?.Id;
            }
            return _objectFactory?.GetObjectById(location ?? "");
        }
    }

    /// <summary>
    /// The system object as a ScriptObject
    /// Usage: system:who(); system.Name;
    /// </summary>
    public dynamic? system => _objectFactory?.GetObject("system");

    /// <summary>
    /// The object this verb is running on (same as 'me')
    /// Usage: this.Name = "New Name"; this:someVerb();
    /// </summary>
    public dynamic? @this
    {
        get
        {
            if (this is UnifiedScriptGlobals unifiedGlobals)
            {
                return _objectFactory?.GetObjectById((unifiedGlobals.This as GameObject)?.Id ?? "");
            }
            else if (this is UnifiedScriptGlobals legacyVerbGlobals && !string.IsNullOrEmpty(legacyVerbGlobals.ThisObjectId))
            {
                return _objectFactory?.GetObjectById(legacyVerbGlobals.ThisObjectId);
            }
            return null;
        }
    }

    /// <summary>
    /// Get object by DBREF number: obj(123) returns object #123
    /// Usage: var target = obj(123); target.Name = "NewName";
    /// </summary>
    public dynamic? obj(int dbref)
    {
        return _objectFactory?.GetObject($"#{dbref}");
    }

    /// <summary>
    /// Call a verb on an object with natural syntax
    /// Usage: CallVerb("player", "getName", "arg1", "arg2")
    /// Note: It's better to use the object syntax: GetObject("player"):getName("arg1", "arg2")
    /// </summary>
    public string CallVerb(string objectReference, string verbName, params object[] args)
    {
        dynamic? obj = GetObject(objectReference);
        if (obj == null) return $"Object '{objectReference}' not found";
        
        return obj.CallVerb(verbName, args);
    }
}
