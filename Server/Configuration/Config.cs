using System;
using System.IO;
using System.Text.Json;

namespace CSMOO.Server.Configuration;

/// <summary>
/// Main configuration class for the CSMOO server
/// </summary>
public class Config
{
    public ServerConfig Server { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    
    private static Config? _instance;
    private static readonly object _lock = new object();
    
    /// <summary>
    /// Gets the singleton configuration instance
    /// </summary>
    public static Config Instance
    {
        get
        {
            lock (_lock)
            {
                return _instance ??= LoadConfig();
            }
        }
    }
    
    /// <summary>
    /// Loads configuration from config.json file
    /// </summary>
    private static Config LoadConfig()
    {
        const string configFile = "config.json";
        
        try
        {
            if (File.Exists(configFile))
            {
                var json = File.ReadAllText(configFile);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                
                return JsonSerializer.Deserialize<Config>(json, options) ?? new Config();
            }
            else
            {
                // Create default config file
                var defaultConfig = new Config();
                SaveConfig(defaultConfig, configFile);
                return defaultConfig;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading config file: {ex.Message}");
            Console.WriteLine("Using default configuration.");
            return new Config();
        }
    }
    
    /// <summary>
    /// Saves configuration to file
    /// </summary>
    private static void SaveConfig(Config config, string configFile)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(configFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config file: {ex.Message}");
        }
    }
}

/// <summary>
/// Server-specific configuration
/// </summary>
public class ServerConfig
{
    public int Port { get; set; } = 1701;
    public bool ShowDebugInConsole { get; set; } = false;
}

/// <summary>
/// Database configuration
/// </summary>
public class DatabaseConfig
{
    public string GameDataFile { get; set; } = "gamedata.db";
    public string LogDataFile { get; set; } = "gamedata-log.db";
}

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
