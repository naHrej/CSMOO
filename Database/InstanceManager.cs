using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Configuration;

namespace CSMOO.Database;

/// <summary>
/// Static wrapper for InstanceManager (backward compatibility)
/// Delegates to InstanceManagerInstance for dependency injection support
/// </summary>
public static class InstanceManager
{
    private static IInstanceManager? _instance;
    
    /// <summary>
    /// Sets the instance manager instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IInstanceManager instance)
    {
        _instance = instance;
    }
    
    private static IInstanceManager Instance => _instance ?? throw new InvalidOperationException("InstanceManager instance not set. Call InstanceManager.SetInstance() first. Static access is no longer supported - use dependency injection.");
    /// <summary>
    /// Creates an instance of a class with full inheritance chain
    /// </summary>
    public static GameObject CreateInstance(string classId, string? location = null)
    {
        return Instance.CreateInstance(classId, location);
    }

    /// <summary>
    /// Destroys an object instance
    /// </summary>
    public static bool DestroyInstance(string objectId)
    {
        return Instance.DestroyInstance(objectId);
    }

    /// <summary>
    /// Moves an object to a new location
    /// </summary>
    public static bool MoveObject(string objectId, string? newLocationId)
    {
        return Instance.MoveObject(objectId, newLocationId);
    }

    /// <summary>
    /// Moves an object to a new location (alternative signature)
    /// </summary>
    public static bool MoveObject(GameObject gameObject, GameObject newLocation)
    {
        return Instance.MoveObject(gameObject, newLocation);
    }

    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    public static List<GameObject> GetObjectsInLocation(string? locationId)
    {
        return Instance.GetObjectsInLocation(locationId);
    }
    
    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    public static List<GameObject> GetObjectsInLocation(GameObject? location)
    {
        return Instance.GetObjectsInLocation(location);
    }
    
    /// <summary>
    /// Gets all objects of a specific class type (including inheritance)
    /// </summary>
    public static List<GameObject> FindObjectsByClass(string classId, bool includeSubclasses = true)
    {
        return Instance.FindObjectsByClass(classId, includeSubclasses);
    }

    /// <summary>
    /// Gets basic statistics about objects in the database
    /// </summary>
    public static Dictionary<string, int> GetObjectStatistics()
    {
        return Instance.GetObjectStatistics();
    }
}



