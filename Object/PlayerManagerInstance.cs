using System.Security.Cryptography;
using System.Text;
using CSMOO.Database;
using LiteDB;

namespace CSMOO.Object;

/// <summary>
/// Instance-based player manager implementation for dependency injection
/// </summary>
public class PlayerManagerInstance : IPlayerManager
{
    private readonly IDbProvider _dbProvider;
    private IObjectManager? _objectManager;
    
    public PlayerManagerInstance(IDbProvider dbProvider)
    {
        _dbProvider = dbProvider;
    }
    
    /// <summary>
    /// Sets the object manager (used to resolve circular dependency)
    /// </summary>
    public void SetObjectManager(IObjectManager objectManager)
    {
        _objectManager = objectManager;
    }
    
    
    /// <summary>
    /// Creates a new player account
    /// </summary>
    public Player CreatePlayer(string name, string password, string? startingRoomId = null)
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
        var passwordHash = HashPassword(password);
        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            PasswordHash = passwordHash,
            Location = _dbProvider.FindOne<GameObject>("gameobjects", o => o.Properties.ContainsKey("isStartingRoom")),
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
                ["passwordhash"] = passwordHash, // Also store in Properties for GameObject conversion
            }
        };

        // Set default player properties from the class
        if (_objectManager == null) throw new InvalidOperationException("ObjectManager not set. This should be set by DI container.");
        var inheritanceChain = _objectManager.GetInheritanceChain(playerClass.Id);
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

        _dbProvider.Insert("players", player);

        // Also insert the player as a GameObject so it can be moved and managed by ObjectManager
        var playerGameObject = new GameObject
        {
            Id = player.Id,
            Properties = player.Properties,
            Location = _dbProvider.FindOne<GameObject>("gameobjects", o => o.Properties.ContainsKey("isStartingRoom")),
            Contents = new List<string>(),
            CreatedAt = player.CreatedAt,
            ModifiedAt = player.ModifiedAt
        };
        _dbProvider.Insert("gameobjects", playerGameObject);

        // Add to starting room if specified
        if (startingRoomId != null)
        {
            if (_objectManager == null) throw new InvalidOperationException("ObjectManager not set. This should be set by DI container.");
            _objectManager.MoveObject(player.Id, startingRoomId);
        }

        return player;
    }

    /// <summary>
    /// Authenticates a player login
    /// </summary>
    public Player? AuthenticatePlayer(string name, string password)
    {
        Player? player = FindPlayerByName(name);
        if (player == null)
            return null;
        return VerifyPassword(password, player.PasswordHash) ? player : null;
    }

    /// <summary>
    /// Connects a player to a session
    /// </summary>
    public void ConnectPlayerToSession(string playerId, Guid sessionGuid)
    {
        if (_objectManager == null) throw new InvalidOperationException("ObjectManager not set. This should be set by DI container.");
        var player = _objectManager.GetObject<Player>(playerId);
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

        _dbProvider.Update("players", player);
    }

    /// <summary>
    /// Disconnects a player from their session
    /// </summary>
    public void DisconnectPlayer(string playerId)
    {
        if (_objectManager == null) throw new InvalidOperationException("ObjectManager not set. This should be set by DI container.");
        var player = _objectManager.GetObject<Player>(playerId);
        if (player == null)
            return;

        player.SessionGuid = null;
        player.IsOnline = false;
        player.ModifiedAt = DateTime.UtcNow;

        _dbProvider.Update("players", player);
    }

    /// <summary>
    /// Finds a player by session GUID
    /// </summary>
    public Player? GetPlayerBySession(Guid sessionGuid)
    {
        if (_objectManager == null) throw new InvalidOperationException("ObjectManager not set. This should be set by DI container.");
        // First try to find in ObjectManager cache
        var allCachedObjects = _objectManager.GetAllObjects();
        var cachedPlayer = allCachedObjects.OfType<Player>().FirstOrDefault(p => p.SessionGuid == sessionGuid);
        if (cachedPlayer != null)
        {
            return cachedPlayer;
        }

        // Fallback to database query and cache the result
        var player = _dbProvider.FindOne<Player>("players", p => p.SessionGuid == sessionGuid);
        if (player != null)
        {
            // Cache the player object for future use
            _objectManager.CacheGameObject(player);
        }
        return player;
    }

    /// <summary>
    /// Gets all online players
    /// </summary>
    public List<Player> GetOnlinePlayers()
    {
        if (_objectManager == null) throw new InvalidOperationException("ObjectManager not set. This should be set by DI container.");
        // First try to get from ObjectManager cache
        var allCachedObjects = _objectManager.GetAllObjects();
        var cachedOnlinePlayers = allCachedObjects.OfType<Player>().Where(p => p.IsOnline).ToList();
        
        // Also check database for any players not in cache
        var dbOnlinePlayers = _dbProvider.Find<Player>("players", p => p.IsOnline).ToList();
        
        // Cache any players from database that aren't already cached
        foreach (var player in dbOnlinePlayers)
        {
            if (!cachedOnlinePlayers.Any(cp => cp.Id == player.Id))
            {
                _objectManager.CacheGameObject(player);
                cachedOnlinePlayers.Add(player);
            }
        }
        
        return cachedOnlinePlayers;
    }

    /// <summary>
    /// Gets all players (both online and offline)
    /// </summary>
    public List<Player> GetAllPlayers()
    {
        if (_objectManager == null) throw new InvalidOperationException("ObjectManager not set. This should be set by DI container.");
        // First try to get from ObjectManager cache
        var allCachedObjects = _objectManager.GetAllObjects();
        var cachedPlayers = allCachedObjects.OfType<Player>().ToList();
        
        // Also check database for any players not in cache
        var dbPlayers = _dbProvider.FindAll<Player>("players").ToList();
        
        // Cache any players from database that aren't already cached
        foreach (var player in dbPlayers)
        {
            if (!cachedPlayers.Any(cp => cp.Id == player.Id))
            {
                _objectManager.CacheGameObject(player);
                cachedPlayers.Add(player);
            }
        }
        
        return cachedPlayers;
    }

    /// <summary>
    /// Find a player by name (case-insensitive)
    /// </summary>
    public Player? FindPlayerByName(string name)
    {
        if (_objectManager == null) throw new InvalidOperationException("ObjectManager not set. This should be set by DI container.");
        // First try to find in ObjectManager cache
        var allCachedObjects = _objectManager.GetAllObjects();
        var cachedPlayer = allCachedObjects.OfType<Player>().FirstOrDefault(p => p.Name.ToLower() == name.ToLower());
        if (cachedPlayer != null)
        {
            // If password hash is missing, try to get it from the database
            if (string.IsNullOrEmpty(cachedPlayer.PasswordHash))
            {
                var dbPlayer = _dbProvider.FindOne<Player>("players", p => p.Id == cachedPlayer.Id);
                if (dbPlayer != null && !string.IsNullOrEmpty(dbPlayer.PasswordHash))
                {
                    cachedPlayer.PasswordHash = dbPlayer.PasswordHash;
                    if (!cachedPlayer.Properties.ContainsKey("passwordhash"))
                    {
                        cachedPlayer.Properties["passwordhash"] = new BsonValue(dbPlayer.PasswordHash);
                    }
                }
            }
            return cachedPlayer;
        }

        // Fallback to database query and cache the result
        var player = _dbProvider.FindOne<Player>("players", p => p.Name.ToLower() == name.ToLower());
        if (player != null)
        {
            // Ensure password hash is in Properties for GameObject conversion compatibility
            if (!string.IsNullOrEmpty(player.PasswordHash) && !player.Properties.ContainsKey("passwordhash"))
            {
                player.Properties["passwordhash"] = new BsonValue(player.PasswordHash);
            }
            // Cache the player object for future use
            _objectManager.CacheGameObject(player);
        }
        return player;
    }

    /// <summary>
    /// Changes a player's password
    /// </summary>
    public void ChangePassword(string playerId, string newPassword)
    {
        if (_objectManager == null) throw new InvalidOperationException("ObjectManager not set. This should be set by DI container.");
        var player = _objectManager.GetObject<Player>(playerId);
        if (player == null)
            throw new ArgumentException($"Player with ID {playerId} not found");

        player.PasswordHash = HashPassword(newPassword);
        player.ModifiedAt = DateTime.UtcNow;

        _dbProvider.Update("players", player);
    }

    private ObjectClass GetOrCreatePlayerClass()
    {
        var playerClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Player");
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

        _dbProvider.Insert("objectclasses", playerClass);
        return playerClass;
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "CSMOO_SALT"));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        var computedHash = HashPassword(password);
        return computedHash == hash;
    }
}
