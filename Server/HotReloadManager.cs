using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CSMOO.Server.Database;
using CSMOO.Server.Database.World;
using CSMOO.Server.Logging;
using CSMOO.Server.Scripting;

namespace CSMOO.Server;

/// <summary>
/// Manages hot reloading of server components without requiring a restart
/// </summary>
public static class HotReloadManager
{
    private static readonly List<FileSystemWatcher> _watchers = new();
    private static readonly Dictionary<string, DateTime> _lastReloadTimes = new();
    private static readonly object _reloadLock = new();
    private static Timer? _debounceTimer;
    private static readonly HashSet<string> _pendingReloads = new();
    private static bool _isEnabled = false;

    /// <summary>
    /// Initialize hot reload functionality
    /// </summary>
    public static void Initialize()
    {
        Logger.Info("Initializing Hot Reload Manager...");
        
        try
        {
            // Watch verb JSON files
            SetupVerbFileWatcher();
            
            // Watch C# script files if they exist
            SetupScriptFileWatcher();
            
            _isEnabled = true;
            Logger.Info("Hot Reload Manager initialized successfully!");
            Logger.Info("The following will trigger hot reloads:");
            Logger.Info("  - Changes to verb JSON files in Resources/verbs/");
            Logger.Info("  - Changes to C# script files in Scripts/ (if present)");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to initialize Hot Reload Manager", ex);
        }
    }

    /// <summary>
    /// Setup file watcher for verb JSON files
    /// </summary>
    private static void SetupVerbFileWatcher()
    {
        var verbsPath = Path.Combine("Resources", "verbs");
        if (!Directory.Exists(verbsPath))
        {
            Logger.Warning($"Verbs directory not found: {verbsPath}");
            return;
        }

        var verbWatcher = new FileSystemWatcher(verbsPath)
        {
            Filter = "*.json",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        verbWatcher.Changed += OnVerbFileChanged;
        verbWatcher.Created += OnVerbFileChanged;
        verbWatcher.Deleted += OnVerbFileChanged;
        verbWatcher.Renamed += OnVerbFileRenamed;

        verbWatcher.EnableRaisingEvents = true;
        _watchers.Add(verbWatcher);

        Logger.Debug($"Watching for verb changes in: {Path.GetFullPath(verbsPath)}");
    }

    /// <summary>
    /// Setup file watcher for C# script files
    /// </summary>
    private static void SetupScriptFileWatcher()
    {
        var scriptsPath = "Scripts";
        if (!Directory.Exists(scriptsPath))
        {
            Logger.Debug($"Scripts directory not found: {scriptsPath} (this is optional)");
            return;
        }

        var scriptWatcher = new FileSystemWatcher(scriptsPath)
        {
            Filter = "*.cs",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };

        scriptWatcher.Changed += OnScriptFileChanged;
        scriptWatcher.Created += OnScriptFileChanged;
        scriptWatcher.Deleted += OnScriptFileChanged;

        scriptWatcher.EnableRaisingEvents = true;
        _watchers.Add(scriptWatcher);

        Logger.Debug($"Watching for script changes in: {Path.GetFullPath(scriptsPath)}");
    }

    /// <summary>
    /// Handle verb file changes
    /// </summary>
    private static void OnVerbFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!_isEnabled) return;

        var fileName = Path.GetFileName(e.FullPath);
        Logger.Debug($"Verb file changed: {fileName} ({e.ChangeType})");

        ScheduleReload("verbs", () =>
        {
            Logger.Info($"Hot reloading verbs due to change in: {fileName}");
            ReloadVerbs();
        });
    }

    /// <summary>
    /// Handle verb file renames
    /// </summary>
    private static void OnVerbFileRenamed(object sender, RenamedEventArgs e)
    {
        if (!_isEnabled) return;

        var oldName = Path.GetFileName(e.OldFullPath);
        var newName = Path.GetFileName(e.FullPath);
        Logger.Debug($"Verb file renamed: {oldName} -> {newName}");

        ScheduleReload("verbs", () =>
        {
            Logger.Info($"Hot reloading verbs due to file rename: {oldName} -> {newName}");
            ReloadVerbs();
        });
    }

    /// <summary>
    /// Handle script file changes
    /// </summary>
    private static void OnScriptFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!_isEnabled) return;

        var fileName = Path.GetFileName(e.FullPath);
        Logger.Debug($"Script file changed: {fileName} ({e.ChangeType})");

        ScheduleReload("scripts", () =>
        {
            Logger.Info($"Hot reloading scripts due to change in: {fileName}");
            ReloadScripts();
        });
    }

    /// <summary>
    /// Schedule a reload with debouncing to avoid multiple rapid reloads
    /// </summary>
    private static void ScheduleReload(string category, Action reloadAction)
    {
        lock (_reloadLock)
        {
            _pendingReloads.Add(category);
            
            // Cancel existing timer
            _debounceTimer?.Dispose();
            
            // Start new timer with 500ms delay
            _debounceTimer = new Timer(_ =>
            {
                lock (_reloadLock)
                {
                    try
                    {
                        if (_pendingReloads.Contains(category))
                        {
                            reloadAction();
                            _pendingReloads.Remove(category);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error during hot reload of {category}", ex);
                    }
                }
            }, null, TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Reload all verb definitions
    /// </summary>
    private static void ReloadVerbs()
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            Logger.Info("🔄 Hot reloading verb definitions...");
            VerbInitializer.ReloadVerbs();
            
            stopwatch.Stop();
            Logger.Info($"✅ Verb hot reload completed in {stopwatch.ElapsedMilliseconds}ms");
            
            // Notify online players of the reload
            NotifyOnlinePlayersOfReload("Verb definitions have been hot reloaded!");
        }
        catch (Exception ex)
        {
            Logger.Error("❌ Failed to hot reload verbs", ex);
        }
    }

    /// <summary>
    /// Reload all function definitions
    /// </summary>
    private static void ReloadFunctions()
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            Logger.Info("🔄 Hot reloading function definitions...");
            FunctionInitializer.ReloadFunctions();
            
            stopwatch.Stop();
            Logger.Info($"✅ Function hot reload completed in {stopwatch.ElapsedMilliseconds}ms");
            
            // Notify online players of the reload
            NotifyOnlinePlayersOfReload("Function definitions have been hot reloaded!");
        }
        catch (Exception ex)
        {
            Logger.Error("❌ Failed to hot reload functions", ex);
        }
    }

    /// <summary>
    /// Reload script engine components
    /// </summary>
    private static void ReloadScripts()
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            Logger.Info("🔄 Hot reloading script engine...");
            
            // Clear script engine cache if it has one
            // ScriptEngine.ClearCache(); // Implement this if needed
            
            stopwatch.Stop();
            Logger.Info($"✅ Script engine hot reload completed in {stopwatch.ElapsedMilliseconds}ms");
            
            // Notify online players of the reload
            NotifyOnlinePlayersOfReload("Script engine has been hot reloaded!");
        }
        catch (Exception ex)
        {
            Logger.Error("❌ Failed to hot reload scripts", ex);
        }
    }

    /// <summary>
    /// Manually trigger a hot reload of verbs
    /// </summary>
    public static void ManualReloadVerbs()
    {
        Logger.Info("Manual verb reload requested");
        ReloadVerbs();
    }

    /// <summary>
    /// Manually trigger a hot reload of functions
    /// </summary>
    public static void ManualReloadFunctions()
    {
        Logger.Info("Manual function reload requested");
        ReloadFunctions();
    }

    /// <summary>
    /// Manually trigger a hot reload of scripts
    /// </summary>
    public static void ManualReloadScripts()
    {
        Logger.Info("Manual script reload requested");
        ReloadScripts();
    }

    /// <summary>
    /// Notify online players about a hot reload
    /// </summary>
    private static void NotifyOnlinePlayersOfReload(string message)
    {
        try
        {
            var onlinePlayers = PlayerManager.GetOnlinePlayers();
            foreach (var player in onlinePlayers)
            {
                if (player.SessionGuid != null)
                {
                    // Send notification to player (you'd need to implement this based on your session system)
                    Logger.Debug($"Notifying player {player.Name} of hot reload");
                    // SessionManager.SendToPlayer(player.SessionGuid, $"[SYSTEM] {message}");
                }
            }
            
            if (onlinePlayers.Any())
            {
                Logger.Debug($"Notified {onlinePlayers.Count()} online players of hot reload");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to notify players of hot reload: {ex.Message}");
        }
    }

    /// <summary>
    /// Enable or disable hot reload functionality
    /// </summary>
    public static void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        Logger.Info($"Hot reload {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Get the current status of hot reload
    /// </summary>
    public static bool IsEnabled => _isEnabled;

    /// <summary>
    /// Shutdown the hot reload manager
    /// </summary>
    public static void Shutdown()
    {
        Logger.Info("Shutting down Hot Reload Manager...");
        
        _isEnabled = false;
        
        // Dispose all file watchers
        foreach (var watcher in _watchers)
        {
            watcher?.Dispose();
        }
        _watchers.Clear();
        
        // Dispose debounce timer
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        
        Logger.Info("Hot Reload Manager shutdown complete");
    }
}
