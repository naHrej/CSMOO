namespace CSMOO.Configuration;

/// <summary>
/// Server-specific configuration
/// </summary>
public class ServerConfig
{
    public int Port { get; set; } = 1701;
    public int WsPort { get; set; } = 1702; // WebSocket port
    public int HttpPort { get; set; } = 1703; // HTTP port
    public bool WsEnabled { get; set; } = true; // Enable WebSocket server
    public bool HttpEnabled { get; set; } = true; // Enable HTTP server
    public bool ShowDebugInConsole { get; set; } = false;
    public string ServerUrl { get; set; } = "http://localhost"; // Base URL for the server (e.g., "http://localhost" or "https://moo.serverhost.com")
}
