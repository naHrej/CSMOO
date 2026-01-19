using System.Diagnostics;
using CSMOO.Configuration;

namespace CSMOO.Logging;

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
/// Static logger class for centralized logging (backward compatibility wrapper)
/// </summary>
public static class Logger
{
    private static ILogger? _instance;
    
    /// <summary>
    /// Sets the logger instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(ILogger instance)
    {
        _instance = instance;
    }
    
    private static ILogger Instance => _instance ?? throw new InvalidOperationException("Logger instance not set. Call Logger.SetInstance() first. Static access is no longer supported - use dependency injection.");
    
    /// <summary>
    /// Initialize logging system and rotate logs if needed
    /// </summary>
    public static void Initialize()
    {
        Instance.Initialize();
    }
    
    /// <summary>
    /// Rotates log files, keeping only the specified number of backups
    /// </summary>
    private static void RotateLogs(string logFilePath, int maxFiles)
    {
        try
        {
            if (!File.Exists(logFilePath)) return;
            
            var directory = Path.GetDirectoryName(logFilePath) ?? "";
            var fileName = Path.GetFileNameWithoutExtension(logFilePath);
            var extension = Path.GetExtension(logFilePath);
            
            // Move existing numbered logs up by one
            for (int i = maxFiles - 1; i >= 1; i--)
            {
                var oldFile = Path.Combine(directory, $"{fileName}.{i}{extension}");
                var newFile = Path.Combine(directory, $"{fileName}.{i + 1}{extension}");
                
                if (File.Exists(oldFile))
                {
                    if (File.Exists(newFile))
                        File.Delete(newFile);
                    File.Move(oldFile, newFile);
                }
            }
            
            // Move current log to .1
            var currentBackup = Path.Combine(directory, $"{fileName}.1{extension}");
            if (File.Exists(currentBackup))
                File.Delete(currentBackup);
            File.Move(logFilePath, currentBackup);
            
            // Remove old logs beyond maxFiles
            for (int i = maxFiles + 1; i <= maxFiles + 10; i++)
            {
                var oldFile = Path.Combine(directory, $"{fileName}.{i}{extension}");
                if (File.Exists(oldFile))
                    File.Delete(oldFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rotating logs: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs a debug message (only when debug mode is enabled)
    /// </summary>
    public static void Debug(string message)
    {
        Instance.Debug(message);
    }
    
    /// <summary>
    /// Logs an informational message
    /// </summary>
    public static void Info(string message)
    {
        Instance.Info(message);
    }
    
    /// <summary>
    /// Logs a warning message
    /// </summary>
    public static void Warning(string message)
    {
        Instance.Warning(message);
    }
    
    /// <summary>
    /// Logs an error message
    /// </summary>
    public static void Error(string message)
    {
        Instance.Error(message);
    }
    
    /// <summary>
    /// Logs an error with exception details
    /// </summary>
    public static void Error(string message, Exception ex)
    {
        Instance.Error(message, ex);
    }
    
    /// <summary>
    /// Logs a game-related message (always goes to game.log)
    /// </summary>
    public static void Game(string message)
    {
        Instance.Game(message);
    }
    
    /// <summary>
    /// Display a stylized startup banner
    /// </summary>
    public static void DisplayBanner()
    {
        Instance.DisplayBanner();
    }
    
    /// <summary>
    /// Display a stylized section header
    /// </summary>
    public static void DisplaySectionHeader(string title)
    {
        Instance.DisplaySectionHeader(title);
    }
    
}
