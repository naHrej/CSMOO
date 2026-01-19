using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Configuration;

namespace CSMOO.Database;

/// <summary>
/// Static wrapper for ClassManager (backward compatibility)
/// Delegates to ClassManagerInstance for dependency injection support
/// </summary>
public static class ClassManager
{
    private static IClassManager? _instance;
    
    /// <summary>
    /// Sets the class manager instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IClassManager instance)
    {
        _instance = instance;
    }
    
    private static IClassManager Instance => _instance ?? throw new InvalidOperationException("ClassManager instance not set. Call ClassManager.SetInstance() first. Static access is no longer supported - use dependency injection.");
    /// <summary>
    /// Creates a new object class definition
    /// </summary>
    public static ObjectClass CreateClass(string name, string? parentClassId = null, string description = "")
    {
        return Instance.CreateClass(name, parentClassId, description);
    }

    /// <summary>
    /// Gets the full inheritance chain for a class (from root to target class)
    /// </summary>
    public static List<ObjectClass> GetInheritanceChain(string classId)
    {
        return Instance.GetInheritanceChain(classId);
    }

    /// <summary>
    /// Checks if a class inherits from another class (directly or indirectly)
    /// </summary>
    public static bool InheritsFrom(string childClassId, string parentClassId)
    {
        return Instance.InheritsFrom(childClassId, parentClassId);
    }

    /// <summary>
    /// Gets all classes that inherit from a given class
    /// </summary>
    public static List<ObjectClass> GetSubclasses(string parentClassId, bool recursive = true)
    {
        return Instance.GetSubclasses(parentClassId, recursive);
    }

    /// <summary>
    /// Deletes a class and optionally its subclasses
    /// </summary>
    public static bool DeleteClass(string classId, bool deleteSubclasses = false)
    {
        return Instance.DeleteClass(classId, deleteSubclasses);
    }

    /// <summary>
    /// Updates a class definition
    /// </summary>
    public static bool UpdateClass(ObjectClass objectClass)
    {
        return Instance.UpdateClass(objectClass);
    }

    /// <summary>
    /// Finds classes by name (case-insensitive)
    /// </summary>
    public static List<ObjectClass> FindClassesByName(string name, bool exactMatch = false)
    {
        return Instance.FindClassesByName(name, exactMatch);
    }
}



