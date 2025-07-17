using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSMOO.Server.Database;
using CSMOO.Server.Commands;
using CSMOO.Server.Session;
using CSMOO.Server.Logging;
using LiteDB;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Comprehensive helper class for script execution, providing all the functions 
/// that built-in commands use so that verbs can replicate their functionality
/// </summary>
public class ScriptHelpers
{
    public readonly Player _player;
    public readonly CommandProcessor _commandProcessor;

    public ScriptHelpers(Player player, CommandProcessor commandProcessor)
    {
        _player = player;
        _commandProcessor = commandProcessor;
    }

    #region Player and Session Management

    /// <summary>
    /// Send a message to the current player
    /// </summary>
    public void Say(string message)
    {
        _commandProcessor.SendToPlayer(message);
    }

    /// <summary>
    /// Send a message to a specific player by session GUID
    /// </summary>
    public void SendToPlayer(string message, Guid? sessionGuid = null)
    {
        _commandProcessor.SendToPlayer(message, sessionGuid);
    }

    /// <summary>
    /// Send a message to a specific player by player object
    /// </summary>
    public void SendToPlayer(string message, Player targetPlayer)
    {
        _commandProcessor.SendToPlayer(message, targetPlayer.SessionGuid);
    }

    /// <summary>
    /// Send a message to all players in the current room
    /// </summary>
    public void SayToRoom(string message, bool excludeSelf = true)
    {
        if (_player?.Location == null) return;

        var playersInRoom = GetPlayersInRoom(_player.Location);
        foreach (var player in playersInRoom)
        {
            if (excludeSelf && player.Id == _player.Id) continue;
            SendToPlayer(message, player);
        }
    }

    /// <summary>
    /// Get list of all online players
    /// </summary>
    public List<Player> GetOnlinePlayers()
    {
        return PlayerManager.GetOnlinePlayers();
    }

    /// <summary>
    /// Get list of players in a specific room
    /// </summary>
    public List<Player> GetPlayersInRoom(string? roomId)
    {
        if (roomId == null) return new List<Player>();
        
        return PlayerManager.GetOnlinePlayers()
            .Where(p => p.Location == roomId)
            .ToList();
    }

    /// <summary>
    /// Find a player by name (case insensitive, partial match)
    /// </summary>
    public Player? FindPlayerByName(string name)
    {
        name = name.ToLower();
        return PlayerManager.GetOnlinePlayers()
            .FirstOrDefault(p => p.Name.ToLower().Contains(name));
    }

    #endregion

    #region Object Resolution and Lookup

    /// <summary>
    /// Resolve object names like "me", "here", "#123", "class:ClassName" to object IDs
    /// </summary>
    public string? ResolveObject(string objectName)
    {
        Logger.Debug($"ScriptHelpers: Resolving object name: '{objectName}'");
        
        string? result = null;
        
        // Handle special keywords first
        switch (objectName.ToLower())
        {
            case "me":
                result = _player.Id;
                break;
            case "here":
                result = _player.Location;
                break;
            case "system":
                result = GetSystemObjectId();
                break;
            default:
                // Check if it's a DBREF (starts with # followed by digits)
                if (objectName.StartsWith("#") && int.TryParse(objectName.Substring(1), out int dbref))
                {
                    var obj = GameDatabase.Instance.GameObjects.FindOne(o => o.DbRef == dbref);
                    result = obj?.Id;
                    Logger.Debug($"DBREF lookup #{dbref} -> {result ?? "not found"}");
                }
                // Check if it's a class reference (starts with "class:" or ends with ".class")
                else if (objectName.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
                {
                    var className = objectName.Substring(6); // Remove "class:" prefix
                    var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                        c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
                    result = objectClass?.Id;
                    Logger.Debug($"Class lookup '{className}' -> {result ?? "not found"}");
                }
                else if (objectName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
                {
                    var className = objectName.Substring(0, objectName.Length - 6); // Remove ".class" suffix
                    var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                        c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
                    result = objectClass?.Id;
                    Logger.Debug($"Class lookup '{className}' -> {result ?? "not found"}");
                }
                else
                {
                    // Try to find by name in current location, then globally, then as a class
                    result = FindObjectByName(objectName);
                    
                    // If not found as an object, try as a class name
                    if (result == null)
                    {
                        var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                            c.Name.Equals(objectName, StringComparison.OrdinalIgnoreCase));
                        
                        if (objectClass != null)
                        {
                            result = objectClass.Id;
                            Logger.Debug($"Found class '{objectName}' -> {result}");
                        }
                    }
                }
                break;
        }
        
        Logger.Debug($"Resolved '{objectName}' to: {result ?? "null"}");
        return result;
    }

    /// <summary>
    /// Find an object by name, first in current room, then globally
    /// </summary>
    public string? FindObjectByName(string name)
    {
        name = name.ToLower();
        
        // First, search in current location (most common case)
        if (_player.Location != null)
        {
            var localObjects = ObjectManager.GetObjectsInLocation(_player.Location);
            var localMatch = localObjects.FirstOrDefault(obj =>
            {
                var objName = ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
                var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
                return objName?.Contains(name) == true || shortDesc?.Contains(name) == true;
            });
            
            if (localMatch != null)
            {
                Logger.Debug($"Found '{name}' locally: #{localMatch.DbRef} ({ObjectManager.GetProperty(localMatch, "name")?.AsString})");
                return localMatch.Id;
            }
        }
        
        // If not found locally, search all players (common for targeting players)
        var players = PlayerManager.GetOnlinePlayers();
        var playerMatch = players.FirstOrDefault(p => p.Name.ToLower().Contains(name));
        if (playerMatch != null)
        {
            var playerObj = GameDatabase.Instance.GameObjects.FindById(playerMatch.Id);
            if (playerObj != null)
            {
                Logger.Debug($"Found player '{name}': #{playerObj.DbRef} ({playerMatch.Name})");
                return playerMatch.Id;
            }
        }
        
        // Finally, search globally (for admin/building purposes)
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        var globalMatch = allObjects.FirstOrDefault(obj =>
        {
            var objName = ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            return objName?.Contains(name) == true || shortDesc?.Contains(name) == true;
        });
        
        if (globalMatch != null)
        {
            Logger.Debug($"Found '{name}' globally: #{globalMatch.DbRef} ({ObjectManager.GetProperty(globalMatch, "name")?.AsString})");
            return globalMatch.Id;
        }
        
        Logger.Debug($"Object '{name}' not found anywhere");
        return null;
    }

    /// <summary>
    /// Get an object by its ID
    /// </summary>
    public GameObject? GetObject(string objectId)
    {
        return GameDatabase.Instance.GameObjects.FindById(objectId);
    }

    /// <summary>
    /// Get the display name for an object
    /// </summary>
    public string GetObjectName(string objectId)
    {
        var obj = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (obj == null) return "unknown object";
        
        var name = ObjectManager.GetProperty(obj, "name")?.AsString;
        if (!string.IsNullOrEmpty(name)) return name;
        
        var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString;
        if (!string.IsNullOrEmpty(shortDesc)) return shortDesc;
        
        return $"object #{obj.DbRef}";
    }

    /// <summary>
    /// Get the system object ID, creating it if it doesn't exist
    /// </summary>
    public string? GetSystemObjectId()
    {
        // Get all objects and filter in memory (LiteDB doesn't support ContainsKey in expressions)
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        var systemObj = allObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true);
        
        if (systemObj == null)
        {
            // System object doesn't exist, create it
            Logger.Debug("System object not found, creating it...");
            // Use Container class instead of abstract Object class
            var containerClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Container");
            if (containerClass != null)
            {
                systemObj = ObjectManager.CreateInstance(containerClass.Id);
                ObjectManager.SetProperty(systemObj, "name", "System");
                ObjectManager.SetProperty(systemObj, "shortDescription", "the system object");
                ObjectManager.SetProperty(systemObj, "longDescription", "This is the system object that holds global verbs and functions.");
                ObjectManager.SetProperty(systemObj, "isSystemObject", true);
                ObjectManager.SetProperty(systemObj, "gettable", false); // Don't allow players to pick up the system
                Logger.Debug($"Created system object with ID: {systemObj.Id}");
            }
            else
            {
                Logger.Error("Could not find Container class to create system object!");
                return null;
            }
        }
        
        Logger.Debug($"Resolved 'system' to object ID: {systemObj?.Id}");
        return systemObj?.Id;
    }

    #endregion

    #region Room and Movement

    /// <summary>
    /// Display the current room with exits, objects, and other players
    /// </summary>
    public void ShowRoom()
    {
        if (_player?.Location == null)
        {
            Say("You are nowhere.");
            return;
        }

        var room = GameDatabase.Instance.GameObjects.FindById(_player.Location);
        if (room == null)
        {
            Say("You are in a void.");
            return;
        }

        var name = ObjectManager.GetProperty(room, "name")?.AsString ?? "Unknown Room";
        var longDesc = ObjectManager.GetProperty(room, "longDescription")?.AsString ?? "You see nothing special.";

        Say($"=== {name} ===");
        Say(longDesc);

        // Show exits
        var exits = WorldManager.GetExitsFromRoom(_player.Location);
        if (exits.Any())
        {
            var exitNames = exits.Select(e => ObjectManager.GetProperty(e, "direction")?.AsString).Where(d => d != null);
            Say($"Exits: {string.Join(", ", exitNames)}");
        }

        // Show objects
        var objects = ObjectManager.GetObjectsInLocation(_player.Location)
            .Where(obj => obj.ClassId != GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Exit")?.Id)
            .Where(obj => obj.ClassId != GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Player")?.Id);

        foreach (var obj in objects)
        {
            var visible = ObjectManager.GetProperty(obj, "visible")?.AsBoolean ?? true;
            if (visible)
            {
                var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString ?? "something";
                Say($"You see {shortDesc} here.");
            }
        }

        // Show other players
        var otherPlayers = GetPlayersInRoom(_player.Location).Where(p => p.Id != _player.Id);
        foreach (var otherPlayer in otherPlayers)
        {
            Say($"{otherPlayer.Name} is here.");
        }
    }

    /// <summary>
    /// Look at a specific object
    /// </summary>
    public void LookAtObject(string target)
    {
        if (_player?.Location == null) return;

        target = target.ToLower();
        var objects = ObjectManager.GetObjectsInLocation(_player.Location);
        
        var targetObject = objects.FirstOrDefault(obj =>
        {
            var name = ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            return name?.Contains(target) == true || shortDesc?.Contains(target) == true;
        });

        if (targetObject == null)
        {
            Say("You don't see that here.");
            return;
        }

        var longDesc = ObjectManager.GetProperty(targetObject, "longDescription")?.AsString ?? "You see nothing special.";
        Say(longDesc);
    }

    /// <summary>
    /// Get all exits from a room
    /// </summary>
    public List<GameObject> GetExitsFromRoom(string roomId)
    {
        return WorldManager.GetExitsFromRoom(roomId);
    }

    /// <summary>
    /// Get all objects in a specific location
    /// </summary>
    public List<GameObject> GetObjectsInLocation(string locationId)
    {
        return ObjectManager.GetObjectsInLocation(locationId);
    }

    /// <summary>
    /// Move an object to a new location
    /// </summary>
    public bool MoveObject(string objectId, string destinationId)
    {
        try
        {
            ObjectManager.MoveObject(objectId, destinationId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to move object {objectId} to {destinationId}: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Object Properties

    /// <summary>
    /// Get a property value from an object
    /// </summary>
    public BsonValue? GetProperty(GameObject obj, string propertyName)
    {
        return ObjectManager.GetProperty(obj, propertyName);
    }

    /// <summary>
    /// Get a property value from an object by ID
    /// </summary>
    public BsonValue? GetProperty(string objectId, string propertyName)
    {
        var obj = GetObject(objectId);
        return obj != null ? ObjectManager.GetProperty(obj, propertyName) : null;
    }

    /// <summary>
    /// Set a property value on an object
    /// </summary>
    public void SetProperty(GameObject obj, string propertyName, object value)
    {
        // Convert object to BsonValue
        BsonValue bsonValue = value switch
        {
            null => BsonValue.Null,
            string s => new BsonValue(s),
            int i => new BsonValue(i),
            long l => new BsonValue(l),
            double d => new BsonValue(d),
            bool b => new BsonValue(b),
            DateTime dt => new BsonValue(dt),
            BsonValue bv => bv,
            _ => new BsonValue(value.ToString() ?? "")
        };
        
        ObjectManager.SetProperty(obj, propertyName, bsonValue);
    }

    /// <summary>
    /// Set a property value on an object by ID
    /// </summary>
    public void SetProperty(string objectId, string propertyName, object value)
    {
        var obj = GetObject(objectId);
        if (obj != null)
        {
            SetProperty(obj, propertyName, value);
        }
    }

    #endregion

    #region Inventory and Object Management

    /// <summary>
    /// Show the player's inventory
    /// </summary>
    public void ShowInventory()
    {
        if (_player == null) return;

        var playerGameObject = GameDatabase.Instance.GameObjects.FindById(_player.Id);
        if (playerGameObject?.Contents == null || !playerGameObject.Contents.Any())
        {
            Say("You are carrying nothing.");
            return;
        }

        Say("You are carrying:");
        foreach (var itemId in playerGameObject.Contents)
        {
            var item = GameDatabase.Instance.GameObjects.FindById(itemId);
            if (item != null)
            {
                var name = ObjectManager.GetProperty(item, "shortDescription")?.AsString ?? "something";
                Say($"  {name}");
            }
        }
    }

    /// <summary>
    /// Find an item in the player's inventory by name
    /// </summary>
    public GameObject? FindItemInInventory(string itemName)
    {
        if (_player == null) return null;

        itemName = itemName.ToLower();
        var playerGameObject = GameDatabase.Instance.GameObjects.FindById(_player.Id);
        if (playerGameObject?.Contents == null) return null;

        return playerGameObject.Contents
            .Select(id => GameDatabase.Instance.GameObjects.FindById(id))
            .FirstOrDefault(obj =>
            {
                if (obj == null) return false;
                var name = ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
                var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
                return name?.Contains(itemName) == true || shortDesc?.Contains(itemName) == true;
            });
    }

    /// <summary>
    /// Find an item in the current room by name
    /// </summary>
    public GameObject? FindItemInRoom(string itemName)
    {
        if (_player?.Location == null) return null;

        itemName = itemName.ToLower();
        var roomObjects = ObjectManager.GetObjectsInLocation(_player.Location);
        
        return roomObjects.FirstOrDefault(obj =>
        {
            var name = ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            return name?.Contains(itemName) == true || shortDesc?.Contains(itemName) == true;
        });
    }

    #endregion

    #region Utility Functions

    /// <summary>
    /// Check if an object is gettable
    /// </summary>
    public bool IsGettable(GameObject obj)
    {
        return ObjectManager.GetProperty(obj, "gettable")?.AsBoolean ?? false;
    }

    /// <summary>
    /// Check if an object is visible
    /// </summary>
    public bool IsVisible(GameObject obj)
    {
        return ObjectManager.GetProperty(obj, "visible")?.AsBoolean ?? true;
    }

    /// <summary>
    /// Get the current player
    /// </summary>
    public Player GetCurrentPlayer()
    {
        return _player;
    }

    /// <summary>
    /// Get the current player's location
    /// </summary>
    public string? GetCurrentLocation()
    {
        return _player?.Location;
    }

    #endregion

    #region Verb Management

    /// <summary>
    /// Get information about a specific verb on an object
    /// </summary>
    public VerbInfo? GetVerbInfo(string objectSpec, string verbName)
    {
        var objectId = ResolveObject(objectSpec);
        if (objectId == null)
        {
            return null;
        }

        var verb = VerbManager.GetVerbsOnObject(objectId)
            .FirstOrDefault(v => v.Name.ToLower() == verbName.ToLower());

        if (verb == null)
        {
            return null;
        }

        return new VerbInfo
        {
            ObjectId = objectId,
            ObjectName = GetObjectName(objectId),
            VerbName = verb.Name,
            Aliases = verb.Aliases,
            Pattern = verb.Pattern,
            Description = verb.Description,
            CreatedBy = verb.CreatedBy,
            CreatedAt = verb.CreatedAt,
            Code = verb.Code ?? "",
            CodeLines = string.IsNullOrEmpty(verb.Code) ? new string[0] : verb.Code.Split('\n')
        };
    }

    #endregion
}

/// <summary>
/// Information about a verb for display purposes
/// </summary>
public class VerbInfo
{
    public string ObjectId { get; set; } = "";
    public string ObjectName { get; set; } = "";
    public string VerbName { get; set; } = "";
    public string? Aliases { get; set; }
    public string? Pattern { get; set; }
    public string? Description { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Code { get; set; } = "";
    public string[] CodeLines { get; set; } = new string[0];
}
