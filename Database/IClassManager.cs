using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Interface for class management operations
/// </summary>
public interface IClassManager
{
    /// <summary>
    /// Creates a new object class definition
    /// </summary>
    ObjectClass CreateClass(string name, string? parentClassId = null, string description = "");
    
    /// <summary>
    /// Gets the full inheritance chain for a class (from root to target class)
    /// </summary>
    List<ObjectClass> GetInheritanceChain(string classId);
    
    /// <summary>
    /// Checks if a class inherits from another class (directly or indirectly)
    /// </summary>
    bool InheritsFrom(string childClassId, string parentClassId);
    
    /// <summary>
    /// Gets all classes that inherit from a given class
    /// </summary>
    List<ObjectClass> GetSubclasses(string parentClassId, bool recursive = true);
    
    /// <summary>
    /// Deletes a class and optionally its subclasses
    /// </summary>
    bool DeleteClass(string classId, bool deleteSubclasses = false);
    
    /// <summary>
    /// Updates a class definition
    /// </summary>
    bool UpdateClass(ObjectClass objectClass);
    
    /// <summary>
    /// Finds classes by name (case-insensitive)
    /// </summary>
    List<ObjectClass> FindClassesByName(string name, bool exactMatch = false);
}
