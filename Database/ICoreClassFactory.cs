using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Interface for core class factory operations
/// </summary>
public interface ICoreClassFactory
{
    /// <summary>
    /// Creates the fundamental object classes that everything inherits from
    /// </summary>
    void CreateCoreClasses();
    
    /// <summary>
    /// Gets the base Object class
    /// </summary>
    ObjectClass? GetBaseObjectClass();
    
    /// <summary>
    /// Gets a core class by name
    /// </summary>
    ObjectClass? GetCoreClass(string className);
}
