using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using CSMOO.Server.WebSocket;

namespace CSMOO.Server.Session
{
    /// <summary>
    /// WebSocket implementation of IClientConnection
    /// </summary>
    public class WebSocketConnection : IClientConnection
    {
        private readonly WebSocketSession _session;

        public WebSocketConnection(WebSocketSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public Guid SessionId => _session.SessionId;

        public bool IsConnected => _session.WebSocket.State == WebSocketState.Open;

        public async Task SendMessageAsync(string message)
        {
            if (!IsConnected)
                return;

            if (_session.ChannelType == WebSocketChannelType.Text)
            {
                await _session.SendTextAsync(message);
            }
            else
            {
                // For JSON channels, wrap the message in a structured format
                var jsonMessage = new
                {
                    type = "message",
                    content = message,
                    timestamp = DateTime.UtcNow
                };
                await _session.SendJsonAsync(jsonMessage);
            }
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                try
                {
                    _session.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Session ended",
                        System.Threading.CancellationToken.None).Wait(1000);
                }
                catch
                {
                    // Ignore errors during disconnect
                }
            }
        }
    }
}
