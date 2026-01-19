using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Configuration;

namespace CSMOO.Database;

/// <summary>
/// Static wrapper for CoreClassFactory (backward compatibility)
/// Delegates to CoreClassFactoryInstance for dependency injection support
/// </summary>
public static class CoreClassFactory
{
    private static ICoreClassFactory? _instance;
    
    /// <summary>
    /// Sets the core class factory instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(ICoreClassFactory instance)
    {
        _instance = instance;
    }
    
    private static ICoreClassFactory Instance => _instance ?? throw new InvalidOperationException("CoreClassFactory instance not set. Call CoreClassFactory.SetInstance() first. Static access is no longer supported - use dependency injection.");
    
    /// <summary>
    /// Creates the fundamental object classes that everything inherits from
    /// </summary>
    public static void CreateCoreClasses()
    {
        Instance.CreateCoreClasses();
    }


    /// <summary>
    /// Gets the base Object class
    /// </summary>
    public static ObjectClass? GetBaseObjectClass()
    {
        return Instance.GetBaseObjectClass();
    }

    /// <summary>
    /// Gets a core class by name
    /// </summary>
    public static ObjectClass? GetCoreClass(string className)
    {
        return Instance.GetCoreClass(className);
    }
}



