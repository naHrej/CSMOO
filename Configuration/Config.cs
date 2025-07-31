using System;
using System.IO;
using System.Text.Json;
using CSMOO.Logging;

namespace CSMOO.Configuration;

/// <summary>
/// Main configuration class for the CSMOO server
/// </summary>
public class Config
{
    public ServerConfig Server { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public ScriptingConfig Scripting { get; set; } = new();
    
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
            Logger.Error($"Error loading config file: {ex.Message}");
            Logger.Info("Using default configuration.");
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
            Logger.Error($"Error saving config file: {ex.Message}");
        }
    }
}
