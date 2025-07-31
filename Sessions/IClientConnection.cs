namespace CSMOO.Sessions;
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


