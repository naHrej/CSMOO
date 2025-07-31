using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CSMOO.Network;

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



