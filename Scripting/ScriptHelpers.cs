using CSMOO.Database;
using CSMOO.Commands;
using CSMOO.Logging;
using CSMOO.Verbs;
using CSMOO.Configuration;
using LiteDB;
using CSMOO.Object;

namespace CSMOO.Scripting;

/// <summary>
/// Comprehensive helper class for script execution, providing all the functions 
/// that built-in commands use so that verbs can replicate their functionality
/// </summary>
public class ScriptHelpers
{
    public readonly Player _player;
    public readonly CommandProcessor _commandProcessor;
    private readonly IObjectManager _objectManager;
    private readonly IPlayerManager _playerManager;
    private readonly IDbProvider _dbProvider;
    private readonly ILogger _logger;
    private readonly IVerbManager _verbManager;
    private readonly IRoomManager _roomManager;

    // Primary constructor with DI dependencies
    public ScriptHelpers(
        Player player, 
        CommandProcessor commandProcessor,
        IObjectManager objectManager,
        IPlayerManager playerManager,
        IDbProvider dbProvider,
        ILogger logger,
        IVerbManager verbManager,
        IRoomManager roomManager)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
        _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
        _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
        _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _verbManager = verbManager ?? throw new ArgumentNullException(nameof(verbManager));
        _roomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
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
    /// Send a message to a specific player
    /// </summary>
    public void notify(Player targetPlayer, string message)
    {
        if (targetPlayer?.SessionGuid != null)
        {
            _commandProcessor.SendToPlayer(message, targetPlayer.SessionGuid);
        }
    }

    /// <summary>
    /// Send a message to a specific player by GameObject (for scripting)
    /// </summary>
    public void notify(GameObject playerObj, string message)
    {
        // Convert GameObject to Player
        var player = _objectManager.GetObject<Player>(playerObj.Id);
        if (player?.SessionGuid != null)
        {
            _commandProcessor.SendToPlayer(message, player.SessionGuid);
        }
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

        var playersInRoom = GetPlayersInRoom(_player.Location.Id);
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
        return _playerManager.GetOnlinePlayers();
    }

    /// <summary>
    /// Get list of players in a specific room
    /// </summary>
    public List<Player> GetPlayersInRoom(string? roomId)
    {
        if (roomId == null) return new List<Player>();
        
        return _playerManager.GetOnlinePlayers()
            .Where(p => p.Location?.Id == roomId)
            .ToList();
    }

    /// <summary>
    /// Find a player by name (case insensitive, partial match)
    /// </summary>
    public Player? FindPlayerByName(string name)
    {
        name = name.ToLower();
        return _playerManager.GetOnlinePlayers()
            .FirstOrDefault(p => p.Name.ToLower().Contains(name));
    }

    #endregion

    #region Object Resolution and Lookup

    /// <summary>
    /// Resolve object names like "me", "here", "#123", "class:ClassName" to object IDs
    /// </summary>
    public string? ResolveObject(string objectName)
    {
        
        string? result = null;
        
        // Handle special keywords first
        switch (objectName.ToLower())
        {
            case "me":
                result = _player.Id;
                break;
            case "here":
                result = _player.Location?.Id;
                break;
            case "system":
                result = GetSystemObjectId();
                break;
            default:
                // Check if it's a DBREF (starts with # followed by digits)
                if (objectName.StartsWith("#") && int.TryParse(objectName.Substring(1), out int dbref))
                {
                    var obj = _objectManager.GetObjectByDbRef(dbref);
                    result = obj?.Id;
                }
                // Check if it's a class reference (starts with "class:" or ends with ".class")
                else if (objectName.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
                {
                    var className = objectName.Substring(6); // Remove "class:" prefix
                    var objectClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => 
                        c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
                    result = objectClass?.Id;
                }
                else if (objectName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
                {
                    var className = objectName.Substring(0, objectName.Length - 6); // Remove ".class" suffix
                    var objectClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => 
                        c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
                    result = objectClass?.Id;
                }
                // Check if it's a direct class ID (like "Room", "Exit", etc.)
                else if (_dbProvider.FindById<ObjectClass>("objectclasses", objectName) != null)
                {
                    result = objectName; // The objectName itself is the class ID
                }
                else
                {
                    // Try to find by name in current location, then globally, then as a class
                    result = FindObjectByName(objectName);
                    
                    // If not found as an object, try as a class name
                    if (result == null)
                    {
                        var objectClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => 
                            c.Name.Equals(objectName, StringComparison.OrdinalIgnoreCase));
                        if (objectClass != null)
                        {
                            result = objectClass.Id;
                        }
                    }
                }
                break;
        }
        
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
            var localObjects = _objectManager.GetObjectsInLocation(_player.Location);
            var localMatch = localObjects.FirstOrDefault(obj =>
            {
                var objName = _objectManager.GetProperty(obj, "name")?.AsString?.ToLower();
                var shortDesc = _objectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
                return objName?.Contains(name) == true || shortDesc?.Contains(name) == true;
            });
            
            if (localMatch != null)
            {
                return localMatch.Id;
            }
        }
        
        // If not found locally, search all players (common for targeting players)
        var players = _playerManager.GetOnlinePlayers();
        var playerMatch = players.FirstOrDefault(p => p.Name.ToLower().Contains(name));
        if (playerMatch != null)
        {
            var playerObj = _objectManager.GetObject(playerMatch.Id);
            if (playerObj != null)
            {
                return playerMatch.Id;
            }
        }
        
        // Finally, search globally (for admin/building purposes)
        var allObjects = _objectManager.GetAllObjects();
        var globalMatch = allObjects.OfType<GameObject>().FirstOrDefault(obj =>
        {
            var objName = _objectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = _objectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            return objName?.Contains(name) == true || shortDesc?.Contains(name) == true;
        });
        
        if (globalMatch != null)
        {
            return globalMatch.Id;
        }
        return null;
    }

    /// <summary>
    /// Get an object by its ID - returns a GameObject for property access
    /// </summary>
    public GameObject? GetObject(string objectId)
    {
        return _objectManager.GetObject(objectId);
    }

    /// <summary>
    /// Get the display name for an object
    /// </summary>
    public string GetObjectName(string objectId)
    {
        var obj = _objectManager.GetObject(objectId);
        if (obj == null) return "unknown object";
        
        var name = _objectManager.GetProperty(obj, "name")?.AsString;
        if (!string.IsNullOrEmpty(name)) return name;
        
        var shortDesc = _objectManager.GetProperty(obj, "shortDescription")?.AsString;
        if (!string.IsNullOrEmpty(shortDesc)) return shortDesc;
        
        return $"object #{obj.DbRef}";
    }

    /// <summary>
    /// Get the system object ID, creating it if it doesn't exist
    /// </summary>
    public string? GetSystemObjectId()
    {
        // Get all objects from ObjectManager cache
        var allObjects = _objectManager.GetAllObjects();
        var systemObj = allObjects.OfType<GameObject>().FirstOrDefault(obj => 
            (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
            (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
        
        if (systemObj == null)
        {
            // System object doesn't exist, create it
            _logger.Warning("System object not found, creating it...");
            // Use Container class instead of abstract Object class
            var containerClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Container");
            if (containerClass != null)
            {
                systemObj = _objectManager.CreateInstance(containerClass.Id);
                _objectManager.SetProperty(systemObj, "name", "System");
                _objectManager.SetProperty(systemObj, "shortDescription", "the system object");
                _objectManager.SetProperty(systemObj, "longDescription", "This is the system object that holds global verbs and functions.");
                _objectManager.SetProperty(systemObj, "isSystemObject", true);
                _objectManager.SetProperty(systemObj, "gettable", false); // Don't allow players to pick up the system
                _logger.Info($"Created system object with ID: {systemObj.Id}");
            }
            else
            {
                _logger.Error("Could not find Container class to create system object!");
                return null;
            }
        }
        return systemObj?.Id;
    }

    #endregion

    #region Player Management

    /// <summary>
    /// Update a player's property in both the database and in-memory object
    /// </summary>
    public void UpdatePlayerProperty(Player player, string propertyName, object value)
    {
        // Update the database record
        var playerRecord = _objectManager.GetObject<Player>(player.Id);
        if (playerRecord != null)
        {
            // Use reflection to set the property on the database record
            var property = typeof(Player).GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(playerRecord, value);
                _dbProvider.Update<Player>("players", playerRecord);
                
                // Also update the in-memory object
                property.SetValue(player, value);
                
            }
            else
            {
                _logger.Warning($"Property '{propertyName}' not found or not writable on Player class");
            }
        }
        else
        {
            _logger.Error($"Player record not found in database for ID: {player.Id}");
        }
    }

    /// <summary>
    /// Update a player's location in both the database and in-memory object
    /// </summary>
    public void UpdatePlayerLocation(Player player, string newLocation)
    {
        UpdatePlayerProperty(player, "Location", newLocation);
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
            if (_player != null) notify(_player, "You are nowhere.");
            return;
        }

        var room = _objectManager.GetObject(_player.Location.Id);
        if (room == null)
        {
            notify(_player, "You are in a void.");
            return;
        }

        var name = _objectManager.GetProperty(room, "name")?.AsString ?? "Unknown Room";
        var longDesc = _objectManager.GetProperty(room, "longDescription")?.AsString ?? "You see nothing special.";

        notify(_player, $"=== {name} ===");
        notify(_player, longDesc);

        // Show exits
        var exits = _roomManager.GetExits(_player.Location);
        if (exits.Any())
        {
            var exitNames = exits.Select(e => _objectManager.GetProperty(e, "direction")?.AsString).Where(d => d != null);
            notify(_player, $"Exits: {string.Join(", ", exitNames)}");
        }

        // Show objects
        var objects = _objectManager.GetObjectsInLocation(_player.Location)
            .Where(obj => obj.ClassId != _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Exit")?.Id)
            .Where(obj => obj.ClassId != _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Player")?.Id);

        foreach (var obj in objects)
        {
            var visible = _objectManager.GetProperty(obj, "visible")?.AsBoolean ?? true;
            if (visible)
            {
                var shortDesc = _objectManager.GetProperty(obj, "shortDescription")?.AsString ?? "something";
                notify(_player, $"You see {shortDesc} here.");
            }
        }

        // Show other players
        var otherPlayers = GetPlayersInRoom(_player.Location.Id).Where(p => p.Id != _player.Id);
        foreach (var otherPlayer in otherPlayers)
        {
            notify(_player, $"{otherPlayer.Name} is here.");
        }
    }

    /// <summary>
    /// Look at a specific object
    /// </summary>
    public void LookAtObject(string target)
    {
        if (_player?.Location == null) return;

        target = target.ToLower();
        var objects = _objectManager.GetObjectsInLocation(_player.Location);
        
        var targetObject = objects.FirstOrDefault(obj =>
        {
            var name = _objectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = _objectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            return name?.Contains(target) == true || shortDesc?.Contains(target) == true;
        });

        if (targetObject == null)
        {
            notify(_player, "You don't see that here.");
            return;
        }

        var longDesc = _objectManager.GetProperty(targetObject, "longDescription")?.AsString ?? "You see nothing special.";
        notify(_player, longDesc);
    }

    /// <summary>
    /// Get all exits from a room
    /// </summary>
    public List<GameObject> GetExits(string roomId)
    {
        return _roomManager.GetExits(roomId);
    }

    /// <summary>
    /// Get all objects in a specific location - returns GameObjects for property access
    /// </summary>
    public List<GameObject> GetObjectsInLocation(string locationId)
    {
        var gameObjects = _objectManager.GetObjectsInLocation(locationId);
        return gameObjects.ToList();
    }

    /// <summary>
    /// Move an object to a new location
    /// </summary>
    public bool MoveObject(string objectId, string destinationId)
    {
        try
        {
            _objectManager.MoveObject(objectId, destinationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to move object {objectId} to {destinationId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Move a player to a new location
    /// </summary>
    public bool MoveObject(Player playerObj, string destinationId)
    {
        try
        {
            ObjectManager.MoveObject(playerObj.Id, destinationId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to move player {playerObj.Id} to {destinationId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Move a game object to a new location
    /// </summary>
    public bool MoveObject(GameObject gameObj, string destinationId)
    {
        try
        {
            ObjectManager.MoveObject(gameObj.Id, destinationId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to move object {gameObj.Id} to {destinationId}: {ex.Message}");
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
        return _objectManager.GetProperty(obj, propertyName);
    }

    /// <summary>
    /// Get a property value from an object by ID
    /// </summary>
    public BsonValue? GetProperty(string objectId, string propertyName)
    {
        var obj = GetObject(objectId);
        return obj != null ? _objectManager.GetProperty(obj, propertyName) : null;
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
        
        _objectManager.SetProperty(obj, propertyName, bsonValue);
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

        var playerGameObject = _objectManager.GetObject(_player.Id);
        if (playerGameObject?.Contents == null || !playerGameObject.Contents.Any())
        {
            notify(_player, "You are carrying nothing.");
            return;
        }

        notify(_player, "You are carrying:");
        foreach (var itemId in playerGameObject.Contents)
        {
            var item = _objectManager.GetObject(itemId);
            if (item != null)
            {
                var name = _objectManager.GetProperty(item, "shortDescription")?.AsString ?? "something";
                notify(_player, $"  {name}");
            }
        }
    }

    /// <summary>
    /// Find an item in the player's inventory by name - returns GameObject
    /// </summary>
    public GameObject? FindItemInInventory(string itemName)
    {
        if (_player == null) return null;

        itemName = itemName.ToLower();
        var playerGameObject = _objectManager.GetObject(_player.Id);
        if (playerGameObject?.Contents == null) return null;

        var foundObject = playerGameObject.Contents
            .Select(id => _objectManager.GetObject(id))
            .FirstOrDefault(obj =>
            {
                if (obj == null) return false;
                var name = _objectManager.GetProperty(obj, "name")?.AsString?.ToLower();
                var shortDesc = _objectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
                return name?.Contains(itemName) == true || shortDesc?.Contains(itemName) == true;
            });
            
        return foundObject;
    }

    /// <summary>
    /// Find an item in the current room by name - returns GameObject
    /// </summary>
    public GameObject? FindItemInRoom(string itemName)
    {
        if (_player?.Location == null) return null;

        itemName = itemName.ToLower();
        var roomObjects = _objectManager.GetObjectsInLocation(_player.Location);
        
        var foundObject = roomObjects.FirstOrDefault(obj =>
        {
            var name = _objectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = _objectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            return name?.Contains(itemName) == true || shortDesc?.Contains(itemName) == true;
        });
        
        return foundObject;
    }

    #endregion

    #region Utility Functions

    /// <summary>
    /// Check if an object is gettable
    /// </summary>
    public bool IsGettable(GameObject obj)
    {
        return _objectManager.GetProperty(obj, "gettable")?.AsBoolean ?? false;
    }

    /// <summary>
    /// Check if an object is visible
    /// </summary>
    public bool IsVisible(GameObject obj)
    {
        return _objectManager.GetProperty(obj, "visible")?.AsBoolean ?? true;
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
        return _player?.Location?.Id;
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

        var verb = _verbManager.GetVerbsOnObject(objectId)
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



