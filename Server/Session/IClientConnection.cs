using System;
using System.Threading.Tasks;

namespace CSMOO.Server.Session
{
    /// <summary>
    /// Interface for different types of client connections (Telnet, WebSocket, etc.)
    /// </summary>
    public interface IClientConnection
    {
        Guid SessionId { get; }
        bool IsConnected { get; }
        Task SendMessageAsync(string message);
        void Disconnect();
    }
}
