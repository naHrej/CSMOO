using LiteDB;
using CSMOO.Logging;
using CSMOO.Database;

namespace CSMOO.Object;
/// <summary>
/// Main facade for object management, delegating to specialized managers
/// </summary>
public static class ObjectManager
{
    // Singleton cache for loaded GameObject instances
    private static readonly Dictionary<string, GameObject> _objectCache = new();

    /// <summary>
    /// Loads all GameObjects from the database into the singleton cache at startup.
    /// </summary>
    public static void LoadAllObjectsToCache()
    {
        var allObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects");
        foreach (var obj in allObjects)
        {
            CacheGameObject(obj);
        }
    }

    /// <summary>
    /// Adds a GameObject to the singleton cache, or returns the cached instance if already present.
    /// </summary>
    public static GameObject? CacheGameObject(GameObject obj)
    {
        if (obj == null) return null;
        if (_objectCache.TryGetValue(obj.Id, out var cached))
            return cached;
        _objectCache[obj.Id] = obj;
        return obj;
    }
    #region Class Management (delegated to ClassManager)
    
    /// <summary>
    /// Creates a new object class definition
    /// </summary>
    public static ObjectClass CreateClass(string name, string? parentClassId = null, string description = "")
        => ClassManager.CreateClass(name, parentClassId, description);

    /// <summary>
    /// Gets the full inheritance chain for a class (from root to target class)
    /// </summary>
    public static List<ObjectClass> GetInheritanceChain(string classId)
        => ClassManager.GetInheritanceChain(classId);

    /// <summary>
    /// Checks if a class inherits from another class (directly or indirectly)
    /// </summary>
    public static bool InheritsFrom(string childClassId, string parentClassId)
        => ClassManager.InheritsFrom(childClassId, parentClassId);

    /// <summary>
    /// Gets all classes that inherit from a given class
    /// </summary>
    public static List<ObjectClass> GetSubclasses(string parentClassId, bool recursive = true)
        => ClassManager.GetSubclasses(parentClassId, recursive);

    /// <summary>
    /// Deletes a class and optionally its subclasses
    /// </summary>
    public static bool DeleteClass(string classId, bool deleteSubclasses = false)
        => ClassManager.DeleteClass(classId, deleteSubclasses);

    /// <summary>
    /// Updates a class definition
    /// </summary>
    public static bool UpdateClass(ObjectClass objectClass)
        => ClassManager.UpdateClass(objectClass);

    /// <summary>
    /// Finds classes by name (case-insensitive)
    /// </summary>
    public static List<ObjectClass> FindClassesByName(string name, bool exactMatch = false)
        => ClassManager.FindClassesByName(name, exactMatch);

    #endregion

    #region Instance Management (delegated to InstanceManager)

    /// <summary>
    /// Creates an instance of a class with full inheritance chain
    /// </summary>
    public static GameObject CreateInstance(string classId, string? location = null)
        => InstanceManager.CreateInstance(classId, location);

    /// <summary>
    /// Destroys an object instance
    /// </summary>
    public static bool DestroyInstance(string objectId)
        => InstanceManager.DestroyInstance(objectId);

    /// <summary>
    /// Moves an object to a new location
    /// </summary>
    public static bool MoveObject(string objectId, string? newLocationId)
        => InstanceManager.MoveObject(objectId, newLocationId);

    /// <summary>
    /// Moves an object to a new location (alternative signature)
    /// </summary>
    public static bool MoveObject(GameObject gameObject, GameObject newLocation)
        => InstanceManager.MoveObject(gameObject, newLocation);

    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    public static List<GameObject> GetObjectsInLocation(string? locationId)
        => InstanceManager.GetObjectsInLocation(locationId);
    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    public static List<GameObject> GetObjectsInLocation(GameObject? location)
        => InstanceManager.GetObjectsInLocation(location);

    /// <summary>
    /// Gets all objects of a specific class type (including inheritance)
    /// </summary>
    public static List<GameObject> FindObjectsByClass(string classId, bool includeSubclasses = true)
        => InstanceManager.FindObjectsByClass(classId, includeSubclasses);

    /// <summary>
    /// Migrates objects to have DbRefs if they don't already have them
    /// </summary>
    public static void MigrateDbRefs()
        => InstanceManager.MigrateDbRefs();

    /// <summary>
    /// Finds an object by its DbRef number
    /// </summary>
    public static GameObject? FindByDbRef(int dbRef)
        => InstanceManager.FindByDbRef(dbRef);

    /// <summary>
    /// Gets basic statistics about objects in the database
    /// </summary>
    public static Dictionary<string, int> GetObjectStatistics()
        => InstanceManager.GetObjectStatistics();

    /// <summary>
    /// Gets an object by ID
    /// </summary>
    public static GameObject? GetObject(string objectId)
    {
        if (string.IsNullOrEmpty(objectId)) return null;
        if (_objectCache.TryGetValue(objectId, out var cached))
            return cached;
        var obj = DbProvider.Instance.FindById<GameObject>("gameobjects", objectId);
        if (obj != null)
            _objectCache[obj.Id] = obj;
        return obj;
    }
    /// <summary>
    /// Gets a typed object by ID
    /// </summary>
    public static T? GetObject<T>(string objectId) where T : GameObject 
    {
        if (string.IsNullOrEmpty(objectId)) return default;
        if (_objectCache.TryGetValue(objectId, out var cachedObj) && cachedObj is T cached)
            return cached;
        T? obj;
        if (typeof(T) == typeof(Player)){
            obj = DbProvider.Instance.FindById<T>("players", objectId);
        }
        else
        {
            obj = DbProvider.Instance.FindById<T>("gameobjects", objectId);
        }



        if (obj != null)
            _objectCache[obj.Id] = obj;
        return obj;
    }

    #endregion

    #region Property Management (delegated to PropertyManager)

    /// <summary>
    /// Gets a property value from an object, checking inheritance chain if not found on instance
    /// </summary>
    public static BsonValue? GetProperty(GameObject gameObject, string propertyName)
        => PropertyManager.GetProperty(gameObject, propertyName);

    // <summary>
    /// Get all objects in the cache
    /// </summary>
    public static List<GameObject> GetAllObjects()
    {
        return _objectCache.Values.ToList();
    }

  

    /// <summary>
    /// Sets a property value on an object instance
    /// </summary>
    public static void SetProperty(GameObject gameObject, string propertyName, BsonValue value)
    {
        // Always update the cached instance
        if (_objectCache.TryGetValue(gameObject.Id, out var cached))
        {
            PropertyManager.SetProperty(cached, propertyName, value);
            DbProvider.Instance.Update("gameobjects", cached);
            Logger.Debug($"[ObjectManager] Property '{propertyName}' set on object {cached.Id} (cached instance) and persisted.");
        }
        else
        {
            PropertyManager.SetProperty(gameObject, propertyName, value);
            _objectCache[gameObject.Id] = gameObject;
            DbProvider.Instance.Update("gameobjects", gameObject);
            Logger.Debug($"[ObjectManager] Property '{propertyName}' set on object {gameObject.Id} (new cache) and persisted.");
        }
    }


    /// <summary>
    /// Removes a property from an object instance
    /// </summary>
    public static bool RemoveProperty(GameObject gameObject, string propertyName)
    {
        // Update the cache with the latest instance
        if (!_objectCache.ContainsKey(gameObject.Id))
            _objectCache[gameObject.Id] = gameObject;
        return PropertyManager.RemoveProperty(gameObject, propertyName);
    }
    /// <summary>
    /// Forces a reload of a GameObject from the database, replacing the cached instance.
    /// </summary>
    public static GameObject? ReloadObject(string objectId)
    {
        var obj = DbProvider.Instance.FindById<GameObject>("gameobjects", objectId);
        if (obj != null)
            _objectCache[obj.Id] = obj;
        return obj;
    }

    /// <summary>
    /// Checks if an object has a property (including inherited properties)
    /// </summary>
    public static bool HasProperty(GameObject gameObject, string propertyName)
        => PropertyManager.HasProperty(gameObject, propertyName);

    /// <summary>
    /// Gets all property names from an object (including inherited properties)
    /// </summary>
    public static string[] GetAllPropertyNames(GameObject gameObject)
        => PropertyManager.GetAllPropertyNames(gameObject);

    /// <summary>
    /// Gets the effective value of a property (resolved through inheritance)
    /// </summary>
    public static T? GetPropertyValue<T>(GameObject gameObject, string propertyName, T? defaultValue = default)
        => PropertyManager.GetPropertyValue(gameObject, propertyName, defaultValue);

    /// <summary>
    /// Sets a strongly-typed property value
    /// </summary>
    public static void SetPropertyValue<T>(GameObject gameObject, string propertyName, T value)
        => PropertyManager.SetPropertyValue(gameObject, propertyName, value);

    #endregion
}



