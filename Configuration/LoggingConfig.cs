namespace CSMOO.Configuration;

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfig
{
    public bool EnableConsoleLogging { get; set; } = true;
    public bool EnableFileLogging { get; set; } = true;
    public string GameLogFile { get; set; } = "logs/game.log";
    public string DebugLogFile { get; set; } = "logs/debug.log";
    public string LogLevel { get; set; } = "Info";
    public int MaxLogFiles { get; set; } = 5;
}
