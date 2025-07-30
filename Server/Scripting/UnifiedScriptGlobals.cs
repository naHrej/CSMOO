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
    public dynamic? This { get; set; }
    
    /// <summary>
    /// Alias for This - the object this script is running on (now directly GameObject with dynamic support)
    /// </summary>
    public dynamic? ThisObject 
    { 
        get => This; 
        set => This = value; 
    }
    
    /// <summary>
    /// The object that called this function (the previous object in the function chain)
    /// </summary>
    public dynamic? Caller { get; set; }
    
    // Unique: string-based ThisObjectId for legacy verb scripts (keep for legacy support)
    public string ThisObjectId { get; set; } = string.Empty;

    // (No unique methods from VerbScriptGlobals needed; retain only original UnifiedScriptGlobals logic)
    
    /// <summary>
    /// The current player as GameObject (now with dynamic support)
    /// </summary>
    private string? _playerId;
    public new GameObject? Player
    {
        get
        {
            if (string.IsNullOrEmpty(_playerId)) return null;
            return Database.ObjectManager.GetObject<Player>(_playerId);
        }
        set
        {
            _playerId = value?.Id;
        }
    }

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
    /// Get the player's location as a GameObject (for scripts that need the actual GameObject)
    /// </summary>
    public GameObject? GetPlayerLocation()
    {
        var playerObj = GetPlayerGameObject();
        return playerObj?.Location;
    }

    /// <summary>
    /// The player's current location as GameObject (alternative to 'here' ScriptObject)
    /// Usage: var location = Location; // Returns GameObject instead of ScriptObject
    /// </summary>
    public GameObject? Location => GetPlayerLocation();

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
                      DbProvider.Instance.FindById<Database.Player>("players", targetPlayer.Id);
        
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
        var player = DbProvider.Instance.FindOne<Database.Player>("players", p => 
            p.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase));
        
        if (player != null) return Database.ObjectManager.GetObject(player.Id);
        
        // Try by ID
        var playerById = DbProvider.Instance.FindById<Database.Player>("players", nameOrId);
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
    /// The current location as GameObject - returns the actual GameObject, not ScriptObject
    /// If script is running on a player: returns player's location
    /// If script is running on a room: returns the room itself
    /// </summary>
    public new dynamic? here
    {
        get
        {
            return Player.Location as dynamic;
        }
    }

    /// <summary>
    /// Check if an object is a room using class inheritance and properties
    /// </summary>
    private bool IsRoom(GameObject obj)
    {
        // Check if it inherits from Room class
        var roomClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Room");
        if (roomClass != null && (obj.ClassId == roomClass.Id || Database.ObjectManager.InheritsFrom(obj.ClassId, roomClass.Id)))
        {
            return true;
        }

        // Fallback: check for explicit room properties
        var isRoomProperty = Database.ObjectManager.GetProperty(obj, "isRoom")?.AsBoolean == true;
        if (isRoomProperty) return true;

        // Additional fallback: check if it has room-like characteristics
        return HasRoomCharacteristics(obj);
    }

    /// <summary>
    /// Check if an object has room-like characteristics
    /// </summary>
    private bool HasRoomCharacteristics(GameObject obj)
    {
        // First check if it inherits from Room class
        var roomClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Room");
        if (roomClass != null && (obj.ClassId == roomClass.Id || Database.ObjectManager.InheritsFrom(obj.ClassId, roomClass.Id)))
        {
            return true;
        }

        // Fallback: check if object has room-like properties
        var hasExits = Database.ObjectManager.GetObjectsInLocation(obj.Id).Any(o => 
            Database.ObjectManager.GetProperty(o, "isExit")?.AsBoolean == true);
        var hasLongDesc = !string.IsNullOrEmpty(Database.ObjectManager.GetProperty(obj, "longDescription")?.AsString);
        var isRoomProperty = Database.ObjectManager.GetProperty(obj, "isRoom")?.AsBoolean == true;
        
        return hasExits || hasLongDesc || isRoomProperty;
    }

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
                          DbProvider.Instance.FindById<Database.Player>("players", playerGameObject?.Id ?? "");
            
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
        var dbPlayer =
                      DbProvider.Instance.FindById<Database.Player>("players", playerGameObject?.Id ?? "");
        
        if (dbPlayer == null)
            throw new InvalidOperationException("No player context available.");

        var objectId = FunctionResolver.ResolveObjectReference(objectRef, dbPlayer.Id, dbPlayer.Location?.Id ?? "");
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
        return engine.ExecuteFunction(function, parameters, dbPlayer, CommandProcessor, objectId);
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
                return GetPlayerGameObject()?.Location?.Id;
            case "system":
                return GetSystemObjectId();
        }
        
        // Check if it's a DBREF (starts with # followed by digits)
        if (objectRef.StartsWith("#") && int.TryParse(objectRef.Substring(1), out int dbref))
        {
            var obj = DbProvider.Instance.FindOne<GameObject>("gameobjects", o => o.DbRef == dbref);
            return obj?.Id;
        }
        
        // Check if it's a class reference
        if (objectRef.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectRef.Substring(6);
            var objectClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => 
                c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return objectClass?.Id;
        }
        
        if (objectRef.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectRef.Substring(0, objectRef.Length - 6);
            var objectClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => 
                c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return objectClass?.Id;
        }

        // Check if it's a direct class ID
        // var classById = GameDatabase.Instance.ObjectClasses.FindById(objectRef); // removed direct usage
        if (DbProvider.Instance.FindById<ObjectClass>("objectclasses", objectRef) is { } classByIdObj)
        {
            return classByIdObj.Id;
        }
        
        // Try to find by name
        // var allObjects = GameDatabase.Instance.GameObjects.FindAll(); // removed direct usage
        var match = DbProvider.Instance.FindAll<GameObject>("gameobjects")
            .FirstOrDefault(obj =>
            {
                var objName = (Database.ObjectManager.GetProperty(obj, "name") as BsonValue)?.AsString;
                return objName?.Equals(objectRef, StringComparison.OrdinalIgnoreCase) == true;
            });
        if (match != null) return match.Id;
        
        // Try as class name
        var objectClass2 = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => 
            c.Name.Equals(objectRef, StringComparison.OrdinalIgnoreCase));
        return objectClass2?.Id;
    }

    /// <summary>
    /// Get system object ID (helper method)
    /// </summary>
    private string GetSystemObjectId()
    {
        // var allObjects = GameDatabase.Instance.GameObjects.FindAll(); // removed direct usage
        var systemObj = DbProvider.Instance.FindAll<GameObject>("gameobjects")
            .FirstOrDefault(obj => 
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
        // Use the here property to get the correct location
        var hereObj = here;
        if (hereObj == null) return null;
        return CallVerb($"#{hereObj.DbRef}", verbName, args);
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
