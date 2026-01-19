using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Database;
using CSMOO.Init;
using CSMOO.Configuration;

namespace CSMOO.Verbs;

/// <summary>
/// Statistics for verb loading operations
/// </summary>
public struct VerbLoadStats
{
    public int Loaded { get; set; }
    public int Skipped { get; set; }
}

/// <summary>
/// Static wrapper for VerbInitializer (backward compatibility)
/// Delegates to VerbInitializerInstance for dependency injection support
/// </summary>
public static class VerbInitializer
{
    private static IVerbInitializer? _instance;
    
    /// <summary>
    /// Sets the verb initializer instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IVerbInitializer instance)
    {
        _instance = instance;
    }
    
    private static IVerbInitializer Instance => _instance ?? throw new InvalidOperationException("VerbInitializer instance not set. Call VerbInitializer.SetInstance() first.");
    
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
            _instance = new VerbInitializerInstance(dbProvider, logger, objectManager);
        }
    }

    /// <summary>
    /// Loads and creates all verbs from C# class definitions
    /// </summary>
    public static void LoadAndCreateVerbs()
    {
        EnsureInstance();
        Instance.LoadAndCreateVerbs();
    }

    /// <summary>
    /// Hot reload all verb definitions (removes old, loads new)
    /// Only removes verbs that were created from resources folder (CreatedBy == "system")
    /// </summary>
    public static void ReloadVerbs()
    {
        EnsureInstance();
        Instance.ReloadVerbs();
    }

    /// <summary>
    /// Force reload ALL verb definitions (removes all verbs, loads new)
    /// Use with caution - this will delete in-game created verbs too!
    /// </summary>
    public static void ForceReloadAllVerbs()
    {
        EnsureInstance();
        Instance.ForceReloadAllVerbs();
    }

}



