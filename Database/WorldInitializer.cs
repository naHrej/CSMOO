using CSMOO.Functions;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Verbs;
using CSMOO.Configuration;

namespace CSMOO.Database;

/// <summary>
/// Static wrapper for WorldInitializer (backward compatibility)
/// Delegates to WorldInitializerInstance for dependency injection support
/// </summary>
public static class WorldInitializer
{
    private static IWorldInitializer? _instance;
    
    /// <summary>
    /// Sets the world initializer instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IWorldInitializer instance)
    {
        _instance = instance;
    }
    
    private static IWorldInitializer Instance => _instance ?? throw new InvalidOperationException("WorldInitializer instance not set. Call WorldInitializer.SetInstance() first. Static access is no longer supported - use dependency injection.");
    /// <summary>
    /// Initializes the basic world structure with core classes
    /// </summary>
    public static void InitializeWorld()
    {
        Instance.InitializeWorld();
    }

    /// <summary>
    /// Gets basic world statistics for display
    /// </summary>
    public static void PrintWorldStatistics()
    {
        Instance.PrintWorldStatistics();
    }
}



