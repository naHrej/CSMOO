using System.Security.Cryptography;
using System.Text;
using CSMOO.Database;
using LiteDB;

namespace CSMOO.Object;

/// <summary>
/// Static wrapper for PlayerManager (backward compatibility)
/// Delegates to PlayerManagerInstance for dependency injection support
/// </summary>
public static class PlayerManager
{
    private static IPlayerManager? _instance;
    
    /// <summary>
    /// Sets the player manager instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IPlayerManager instance)
    {
        _instance = instance;
    }
    
    private static IPlayerManager Instance => _instance ?? throw new InvalidOperationException("PlayerManager instance not set. Call PlayerManager.SetInstance() first.");
    
    /// <summary>
    /// Ensures an instance exists (creates default if not set)
    /// </summary>
    private static void EnsureInstance()
    {
        if (_instance == null)
        {
            _instance = new PlayerManagerInstance(DbProvider.Instance);
        }
    }
    /// <summary>
    /// Creates a new player account
    /// </summary>
    public static Player CreatePlayer(string name, string password, string? startingRoomId = null)
    {
        EnsureInstance();
        return Instance.CreatePlayer(name, password, startingRoomId);
    }

    /// <summary>
    /// Authenticates a player login
    /// </summary>
    public static Player? AuthenticatePlayer(string name, string password)
    {
        EnsureInstance();
        return Instance.AuthenticatePlayer(name, password);
    }

    /// <summary>
    /// Connects a player to a session
    /// </summary>
    public static void ConnectPlayerToSession(string playerId, Guid sessionGuid)
    {
        EnsureInstance();
        Instance.ConnectPlayerToSession(playerId, sessionGuid);
    }

    /// <summary>
    /// Disconnects a player from their session
    /// </summary>
    public static void DisconnectPlayer(string playerId)
    {
        EnsureInstance();
        Instance.DisconnectPlayer(playerId);
    }

    /// <summary>
    /// Finds a player by session GUID
    /// </summary>
    public static Player? GetPlayerBySession(Guid sessionGuid)
    {
        EnsureInstance();
        return Instance.GetPlayerBySession(sessionGuid);
    }

    /// <summary>
    /// Gets all online players
    /// </summary>
    public static List<Player> GetOnlinePlayers()
    {
        EnsureInstance();
        return Instance.GetOnlinePlayers();
    }

    /// <summary>
    /// Gets all players (both online and offline)
    /// </summary>
    public static List<Player> GetAllPlayers()
    {
        EnsureInstance();
        return Instance.GetAllPlayers();
    }

    /// <summary>
    /// Find a player by name (case-insensitive)
    /// </summary>
    public static Player? FindPlayerByName(string name)
    {
        EnsureInstance();
        return Instance.FindPlayerByName(name);
    }

    /// <summary>
    /// Changes a player's password
    /// </summary>
    public static void ChangePassword(string playerId, string newPassword)
    {
        EnsureInstance();
        Instance.ChangePassword(playerId, newPassword);
    }
}



