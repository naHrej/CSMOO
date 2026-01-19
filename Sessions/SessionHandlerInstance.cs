using System.Net.Sockets;
using CSMOO.Object;
using CSMOO.Database;

namespace CSMOO.Sessions;

/// <summary>
/// Instance-based session handler implementation for dependency injection
/// </summary>
public class SessionHandlerInstance : ISessionHandler
{
    private readonly IPlayerManager _playerManager;
    private readonly object _lock = new object();
    private readonly List<SessionInfo> _activeSessions = new List<SessionInfo>();
    
    public SessionHandlerInstance(IPlayerManager playerManager)
    {
        _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
    }
    
    public IReadOnlyList<SessionInfo> ActiveSessions 
    { 
        get 
        { 
            lock (_lock) 
            { 
                return _activeSessions.ToList(); 
            } 
        }
    }
    
    public void AddSession(Guid clientGuid, TcpClient client)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        
        var connection = new TelnetConnection(clientGuid, client);
        var sessionInfo = new SessionInfo(clientGuid, connection);
        
        lock (_lock)
        {
            _activeSessions.Add(sessionInfo);
        }
    }

    public void AddSession(Guid clientGuid, IClientConnection connection)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        
        var sessionInfo = new SessionInfo(clientGuid, connection);
        
        lock (_lock)
        {
            _activeSessions.Add(sessionInfo);
        }
    }

    public bool RemoveSession(Guid clientGuid)
    {
        lock (_lock)
        {
            for (int i = 0; i < _activeSessions.Count; i++)
            {
                if (_activeSessions[i].ClientGuid == clientGuid)
                {
                    // Disconnect the player if they're logged in
                    var player = _playerManager.GetPlayerBySession(clientGuid);
                    if (player != null)
                    {
                        _playerManager.DisconnectPlayer(player.Id);
                    }
                    
                    _activeSessions.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Gets the player associated with a session, if any
    /// </summary>
    public Player? GetPlayerForSession(Guid sessionGuid)
    {
        return _playerManager.GetPlayerBySession(sessionGuid);
    }

    /// <summary>
    /// Authenticates and connects a player to a session
    /// </summary>
    public bool LoginPlayer(Guid sessionGuid, string playerName, string password)
    {
        var player = _playerManager.AuthenticatePlayer(playerName, password);
        if (player == null)
            return false;

        _playerManager.ConnectPlayerToSession(player.Id, sessionGuid);
        return true;
    }
}
