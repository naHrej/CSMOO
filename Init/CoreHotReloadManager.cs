using System.Reflection;
using CSMOO.Core;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Configuration;
using CSMOO.Database;

namespace CSMOO.Init;

/// <summary>
/// Static wrapper for CoreHotReloadManager (backward compatibility)
/// Delegates to CoreHotReloadManagerInstance for dependency injection support
/// </summary>
public static class CoreHotReloadManager
{
    private static ICoreHotReloadManager? _instance;
    
    /// <summary>
    /// Sets the core hot reload manager instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(ICoreHotReloadManager instance)
    {
        _instance = instance;
    }
    
    private static ICoreHotReloadManager Instance => _instance ?? throw new InvalidOperationException("CoreHotReloadManager instance not set. Call CoreHotReloadManager.SetInstance() first.");
    
    /// <summary>
    /// Ensures an instance exists (creates default if not set)
    /// </summary>
    private static void EnsureInstance()
    {
        if (_instance == null)
        {
            // Create default instances for backward compatibility
            var config = Config.Instance;
            var logger = new LoggerInstance(config);
            var dbProvider = DbProvider.Instance;
            var playerManager = new PlayerManagerInstance(dbProvider);
            var permissionManager = new PermissionManagerInstance(dbProvider, logger);
            _instance = new CoreHotReloadManagerInstance(logger, playerManager, permissionManager);
        }
    }

    /// <summary>
    /// Initialize core application hot reload capabilities
    /// </summary>
    public static void Initialize()
    {
        EnsureInstance();
        Instance.Initialize();
    }

    /// <summary>
    /// Check if .NET hot reload is available and active
    /// </summary>
    public static bool IsHotReloadSupported()
    {
        EnsureInstance();
        return Instance.IsHotReloadSupported();
    }

    /// <summary>
    /// Get the current status of core hot reload
    /// </summary>
    public static string GetStatus()
    {
        EnsureInstance();
        return Instance.GetStatus();
    }

    /// <summary>
    /// Force trigger a hot reload notification (for testing)
    /// </summary>
    public static void TriggerTestNotification()
    {
        EnsureInstance();
        Instance.TriggerTestNotification();
    }

    /// <summary>
    /// Shutdown the core hot reload manager
    /// </summary>
    public static void Shutdown()
    {
        if (_instance != null)
        {
            Instance.Shutdown();
        }
    }
}
