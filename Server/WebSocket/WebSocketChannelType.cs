using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CSMOO.Server.WebSocket;

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

