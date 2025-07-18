using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CSMOO.Server.WebSocket
{
    /// <summary>
    /// Represents the type of WebSocket communication channel
    /// </summary>
    public enum WebSocketChannelType
    {
        /// <summary>
        /// Text-based communication similar to telnet
        /// </summary>
        Text,
        
        /// <summary>
        /// JSON-based structured communication for APIs
        /// </summary>
        Json
    }

    /// <summary>
    /// Represents a WebSocket session with channel type and subscription management
    /// </summary>
    public class WebSocketSession
    {
        private readonly HashSet<string> _subscribedChannels = new HashSet<string>();
        private readonly object _channelsLock = new object();

        public Guid SessionId { get; }
        public System.Net.WebSockets.WebSocket WebSocket { get; }
        public WebSocketChannelType ChannelType { get; }
        public DateTime ConnectedAt { get; }

        public WebSocketSession(Guid sessionId, System.Net.WebSockets.WebSocket webSocket, WebSocketChannelType channelType)
        {
            SessionId = sessionId;
            WebSocket = webSocket;
            ChannelType = channelType;
            ConnectedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Send a text message to the WebSocket client
        /// </summary>
        public async Task SendTextAsync(string message)
        {
            if (WebSocket.State != WebSocketState.Open)
                return;

            var bytes = Encoding.UTF8.GetBytes(message);
            await WebSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        /// <summary>
        /// Send a JSON object to the WebSocket client
        /// </summary>
        public async Task SendJsonAsync(object obj)
        {
            if (WebSocket.State != WebSocketState.Open)
                return;

            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var bytes = Encoding.UTF8.GetBytes(json);
            await WebSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        /// <summary>
        /// Subscribe to a channel for receiving broadcast messages
        /// </summary>
        public void SubscribeToChannel(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
                return;

            lock (_channelsLock)
            {
                _subscribedChannels.Add(channel.ToLowerInvariant());
            }
        }

        /// <summary>
        /// Unsubscribe from a channel
        /// </summary>
        public void UnsubscribeFromChannel(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
                return;

            lock (_channelsLock)
            {
                _subscribedChannels.Remove(channel.ToLowerInvariant());
            }
        }

        /// <summary>
        /// Check if the session is subscribed to a specific channel
        /// </summary>
        public bool IsSubscribedToChannel(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
                return false;

            lock (_channelsLock)
            {
                return _subscribedChannels.Contains(channel.ToLowerInvariant());
            }
        }

        /// <summary>
        /// Get all subscribed channels
        /// </summary>
        public List<string> GetSubscribedChannels()
        {
            lock (_channelsLock)
            {
                return new List<string>(_subscribedChannels);
            }
        }
    }

    /// <summary>
    /// Information about a WebSocket session for monitoring and management
    /// </summary>
    public class WebSocketSessionInfo
    {
        public Guid SessionId { get; set; }
        public WebSocketChannelType ChannelType { get; set; }
        public DateTime ConnectedAt { get; set; }
        public WebSocketState State { get; set; }
        public List<string> SubscribedChannels { get; set; } = new List<string>();
    }
}
