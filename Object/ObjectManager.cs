using LiteDB;
using CSMOO.Logging;
using CSMOO.Database;
using CSMOO.Verbs;

namespace CSMOO.Object;
/// <summary>
/// Static wrapper for ObjectManager (backward compatibility)
/// Delegates to ObjectManagerInstance for dependency injection support
/// </summary>
public static class ObjectManager
{
    private static IObjectManager? _instance;
    
    /// <summary>
    /// Sets the object manager instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IObjectManager instance)
    {
        _instance = instance;
    }
    
    private static IObjectManager Instance => _instance ?? throw new InvalidOperationException("ObjectManager instance not set. Call ObjectManager.SetInstance() first.");
    
    /// <summary>
    /// Ensures an instance exists (creates default if not set)
    /// </summary>
    private static void EnsureInstance()
    {
        if (_instance == null)
        {
            _instance = new ObjectManagerInstance(DbProvider.Instance);
        }
    }

    /// <summary>
    /// Loads all GameObjects from the database into the singleton cache at startup.
    /// </summary>
    public static void LoadAllObjectsToCache()
    {
        EnsureInstance();
        Instance.LoadAllObjectsToCache();
    }

    /// <summary>
    /// Loads all ObjectClasses from the database into the singleton cache at startup.
    /// </summary>
    public static void LoadAllObjectClassesToCache()
    {
        EnsureInstance();
        Instance.LoadAllObjectClassesToCache();
    }

    /// <summary>
    /// Adds a GameObject to the singleton cache, or returns the cached instance if already present.
    /// </summary>
    public static GameObject? CacheGameObject(GameObject obj)
    {
        EnsureInstance();
        return Instance.CacheGameObject(obj);
    }

    /// <summary>
    /// Adds an ObjectClass to the singleton cache, or returns the cached instance if already present.
    /// </summary>
    public static ObjectClass? CacheObjectClass(ObjectClass objClass)
    {
        EnsureInstance();
        return Instance.CacheObjectClass(objClass);
    }
    #region Class Management (delegated to ClassManager)
    
    /// <summary>
    /// Creates a new object class definition
    /// </summary>
    public static ObjectClass CreateClass(string name, string? parentClassId = null, string description = "")
    {
        EnsureInstance();
        return Instance.CreateClass(name, parentClassId, description);
    }

    /// <summary>
    /// Gets the full inheritance chain for a class (from root to target class)
    /// </summary>
    public static List<ObjectClass> GetInheritanceChain(string classId)
    {
        EnsureInstance();
        return Instance.GetInheritanceChain(classId);
    }

    /// <summary>
    /// Checks if a class inherits from another class (directly or indirectly)
    /// </summary>
    public static bool InheritsFrom(string childClassId, string parentClassId)
    {
        EnsureInstance();
        return Instance.InheritsFrom(childClassId, parentClassId);
    }

    /// <summary>
    /// Gets all classes that inherit from a given class
    /// </summary>
    public static List<ObjectClass> GetSubclasses(string parentClassId, bool recursive = true)
    {
        EnsureInstance();
        return Instance.GetSubclasses(parentClassId, recursive);
    }

    /// <summary>
    /// Deletes a class and optionally its subclasses
    /// </summary>
    public static bool DeleteClass(string classId, bool deleteSubclasses = false)
    {
        EnsureInstance();
        return Instance.DeleteClass(classId, deleteSubclasses);
    }

    /// <summary>
    /// Updates a class definition
    /// </summary>
    public static bool UpdateClass(ObjectClass objectClass)
    {
        EnsureInstance();
        return Instance.UpdateClass(objectClass);
    }

    /// <summary>
    /// Finds classes by name (case-insensitive)
    /// </summary>
    public static List<ObjectClass> FindClassesByName(string name, bool exactMatch = false)
    {
        EnsureInstance();
        return Instance.FindClassesByName(name, exactMatch);
    }

    #endregion

    #region Instance Management (delegated to InstanceManager)

    /// <summary>
    /// Creates an instance of a class with full inheritance chain
    /// </summary>
    public static GameObject CreateInstance(string classId, string? location = null)
    {
        EnsureInstance();
        return Instance.CreateInstance(classId, location);
    }

    /// <summary>
    /// Destroys an object instance
    /// </summary>
    public static bool DestroyInstance(string objectId)
    {
        EnsureInstance();
        return Instance.DestroyInstance(objectId);
    }

    /// <summary>
    /// Moves an object to a new location
    /// </summary>
    public static bool MoveObject(string objectId, string? newLocationId)
    {
        EnsureInstance();
        return Instance.MoveObject(objectId, newLocationId);
    }

    /// <summary>
    /// Moves an object to a new location (alternative signature)
    /// </summary>
    public static bool MoveObject(GameObject gameObject, GameObject newLocation)
    {
        EnsureInstance();
        return Instance.MoveObject(gameObject, newLocation);
    }

    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    public static List<GameObject> GetObjectsInLocation(string? locationId)
    {
        EnsureInstance();
        return Instance.GetObjectsInLocation(locationId);
    }
    
    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    public static List<GameObject> GetObjectsInLocation(GameObject? location)
    {
        EnsureInstance();
        return Instance.GetObjectsInLocation(location);
    }

    /// <summary>
    /// Gets all objects of a specific class type (including inheritance)
    /// </summary>
    public static List<GameObject> FindObjectsByClass(string classId, bool includeSubclasses = true)
    {
        EnsureInstance();
        return Instance.FindObjectsByClass(classId, includeSubclasses);
    }

    /// <summary>
    /// Finds an object by its DbRef number
    /// </summary>
    public static GameObject? FindByDbRef(int dbRef)
    {
        EnsureInstance();
        return Instance.FindByDbRef(dbRef);
    }

    /// <summary>
    /// Gets basic statistics about objects in the database
    /// </summary>
    public static Dictionary<string, int> GetObjectStatistics()
    {
        EnsureInstance();
        return Instance.GetObjectStatistics();
    }

    /// <summary>
    /// Gets an object by ID
    /// </summary>
    public static GameObject? GetObject(string objectId)
    {
        EnsureInstance();
        return Instance.GetObject(objectId);
    }
    
    /// <summary>
    /// Gets a typed object by ID
    /// </summary>
    public static T? GetObject<T>(string objectId) where T : GameObject
    {
        EnsureInstance();
        return Instance.GetObject<T>(objectId);
    }

    /// <summary>
    /// Gets a GameObject by DbRef number
    /// </summary>
    public static GameObject? GetObjectByDbRef(int dbRef)
    {
        EnsureInstance();
        return Instance.GetObjectByDbRef(dbRef);
    }

    public static ObjectClass? GetClass(string classId)
    {
        EnsureInstance();
        return Instance.GetClass(classId);
    }
    
    public static ObjectClass? GetClassByName(string className)
    {
        EnsureInstance();
        return Instance.GetClassByName(className);
    }
    #endregion

    #region Property Management (delegated to PropertyManager)

    /// <summary>
    /// Gets a property value from an object, checking inheritance chain if not found on instance
    /// </summary>
    public static BsonValue? GetProperty(GameObject gameObject, string propertyName)
    {
        EnsureInstance();
        return Instance.GetProperty(gameObject, propertyName);
    }

    /// <summary>
    /// Get all objects in the cache
    /// </summary>
    public static List<dynamic> GetAllObjects()
    {
        EnsureInstance();
        return Instance.GetAllObjects();
    }

    /// <summary>
    /// Get all object classes in the cache
    /// </summary>
    public static List<ObjectClass> GetAllObjectClasses()
    {
        EnsureInstance();
        return Instance.GetAllObjectClasses();
    }

  

    /// <summary>
    /// Sets a property value on an object instance
    /// </summary>
    public static void SetProperty(GameObject gameObject, string propertyName, BsonValue value)
    {
        EnsureInstance();
        Instance.SetProperty(gameObject, propertyName, value);
    }

    public static bool UpdateObject(GameObject gameObject)
    {
        EnsureInstance();
        return Instance.UpdateObject(gameObject);
    }
    
    /// <summary>
    /// Removes a property from an object instance
    /// </summary>
    public static bool RemoveProperty(GameObject gameObject, string propertyName)
    {
        EnsureInstance();
        return Instance.RemoveProperty(gameObject, propertyName);
    }

    public static string[] GetPropertyNames(GameObject gameObject)
    {
        EnsureInstance();
        return Instance.GetPropertyNames(gameObject);
    }
    
    /// <summary>
    /// Forces a reload of a GameObject from the database, replacing the cached instance.
    /// </summary>
    public static GameObject? ReloadObject(string objectId)
    {
        EnsureInstance();
        return Instance.ReloadObject(objectId);
    }

    /// <summary>
    /// Checks if an object has a property (including inherited properties)
    /// </summary>
    public static bool HasProperty(GameObject gameObject, string propertyName)
    {
        EnsureInstance();
        return Instance.HasProperty(gameObject, propertyName);
    }

    /// <summary>
    /// Gets all property names from an object (including inherited properties)
    /// </summary>
    public static string[] GetAllPropertyNames(GameObject gameObject)
    {
        EnsureInstance();
        return Instance.GetAllPropertyNames(gameObject);
    }

    /// <summary>
    /// Gets the effective value of a property (resolved through inheritance)
    /// </summary>
    public static T? GetPropertyValue<T>(GameObject gameObject, string propertyName, T? defaultValue = default)
    {
        EnsureInstance();
        return Instance.GetPropertyValue(gameObject, propertyName, defaultValue);
    }

    /// <summary>
    /// Sets a strongly-typed property value
    /// </summary>
    public static void SetPropertyValue<T>(GameObject gameObject, string propertyName, T value)
    {
        EnsureInstance();
        Instance.SetPropertyValue(gameObject, propertyName, value);
    }

    #endregion
}



