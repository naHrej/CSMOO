namespace CSMOO.Server.Configuration;

/// <summary>
/// Database configuration
/// </summary>
public class DatabaseConfig
{
    public string GameDataFile { get; set; } = "gamedata.db";
    public string LogDataFile { get; set; } = "gamedata-log.db";
}
