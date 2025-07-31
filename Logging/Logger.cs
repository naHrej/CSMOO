using System;
using System.IO;
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
/// Static logger class for centralized logging
/// </summary>
public static class Logger
{
    private static readonly object _lock = new object();
    private static bool _logsRotated = false;
    
    /// <summary>
    /// Initialize logging system and rotate logs if needed
    /// </summary>
    public static void Initialize()
    {
        if (_logsRotated) return;
        
        lock (_lock)
        {
            if (_logsRotated) return;
            
            var config = Config.Instance.Logging;
            
            // Create logs directory if it doesn't exist
            var logsDir = Path.GetDirectoryName(config.GameLogFile);
            if (!string.IsNullOrEmpty(logsDir) && !Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            
            var debugLogsDir = Path.GetDirectoryName(config.DebugLogFile);
            if (!string.IsNullOrEmpty(debugLogsDir) && !Directory.Exists(debugLogsDir))
            {
                Directory.CreateDirectory(debugLogsDir);
            }
            
            // Rotate existing logs
            RotateLogs(config.GameLogFile, config.MaxLogFiles);
            RotateLogs(config.DebugLogFile, config.MaxLogFiles);
            
            _logsRotated = true;
        }
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
                WriteColoredConsole(logMessage, ConsoleColor.Green);
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
        
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var levelStr = level.ToString().ToUpper();
        var logMessage = $"[{timestamp}] [{levelStr}] {message}";
        
        lock (_lock)
        {
            // Write to console (debug messages only appear in console when ShowDebugInConsole is true)
            if (config.EnableConsoleLogging && (level != LogLevel.Debug || serverConfig.ShowDebugInConsole))
            {
                WriteColoredConsole(logMessage, GetLogLevelColor(level));
            }
            
            // Write to appropriate log file (file logging respects LogLevel setting, not ShowDebugInConsole)
            if (config.EnableFileLogging)
            {
                try
                {
                    string logFile;
                    if (level == LogLevel.Debug)
                    {
                        logFile = config.DebugLogFile;
                    }
                    else
                    {
                        logFile = config.GameLogFile;
                    }
                    
                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(logFile);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    File.AppendAllText(logFile, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to log file: {ex.Message}");
                }
            }
        }
    }
    
    /// <summary>
    /// Get the console color for a given log level
    /// </summary>
    private static ConsoleColor GetLogLevelColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.Cyan,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => ConsoleColor.White
        };
    }
    
    /// <summary>
    /// Write a colored message to console
    /// </summary>
    private static void WriteColoredConsole(string message, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
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
    
    /// <summary>
    /// Get the current version from Git (tag or commit hash)
    /// </summary>
    private static string GetVersion()
    {
        try
        {
            // First try to get the latest tag
            var tagProcess = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "describe --tags --exact-match HEAD",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(tagProcess))
            {
                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        return process.StandardOutput.ReadToEnd().Trim();
                    }
                }
            }

            // If no tag, get short commit hash
            var commitProcess = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --short HEAD",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(commitProcess))
            {
                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        var hash = process.StandardOutput.ReadToEnd().Trim();
                        return $"dev-{hash}";
                    }
                }
            }
        }
        catch
        {
            // Fall back to default if Git is not available
        }

        return "1.0.0";
    }

    /// <summary>
    /// Display a stylized startup banner
    /// </summary>
    public static void DisplayBanner()
    {
        var config = Config.Instance.Logging;
        if (!config.EnableConsoleLogging) return;
        
        var originalColor = Console.ForegroundColor;
        try
        {
            var bannerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "banner.txt");
            
            string bannerContent;
            if (File.Exists(bannerPath))
            {
                bannerContent = File.ReadAllText(bannerPath);
            }
            else
            {
                // Fallback banner if file is missing
                bannerContent = @"╔══════════════════════════════════════════════════════════════════════════════╗
║                                                                              ║
║                                    CSMOO                                     ║
║                           Multi-User Object Oriented Server                 ║
║                                  Version {VERSION}                          ║
║                                                                              ║
╚══════════════════════════════════════════════════════════════════════════════╝";
            }
            
            // Replace version placeholder
            var version = GetVersion();
            bannerContent = bannerContent.Replace("{VERSION}", version);
            
            // Split into lines and apply colors
            var lines = bannerContent.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Multi-User Object Oriented Server"))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                else if (line.Contains("Version"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                }
                
                Console.WriteLine(line.TrimEnd('\r'));
            }
            
            Console.WriteLine();
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }
    
    /// <summary>
    /// Display a stylized section header
    /// </summary>
    public static void DisplaySectionHeader(string title)
    {
        var config = Config.Instance.Logging;
        if (!config.EnableConsoleLogging) return;
        
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine();
            Console.WriteLine($"▓▓▓ {title} ▓▓▓");
            Console.WriteLine();
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }
}
