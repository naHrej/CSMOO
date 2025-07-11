using System;
using System.IO;
using CSMOO.Server.Configuration;

namespace CSMOO.Server.Logging;

/// <summary>
/// Log levels for different types of messages
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

/// <summary>
/// Static logger class for centralized logging
/// </summary>
public static class Logger
{
    private static readonly object _lock = new object();
    
    /// <summary>
    /// Logs a debug message (only when debug mode is enabled)
    /// </summary>
    public static void Debug(string message)
    {
        Log(LogLevel.Debug, message);
    }
    
    /// <summary>
    /// Logs an informational message
    /// </summary>
    public static void Info(string message)
    {
        Log(LogLevel.Info, message);
    }
    
    /// <summary>
    /// Logs a warning message
    /// </summary>
    public static void Warning(string message)
    {
        Log(LogLevel.Warning, message);
    }
    
    /// <summary>
    /// Logs an error message
    /// </summary>
    public static void Error(string message)
    {
        Log(LogLevel.Error, message);
    }
    
    /// <summary>
    /// Logs an error with exception details
    /// </summary>
    public static void Error(string message, Exception ex)
    {
        Log(LogLevel.Error, $"{message}: {ex.Message}\nStack trace: {ex.StackTrace}");
    }
    
    /// <summary>
    /// Logs a game-related message (always goes to game.log)
    /// </summary>
    public static void Game(string message)
    {
        var config = Config.Instance.Logging;
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logMessage = $"[{timestamp}] [GAME] {message}";
        
        lock (_lock)
        {
            // Always write to console for game messages
            if (config.EnableConsoleLogging)
            {
                Console.WriteLine(logMessage);
            }
            
            // Write to game log file
            if (config.EnableFileLogging)
            {
                try
                {
                    File.AppendAllText(config.GameLogFile, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to game log: {ex.Message}");
                }
            }
        }
    }
    
    /// <summary>
    /// Core logging method
    /// </summary>
    private static void Log(LogLevel level, string message)
    {
        var config = Config.Instance.Logging;
        var serverConfig = Config.Instance.Server;
        
        // Check if we should log this level
        if (!ShouldLog(level, config.LogLevel))
            return;
        
        // For debug messages, only log if debug mode is enabled
        if (level == LogLevel.Debug && !serverConfig.DebugMode)
            return;
        
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var levelStr = level.ToString().ToUpper();
        var logMessage = $"[{timestamp}] [{levelStr}] {message}";
        
        lock (_lock)
        {
            // Write to console (except debug messages unless debug mode is on)
            if (config.EnableConsoleLogging && (level != LogLevel.Debug || serverConfig.DebugMode))
            {
                Console.WriteLine(logMessage);
            }
            
            // Write to appropriate log file
            if (config.EnableFileLogging)
            {
                try
                {
                    if (level == LogLevel.Debug)
                    {
                        // Debug messages go to debug.log
                        File.AppendAllText(config.DebugLogFile, logMessage + Environment.NewLine);
                    }
                    else
                    {
                        // Other messages go to game.log
                        File.AppendAllText(config.GameLogFile, logMessage + Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to log file: {ex.Message}");
                }
            }
        }
    }
    
    /// <summary>
    /// Determines if a message should be logged based on the configured log level
    /// </summary>
    private static bool ShouldLog(LogLevel messageLevel, string configuredLevel)
    {
        var threshold = configuredLevel.ToLower() switch
        {
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Info,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            _ => LogLevel.Info
        };
        
        return messageLevel >= threshold;
    }
}
