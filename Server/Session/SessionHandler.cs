using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using CSMOO.Server.Database;

namespace CSMOO.Server.Session;

    static class SessionHandler
    {
        private static readonly object _lock = new object();
        private static readonly List<SessionInfo> _activeSessions = new List<SessionInfo>();
        
        public static IReadOnlyList<SessionInfo> ActiveSessions 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _activeSessions.ToList(); 
                } 
            } 
        }
        
        public static void AddSession(Guid clientGuid, TcpClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            
            var connection = new TelnetConnection(clientGuid, client);
            var sessionInfo = new SessionInfo(clientGuid, connection);
            
            lock (_lock)
            {
                _activeSessions.Add(sessionInfo);
            }
        }

        public static void AddSession(Guid clientGuid, IClientConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            
            var sessionInfo = new SessionInfo(clientGuid, connection);
            
            lock (_lock)
            {
                _activeSessions.Add(sessionInfo);
            }
        }

        public static bool RemoveSession(Guid clientGuid)
        {
            lock (_lock)
            {
                for (int i = 0; i < _activeSessions.Count; i++)
                {
                    if (_activeSessions[i].ClientGuid == clientGuid)
                    {
                        // Disconnect the player if they're logged in
                        var player = PlayerManager.GetPlayerBySession(clientGuid);
                        if (player != null)
                        {
                            PlayerManager.DisconnectPlayer(player.Id);
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
        public static Player? GetPlayerForSession(Guid sessionGuid)
        {
            return PlayerManager.GetPlayerBySession(sessionGuid);
        }

        /// <summary>
        /// Authenticates and connects a player to a session
        /// </summary>
        public static bool LoginPlayer(Guid sessionGuid, string playerName, string password)
        {
            var player = PlayerManager.AuthenticatePlayer(playerName, password);
            if (player == null)
                return false;

            PlayerManager.ConnectPlayerToSession(player.Id, sessionGuid);
            return true;
        }
    }

