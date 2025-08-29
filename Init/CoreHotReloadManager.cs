using System.Reflection;
using CSMOO.Core;
using CSMOO.Logging;
using CSMOO.Object;

namespace CSMOO.Init;

/// <summary>
/// Enhanced hot reload manager that supports .NET application code hot reload
/// in addition to verb and script reloading
/// </summary>
public static class CoreHotReloadManager
{
    private static FileSystemWatcher? _coreCodeWatcher;
    private static readonly object _reloadLock = new();
    private static Timer? _debounceTimer;
    private static bool _isWatchingCoreCode = false;
    private static bool _isDevelopmentMode = false;

    /// <summary>
    /// Initialize core application hot reload capabilities
    /// </summary>
    public static void Initialize()
    {
        Logger.Info("Initializing Core Hot Reload Manager...");

        // Check if we're in development mode
        _isDevelopmentMode = IsInDevelopmentMode();

        if (_isDevelopmentMode)
        {
            Logger.Info("🔥 Development mode detected - enabling core code hot reload");
            SetupCoreCodeWatcher();
            
            // Set up hot reload event handlers if available
            SetupHotReloadHandlers();
        }
        else
        {
            Logger.Info("Production mode detected - core code hot reload disabled");
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
    private static void SetupCoreCodeWatcher()
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
            Logger.Warning($"Failed to setup core code watcher: {ex.Message}");
        }
    }

    /// <summary>
    /// Setup .NET Hot Reload event handlers
    /// </summary>
    private static void SetupHotReloadHandlers()
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
            Logger.Error($"Failed to setup .NET Hot Reload handlers: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle core code file changes
    /// </summary>
    private static void OnCoreCodeChanged(object sender, FileSystemEventArgs e)
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
            Logger.Info($"🔄 Core code change detected in: {fileName}");
            Logger.Info("💡 To apply changes, use 'dotnet watch' or restart with hot reload enabled");
            
            // Notify administrators
            NotifyAdminsOfCodeChange(fileName);
        });
    }

    /// <summary>
    /// Called before .NET hot reload occurs
    /// </summary>
    private static void OnBeforeHotReload(Type[] updatedTypes)
    {
        Logger.Info("🔄 .NET Hot Reload starting...");
        
        if (updatedTypes?.Length > 0)
        {
            Logger.Info($"📝 Updating {updatedTypes.Length} type(s):");
            foreach (var type in updatedTypes)
            {
                Logger.Info($"  • {type.FullName}");
            }
        }
    }

    /// <summary>
    /// Called after .NET hot reload completes
    /// </summary>
    private static void OnAfterHotReload(Type[] updatedTypes)
    {
        Logger.Info("✅ .NET Hot Reload completed successfully!");
        
        // Notify online admins
        try
        {
            var message = $"🔥 Core application code has been hot reloaded! ({updatedTypes?.Length ?? 0} types updated)";
            NotifyOnlineAdmins(message);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to notify admins of hot reload: {ex.Message}");
        }
    }

    /// <summary>
    /// Schedule a core reload notification with debouncing
    /// </summary>
    private static void ScheduleCoreReload(Action reloadAction)
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
                        Logger.Error($"Error during core code change notification: {ex.Message}");
                    }
                }
            }, null, TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Notify administrators of code changes
    /// </summary>
    private static void NotifyAdminsOfCodeChange(string fileName)
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
            Logger.Error($"Failed to notify admins of code change: {ex.Message}");
        }
    }

    /// <summary>
    /// Notify online administrators
    /// </summary>
    private static void NotifyOnlineAdmins(string message)
    {
        try
        {
            var onlinePlayers = PlayerManager.GetOnlinePlayers();
            var adminPlayers = onlinePlayers.Where(p => PermissionManager.HasFlag(p, PermissionManager.Flag.Admin));
            
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
            Logger.Error($"Failed to notify admins: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if .NET hot reload is available and active
    /// </summary>
    public static bool IsHotReloadSupported()
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
    public static string GetStatus()
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
    public static void TriggerTestNotification()
    {
        if (!_isDevelopmentMode)
        {
            Logger.Warning("Cannot trigger test notification in production mode");
            return;
        }

        Logger.Info("🧪 Triggering test hot reload notification...");
        OnAfterHotReload(new[] { typeof(CoreHotReloadManager) });
    }

    /// <summary>
    /// Shutdown the core hot reload manager
    /// </summary>
    public static void Shutdown()
    {
        if (_isDevelopmentMode)
        {
            Logger.Info("Shutting down Core Hot Reload Manager...");
        }
        
        _coreCodeWatcher?.Dispose();
        _coreCodeWatcher = null;
        
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        
        _isWatchingCoreCode = false;
    }
}
