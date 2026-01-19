using CSMOO.Core;
using CSMOO.Functions;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Verbs;
using CSMOO.Configuration;
using CSMOO.Database;

namespace CSMOO.Init;

/// <summary>
/// Static wrapper for HotReloadManager (backward compatibility)
/// Delegates to HotReloadManagerInstance for dependency injection support
/// </summary>
public static class HotReloadManager
{
    private static IHotReloadManager? _instance;
    
    /// <summary>
    /// Sets the hot reload manager instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IHotReloadManager instance)
    {
        _instance = instance;
    }
    
    private static IHotReloadManager Instance => _instance ?? throw new InvalidOperationException("HotReloadManager instance not set. Call HotReloadManager.SetInstance() first.");
    
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
            var classManager = new ClassManagerInstance(dbProvider, logger);
            var objectManager = new ObjectManagerInstance(dbProvider, classManager);
            var playerManager = new PlayerManagerInstance(dbProvider);
            var verbInitializer = new VerbInitializerInstance(dbProvider, logger, objectManager);
            var gameDatabase = GameDatabase.Instance;
            var functionManager = new FunctionManagerInstance(gameDatabase);
            var functionInitializer = new FunctionInitializerInstance(dbProvider, logger, objectManager, functionManager);
            _instance = new HotReloadManagerInstance(logger, config, verbInitializer, functionInitializer, playerManager);
        }
    }

    /// <summary>
    /// Initialize hot reload functionality
    /// </summary>
    public static void Initialize()
    {
        EnsureInstance();
        Instance.Initialize();
    }

    /// <summary>
    /// Manually trigger a hot reload of verbs
    /// </summary>
    public static void ManualReloadVerbs()
    {
        EnsureInstance();
        Instance.ManualReloadVerbs();
    }

    /// <summary>
    /// Manually trigger a hot reload of functions
    /// </summary>
    public static void ManualReloadFunctions()
    {
        EnsureInstance();
        Instance.ManualReloadFunctions();
    }

    /// <summary>
    /// Manually trigger a hot reload of scripts
    /// </summary>
    public static void ManualReloadScripts()
    {
        EnsureInstance();
        Instance.ManualReloadScripts();
    }

    /// <summary>
    /// Enable or disable hot reload functionality
    /// </summary>
    public static void SetEnabled(bool enabled)
    {
        EnsureInstance();
        Instance.SetEnabled(enabled);
    }

    /// <summary>
    /// Get the current status of hot reload
    /// </summary>
    public static bool IsEnabled
    {
        get
        {
            EnsureInstance();
            return Instance.IsEnabled;
        }
    }

    /// <summary>
    /// Shutdown the hot reload manager
    /// </summary>
    public static void Shutdown()
    {
        if (_instance != null)
        {
            Instance.Shutdown();
        }
    }
}
