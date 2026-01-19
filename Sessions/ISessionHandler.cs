using System.Net.Sockets;
using CSMOO.Object;

namespace CSMOO.Sessions;

/// <summary>
/// Interface for session management
/// </summary>
public interface ISessionHandler
{
    /// <summary>
    /// Gets the list of active sessions
    /// </summary>
    IReadOnlyList<SessionInfo> ActiveSessions { get; }
    
    /// <summary>
    /// Adds a new session with a TcpClient
    /// </summary>
    void AddSession(Guid clientGuid, TcpClient client);
    
    /// <summary>
    /// Adds a new session with an IClientConnection
    /// </summary>
    void AddSession(Guid clientGuid, IClientConnection connection);
    
    /// <summary>
    /// Removes a session by client GUID
    /// </summary>
    bool RemoveSession(Guid clientGuid);
    
    /// <summary>
    /// Gets the player associated with a session, if any
    /// </summary>
    Player? GetPlayerForSession(Guid sessionGuid);
    
    /// <summary>
    /// Authenticates and connects a player to a session
    /// </summary>
    bool LoginPlayer(Guid sessionGuid, string playerName, string password);
}
