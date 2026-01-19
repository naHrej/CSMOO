using System.Reflection;
using CSMOO.Core;
using CSMOO.Logging;
using CSMOO.Object;

namespace CSMOO.Init;

/// <summary>
/// Instance-based core hot reload manager implementation for dependency injection
/// </summary>
public class CoreHotReloadManagerInstance : ICoreHotReloadManager
{
    private readonly ILogger _logger;
    private readonly IPlayerManager _playerManager;
    private readonly IPermissionManager _permissionManager;
    
    private FileSystemWatcher? _coreCodeWatcher;
    private readonly object _reloadLock = new();
    private Timer? _debounceTimer;
    private bool _isWatchingCoreCode = false;
    private bool _isDevelopmentMode = false;
    
    public CoreHotReloadManagerInstance(
        ILogger logger,
        IPlayerManager playerManager,
        IPermissionManager permissionManager)
    {
        _logger = logger;
        _playerManager = playerManager;
        _permissionManager = permissionManager;
    }
    
    /// <summary>
    /// Initialize core application hot reload capabilities
    /// </summary>
    public void Initialize()
    {
        _logger.Info("Initializing Core Hot Reload Manager...");

        // Check if we're in development mode
        _isDevelopmentMode = IsInDevelopmentMode();

        if (_isDevelopmentMode)
        {
            _logger.Info("🔥 Development mode detected - enabling core code hot reload");
            SetupCoreCodeWatcher();
            
            // Set up hot reload event handlers if available
            SetupHotReloadHandlers();
        }
        else
        {
            _logger.Info("Production mode detected - core code hot reload disabled");
        }
    }

    /// <summary>
    /// Check if we're running in development mode
    /// </summary>
    private static bool IsInDevelopmentMode()
    {
        // Check various indicators that we're in development
        var isDevelopment = 
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development" ||
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ||
            Directory.Exists("Server") || // Source code directory exists
            File.Exists("CSMOO.csproj") || // Project file exists
            System.Diagnostics.Debugger.IsAttached; // Debugger attached

        return isDevelopment;
    }

    /// <summary>
    /// Setup file watcher for core C# application files
    /// </summary>
    private void SetupCoreCodeWatcher()
    {
        try
        {
            // Watch the Server directory and root for C# files
            var watchPaths = new List<string>();
            
            if (Directory.Exists("Server"))
                watchPaths.Add("Server");
            
            // Also watch root directory for any C# files
            if (Directory.GetFiles(".", "*.cs").Length > 0)
                watchPaths.Add(".");

            foreach (var path in watchPaths)
            {
                var watcher = new FileSystemWatcher(path)
                {
                    Filter = "*.cs",
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
                };

                watcher.Changed += OnCoreCodeChanged;
                watcher.Created += OnCoreCodeChanged;
                watcher.EnableRaisingEvents = true;
            }

            _isWatchingCoreCode = true;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to setup core code watcher: {ex.Message}");
        }
    }

    /// <summary>
    /// Setup .NET Hot Reload event handlers
    /// </summary>
    private void SetupHotReloadHandlers()
    {
        try
        {
            // Try to hook into .NET's hot reload events if available
            // This uses reflection to access internal APIs safely

            var hotReloadType = Type.GetType("System.Reflection.Metadata.MetadataUpdater, System.Private.CoreLib");
            if (hotReloadType != null)
            {
                // Look for hot reload events
                var beforeUpdateEvent = hotReloadType.GetEvent("BeforeUpdate", BindingFlags.Static | BindingFlags.Public);
                var afterUpdateEvent = hotReloadType.GetEvent("AfterUpdate", BindingFlags.Static | BindingFlags.Public);

                if (beforeUpdateEvent != null)
                {
                    var handler = new Action<Type[]>(OnBeforeHotReload);
                    beforeUpdateEvent.AddEventHandler(null, handler);
                }

                if (afterUpdateEvent != null)
                {
                    var handler = new Action<Type[]>(OnAfterHotReload);
                    afterUpdateEvent.AddEventHandler(null, handler);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to setup .NET Hot Reload handlers: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle core code file changes
    /// </summary>
    private void OnCoreCodeChanged(object sender, FileSystemEventArgs e)
    {
        if (!_isDevelopmentMode) return;

        var fileName = Path.GetFileName(e.FullPath);
        
        // Ignore temporary files and files that shouldn't trigger reloads
        if (fileName.StartsWith(".") || 
            fileName.Contains("~") || 
            fileName.EndsWith(".tmp") ||
            fileName.EndsWith(".swp"))
        {
            return;
        }

        ScheduleCoreReload(() =>
        {
            _logger.Info($"🔄 Core code change detected in: {fileName}");
            _logger.Info("💡 To apply changes, use 'dotnet watch' or restart with hot reload enabled");
            
            // Notify administrators
            NotifyAdminsOfCodeChange(fileName);
        });
    }

    /// <summary>
    /// Called before .NET hot reload occurs
    /// </summary>
    private void OnBeforeHotReload(Type[] updatedTypes)
    {
        _logger.Info("🔄 .NET Hot Reload starting...");
        
        if (updatedTypes?.Length > 0)
        {
            _logger.Info($"📝 Updating {updatedTypes.Length} type(s):");
            foreach (var type in updatedTypes)
            {
                _logger.Info($"  • {type.FullName}");
            }
        }
    }

    /// <summary>
    /// Called after .NET hot reload completes
    /// </summary>
    private void OnAfterHotReload(Type[] updatedTypes)
    {
        _logger.Info("✅ .NET Hot Reload completed successfully!");
        
        // Notify online admins
        try
        {
            var message = $"🔥 Core application code has been hot reloaded! ({updatedTypes?.Length ?? 0} types updated)";
            NotifyOnlineAdmins(message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to notify admins of hot reload: {ex.Message}");
        }
    }

    /// <summary>
    /// Schedule a core reload notification with debouncing
    /// </summary>
    private void ScheduleCoreReload(Action reloadAction)
    {
        lock (_reloadLock)
        {
            // Cancel existing timer
            _debounceTimer?.Dispose();
            
            // Start new timer with 1 second delay (longer for core code)
            _debounceTimer = new Timer(_ =>
            {
                lock (_reloadLock)
                {
                    try
                    {
                        reloadAction();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error during core code change notification: {ex.Message}");
                    }
                }
            }, null, TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Notify administrators of code changes
    /// </summary>
    private void NotifyAdminsOfCodeChange(string fileName)
    {
        try
        {
            var message = $"📝 Core code file changed: {fileName}";
            
            if (_isDevelopmentMode)
            {
                message += "\n💡 Tip: Use 'dotnet watch run' for automatic hot reload";
            }
            
            NotifyOnlineAdmins(message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to notify admins of code change: {ex.Message}");
        }
    }

    /// <summary>
    /// Notify online administrators
    /// </summary>
    private void NotifyOnlineAdmins(string message)
    {
        try
        {
            var onlinePlayers = _playerManager.GetOnlinePlayers();
            var adminPlayers = onlinePlayers.Where(p => _permissionManager.HasFlag(p, PermissionManager.Flag.Admin));
            
            foreach (var admin in adminPlayers)
            {
                if (admin.SessionGuid != null)
                {
                    Builtins.Notify(admin, message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to notify admins: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if .NET hot reload is available and active
    /// </summary>
    public bool IsHotReloadSupported()
    {
        try
        {
            // Check if hot reload is available
            var hotReloadType = Type.GetType("System.Reflection.Metadata.MetadataUpdater, System.Private.CoreLib");
            return hotReloadType != null && _isDevelopmentMode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get the current status of core hot reload
    /// </summary>
    public string GetStatus()
    {
        if (!_isDevelopmentMode)
        {
            return "❌ Core hot reload disabled (Production mode)";
        }

        var status = "🔥 Core hot reload enabled (Development mode)\n";
        
        if (IsHotReloadSupported())
        {
            status += "✅ .NET Hot Reload APIs available\n";
        }
        else
        {
            status += "⚠️ .NET Hot Reload APIs not available - file watching only\n";
        }

        if (_isWatchingCoreCode)
        {
            status += "👀 Watching core C# files for changes\n";
        }

        status += "\n💡 For best experience, run with: dotnet watch run";

        return status;
    }

    /// <summary>
    /// Force trigger a hot reload notification (for testing)
    /// </summary>
    public void TriggerTestNotification()
    {
        if (!_isDevelopmentMode)
        {
            _logger.Warning("Cannot trigger test notification in production mode");
            return;
        }

        _logger.Info("🧪 Triggering test hot reload notification...");
        OnAfterHotReload(new[] { typeof(CoreHotReloadManager) });
    }

    /// <summary>
    /// Shutdown the core hot reload manager
    /// </summary>
    public void Shutdown()
    {
        if (_isDevelopmentMode)
        {
            _logger.Info("Shutting down Core Hot Reload Manager...");
        }
        
        _coreCodeWatcher?.Dispose();
        _coreCodeWatcher = null;
        
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        
        _isWatchingCoreCode = false;
    }
}
