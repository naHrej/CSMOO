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
    public static string GetProperty(string objectId, string propertyName)
    {
        var property = ObjectManager.GetProperty(objectId, propertyName) as BsonValue;
        return property?.AsString ?? "";
    }
    
    /// <summary>
    /// Get the string value of an object property with default
    /// </summary>
    public static string GetProperty(string objectId, string propertyName, string defaultValue)
    {
        var property = ObjectManager.GetProperty(objectId, propertyName) as BsonValue;
        return property?.AsString ?? defaultValue;
    }
    
    /// <summary>
    /// Get the boolean value of an object property
    /// </summary>
    public static bool GetBoolProperty(string objectId, string propertyName, bool defaultValue = false)
    {
        var property = ObjectManager.GetProperty(objectId, propertyName) as BsonValue;
        return property?.AsBoolean ?? defaultValue;
    }
    
    /// <summary>
    /// Set a property on an object
    /// </summary>
    public static void SetProperty(string objectId, string propertyName, string value)
    {
        var obj = FindObject(objectId);
        if (obj != null)
        {
            ObjectManager.SetProperty(obj, propertyName, value);
        }
    }
    
    /// <summary>
    /// Set a boolean property on an object
    /// </summary>
    public static void SetBoolProperty(string objectId, string propertyName, bool value)
    {
        var obj = FindObject(objectId);
        if (obj != null)
        {
            ObjectManager.SetProperty(obj, propertyName, value);
        }
    }
    
    /// <summary>
    /// Get all objects in a location
    /// </summary>
    public static List<GameObject> GetObjectsInLocation(string locationId)
    {
        return ObjectManager.GetObjectsInLocation(locationId);
    }
    
    /// <summary>
    /// Move an object to a new location
    /// </summary>
    public static bool MoveObject(string objectId, string newLocationId)
    {
        var obj = FindObject(objectId);
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
    public static string GetObjectName(string objectId)
    {
        return GetProperty(objectId, "name", "something");
    }
    
    /// <summary>
    /// Get the short description of an object
    /// </summary>
    public static string GetObjectShortDesc(string objectId)
    {
        return GetProperty(objectId, "shortDescription");
    }
    
    /// <summary>
    /// Get the long description of an object
    /// </summary>
    public static string GetObjectLongDesc(string objectId)
    {
        return GetProperty(objectId, "longDescription");
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
    public static Player FindPlayerByPartialName(string partialName)
    {
        return GameDatabase.Instance.Players.FindOne(p => 
            p.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Find a player by ID
    /// </summary>
    public static Player FindPlayerById(string playerId)
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
    
    #endregion
    
    #region Object Finding and Resolution
    
    /// <summary>
    /// Smart object resolution - finds players first, then objects by name
    /// </summary>
    public static string ResolveObject(string objectName, Player currentPlayer)
    {
        if (string.IsNullOrEmpty(objectName)) return null;
        
        // Handle special keywords
        switch (objectName.ToLower())
        {
            case "me":
                return currentPlayer.Id;
            case "here":
                return currentPlayer.Location;
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
                var objName = GetObjectName(obj.Id);
                return objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
            });
            
            if (foundObject != null)
            {
                return foundObject.Id;
            }
        }
        
        // Try player inventory
        var inventory = GetObjectsInLocation(currentPlayer.Id);
        var inventoryObject = inventory.FirstOrDefault(obj =>
        {
            var objName = GetObjectName(obj.Id);
            return objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
        });
        
        return inventoryObject?.Id;
    }
    
    /// <summary>
    /// Find an object by name in the current room
    /// </summary>
    public static GameObject FindObjectInRoom(string objectName, Player currentPlayer)
    {
        if (currentPlayer.Location == null) return null;
        
        var objectsInRoom = GetObjectsInLocation(currentPlayer.Location);
        return objectsInRoom.FirstOrDefault(obj =>
        {
            var objName = GetObjectName(obj.Id);
            return objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
        });
    }
    
    /// <summary>
    /// Find an object by name in player's inventory
    /// </summary>
    public static GameObject FindObjectInInventory(string objectName, Player currentPlayer)
    {
        var inventory = GetObjectsInLocation(currentPlayer.Id);
        return inventory.FirstOrDefault(obj =>
        {
            var objName = GetObjectName(obj.Id);
            return objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
        });
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
    
    #endregion
    
    #region Player Identification
    
    /// <summary>
    /// Check if an object represents a player and return the player
    /// </summary>
    public static Player GetPlayerFromObject(string objectId)
    {
        var playerIdProperty = GetProperty(objectId, "playerId");
        if (!string.IsNullOrEmpty(playerIdProperty))
        {
            return FindPlayerById(playerIdProperty);
        }
        return null;
    }
    
    /// <summary>
    /// Check if an object ID directly represents a player
    /// </summary>
    public static bool IsPlayerObject(string objectId)
    {
        // Check if this objectId is actually a player ID
        var player = FindPlayerById(objectId);
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
    public static void NotifyRoom(string roomId, string message, Player excludePlayer = null)
    {
        var playersInRoom = GetObjectsInLocation(roomId);
        foreach (var obj in playersInRoom)
        {
            var player = GetPlayerFromObject(obj.Id);
            if (player != null && (excludePlayer == null || player.Id != excludePlayer.Id))
            {
                // The script engine will handle the actual notification
                // This is a placeholder for the interface
            }
        }
    }
    
    #endregion
    
    #region Utility Functions
    
    /// <summary>
    /// Get a friendly display name for an object
    /// </summary>
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
    /// Check if an object is gettable
    /// </summary>
    public static bool IsGettable(string objectId)
    {
        return GetBoolProperty(objectId, "gettable", false);
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
    public static ObjectClass GetObjectClass(string objectId)
    {
        var obj = FindObject(objectId);
        if (obj != null && !string.IsNullOrEmpty(obj.ClassId))
        {
            return GameDatabase.Instance.ObjectClasses.FindById(obj.ClassId);
        }
        return null;
    }

    /// <summary>
    /// Get current player from script context
    /// </summary>
    public static Player GetCurrentPlayer()
    {
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
    /// Get property of an object by GameObject reference
    /// </summary>
    public static BsonValue? GetProperty(GameObject obj, string propertyName)
    {
        return ObjectManager.GetProperty(obj, propertyName);
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
                .Where(obj => obj.ClassId != GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Exit")?.Id)
                .Where(obj => obj.ClassId != GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Player")?.Id);

            foreach (var obj in objects)
            {
                var visible = GetProperty(obj, "visible")?.AsBoolean ?? true;
                if (visible)
                {
                    var shortDesc = GetProperty(obj, "shortDescription")?.AsString ?? "something";
                    Notify(currentPlayer, $"You see {shortDesc} here.");
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
            var name = GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            return name?.Contains(target) == true || shortDesc?.Contains(target) == true;
        });

        if (targetObject == null)
        {
            Notify(currentPlayer, "You don't see that here.");
            return;
        }

        var longDesc = GetProperty(targetObject, "longDescription")?.AsString ?? "You see nothing special.";
        Notify(currentPlayer, longDesc);
    }

    /// <summary>
    /// Find an item in the current room by name
    /// </summary>
    public static GameObject? FindItemInRoom(string itemName)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer?.Location == null) return null;

        itemName = itemName.ToLower();
        var roomObjects = GetObjectsInLocation(currentPlayer.Location);
        
        return roomObjects.FirstOrDefault(obj =>
        {
            var name = GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            return name?.Contains(itemName) == true || shortDesc?.Contains(itemName) == true;
        });
    }

    /// <summary>
    /// Find an item in the player's inventory by name
    /// </summary>
    public static GameObject? FindItemInInventory(string itemName)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null) return null;

        itemName = itemName.ToLower();
        var playerGameObject = GameDatabase.Instance.GameObjects.FindById(currentPlayer.Id);
        if (playerGameObject?.Contents == null) return null;

        return playerGameObject.Contents
            .Select(id => GameDatabase.Instance.GameObjects.FindById(id))
            .FirstOrDefault(obj =>
            {
                if (obj == null) return false;
                var name = GetProperty(obj, "name")?.AsString?.ToLower();
                var shortDesc = GetProperty(obj, "shortDescription")?.AsString?.ToLower();
                return name?.Contains(itemName) == true || shortDesc?.Contains(itemName) == true;
            });
    }

    /// <summary>
    /// Move an object to a new location
    /// </summary>
    public static bool MoveObject(GameObject gameObj, GameObject destination)
    {
        try
        {
            ObjectManager.MoveObject(gameObj.Id, destination.Id);
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
    public static List<GameObject> GetExitsFromRoom(string roomId)
    {
        return WorldManager.GetExitsFromRoom(roomId);
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
                var name = GetProperty(item.Id, "shortDescription") ?? "something";
                Notify(currentPlayer, $"  {name}");
            }
        }
    }

    /// <summary>
    /// Get information about a specific verb on an object
    /// </summary>
    public static VerbInfo? GetVerbInfo(string objectSpec, string verbName)
    {
        var objectId = ResolveObject(objectSpec, GetCurrentPlayer());
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
