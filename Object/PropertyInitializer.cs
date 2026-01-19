using LiteDB;
using CSMOO.Logging;
using CSMOO.Database;
using CSMOO.Init;
using CSMOO.Configuration;

namespace CSMOO.Object;

/// <summary>
/// Static wrapper for PropertyInitializer (backward compatibility)
/// Delegates to PropertyInitializerInstance for dependency injection support
/// </summary>
public static class PropertyInitializer
{
    private static IPropertyInitializer? _instance;
    
    /// <summary>
    /// Sets the property initializer instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IPropertyInitializer instance)
    {
        _instance = instance;
    }
    
    private static IPropertyInitializer Instance => _instance ?? throw new InvalidOperationException("PropertyInitializer instance not set. Call PropertyInitializer.SetInstance() first.");
    
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
            _instance = new PropertyInitializerInstance(dbProvider, logger, objectManager);
        }
    }

    /// <summary>
    /// Loads and sets all properties from C# class definitions
    /// </summary>
    public static void LoadAndSetProperties()
    {
        EnsureInstance();
        Instance.LoadAndSetProperties();
    }

    /// <summary>
    /// Hot reload all property definitions
    /// </summary>
    public static void ReloadProperties()
    {
        EnsureInstance();
        Instance.ReloadProperties();
    }

    /// <summary>
    /// Loads and creates properties from all C# class definitions in Resources directory
    /// </summary>
    public static (int Loaded, int Skipped) LoadProperties()
    {
        EnsureInstance();
        return Instance.LoadProperties();
    }

}
