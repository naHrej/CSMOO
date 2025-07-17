using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LiteDB;

namespace CSMOO.Server.Database;

/// <summary>
/// Manages player accounts and their connection to sessions
/// </summary>
public static class PlayerManager
{
    /// <summary>
    /// Creates a new player account
    /// </summary>
    public static Player CreatePlayer(string name, string password, string? startingRoomId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Player name cannot be empty");

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty");

        // Check if player name already exists
        var existingPlayer = GameDatabase.Instance.Players.FindOne(p => p.Name.ToLower() == name.ToLower());
        if (existingPlayer != null)
            throw new InvalidOperationException($"Player name '{name}' already exists");

        // Find or create the Player class
        var playerClass = GetOrCreatePlayerClass();

        // Create the player as an instance of the Player class
        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            ClassId = playerClass.Id,
            Name = name,
            PasswordHash = HashPassword(password),
            Location = startingRoomId,
            Properties = new BsonDocument()
        };

        // Set default player properties from the class
        var inheritanceChain = ObjectManager.GetInheritanceChain(playerClass.Id);
        foreach (var classInChain in inheritanceChain)
        {
            foreach (var property in classInChain.Properties)
            {
                if (!player.Properties.ContainsKey(property.Key))
                {
                    player.Properties[property.Key] = property.Value;
                }
            }
        }

        GameDatabase.Instance.Players.Insert(player);

        // Also insert the player as a GameObject so it can be moved and managed by ObjectManager
        var playerGameObject = new GameObject
        {
            Id = player.Id,
            ClassId = player.ClassId,
            Properties = player.Properties,
            Location = startingRoomId,
            Contents = new List<string>(),
            CreatedAt = player.CreatedAt,
            ModifiedAt = player.ModifiedAt
        };
        GameDatabase.Instance.GameObjects.Insert(playerGameObject);

        // Add to starting room if specified
        if (startingRoomId != null)
        {
            ObjectManager.MoveObject(player.Id, startingRoomId);
        }

        return player;
    }

    /// <summary>
    /// Authenticates a player login
    /// </summary>
    public static Player? AuthenticatePlayer(string name, string password)
    {
        var player = GameDatabase.Instance.Players.FindOne(p => p.Name.ToLower() == name.ToLower());
        if (player == null)
            return null;

        return VerifyPassword(password, player.PasswordHash) ? player : null;
    }

    /// <summary>
    /// Connects a player to a session
    /// </summary>
    public static void ConnectPlayerToSession(string playerId, Guid sessionGuid)
    {
        var player = GameDatabase.Instance.Players.FindById(playerId);
        if (player == null)
            throw new ArgumentException($"Player with ID {playerId} not found");

        // Disconnect any existing session for this player
        if (player.SessionGuid.HasValue)
        {
            DisconnectPlayer(playerId);
        }

        player.SessionGuid = sessionGuid;
        player.IsOnline = true;
        player.LastLogin = DateTime.UtcNow;
        player.ModifiedAt = DateTime.UtcNow;

        GameDatabase.Instance.Players.Update(player);
    }

    /// <summary>
    /// Disconnects a player from their session
    /// </summary>
    public static void DisconnectPlayer(string playerId)
    {
        var player = GameDatabase.Instance.Players.FindById(playerId);
        if (player == null)
            return;

        player.SessionGuid = null;
        player.IsOnline = false;
        player.ModifiedAt = DateTime.UtcNow;

        GameDatabase.Instance.Players.Update(player);
    }

    /// <summary>
    /// Finds a player by session GUID
    /// </summary>
    public static Player? GetPlayerBySession(Guid sessionGuid)
    {
        return GameDatabase.Instance.Players.FindOne(p => p.SessionGuid == sessionGuid);
    }

    /// <summary>
    /// Gets all online players
    /// </summary>
    public static System.Collections.Generic.List<Player> GetOnlinePlayers()
    {
        return GameDatabase.Instance.Players.Find(p => p.IsOnline).ToList();
    }

    /// <summary>
    /// Find a player by name (case-insensitive)
    /// </summary>
    public static Player? FindPlayerByName(string name)
    {
        return GameDatabase.Instance.Players.FindOne(p => p.Name.ToLower() == name.ToLower());
    }

    /// <summary>
    /// Changes a player's password
    /// </summary>
    public static void ChangePassword(string playerId, string newPassword)
    {
        var player = GameDatabase.Instance.Players.FindById(playerId);
        if (player == null)
            throw new ArgumentException($"Player with ID {playerId} not found");

        player.PasswordHash = HashPassword(newPassword);
        player.ModifiedAt = DateTime.UtcNow;

        GameDatabase.Instance.Players.Update(player);
    }

    private static ObjectClass GetOrCreatePlayerClass()
    {
        var playerClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Player");
        if (playerClass != null)
            return playerClass;

        // Create the base Player class if it doesn't exist
        playerClass = new ObjectClass
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Player",
            Description = "Base class for all player characters",
            Properties = new BsonDocument
            {
                ["maxHealth"] = 100,
                ["health"] = 100,
                ["level"] = 1,
                ["experience"] = 0,
                ["description"] = "A player character."
            }
        };

        GameDatabase.Instance.ObjectClasses.Insert(playerClass);
        return playerClass;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "CSMOO_SALT"));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        var computedHash = HashPassword(password);
        return computedHash == hash;
    }
}
