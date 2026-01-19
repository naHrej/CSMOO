using CSMOO.Core;
using CSMOO.Functions;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Verbs;
using CSMOO.Configuration;

namespace CSMOO.Init;

/// <summary>
/// Instance-based hot reload manager implementation for dependency injection
/// </summary>
public class HotReloadManagerInstance : IHotReloadManager
{
    private readonly ILogger _logger;
    private readonly IConfig _config;
    private readonly IVerbInitializer _verbInitializer;
    private readonly IFunctionInitializer _functionInitializer;
    private readonly IPlayerManager _playerManager;
    
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly Dictionary<string, DateTime> _lastReloadTimes = new();
    private readonly object _reloadLock = new();
    private Timer? _debounceTimer;
    private readonly HashSet<string> _pendingReloads = new();
    private bool _isEnabled = false;
    
    public bool IsEnabled => _isEnabled;
    
    public HotReloadManagerInstance(
        ILogger logger,
        IConfig config,
        IVerbInitializer verbInitializer,
        IFunctionInitializer functionInitializer,
        IPlayerManager playerManager)
    {
        _logger = logger;
        _config = config;
        _verbInitializer = verbInitializer;
        _functionInitializer = functionInitializer;
        _playerManager = playerManager;
    }
    
    /// <summary>
    /// Initialize hot reload functionality
    /// </summary>
    public void Initialize()
    {
        _logger.Info("Initializing Hot Reload Manager...");
        
        try
        {
            // Read config.json to determine if hot reload should be enabled
            bool enabled = false;
            try
            {
                var configText = File.ReadAllText("config.json");
                var configObj = System.Text.Json.JsonDocument.Parse(configText);
                if (configObj.RootElement.TryGetProperty("HotReload", out var hotReloadObj))
                {
                    if (hotReloadObj.TryGetProperty("Enabled", out var enabledProp))
                    {
                        enabled = enabledProp.GetBoolean();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Could not read HotReload config: {ex.Message}");
            }

            SetEnabled(enabled);

            // Watch verb JSON files
            SetupVerbFileWatcher();

            // Watch C# script files if they exist
            SetupScriptFileWatcher();

            // Watch all resource files (functions, classes, etc.)
            SetupResourceFileWatcher();

            _logger.Info("Hot Reload Manager initialized successfully!");
            _logger.Info("The following will trigger hot reloads:");
            _logger.Info("  - Changes to verb JSON files in resources/verbs/");
            _logger.Info("  - Changes to C# script files in Scripts/ (if present)");
            _logger.Info("  - Changes to other resource files in resources/");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to initialize Hot Reload Manager", ex);
        }
    }

    /// <summary>
    /// Setup file watcher for verb JSON files
    /// </summary>
    private void SetupVerbFileWatcher()
    {
        // Try multiple path strategies to find the verbs directory
        var possiblePaths = new List<string>();
        
        // Strategy 1: Application base directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        possiblePaths.Add(Path.Combine(appDirectory, "Resources", "verbs"));
        
        // Strategy 2: Current working directory
        var workingDirectory = Directory.GetCurrentDirectory();
        possiblePaths.Add(Path.Combine(workingDirectory, "Resources", "verbs"));
        
        // Strategy 3: Relative path from current directory
        possiblePaths.Add(Path.Combine("Resources", "verbs"));
        
        // Strategy 4: Check if we're in a subdirectory and need to go up
        var currentDir = Directory.GetCurrentDirectory();
        var parentDir = Directory.GetParent(currentDir);
        if (parentDir != null)
        {
            possiblePaths.Add(Path.Combine(parentDir.FullName, "Resources", "verbs"));
        }
        
        string? verbsPath = null;
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                verbsPath = path;
                break;
            }
        }
        
        if (verbsPath == null)
        {
            var searchedPaths = string.Join(", ", possiblePaths);
            _logger.Warning($"Verbs directory not found. Searched: {searchedPaths}");
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
    }

    /// <summary>
    /// Setup file watcher for C# script files
    /// </summary>
    private void SetupScriptFileWatcher()
    {
        var scriptsPath = "Scripts";
        if (!Directory.Exists(scriptsPath))
        {
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
    }

    /// <summary>
    /// Setup file watcher for all resource files (functions, classes, etc.)
    /// </summary>
    private void SetupResourceFileWatcher()
    {
        // Try multiple path strategies to find the resources directory
        var possiblePaths = new List<string>();

        // Strategy 1: Application base directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        possiblePaths.Add(Path.Combine(appDirectory, "Resources"));

        // Strategy 2: Current working directory
        var workingDirectory = Directory.GetCurrentDirectory();
        possiblePaths.Add(Path.Combine(workingDirectory, "Resources"));

        // Strategy 3: Relative path from current directory
        possiblePaths.Add(Path.Combine("Resources"));

        // Strategy 4: Parent directory
        var currentDir = Directory.GetCurrentDirectory();
        var parentDir = Directory.GetParent(currentDir);
        if (parentDir != null)
        {
            possiblePaths.Add(Path.Combine(parentDir.FullName, "Resources"));
        }

        string? resourcesPath = null;
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                resourcesPath = path;
                break;
            }
        }

        if (resourcesPath == null)
        {
            var searchedPaths = string.Join(", ", possiblePaths);
            _logger.Warning($"Resources directory not found. Searched: {searchedPaths}");
            return;
        }

        var resourceWatcher = new FileSystemWatcher(resourcesPath)
        {
            Filter = "*.*",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        resourceWatcher.Changed += OnResourceFileChanged;
        resourceWatcher.Created += OnResourceFileChanged;
        resourceWatcher.Deleted += OnResourceFileChanged;
        resourceWatcher.Renamed += OnResourceFileRenamed;

        resourceWatcher.EnableRaisingEvents = true;
        _watchers.Add(resourceWatcher);
    }

    /// <summary>
    /// Handle verb file changes
    /// </summary>
    private void OnVerbFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!_isEnabled) return;

        var fileName = Path.GetFileName(e.FullPath);

        ScheduleReload("verbs", () =>
        {
            _logger.Info($"Hot reloading verbs due to change in: {fileName}");
            ReloadVerbs();
        });
    }

    /// <summary>
    /// Handle verb file renames
    /// </summary>
    private void OnVerbFileRenamed(object sender, RenamedEventArgs e)
    {
        if (!_isEnabled) return;

        var oldName = Path.GetFileName(e.OldFullPath);
        var newName = Path.GetFileName(e.FullPath);

        ScheduleReload("verbs", () =>
        {
            _logger.Info($"Hot reloading verbs due to file rename: {oldName} -> {newName}");
            ReloadVerbs();
        });
    }

    /// <summary>
    /// Handle script file changes
    /// </summary>
    private void OnScriptFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!_isEnabled) return;

        var fileName = Path.GetFileName(e.FullPath);

        ScheduleReload("scripts", () =>
        {
            _logger.Info($"Hot reloading scripts due to change in: {fileName}");
            ReloadScripts();
        });
    }

    /// <summary>
    /// Handle resource file changes
    /// </summary>
    private void OnResourceFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!_isEnabled) return;

        var fileName = Path.GetFileName(e.FullPath);

        // You can customize this to reload functions, classes, etc.
        ScheduleReload("functions", () =>
        {
            _logger.Info($"Hot reloading functions due to change in: {fileName}");
            ReloadFunctions();
        });
    }

    /// <summary>
    /// Handle resource file renames
    /// </summary>
    private void OnResourceFileRenamed(object sender, RenamedEventArgs e)
    {
        if (!_isEnabled) return;

        var oldName = Path.GetFileName(e.OldFullPath);
        var newName = Path.GetFileName(e.FullPath);

        ScheduleReload("functions", () =>
        {
            _logger.Info($"Hot reloading functions due to file rename: {oldName} -> {newName}");
            ReloadFunctions();
        });
    }

    /// <summary>
    /// Schedule a reload with debouncing to avoid multiple rapid reloads
    /// </summary>
    private void ScheduleReload(string category, Action reloadAction)
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
                        _logger.Error($"Error during hot reload of {category}", ex);
                    }
                }
            }, null, TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Reload all verb definitions
    /// </summary>
    private void ReloadVerbs()
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            _logger.Info("🔄 Hot reloading verb definitions...");
            _verbInitializer.ReloadVerbs();
            
            stopwatch.Stop();
            _logger.Info($"✅ Verb hot reload completed in {stopwatch.ElapsedMilliseconds}ms");
            
            // Notify online players of the reload
            NotifyOnlinePlayersOfReload("Verb definitions have been hot reloaded!");
        }
        catch (Exception ex)
        {
            _logger.Error("❌ Failed to hot reload verbs", ex);
        }
    }

    /// <summary>
    /// Reload all function definitions
    /// </summary>
    private void ReloadFunctions()
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            _logger.Info("🔄 Hot reloading function definitions...");
            _functionInitializer.ReloadFunctions();
            
            stopwatch.Stop();
            _logger.Info($"✅ Function hot reload completed in {stopwatch.ElapsedMilliseconds}ms");
            
            // Notify online players of the reload
            NotifyOnlinePlayersOfReload("Function definitions have been hot reloaded!");
        }
        catch (Exception ex)
        {
            _logger.Error("❌ Failed to hot reload functions", ex);
        }
    }

    /// <summary>
    /// Reload script engine components
    /// </summary>
    private void ReloadScripts()
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            _logger.Info("🔄 Hot reloading script engine...");
            
            // Clear script engine cache if it has one
            // ScriptEngine.ClearCache(); // Implement this if needed
            
            stopwatch.Stop();
            _logger.Info($"✅ Script engine hot reload completed in {stopwatch.ElapsedMilliseconds}ms");
            
            // Notify online players of the reload
            NotifyOnlinePlayersOfReload("Script engine has been hot reloaded!");
        }
        catch (Exception ex)
        {
            _logger.Error("❌ Failed to hot reload scripts", ex);
        }
    }

    /// <summary>
    /// Manually trigger a hot reload of verbs
    /// </summary>
    public void ManualReloadVerbs()
    {
        _logger.Info("Manual verb reload requested");
        ReloadVerbs();
    }

    /// <summary>
    /// Manually trigger a hot reload of functions
    /// </summary>
    public void ManualReloadFunctions()
    {
        _logger.Info("Manual function reload requested");
        ReloadFunctions();
    }

    /// <summary>
    /// Manually trigger a hot reload of scripts
    /// </summary>
    public void ManualReloadScripts()
    {
        _logger.Info("Manual script reload requested");
        ReloadScripts();
    }

    /// <summary>
    /// Notify online players about a hot reload
    /// </summary>
    private void NotifyOnlinePlayersOfReload(string message)
    {
        try
        {
            var onlinePlayers = _playerManager.GetOnlinePlayers();
            foreach (var player in onlinePlayers)
            {
                if (player.SessionGuid != null)
                {
                    Builtins.Notify(player, message);
                }
            }
            
            if (onlinePlayers.Any())
            {
                _logger.Info($"Notified {onlinePlayers.Count()} online players of hot reload");
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to notify players of hot reload: {ex.Message}");
        }
    }

    /// <summary>
    /// Enable or disable hot reload functionality
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        _logger.Info($"Hot reload {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Shutdown the hot reload manager
    /// </summary>
    public void Shutdown()
    {
        _logger.Info("Shutting down Hot Reload Manager...");
        
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
        
        _logger.Info("Hot Reload Manager shutdown complete");
    }
}
