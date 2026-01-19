using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Interface for instance management operations
/// </summary>
public interface IInstanceManager
{
    /// <summary>
    /// Creates an instance of a class with full inheritance chain
    /// </summary>
    GameObject CreateInstance(string classId, string? location = null);
    
    /// <summary>
    /// Destroys an object instance
    /// </summary>
    bool DestroyInstance(string objectId);
    
    /// <summary>
    /// Moves an object to a new location
    /// </summary>
    bool MoveObject(string objectId, string? newLocationId);
    
    /// <summary>
    /// Moves an object to a new location (alternative signature)
    /// </summary>
    bool MoveObject(GameObject gameObject, GameObject newLocation);
    
    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    List<GameObject> GetObjectsInLocation(string? locationId);
    
    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    List<GameObject> GetObjectsInLocation(GameObject? location);
    
    /// <summary>
    /// Gets all objects of a specific class type (including inheritance)
    /// </summary>
    List<GameObject> FindObjectsByClass(string classId, bool includeSubclasses = true);
    
    /// <summary>
    /// Gets basic statistics about objects in the database
    /// </summary>
    Dictionary<string, int> GetObjectStatistics();
}
