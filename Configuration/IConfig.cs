namespace CSMOO.Configuration;

/// <summary>
/// Interface for configuration management
/// </summary>
public interface IConfig
{
    ServerConfig Server { get; }
    DatabaseConfig Database { get; }
    LoggingConfig Logging { get; }
    ScriptingConfig Scripting { get; }
}
