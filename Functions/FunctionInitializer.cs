using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Database;
using CSMOO.Init;
using CSMOO.Configuration;

namespace CSMOO.Functions;

/// <summary>
/// Static wrapper for FunctionInitializer (backward compatibility)
/// Delegates to FunctionInitializerInstance for dependency injection support
/// </summary>
public static class FunctionInitializer
{
    private static IFunctionInitializer? _instance;
    
    /// <summary>
    /// Sets the function initializer instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IFunctionInitializer instance)
    {
        _instance = instance;
    }
    
    private static IFunctionInitializer Instance => _instance ?? throw new InvalidOperationException("FunctionInitializer instance not set. Call FunctionInitializer.SetInstance() first.");
    
    /// <summary>
    /// Ensures an instance exists (creates default if not set)
    /// </summary>
    private static void EnsureInstance()
    {
        if (_instance == null)
        {
            // Create default instances for backward compatibility
            var dbProvider = DbProvider.Instance;
            var logger = new LoggerInstance(Config.Instance);
            var classManager = new ClassManagerInstance(dbProvider, logger);
            var objectManager = new ObjectManagerInstance(dbProvider, classManager);
            var gameDatabase = GameDatabase.Instance;
            var functionManager = new FunctionManagerInstance(gameDatabase);
            _instance = new FunctionInitializerInstance(dbProvider, logger, objectManager, functionManager);
        }
    }

    /// <summary>
    /// Loads and creates all functions from C# class definitions
    /// </summary>
    public static void LoadAndCreateFunctions()
    {
        EnsureInstance();
        Instance.LoadAndCreateFunctions();
    }

    /// <summary>
    /// Hot reload all function definitions (removes old, loads new)
    /// Only removes functions that were created from resources folder (CreatedBy == "system")
    /// </summary>
    public static void ReloadFunctions()
    {
        EnsureInstance();
        Instance.ReloadFunctions();
    }

    /// <summary>
    /// Force reload ALL function definitions (removes all functions, loads new)
    /// Use with caution - this will delete in-game created functions too!
    /// </summary>
    public static void ForceReloadAllFunctions()
    {
        EnsureInstance();
        Instance.ForceReloadAllFunctions();
    }

    /// <summary>
    /// Loads and creates functions from all C# definitions in Resources directory
    /// </summary>
    public static (int Loaded, int Skipped) LoadFunctions()
    {
        EnsureInstance();
        return Instance.LoadFunctions();
    }

}



