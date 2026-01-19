namespace CSMOO.Object;

/// <summary>
/// Interface for player management operations
/// </summary>
public interface IPlayerManager
{
    /// <summary>
    /// Creates a new player account
    /// </summary>
    Player CreatePlayer(string name, string password, string? startingRoomId = null);
    
    /// <summary>
    /// Authenticates a player login
    /// </summary>
    Player? AuthenticatePlayer(string name, string password);
    
    /// <summary>
    /// Connects a player to a session
    /// </summary>
    void ConnectPlayerToSession(string playerId, Guid sessionGuid);
    
    /// <summary>
    /// Disconnects a player from their session
    /// </summary>
    void DisconnectPlayer(string playerId);
    
    /// <summary>
    /// Finds a player by session GUID
    /// </summary>
    Player? GetPlayerBySession(Guid sessionGuid);
    
    /// <summary>
    /// Gets all online players
    /// </summary>
    List<Player> GetOnlinePlayers();
    
    /// <summary>
    /// Gets all players (both online and offline)
    /// </summary>
    List<Player> GetAllPlayers();
    
    /// <summary>
    /// Find a player by name (case-insensitive)
    /// </summary>
    Player? FindPlayerByName(string name);
    
    /// <summary>
    /// Changes a player's password
    /// </summary>
    void ChangePassword(string playerId, string newPassword);
}
