using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Verbs;
using CSMOO.Functions;
using LiteDB;
using CSMOO.Object;

namespace CSMOO.Scripting;

/// <summary>
/// Built-in functions for verb scripts - provides clean, consistent API without casting or long namespaces
/// </summary>
public static class Builtins
{
    /// <summary>
    /// Current script context - set by the script engine before execution (legacy, now UnifiedScriptGlobals)
    /// </summary>
    public static ScriptGlobals? CurrentContext { get; set; }
    
    /// <summary>
    /// Unified script context - set by the UnifiedScriptEngine before execution
    /// </summary>
    public static ScriptGlobals? UnifiedContext { get; set; }

    #region Object Management

    /// <summary>
    /// Find a game object by its ID
    /// </summary>
    public static dynamic? FindObject(string objectId)
    {
        if (string.IsNullOrEmpty(objectId)) return null;
        return ObjectManager.GetObject(objectId);
    }
    
    
    /// <summary>
    /// Get the string value of an object property (GameObject overload)
    /// </summary>
    public static BsonValue? GetProperty(GameObject obj, string propertyName)
    {
        return ObjectManager.GetProperty(obj, propertyName);
    }

    public static string[] GetAllPropertyNames(GameObject obj)
    {
        return ObjectManager.GetPropertyNames(obj);
    }

  
    
    /// <summary>
    /// Get the string value of an object property with default (GameObject overload)
    /// </summary>
    public static string GetProperty(GameObject obj, string propertyName, string defaultValue = "")
    {
        var property = ObjectManager.GetProperty(obj, propertyName) as BsonValue;
        return property?.AsString ?? defaultValue;
    }
    
    
    /// <summary>
    /// Get the boolean value of an object property (GameObject overload)
    /// </summary>
    public static bool GetBoolProperty(GameObject obj, string propertyName, bool defaultValue = false)
    {
        var property = ObjectManager.GetProperty(obj, propertyName) as BsonValue;
        return property?.AsBoolean ?? defaultValue;
    }
    

    
    /// <summary>
    /// Set a property on an object (GameObject overload)
    /// </summary>
    public static void SetProperty(GameObject obj, string propertyName, string value)
    {
        if (obj != null)
        {
            ObjectManager.SetProperty(obj, propertyName, value);
        }
    }
    

    
    /// <summary>
    /// Set a boolean property on an object (GameObject overload)
    /// </summary>
    public static void SetBoolProperty(GameObject obj, string propertyName, bool value)
    {
        if (obj != null)
        {
            ObjectManager.SetProperty(obj, propertyName, value);
        }       
    }
    
    /// <summary>
    /// Get all objects in a location - returns GameObject dynamic objects
    /// </summary>
    public static List<dynamic> GetObjectsInLocation(string locationId)
    {
        var gameObjects = ObjectManager.GetObjectsInLocation(locationId);
        return gameObjects.Cast<dynamic>().ToList();
    }
    

    
    /// <summary>
    /// Move an object to a new location (GameObject overload)
    /// </summary>
    public static bool MoveObject(GameObject obj, string newLocationId)
    {
        if (obj != null)
        {
            ObjectManager.SetProperty(obj, "location", newLocationId);
            return true;
        }
        return false;
    }
    
    
    /// <summary>
    /// Get the name of an object (GameObject overload)
    /// </summary>
    public static string GetObjectName(GameObject obj)
    {
        return GetObjectName(obj);
    }
    
  
    
    /// <summary>
    /// Get the short description of an object (GameObject overload)
    /// </summary>
    public static string GetObjectShortDesc(GameObject obj)
    {
        return GetObjectShortDesc(obj);
    }
    

    
    /// <summary>
    /// Get the long description of an object (GameObject overload)
    /// </summary>
    public static string GetObjectLongDesc(GameObject obj)
    {
        return GetProperty(obj, "longDescription");
    }


    #endregion

    #region Player Management

    /// <summary>
    /// Find a player by name
    /// </summary>
    public static dynamic? FindPlayer(string playerName)
    {
            return ObjectManager.GetAllObjects()
            .OfType<Player>()
            .FirstOrDefault(p => p.Name.Contains(playerName, StringComparison.OrdinalIgnoreCase));
       
    }
    
    
    /// <summary>
    /// Find a player by ID
    /// </summary>
    public static dynamic? FindPlayerById(string playerId)
    {
        return ObjectManager.GetObject<Player>(playerId);
    }
    
    /// <summary>
    /// Get all online players
    /// </summary>
    public static List<dynamic> GetOnlinePlayers()
    {
        return PlayerManager.GetOnlinePlayers().AsEnumerable().Cast<dynamic>().ToList();
    }
    
    /// <summary>
    /// Get all players (online and offline) - useful for lambda filtering
    /// </summary>
    public static List<dynamic> GetAllPlayers()
    {
        return ObjectManager.GetAllObjects()
            .OfType<Player>()
            .Cast<dynamic>()
            .ToList();
    }
    
    /// <summary>
    /// Get all game objects - useful for lambda filtering and searching
    /// </summary>
    public static List<dynamic> GetAllObjects()
    {
        return ObjectManager.GetAllObjects()
            .OfType<GameObject>()
            .Cast<dynamic>()
            .ToList();
    }
    
    /// <summary>
    /// Get all object classes - useful for lambda filtering
    /// </summary>
    public static List<dynamic> GetAllObjectClasses()
    {
        return ObjectManager.GetAllObjectClasses().Cast<dynamic>().ToList();
    }

    public static List<dynamic> GetObjectsByClass(string className)
    {
        if (string.IsNullOrEmpty(className)) return new List<dynamic>();
        
        var objectClass = ObjectManager.GetClassByName(className);
        
        if (objectClass == null) return new List<dynamic>();
        
        return ObjectManager.GetAllObjects()
            .Where(obj => obj.ClassId == objectClass.Id)
            .Cast<dynamic>()
            .ToList();
    }
    
    /// <summary>
    /// Get an object class by its name
    /// </summary>
    public static ObjectClass? GetClassByName(string className)
    {
        if (string.IsNullOrEmpty(className)) return null;
        return ObjectManager.GetClassByName(className);
    }
    
    /// <summary>
    /// Get an object class by its ID
    /// </summary>
    public static ObjectClass? GetClass(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return null;
        return ObjectManager.GetClass(classId);
    }

    public static List<Verb> GetVerbsOnClass(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return new List<Verb>();
        return DbProvider.Instance.FindVerbsByObjectId(classId).ToList();
    }

    public static List<Function> GetFunctionsOnClass(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return new List<Function>();
        return FunctionResolver.GetFunctionsForObject(classId, true);
    }
    
    /// <summary>
    /// Check if a player has a specific permission flag
    /// </summary>
    public static bool HasFlag(Player player, string flagName)
    {
        if (Enum.TryParse<PermissionManager.Flag>(flagName, true, out var flag))
        {
            return PermissionManager.HasFlag(player, flag);
        }
        return false;
    }
    
    /// <summary>
    /// Check if a player has Admin flag
    /// </summary>
    public static bool IsAdmin(Player player)
    {
        return PermissionManager.HasFlag(player, PermissionManager.Flag.Admin);
    }
    
    /// <summary>
    /// Check if a player has Moderator flag
    /// </summary>
    public static bool IsModerator(Player player)
    {
        return PermissionManager.HasFlag(player, PermissionManager.Flag.Moderator);
    }
    
    /// <summary>
    /// Check if a player has Programmer flag
    /// </summary>
    public static bool IsProgrammer(Player player)
    {
        return PermissionManager.HasFlag(player, PermissionManager.Flag.Programmer);
    }
    

    
    /// <summary>
    /// Get formatted flags string for a player
    /// </summary>
    public static string GetPlayerFlagsString(Player player)
    {
        return PermissionManager.GetFlagsString(player);
    }
    
    /// <summary>
    /// Check if a player has Admin flag (dynamic overload for UnifiedScriptEngine)
    /// </summary>
    public static bool IsAdmin(dynamic? player)
    {
        if (player == null) return false;
        
        // Handle GameObject wrapper
        if (player is GameObject gameObject)
        {
            var dbPlayer = ObjectManager.GetObject<Player>( gameObject.Id);
            return dbPlayer != null && PermissionManager.HasFlag(dbPlayer, PermissionManager.Flag.Admin);
        }
        
        // Handle direct Database.Player
        if (player is Player dbPlayerDirect)
        {
            return PermissionManager.HasFlag(dbPlayerDirect, PermissionManager.Flag.Admin);
        }
        
        // Handle dynamic object with Id property
        if (player.Id != null)
        {
            var dbPlayer = ObjectManager.GetObject<Player>( (string)player.Id);
            return dbPlayer != null && PermissionManager.HasFlag(dbPlayer, PermissionManager.Flag.Admin);
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a player has Moderator flag (dynamic overload for UnifiedScriptEngine)
    /// </summary>
    public static bool IsModerator(dynamic? player)
    {
        if (player == null) return false;
        
        // Handle GameObject wrapper
        if (player is GameObject gameObject)
        {
            var dbPlayer = ObjectManager.GetObject<Player>(gameObject.Id);
            return dbPlayer != null && PermissionManager.HasFlag(dbPlayer, PermissionManager.Flag.Moderator);
        }
        
        // Handle direct Database.Player
        if (player is Player dbPlayerDirect)
        {
            return PermissionManager.HasFlag(dbPlayerDirect, PermissionManager.Flag.Moderator);
        }
        
        // Handle dynamic object with Id property
        if (player.Id != null)
        {
            var dbPlayer = ObjectManager.GetObject<Player>( (string)player.Id);
            return dbPlayer != null && PermissionManager.HasFlag(dbPlayer, PermissionManager.Flag.Moderator);
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a player has Programmer flag (dynamic overload for UnifiedScriptEngine)
    /// </summary>
    public static bool IsProgrammer(dynamic? player)
    {
        if (player == null) return false;
        
        // Handle GameObject wrapper
        if (player is GameObject gameObject)
        {
            var dbPlayer = ObjectManager.GetObject<Player>( gameObject.Id);
            return dbPlayer != null && PermissionManager.HasFlag(dbPlayer, PermissionManager.Flag.Programmer);
        }
        
        // Handle direct Database.Player
        if (player is Player dbPlayerDirect)
        {
            return PermissionManager.HasFlag(dbPlayerDirect, PermissionManager.Flag.Programmer);
        }
        
        // Handle dynamic object with Id property
        if (player.Id != null)
        {
            var dbPlayer = ObjectManager.GetObject<Player>( (string)player.Id);
            return dbPlayer != null && PermissionManager.HasFlag(dbPlayer, PermissionManager.Flag.Programmer);
        }
        
        return false;
    }
    
    /// <summary>
    /// Get all flags for a player as a list of strings (dynamic overload for UnifiedScriptEngine)
    /// </summary>
    public static List<string> GetPlayerFlags(GameObject? player)
    {
        if (player == null) return new List<string>();
        
        // Handle GameObject wrapper
        if (player is GameObject gameObject)
        {
            var dbPlayer = ObjectManager.GetObject<Player>(gameObject.Id);
            return dbPlayer != null ? PermissionManager.GetPlayerFlags(dbPlayer).Select(f => f.ToString()).ToList() : new List<string>();
        }
        
        // Handle direct Database.Player
        if (player is Player dbPlayerDirect)
        {
            return PermissionManager.GetPlayerFlags(dbPlayerDirect).Select(f => f.ToString()).ToList();
        }
        
        // Handle dynamic object with Id property
        if (player.Id != null)
        {
            var dbPlayer = ObjectManager.GetObject<Player>((string)player.Id);
            return dbPlayer != null ? PermissionManager.GetPlayerFlags(dbPlayer).Select(f => f.ToString()).ToList() : new List<string>();
        }
        
        return new List<string>();
    }
    
    #endregion
    
    #region Object Finding and Resolution
    
    /// <summary>
    /// Smart object resolution - finds players first, then objects by name
    /// </summary>
    public static string? ResolveObject(string objectName, Player currentPlayer)
    {
        if (string.IsNullOrEmpty(objectName)) return null;
        
        // Handle special keywords
        switch (objectName.ToLower())
        {
            case "me":
                return currentPlayer.Id;
            case "here":
                return currentPlayer.Location?.Id;
            case "system":
                // Find the system object
                var allObjects = ObjectManager.GetAllObjects();
                var systemObj = allObjects.FirstOrDefault(obj =>
                    (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
                    (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
                return systemObj?.Id;
        }
        
        // Check if it's a DBREF (starts with # followed by digits)
        if (objectName.StartsWith("#") && int.TryParse(objectName.Substring(1), out int dbref))
        {
            var obj = ObjectManager.FindByDbRef(dbref);
            return obj?.Id;
        }

        // Check if it's a class reference (starts with "class:" or ends with ".class")
        if (objectName.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectName.Substring(6); // Remove "class:" prefix
            var objectClass = ObjectManager.GetClassByName(className);
            return objectClass?.Id;
        }
        
        if (objectName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectName.Substring(0, objectName.Length - 6); // Remove ".class" suffix
            var objectClass = ObjectManager.GetClassByName(className);
            return objectClass?.Id;
        }

        // Check if it's a direct class ID (like "Room", "Exit", etc.)
        var classById = ObjectManager.GetClass(objectName);
        if (classById != null)
        {
            return classById.Id;
        }
        
        // Try to find a player first
        var player = FindPlayer(objectName);
        if (player != null)
        {
            return player.Id;
        }
        
        // Try to find object by name in current location
        if (currentPlayer.Location != null)
        {
            var objectsInRoom = GetObjectsInLocation(currentPlayer.Location.Id);
            var foundObject = objectsInRoom.FirstOrDefault(obj =>
            {
                var gameObject = obj.GameObject as GameObject;
                if (gameObject == null) return false;
                var objName = GetObjectName(gameObject);
                return objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
            });
            
            if (foundObject != null)
            {
                var gameObject = foundObject.GameObject as GameObject;
                if (gameObject != null) return gameObject.Id;
            }
        }
        
        // Try player inventory
        var inventory = GetObjectsInLocation(currentPlayer.Id);
        var inventoryObject = inventory.FirstOrDefault(obj =>
        {
            var gameObject = obj.GameObject as GameObject;
            if (gameObject == null) return false;
            var objName = GetObjectName(gameObject);
            return objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
        });
        
        if (inventoryObject != null)
        {
            var gameObject = inventoryObject.GameObject as GameObject;
            if (gameObject != null) return gameObject.Id;
        }
        
        // If not found as an object, try as a class name
        var directClass = ObjectManager.GetClassByName(objectName);

        if (directClass != null)
        {
            return directClass.Id;
        }

        // Finally, search globally for any object with a matching name
        var globalObjects = ObjectManager.GetAllObjects();
        var globalObject = globalObjects.FirstOrDefault(obj =>
        {
            var objName = GetObjectName(obj);
            return objName.Equals(objectName, StringComparison.OrdinalIgnoreCase) ||
                   objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
        });
        
        return globalObject?.Id;
    }
    
    /// <summary>
    /// Find an object by name in the current room
    /// </summary>
    public static dynamic? FindObjectInRoom(string objectName, Player currentPlayer)
    {
        if (currentPlayer.Location == null) return null;
        
        var objectsInRoom = GetObjectsInLocation(currentPlayer.Location.Id);
        var foundObject = objectsInRoom.FirstOrDefault(obj =>
        {
            var gameObject = obj.GameObject as GameObject;
            if (gameObject == null) return false;
            var objName = GetObjectName(gameObject);
            return objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
        });
        
        return foundObject?.GameObject as GameObject;
    }
    
    /// <summary>
    /// Find an object by name in player's inventory
    /// </summary>
    public static dynamic? FindObjectInInventory(string objectName, Player currentPlayer)
    {
        var inventory = GetObjectsInLocation(currentPlayer.Id);
        var foundObject = inventory.FirstOrDefault(obj =>
        {
            var gameObject = obj.GameObject as GameObject;
            if (gameObject == null) return false;
            var objName = GetObjectName(gameObject);
            return objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
        });
        
        return foundObject?.GameObject as GameObject;
    }
    
    public static dynamic? FindObjectById(string objectId)
    {
        if (string.IsNullOrEmpty(objectId)) return null;
        return ObjectManager.GetObject(objectId);
    }
    
    #endregion

    #region Verb Management

    /// <summary>
    /// Get all verbs on an object
    /// </summary>
    public static List<Verb> GetVerbsOnObject(string objectId)
    {
        return VerbManager.GetVerbsOnObject(objectId);
    }

    /// <summary>
    /// Get all verbs on an object (GameObject overload)
    /// </summary>
    public static List<Verb> GetVerbsOnObject(GameObject obj)
    {
        return GetVerbsOnObject(obj.Id);
    }

    /// <summary>
    /// Get all functions on an object
    /// </summary>
    public static List<Function> GetFunctionsOnObject(string objectId)
    {
        return FunctionResolver.GetFunctionsForObject(objectId, true);
    }

    /// <summary>
    /// Get all functions on an object (GameObject overload)
    /// </summary>
    public static List<Function> GetFunctionsOnObject(GameObject obj)
    {
        return GetFunctionsOnObject(obj.Id);
    }

    /// <summary>
    /// Find a specific function on an object (with inheritance)
    /// </summary>
    public static Function? FindFunction(string objectId, string functionName)
    {
        return FunctionResolver.FindFunction(objectId, functionName);
    }
    
    /// <summary>
    /// Find a specific function on an object (GameObject overload)
    /// </summary>
    public static Function? FindFunction(GameObject obj, string functionName)
    {
        return FindFunction(obj.Id, functionName);
    }
    
    #endregion
    
    #region Player Identification
    

    
    /// <summary>
    /// Check if an object represents a player and return the player (GameObject overload)
    /// </summary>
    public static dynamic? GetPlayerFromObject(GameObject obj)
    {
        var playerIdProperty = GetProperty(obj, "playerId");
        if (!string.IsNullOrEmpty(playerIdProperty))
        {
            return FindPlayerById(playerIdProperty);
        }
        return null;
    }

    
    /// <summary>
    /// Check if an object directly represents a player (GameObject overload)
    /// </summary>    
    public static bool IsPlayerObject(GameObject obj)
    {
                // Check if this objectId is actually a player ID
        var player = FindPlayerById(obj.Id);
        return player != null;
    }
    
    #endregion
    
    #region Messaging
    
    /// <summary>
    /// Send a message to a player
    /// </summary>
    public static void Notify(Player player, string message)
    {
        if (player?.SessionGuid != null && CurrentContext?.CommandProcessor != null)
        {
            CurrentContext.CommandProcessor.SendToPlayer(message, player.SessionGuid);
        }
    }
    
    /// <summary>
    /// Send a message to all players in a room
    /// </summary>
    public static void NotifyRoom(string roomId, string message, Player? excludePlayer = null)
    {
        var playersInRoom = GetObjectsInLocation(roomId);
        foreach (var obj in playersInRoom)
        {
            // Extract the GameObject from the dynamic wrapper
            var gameObject = obj.GameObject as GameObject;
            if (gameObject != null)
            {
                var player = GetPlayerFromObject(gameObject);
                if (player != null && (excludePlayer == null || player?.Id != excludePlayer.Id))
                {
                    // The script engine will handle the actual notification
                    // This is a placeholder for the interface
                }
            }
        }
    }
    
    #endregion
    
    #region Utility Functions
    

    
    /// <summary>
    /// Get a friendly display name for an object (GameObject overload)
    /// </summary>
    public static string GetDisplayName(GameObject obj)
    {
        var name = GetObjectName(obj);
        var shortDesc = GetObjectShortDesc(obj);
        
        if (!string.IsNullOrEmpty(shortDesc))
        {
            return $"{name} ({shortDesc})";
        }
        return name;
        //return GetDisplayName(obj.Id);
    }
    

    /// <summary>
    /// Check if an object is gettable (GameObject overload)
    /// </summary>
    public static bool IsGettable(GameObject obj)
    {
         return GetBoolProperty(obj, "gettable", false);
    }
    
    /// <summary>
    /// Join arguments into a single string starting from a specific index
    /// </summary>
    public static string JoinArgs(List<string> args, int startIndex = 0)
    {
        if (args == null || startIndex >= args.Count) return "";
        return string.Join(" ", args.Skip(startIndex));
    }
    

    
    /// <summary>
    /// Get the class of an object (GameObject overload)
    /// </summary>
    public static dynamic? GetObjectClass(GameObject obj)
    {
        if (obj != null && !string.IsNullOrEmpty(obj.ClassId))
        {
            return ObjectManager.GetClass(obj.ClassId);
        }
        return null;
    }

    /// <summary>
    /// Get current player from script context
    /// </summary>
    public static dynamic? GetCurrentPlayer()
    {
        // Check unified context first, then fall back to old context
        if (((Player?)UnifiedContext?.Player) != null)
        {
            // Convert GameObject to Database.Player
            var unifiedPlayer = (Player?)UnifiedContext.Player;
            if (unifiedPlayer != null)
                return unifiedPlayer;
            var id = unifiedPlayer?.Id;
            if (!string.IsNullOrEmpty(id))
                return ObjectManager.GetObject<Player>(id);
            return null;
        }
        return (Player?)CurrentContext?.Player;
    }

    /// <summary>
    /// Get players in a room
    /// </summary>
    public static List<dynamic> GetPlayersInRoom(string roomId)
    {
        if (roomId == null) return new List<dynamic>();
        
        return PlayerManager.GetOnlinePlayers()
            .Where(p => p.Location?.Id == roomId)
            .ToList<dynamic>();
    }
    
    /// <summary>
    /// Get players in a room
    /// </summary>
    public static List<dynamic> GetPlayersInRoom(GameObject room)
    {
        if (room is null) return new List<dynamic>();
        return PlayerManager.GetOnlinePlayers()
            .Where(p => p.Location?.Id == room.Id)
            .Cast<dynamic>()
            .ToList();
    }
    #endregion

    #region Room and Movement Helpers

    /// <summary>
    /// Simple room display - just return the room description
    /// </summary>
    public static string GetRoomDescription()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer?.Location == null) return "You are nowhere.";

        var room = ObjectManager.GetObject(currentPlayer.Location.Id);
        if (room == null) return "You are in a void.";

        var name = GetProperty(room, "name")?.AsString ?? "Unknown Room";
        var longDesc = GetProperty(room, "longDescription")?.AsString ?? "You see nothing special.";

        return $"=== {name} ===\n{longDesc}";
    }

    /// <summary>
    /// Display room information to current player
    /// </summary>
    public static void ShowRoom()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null) return;
        
        Notify(currentPlayer, GetRoomDescription());
        
        // Show exits
        if (currentPlayer.Location != null)
        {
            var exits = WorldManager.GetExits(currentPlayer.Location);
            if (exits.Any())
            {
                var exitNames = ((IEnumerable<GameObject>)exits).Select(e => GetProperty(e, "direction")?.AsString).Where(d => d != null);
                Notify(currentPlayer, $"Exits: {string.Join(", ", exitNames)}");
            }

            // Show objects
            var exitClassId = ObjectManager.GetClassByName("Exit")?.Id;
            var playerClassId = ObjectManager.GetClassByName("Player")?.Id;
            var objects = ((IEnumerable<dynamic>)GetObjectsInLocation(currentPlayer.Location.Id))
                .Where(obj => {
                    var gameObject = obj.GameObject as GameObject;
                    return gameObject != null && 
                           gameObject.ClassId != exitClassId &&
                           gameObject.ClassId != playerClassId;
                });

            foreach (var obj in objects)
            {
                var gameObject = obj.GameObject as GameObject;
                if (gameObject != null)
                {
                    var visible = GetProperty(gameObject, "visible")?.AsBoolean ?? true;
                    if (visible)
                    {
                        var shortDesc = GetProperty(gameObject, "shortDescription")?.AsString ?? "something";
                        Notify(currentPlayer, $"You see {shortDesc} here.");
                    }
                }
            }

            // Show other players
            var otherPlayers = ((IEnumerable<dynamic>)GetPlayersInRoom(currentPlayer.Location)).Where(p => p.Id != currentPlayer.Id);
            foreach (var otherPlayer in otherPlayers)
            {
                Notify(currentPlayer, $"{otherPlayer.Name} is here.");
            }
        }
    }

    /// <summary>
    /// Look at a specific object
    /// </summary>
    public static void LookAtObject(string target)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer?.Location == null) return;

        target = target.ToLower();
        var objects = GetObjectsInLocation(currentPlayer.Location.Id);
        
        var targetObject = ((IEnumerable<dynamic>)objects).FirstOrDefault(obj =>
        {
            var gameObject = obj.GameObject as GameObject;
            if (gameObject == null) return false;
            var name = GetProperty(gameObject, "name")?.AsString?.ToLower();
            var shortDesc = GetProperty(gameObject, "shortDescription")?.AsString?.ToLower();
            return name?.Contains(target) == true || shortDesc?.Contains(target) == true;
        });

        if (targetObject == null)
        {
            Notify(currentPlayer, "You don't see that here.");
            return;
        }

        var gameObj = targetObject.GameObject as GameObject;
        if (gameObj != null)
        {
            var longDesc = GetProperty(gameObj, "longDescription")?.AsString ?? "You see nothing special.";
            Notify(currentPlayer, longDesc);
        }
    }

    /// <summary>
    /// Find an item in the current room by name - returns dynamic wrapper
    /// </summary>
    public static dynamic? FindItemInRoom(string itemName)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer?.Location == null) return null;

        itemName = itemName.ToLower();
        var roomObjects = GetObjectsInLocation(currentPlayer.Location.Id);
        
        var foundObject = ((IEnumerable<GameObject>)roomObjects).FirstOrDefault(gameObject =>
        {
            if (gameObject == null) return false;
            var name = GetProperty(gameObject, "name")?.AsString?.ToLower();
            var shortDesc = GetProperty(gameObject, "shortDescription")?.AsString?.ToLower();
            return name?.Contains(itemName) == true || shortDesc?.Contains(itemName) == true;
        });
        
        return foundObject;
    }

    /// <summary>
    /// Find an item in the player's inventory by name - returns dynamic wrapper
    /// </summary>
    public static dynamic? FindItemInInventory(string itemName)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null) return null;

        itemName = itemName.ToLower();
        var playerGameObject = ObjectManager.GetObject(currentPlayer.Id);
        if (playerGameObject?.Contents == null) return null;

        var foundObject = ((IEnumerable<string>)playerGameObject.Contents)
            .Select(id => ObjectManager.GetObject(id))
            .FirstOrDefault(obj =>
            {
                if (obj == null) return false;
                var name = GetProperty(obj, "name")?.AsString?.ToLower();
                var shortDesc = GetProperty(obj, "shortDescription")?.AsString?.ToLower();
                return name?.Contains(itemName) == true || shortDesc?.Contains(itemName) == true;
            });
        return foundObject;
    }

    /// <summary>
    /// Move an object to a new location
    /// </summary>
    public static bool MoveObject(GameObject gameObj, GameObject destination)
    {
        try
        {
            ObjectManager.MoveObject(gameObj, destination);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to move object {gameObj.Id} to {destination.Id}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Send a message to all players in the current room
    /// </summary>
    public static void SayToRoom(string message, bool excludeSelf = true)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer?.Location == null) return;

        var playersInRoom = GetPlayersInRoom(currentPlayer.Location);
        foreach (var player in playersInRoom)
        {
            if (excludeSelf && player.Id == currentPlayer.Id) continue;
            var targetPlayer = ObjectManager.GetObject<Player>(player.Id);
            if (targetPlayer != null)
            {
                Notify(targetPlayer, message);
            }
        }
    }

    /// <summary>
    /// Get all exits from a room
    /// </summary>
    public static List<dynamic> GetExits(GameObject room)
    {
        return WorldManager.GetExits(room);
    }

    public static List<dynamic> GetContents(GameObject room)
    {
        return RoomManager.GetItems(room);
    }


    /// <summary>
    /// Show the player's inventory
    /// </summary>
    public static void ShowInventory()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null) return;

        var playerGameObject = ObjectManager.GetObject(currentPlayer.Id);
        if (playerGameObject?.Contents == null || !playerGameObject!.Contents.Any())
        {
            Notify(currentPlayer, "You are carrying nothing.");
            return;
        }

        Notify(currentPlayer, "You are carrying:");
        foreach (var itemId in playerGameObject!.Contents)
        {
            var item = ObjectManager.GetObject(itemId);
            if (item != null)
            {
                var name = GetProperty(item, "shortDescription") ?? "something";
                Notify(currentPlayer, $"  {name}");
            }
        }
    }

    /// <summary>
    /// Get information about a specific verb on an object
    /// </summary>
    public static VerbInfo? GetVerbInfo(string objectSpec, string verbName)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null) return null;
        
        var objectId = ResolveObject(objectSpec, currentPlayer);
        if (objectId == null)
        {
            return null;
        }

        var verb = ((IEnumerable<Verb>)VerbManager.GetVerbsOnObject(objectId))
            .FirstOrDefault(v => v.Name.ToLower() == verbName.ToLower());

        if (verb == null)
        {
            return null;
        }

        var obj = ObjectManager.GetObject(objectId);
        return new VerbInfo
        {
            ObjectId = objectId,
            ObjectName = obj != null ? GetObjectName(obj) : objectId,
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

    #region Lambda-Friendly Helper Methods

    /// <summary>
    /// Execute an action for each player matching a condition
    /// Usage: ForEachPlayer(p => p.IsOnline, p => notify(p, "Hello!"));
    /// </summary>
    public static void ForEachPlayer(Func<dynamic, bool> predicate, Action<dynamic> action)
    {
        var players = GetAllPlayers().Where(predicate);
        foreach (var player in players)
        {
            try
            {
                action(player);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error executing action on player {player.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Execute an action for each object matching a condition
    /// Usage: ForEachObject(obj => GetProperty(obj.Id, "type") == "weapon", obj => SetProperty(obj.Id, "sharpened", "true"));
    /// </summary>
    public static void ForEachObject(Func<dynamic, bool> predicate, Action<dynamic> action)
    {
        var objects = GetAllObjects().Where(predicate);
        foreach (var obj in objects)
        {
            try
            {
                action(obj);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error executing action on object {obj.Id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Find players matching a condition - returns a list for further processing
    /// Usage: var admins = FindPlayers(p => IsAdmin(p));
    /// </summary>
    public static List<dynamic> FindPlayers(Func<dynamic, bool> predicate)
    {
        return GetAllPlayers().Where(predicate).ToList();
    }

    /// <summary>
    /// Find objects matching a condition - returns a list for further processing
    /// Usage: var weapons = FindObjects(obj => GetProperty(obj.Id, "type") == "weapon");
    /// </summary>
    public static List<dynamic> FindObjects(Func<dynamic, bool> predicate)
    {
        return GetAllObjects().Where(predicate).ToList();
    }

    /// <summary>
    /// Find objects in a location matching a condition
    /// Usage: var redItems = FindObjectsInLocation(roomId, obj => GetProperty(obj.Id, "color") == "red");
    /// </summary>
    public static List<dynamic> FindObjectsInLocation(string locationId, Func<dynamic, bool> predicate)
    {
        return GetObjectsInLocation(locationId).Where(predicate).ToList();
    }

    /// <summary>
    /// Find objects in a location matching a condition (strongly-typed for GameObject)
    /// Usage: var redItems = FindObjectsInLocationTyped(roomId, obj => obj.color == "red");
    /// </summary>
    public static List<dynamic> FindObjectsInLocationTyped(string locationId, Func<GameObject, bool> predicate)
    {
        var objects = GetObjectsInLocation(locationId);
        return objects.Cast<GameObject>().Where(predicate).Cast<dynamic>().ToList();
    }

    /// <summary>
    /// Filter dynamic objects with strongly-typed predicate
    /// Usage: var filtered = FilterObjects(GetObjectsInLocation(roomId), obj => obj.visible == true);
    /// </summary>
    public static List<dynamic> FilterObjects(IEnumerable<dynamic> objects, Func<GameObject, bool> predicate)
    {
        return objects.Cast<GameObject>().Where(predicate).Cast<dynamic>().ToList();
    }

    /// <summary>
    /// Transform dynamic objects to another type
    /// Usage: var names = SelectFromObjects(GetObjectsInLocation(roomId), obj => obj.name);
    /// </summary>
    public static List<dynamic> SelectFromObjects<T>(IEnumerable<dynamic> objects, Func<GameObject, T> selector)
    {
        return objects.Cast<GameObject>().Select(selector).Cast<dynamic>().ToList();
    }

    /// <summary>
    /// Count players matching a condition
    /// Usage: var onlineCount = CountPlayers(p => p.IsOnline);
    /// </summary>
    public static int CountPlayers(Func<dynamic, bool> predicate)
    {
        return GetAllPlayers().Count(predicate);
    }

    /// <summary>
    /// Count objects matching a condition
    /// Usage: var weaponCount = CountObjects(obj => GetProperty(obj.Id, "type") == "weapon");
    /// </summary>
    public static int CountObjects(Func<dynamic, bool> predicate)
    {
        return GetAllObjects().Count(predicate);
    }

    /// <summary>
    /// Check if any player matches a condition
    /// Usage: var hasAdmin = AnyPlayer(p => IsAdmin(p));
    /// </summary>
    public static bool AnyPlayer(Func<dynamic, bool> predicate)
    {
        return GetAllPlayers().Any(predicate);
    }

    /// <summary>
    /// Check if any object matches a condition
    /// Usage: var hasWeapon = AnyObject(obj => GetProperty(obj.Id, "type") == "weapon");
    /// </summary>
    public static bool AnyObject(Func<dynamic, bool> predicate)
    {
        return GetAllObjects().Any(predicate);
    }

    /// <summary>
    /// Transform a list of objects using a lambda
    /// Usage: var names = Transform(GetObjectsInLocation(roomId), obj => obj.name);
    /// </summary>
    public static List<T> Transform<TSource, T>(IEnumerable<TSource> source, Func<TSource, T> selector)
    {
        return source.Select(selector).ToList();
    }

    #endregion

    #region Script Execution

    /// <summary>
    /// Execute C# script code with the same environment as verb/function execution
    /// </summary>
    public static string ExecuteScript(string scriptCode, Player player, Commands.CommandProcessor commandProcessor, string? thisObjectId = null, string? input = null)
    {
        try
        {
            var engine = new ScriptEngine();
            
            // Create a temporary verb structure for execution
            var tempVerb = new Verb
            {
                Name = "script",
                Code = scriptCode,
                ObjectId = player?.Id ?? "system"
            };

            if(player == null)
            {
                throw new ArgumentNullException(nameof(player), "Player cannot be null");
            }
            
            return engine.ExecuteVerb(tempVerb, input ?? "", player, commandProcessor, thisObjectId);
        }
        catch (Exception ex)
        {
            Logger.Error($"Script execution error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Execute C# script code with the same environment as verb/function execution (overload with variables)
    /// </summary>
    public static string ExecuteScript(string scriptCode, Player player, Commands.CommandProcessor commandProcessor, string? thisObjectId = null, string? input = null, Dictionary<string, string>? variables = null)
    {
        try
        {
            var engine = new ScriptEngine();
            
            // Create a temporary verb structure for execution
            var tempVerb = new Verb
            {
                Name = "script",
                Code = scriptCode,
                ObjectId = thisObjectId ?? "system"
            };
            
            return engine.ExecuteVerb(tempVerb, input ?? "", player, commandProcessor, thisObjectId, variables);
        }
        catch (Exception ex)
        {
            Logger.Error($"Script execution error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Execute C# script code with the same environment as verb/function execution (GameObject overload)
    /// </summary>
    public static string ExecuteScript(string scriptCode, GameObject player, Commands.CommandProcessor commandProcessor, GameObject thisObject, string? input = null)
    {
        try
        {
            // Look up the Database.Player from the GameObject player
            var dbPlayer = ObjectManager.GetObject<Player>(player.Id);
            if (dbPlayer == null)
            {
                throw new ArgumentException($"Player with ID '{player.Id}' not found in database");
            }
            // Use the object ID directly from GameObject
            var objectId = thisObject?.Id ?? "system";
            return ExecuteScript(scriptCode, dbPlayer, commandProcessor, objectId, input);
        }
        catch (Exception ex)
        {
            Logger.Error($"Script execution error: {ex.Message}");
            throw;
        }
    }

    #endregion

}



