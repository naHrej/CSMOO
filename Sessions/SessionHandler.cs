using System.Net.Sockets;
using CSMOO.Object;
using CSMOO.Configuration;
using CSMOO.Database;

namespace CSMOO.Sessions;

/// <summary>
/// Static wrapper for SessionHandler with backward compatibility
/// </summary>
public static class SessionHandler
{
    private static ISessionHandler? _instance;
    
    public static void SetInstance(ISessionHandler instance)
    {
        _instance = instance;
    }
    
    private static ISessionHandler Instance => _instance ?? throw new InvalidOperationException("SessionHandler instance not set. Call SessionHandler.SetInstance() first.");
    
    public static IReadOnlyList<SessionInfo> ActiveSessions 
    { 
        get => Instance.ActiveSessions;
    }
    
    public static void AddSession(Guid clientGuid, TcpClient client)
    {
        Instance.AddSession(clientGuid, client);
    }

    public static void AddSession(Guid clientGuid, IClientConnection connection)
    {
        Instance.AddSession(clientGuid, connection);
    }

    public static bool RemoveSession(Guid clientGuid)
    {
        return Instance.RemoveSession(clientGuid);
    }

    /// <summary>
    /// Gets the player associated with a session, if any
    /// </summary>
    public static Player? GetPlayerForSession(Guid sessionGuid)
    {
        return Instance.GetPlayerForSession(sessionGuid);
    }

    /// <summary>
    /// Authenticates and connects a player to a session
    /// </summary>
    public static bool LoginPlayer(Guid sessionGuid, string playerName, string password)
    {
        return Instance.LoginPlayer(sessionGuid, playerName, password);
    }
}


