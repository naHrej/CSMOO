using System.Security.Cryptography;
using System.Text;
using CSMOO.Database;
using LiteDB;

namespace CSMOO.Object;

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
        var existingPlayer = FindPlayerByName(name);
        if (existingPlayer != null)
            throw new InvalidOperationException($"Player name '{name}' already exists");

        // Find or create the Player class
        var playerClass = GetOrCreatePlayerClass();

        // Create the player as an instance of the Player class
        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            PasswordHash = HashPassword(password),
            Location = DbProvider.Instance.FindOne<GameObject>("gameobjects", o => o.Properties.ContainsKey("isStartingRoom")),
            Properties = new BsonDocument
            {
                ["name"] = name,
                ["location"] = startingRoomId,
                ["isonline"] = false,
                ["lastlogin"] = DateTime.UtcNow,
                ["permissions"] = new BsonArray(),
                ["createdAt"] = DateTime.UtcNow,
                ["modifiedAt"] = DateTime.UtcNow,
                ["classid"] = playerClass.Id,
            }
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

        DbProvider.Instance.Insert("players", player);

        // Also insert the player as a GameObject so it can be moved and managed by ObjectManager
        var playerGameObject = new GameObject
        {
            Id = player.Id,
            Properties = player.Properties,
            Location = DbProvider.Instance.FindOne<GameObject>("gameobjects", o => o.Properties.ContainsKey("isStartingRoom")),
            Contents = new List<string>(),
            CreatedAt = player.CreatedAt,
            ModifiedAt = player.ModifiedAt

        };
        DbProvider.Instance.Insert("gameobjects", playerGameObject);

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
        Player? player = FindPlayerByName(name);
        if (player == null)
            return null;
        return VerifyPassword(password, player.PasswordHash) ? player : null;
    }

    /// <summary>
    /// Connects a player to a session
    /// </summary>
    public static void ConnectPlayerToSession(string playerId, Guid sessionGuid)
    {
        var player = ObjectManager.GetObject<Player>( playerId);
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

        DbProvider.Instance.Update("players", player);
    }

    /// <summary>
    /// Disconnects a player from their session
    /// </summary>
    public static void DisconnectPlayer(string playerId)
    {
        var player = ObjectManager.GetObject<Player>(playerId);
        if (player == null)
            return;

        player.SessionGuid = null;
        player.IsOnline = false;
        player.ModifiedAt = DateTime.UtcNow;

        DbProvider.Instance.Update("players", player);
    }

    /// <summary>
    /// Finds a player by session GUID
    /// </summary>
    public static Player? GetPlayerBySession(Guid sessionGuid)
    {
        // First try to find in ObjectManager cache
        var allCachedObjects = ObjectManager.GetAllObjects();
        var cachedPlayer = allCachedObjects.OfType<Player>().FirstOrDefault(p => p.SessionGuid == sessionGuid);
        if (cachedPlayer != null)
        {
            return cachedPlayer;
        }

        // Fallback to database query and cache the result
        var player = DbProvider.Instance.FindOne<Player>("players", p => p.SessionGuid == sessionGuid);
        if (player != null)
        {
            // Cache the player object for future use
            ObjectManager.CacheGameObject(player);
        }
        return player;
    }

    /// <summary>
    /// Gets all online players
    /// </summary>
    public static System.Collections.Generic.List<Player> GetOnlinePlayers()
    {
        // First try to get from ObjectManager cache
        var allCachedObjects = ObjectManager.GetAllObjects();
        var cachedOnlinePlayers = allCachedObjects.OfType<Player>().Where(p => p.IsOnline).ToList();
        
        // Also check database for any players not in cache
        var dbOnlinePlayers = DbProvider.Instance.Find<Player>("players", p => p.IsOnline).ToList();
        
        // Cache any players from database that aren't already cached
        foreach (var player in dbOnlinePlayers)
        {
            if (!cachedOnlinePlayers.Any(cp => cp.Id == player.Id))
            {
                ObjectManager.CacheGameObject(player);
                cachedOnlinePlayers.Add(player);
            }
        }
        
        return cachedOnlinePlayers;
    }

    /// <summary>
    /// Gets all players (both online and offline)
    /// </summary>
    public static System.Collections.Generic.List<Player> GetAllPlayers()
    {
        // First try to get from ObjectManager cache
        var allCachedObjects = ObjectManager.GetAllObjects();
        var cachedPlayers = allCachedObjects.OfType<Player>().ToList();
        
        // Also check database for any players not in cache
        var dbPlayers = DbProvider.Instance.FindAll<Player>("players").ToList();
        
        // Cache any players from database that aren't already cached
        foreach (var player in dbPlayers)
        {
            if (!cachedPlayers.Any(cp => cp.Id == player.Id))
            {
                ObjectManager.CacheGameObject(player);
                cachedPlayers.Add(player);
            }
        }
        
        return cachedPlayers;
    }

    /// <summary>
    /// Find a player by name (case-insensitive)
    /// </summary>
    public static Player? FindPlayerByName(string name)
    {
        // First try to find in ObjectManager cache
        var allCachedObjects = ObjectManager.GetAllObjects();
        var cachedPlayer = allCachedObjects.OfType<Player>().FirstOrDefault(p => p.Name.ToLower() == name.ToLower());
        if (cachedPlayer != null)
        {
            return cachedPlayer;
        }

        // Fallback to database query and cache the result
        var player = DbProvider.Instance.FindOne<Player>("players", p => p.Name.ToLower() == name.ToLower());
        if (player != null)
        {
            // Cache the player object for future use
            ObjectManager.CacheGameObject(player);
        }
        return player;
    }

    /// <summary>
    /// Changes a player's password
    /// </summary>
    public static void ChangePassword(string playerId, string newPassword)
    {
        var player = ObjectManager.GetObject<Player>(playerId);
        if (player == null)
            throw new ArgumentException($"Player with ID {playerId} not found");
  

        player.PasswordHash = HashPassword(newPassword);
        player.ModifiedAt = DateTime.UtcNow;

        DbProvider.Instance.Update("players", player);
    }

    private static ObjectClass GetOrCreatePlayerClass()
    {
        var playerClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Player");
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

        DbProvider.Instance.Insert("objectclasses", playerClass);
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



