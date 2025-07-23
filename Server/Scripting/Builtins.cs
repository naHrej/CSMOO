using System;
using System.Collections.Generic;
using System.Linq;
using CSMOO.Server.Database;
using CSMOO.Server.Database.Models;
using CSMOO.Server.Scripting;
using CSMOO.Server.Logging;
using LiteDB;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Built-in functions for verb scripts - provides clean, consistent API without casting or long namespaces
/// </summary>
public static class Builtins
{
    /// <summary>
    /// Current script context - set by the script engine before execution
    /// </summary>
    public static VerbScriptGlobals? CurrentContext { get; set; }
    
    /// <summary>
    /// Unified script context - set by the UnifiedScriptEngine before execution
    /// </summary>
    public static UnifiedScriptGlobals? UnifiedContext { get; set; }

    #region Object Management

    /// <summary>
    /// Find a game object by its ID
    /// </summary>
    public static GameObject FindObject(string objectId)
    {
        return GameDatabase.Instance.GameObjects.FindById(objectId);
    }
    
    /// <summary>
    /// Get the string value of an object property
    /// </summary>
    [Obsolete("Use GetProperty(GameObject, string) instead")]
    public static BsonValue? GetProperty(string objectId, string propertyName)
    {
        return ObjectManager.GetProperty(objectId, propertyName);
    }
    
    /// <summary>
    /// Get the string value of an object property (GameObject overload)
    /// </summary>
    public static BsonValue? GetProperty(GameObject obj, string propertyName)
    {
        return ObjectManager.GetProperty(obj, propertyName);
    }

    /// <summary>
    /// Get the string value of an object property with default
    /// </summary>
    [Obsolete("Use GetProperty(GameObject, string, string) instead")]
    public static string GetProperty(string objectId, string propertyName, string defaultValue = "")
    {
        var property = ObjectManager.GetProperty(objectId, propertyName) as BsonValue;
        return property?.AsString ?? defaultValue;
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
    /// Get the boolean value of an object property
    /// </summary>
    [Obsolete("Use GetBoolProperty(GameObject, string, bool) instead")]
    public static bool GetBoolProperty(string objectId, string propertyName, bool defaultValue = false)
    {
        var property = ObjectManager.GetProperty(objectId, propertyName) as BsonValue;
        return property?.AsBoolean ?? defaultValue;
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
    /// Set a property on an object
    /// </summary>
    [Obsolete("Use SetProperty(GameObject, string, BsonValue) instead")]
    public static void SetProperty(string objectId, string propertyName, string value)
    {
        var obj = FindObject(objectId);
        SetProperty(obj, propertyName, value);
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
    /// Set a boolean property on an object
    /// </summary>
    [Obsolete("Use SetBoolProperty(GameObject, string, bool) instead")]
    public static void SetBoolProperty(string objectId, string propertyName, bool value)
    {
        var obj = FindObject(objectId);
        SetBoolProperty(obj, propertyName, value);
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
    /// Move an object to a new location (String id overload)
    /// </summary>
    [Obsolete("Use MoveObject(GameObject, string) instead")]
    public static bool MoveObject(string objectId, string newLocationId)
    {
        var obj = FindObject(objectId);
        return MoveObject(obj, newLocationId);
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
    /// Get the name of an object
    /// </summary>
    [Obsolete("Use GetObjectName(GameObject) instead")]
    public static string GetObjectName(string objectId)
    {
        return GetProperty(objectId, "name", "something");
    }
    
    /// <summary>
    /// Get the name of an object (GameObject overload)
    /// </summary>
    public static string GetObjectName(GameObject obj)
    {
        return GetObjectName(obj);
    }
    
    /// <summary>
    /// Get the short description of an object
    /// </summary>
    [Obsolete("Use GetObjectShortDesc(GameObject) instead")]
    public static string GetObjectShortDesc(string objectId)
    {
        return GetProperty(objectId, "shortDescription");
    }
    
    /// <summary>
    /// Get the short description of an object (GameObject overload)
    /// </summary>
    public static string GetObjectShortDesc(GameObject obj)
    {
        return GetObjectShortDesc(obj);
    }
    
    /// <summary>
    /// Get the long description of an object
    /// </summary>
    [Obsolete("Use GetObjectLongDesc(GameObject) instead")]
    public static string GetObjectLongDesc(string objectId)
    {
        return GetProperty(objectId, "longDescription");
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
    public static Player FindPlayer(string playerName)
    {
        return GameDatabase.Instance.Players.FindOne(p =>
            p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Find a player by partial name match
    /// </summary>
    public static Player? FindPlayerByPartialName(string partialName)
    {
        return GameDatabase.Instance.Players.FindOne(p => 
            p.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Find a player by ID
    /// </summary>
    public static Player? FindPlayerById(string playerId)
    {
        return GameDatabase.Instance.Players.FindById(playerId);
    }
    
    /// <summary>
    /// Get all online players
    /// </summary>
    public static List<Player> GetOnlinePlayers()
    {
        return PlayerManager.GetOnlinePlayers();
    }
    
    /// <summary>
    /// Get all players (online and offline) - useful for lambda filtering
    /// </summary>
    public static List<Player> GetAllPlayers()
    {
        return GameDatabase.Instance.Players.FindAll().ToList();
    }
    
    /// <summary>
    /// Get all game objects - useful for lambda filtering and searching
    /// </summary>
    public static List<GameObject> GetAllObjects()
    {
        return GameDatabase.Instance.GameObjects.FindAll().ToList();
    }
    
    /// <summary>
    /// Get all object classes - useful for lambda filtering
    /// </summary>
    public static List<ObjectClass> GetAllObjectClasses()
    {
        return GameDatabase.Instance.ObjectClasses.FindAll().ToList();
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
    /// Get all flags for a player as a list of strings
    /// </summary>
    public static List<string> GetPlayerFlags(Player player)
    {
        return PermissionManager.GetPlayerFlags(player).Select(f => f.ToString()).ToList();
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
            var dbPlayer = GameDatabase.Instance.Players.FindById(gameObject.Id);
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
            var dbPlayer = GameDatabase.Instance.Players.FindById((string)player.Id);
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
            var dbPlayer = GameDatabase.Instance.Players.FindById(gameObject.Id);
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
            var dbPlayer = GameDatabase.Instance.Players.FindById((string)player.Id);
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
            var dbPlayer = GameDatabase.Instance.Players.FindById(gameObject.Id);
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
            var dbPlayer = GameDatabase.Instance.Players.FindById((string)player.Id);
            return dbPlayer != null && PermissionManager.HasFlag(dbPlayer, PermissionManager.Flag.Programmer);
        }
        
        return false;
    }
    
    /// <summary>
    /// Get all flags for a player as a list of strings (dynamic overload for UnifiedScriptEngine)
    /// </summary>
    public static List<string> GetPlayerFlags(dynamic? player)
    {
        if (player == null) return new List<string>();
        
        // Handle GameObject wrapper
        if (player is GameObject gameObject)
        {
            var dbPlayer = GameDatabase.Instance.Players.FindById(gameObject.Id);
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
            var dbPlayer = GameDatabase.Instance.Players.FindById((string)player.Id);
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
                return currentPlayer.Location;
            case "system":
                // Find the system object
                var allObjects = GameDatabase.Instance.GameObjects.FindAll();
                var systemObj = allObjects.FirstOrDefault(obj =>
                    (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
                    (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
                return systemObj?.Id;
        }
        
        // Check if it's a DBREF (starts with # followed by digits)
        if (objectName.StartsWith("#") && int.TryParse(objectName.Substring(1), out int dbref))
        {
            var obj = GameDatabase.Instance.GameObjects.FindOne(o => o.DbRef == dbref);
            return obj?.Id;
        }

        // Check if it's a class reference (starts with "class:" or ends with ".class")
        if (objectName.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectName.Substring(6); // Remove "class:" prefix
            var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c =>
                c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return objectClass?.Id;
        }
        
        if (objectName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectName.Substring(0, objectName.Length - 6); // Remove ".class" suffix
            var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return objectClass?.Id;
        }

        // Check if it's a direct class ID (like "obj_room", "obj_exit", etc.)
        var classById = GameDatabase.Instance.ObjectClasses.FindById(objectName);
        if (classById != null)
        {
            return classById.Id;
        }
        
        // Try to find a player first
        var player = FindPlayerByPartialName(objectName);
        if (player != null)
        {
            return player.Id;
        }
        
        // Try to find object by name in current location
        if (currentPlayer.Location != null)
        {
            var objectsInRoom = GetObjectsInLocation(currentPlayer.Location);
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
        var directClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
            c.Name.Equals(objectName, StringComparison.OrdinalIgnoreCase));
        
        if (directClass != null)
        {
            return directClass.Id;
        }

        // Finally, search globally for any object with a matching name
        var globalObjects = GameDatabase.Instance.GameObjects.FindAll();
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
    public static GameObject? FindObjectInRoom(string objectName, Player currentPlayer)
    {
        if (currentPlayer.Location == null) return null;
        
        var objectsInRoom = GetObjectsInLocation(currentPlayer.Location);
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
    public static GameObject? FindObjectInInventory(string objectName, Player currentPlayer)
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
    /// Check if an object represents a player and return the player
    /// </summary>
    [Obsolete("Use Builtins.GetPlayerFromObject(GameObject) instead")]
    public static Player? GetPlayerFromObject(string objectId)
    {
        var playerIdProperty = GetProperty(objectId, "playerId");
        if (!string.IsNullOrEmpty(playerIdProperty))
        {
            return FindPlayerById(playerIdProperty);
        }
        return null;
    }
    
    /// <summary>
    /// Check if an object represents a player and return the player (GameObject overload)
    /// </summary>
    public static Player? GetPlayerFromObject(GameObject obj)
    {
        var playerIdProperty = GetProperty(obj, "playerId");
        if (!string.IsNullOrEmpty(playerIdProperty))
        {
            return FindPlayerById(playerIdProperty);
        }
        return null;
    }

    /// <summary>
    /// Check if an object ID directly represents a player
    /// </summary>
    [Obsolete("Use Builtins.IsPlayerObject(GameObject) instead")]
    public static bool IsPlayerObject(string objectId)
    {
        // Check if this objectId is actually a player ID
        var player = FindPlayerById(objectId);
        return player != null;
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
                if (player != null && (excludePlayer == null || player.Id != excludePlayer.Id))
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
    /// Get a friendly display name for an object
    /// </summary>
    [Obsolete("Use Builtins.GetDisplayName(GameObject) instead")]
    public static string GetDisplayName(string objectId)
    {
        var name = GetObjectName(objectId);
        var shortDesc = GetObjectShortDesc(objectId);
        
        if (!string.IsNullOrEmpty(shortDesc))
        {
            return $"{name} ({shortDesc})";
        }
        return name;
    }
    
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
    /// Check if an object is gettable
    /// </summary>
    [Obsolete("Use Builtins.IsGettable(GameObject) instead")]
    public static bool IsGettable(string objectId)
    {
        return GetBoolProperty(objectId, "gettable", false);
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
    /// Get the class of an object
    /// </summary>
    [Obsolete("Use Builtins.GetObjectClass(GameObject) instead")]
    public static ObjectClass? GetObjectClass(string objectId)
    {
        var obj = FindObject(objectId);
        if (obj != null && !string.IsNullOrEmpty(obj.ClassId))
        {
            return GameDatabase.Instance.ObjectClasses.FindById(obj.ClassId);
        }
        return null;
    }
    
    /// <summary>
    /// Get the class of an object (GameObject overload)
    /// </summary>
    public static ObjectClass? GetObjectClass(GameObject obj)
    {
        if (obj != null && !string.IsNullOrEmpty(obj.ClassId))
        {
            return GameDatabase.Instance.ObjectClasses.FindById(obj.ClassId);
        }
        return null;
    }

    /// <summary>
    /// Get current player from script context
    /// </summary>
    public static Player? GetCurrentPlayer()
    {
        // Check unified context first, then fall back to old context
        if (UnifiedContext?.Player != null)
        {
            // Convert GameObject to Database.Player
            return UnifiedContext.Player as Database.Player ?? 
                   GameDatabase.Instance.Players.FindById(UnifiedContext.Player.Id);
        }
        
        return CurrentContext?.Player;
    }

    /// <summary>
    /// Get players in a room
    /// </summary>
    public static List<Player> GetPlayersInRoom(string roomId)
    {
        if (roomId == null) return new List<Player>();
        
        return PlayerManager.GetOnlinePlayers()
            .Where(p => p.Location == roomId)
            .ToList();
    }
    
    /// <summary>
    /// Get players in a room
    /// </summary>
    public static List<Player> GetPlayersInRoom(GameObject room)
    {
        if (room is null) return new List<Player>();
        return PlayerManager.GetOnlinePlayers()
            .Where(p => p.Location == room.Id)
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

        var room = GameDatabase.Instance.GameObjects.FindById(currentPlayer.Location);
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
            var exits = WorldManager.GetExitsFromRoom(currentPlayer.Location);
            if (exits.Any())
            {
                var exitNames = exits.Select(e => GetProperty(e, "direction")?.AsString).Where(d => d != null);
                Notify(currentPlayer, $"Exits: {string.Join(", ", exitNames)}");
            }

            // Show objects
            var objects = GetObjectsInLocation(currentPlayer.Location)
                .Where(obj => {
                    var gameObject = obj.GameObject as GameObject;
                    return gameObject != null && 
                           gameObject.ClassId != GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Exit")?.Id &&
                           gameObject.ClassId != GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Player")?.Id;
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
            var otherPlayers = GetPlayersInRoom(currentPlayer.Location).Where(p => p.Id != currentPlayer.Id);
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
        var objects = GetObjectsInLocation(currentPlayer.Location);
        
        var targetObject = objects.FirstOrDefault(obj =>
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
        var roomObjects = GetObjectsInLocation(currentPlayer.Location);
        
        var foundObject = roomObjects.FirstOrDefault(obj =>
        {
            var gameObject = obj as GameObject;
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
        var playerGameObject = GameDatabase.Instance.GameObjects.FindById(currentPlayer.Id);
        if (playerGameObject?.Contents == null) return null;

        var foundObject = playerGameObject.Contents
            .Select(id => GameDatabase.Instance.GameObjects.FindById(id))
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
            var targetPlayer = GameDatabase.Instance.Players.FindById(player.Id);
            if (targetPlayer != null)
            {
                Notify(targetPlayer, message);
            }
        }
    }

    /// <summary>
    /// Get all exits from a room
    /// </summary>
    [Obsolete("Use Builtins.GetExitsFromRoom(GameObject) instead")]
    public static List<GameObject> GetExitsFromRoom(string roomId)
    {
        return WorldManager.GetExitsFromRoom(roomId);
    }
    /// <summary>
    /// Get all exits from a room
    /// </summary>
    public static List<GameObject> GetExitsFromRoom(GameObject room)
    {
        return WorldManager.GetExitsFromRoom(room);
    }

    /// <summary>
    /// Show the player's inventory
    /// </summary>
    public static void ShowInventory()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null) return;

        var playerGameObject = GameDatabase.Instance.GameObjects.FindById(currentPlayer.Id);
        if (playerGameObject?.Contents == null || !playerGameObject.Contents.Any())
        {
            Notify(currentPlayer, "You are carrying nothing.");
            return;
        }

        Notify(currentPlayer, "You are carrying:");
        foreach (var itemId in playerGameObject.Contents)
        {
            var item = GameDatabase.Instance.GameObjects.FindById(itemId);
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

    #region Lambda-Friendly Helper Methods

    /// <summary>
    /// Execute an action for each player matching a condition
    /// Usage: ForEachPlayer(p => p.IsOnline, p => notify(p, "Hello!"));
    /// </summary>
    public static void ForEachPlayer(Func<Player, bool> predicate, Action<Player> action)
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
    public static void ForEachObject(Func<GameObject, bool> predicate, Action<GameObject> action)
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
    public static List<Player> FindPlayers(Func<Player, bool> predicate)
    {
        return GetAllPlayers().Where(predicate).ToList();
    }

    /// <summary>
    /// Find objects matching a condition - returns a list for further processing
    /// Usage: var weapons = FindObjects(obj => GetProperty(obj.Id, "type") == "weapon");
    /// </summary>
    public static List<GameObject> FindObjects(Func<GameObject, bool> predicate)
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
    public static List<GameObject> FindObjectsInLocationTyped(string locationId, Func<GameObject, bool> predicate)
    {
        var objects = GetObjectsInLocation(locationId);
        return objects.Cast<GameObject>().Where(predicate).ToList();
    }

    /// <summary>
    /// Filter dynamic objects with strongly-typed predicate
    /// Usage: var filtered = FilterObjects(GetObjectsInLocation(roomId), obj => obj.visible == true);
    /// </summary>
    public static List<GameObject> FilterObjects(IEnumerable<dynamic> objects, Func<GameObject, bool> predicate)
    {
        return objects.Cast<GameObject>().Where(predicate).ToList();
    }

    /// <summary>
    /// Transform dynamic objects to another type
    /// Usage: var names = SelectFromObjects(GetObjectsInLocation(roomId), obj => obj.name);
    /// </summary>
    public static List<T> SelectFromObjects<T>(IEnumerable<dynamic> objects, Func<GameObject, T> selector)
    {
        return objects.Cast<GameObject>().Select(selector).ToList();
    }

    /// <summary>
    /// Count players matching a condition
    /// Usage: var onlineCount = CountPlayers(p => p.IsOnline);
    /// </summary>
    public static int CountPlayers(Func<Player, bool> predicate)
    {
        return GetAllPlayers().Count(predicate);
    }

    /// <summary>
    /// Count objects matching a condition
    /// Usage: var weaponCount = CountObjects(obj => GetProperty(obj.Id, "type") == "weapon");
    /// </summary>
    public static int CountObjects(Func<GameObject, bool> predicate)
    {
        return GetAllObjects().Count(predicate);
    }

    /// <summary>
    /// Check if any player matches a condition
    /// Usage: var hasAdmin = AnyPlayer(p => IsAdmin(p));
    /// </summary>
    public static bool AnyPlayer(Func<Player, bool> predicate)
    {
        return GetAllPlayers().Any(predicate);
    }

    /// <summary>
    /// Check if any object matches a condition
    /// Usage: var hasWeapon = AnyObject(obj => GetProperty(obj.Id, "type") == "weapon");
    /// </summary>
    public static bool AnyObject(Func<GameObject, bool> predicate)
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
    public static string ExecuteScript(string scriptCode, Database.Player player, Commands.CommandProcessor commandProcessor, string? thisObjectId = null, string? input = null)
    {
        try
        {
            var engine = new UnifiedScriptEngine();
            
            // Create a temporary verb structure for execution
            var tempVerb = new Verb
            {
                Name = "script",
                Code = scriptCode,
                ObjectId = thisObjectId ?? "system"
            };
            
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
    public static string ExecuteScript(string scriptCode, Database.Player player, Commands.CommandProcessor commandProcessor, string? thisObjectId = null, string? input = null, Dictionary<string, string>? variables = null)
    {
        try
        {
            var engine = new UnifiedScriptEngine();
            
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
            var dbPlayer = GameDatabase.Instance.Players.FindById(player.Id);
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
