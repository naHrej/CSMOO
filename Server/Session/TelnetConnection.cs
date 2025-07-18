using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSMOO.Server.Session
{
    /// <summary>
    /// Telnet implementation of IClientConnection
    /// </summary>
    public class TelnetConnection : IClientConnection
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;

        public TelnetConnection(Guid sessionId, TcpClient client)
        {
            SessionId = sessionId;
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _stream = _client.GetStream();
        }

        public Guid SessionId { get; }

        public bool IsConnected => _client.Connected;

        public async Task SendMessageAsync(string message)
        {
            if (!IsConnected)
                return;

            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(bytes, 0, bytes.Length);
                await _stream.FlushAsync();
            }
            catch
            {
                // Connection may have been closed
            }
        }

        public void Disconnect()
        {
            try
            {
                _client.Close();
            }
            catch
            {
                // Ignore errors during disconnect
            }
        }
    }
}
