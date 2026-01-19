using LiteDB;
using CSMOO.Database;
using CSMOO.Configuration;
using CSMOO.Logging;

namespace CSMOO.Object;

/// <summary>
/// Instance-based object manager implementation for dependency injection
/// </summary>
public class ObjectManagerInstance : IObjectManager
{
    private readonly IDbProvider _dbProvider;
    private readonly IClassManager _classManager;
    private IPropertyManager? _propertyManager;
    private IInstanceManager? _instanceManager;
    
    // Instance cache for loaded GameObject instances
    private readonly Dictionary<string, GameObject> _objectCache = new();
    
    // Instance cache for loaded ObjectClass instances
    private readonly Dictionary<string, ObjectClass> _objectClassCache = new();
    
    public ObjectManagerInstance(IDbProvider dbProvider)
    {
        _dbProvider = dbProvider;
        // For now, create instances directly (will be injected later)
        var logger = new LoggerInstance(Config.Instance);
        _classManager = new ClassManagerInstance(dbProvider, logger);
    }
    
    public ObjectManagerInstance(IDbProvider dbProvider, IClassManager classManager)
    {
        _dbProvider = dbProvider;
        _classManager = classManager;
    }
    
    /// <summary>
    /// Sets the property manager (used to resolve circular dependency)
    /// </summary>
    public void SetPropertyManager(IPropertyManager propertyManager)
    {
        _propertyManager = propertyManager;
    }
    
    private IPropertyManager PropertyManager
    {
        get
        {
            if (_propertyManager == null)
            {
                // Fallback to static for backward compatibility
                throw new InvalidOperationException("PropertyManager not set. This should be set by DI container.");
            }
            return _propertyManager;
        }
    }
    
    /// <summary>
    /// Sets the instance manager (used to resolve circular dependency)
    /// </summary>
    public void SetInstanceManager(IInstanceManager instanceManager)
    {
        if (instanceManager == null)
            throw new ArgumentNullException(nameof(instanceManager));
        _instanceManager = instanceManager;
    }
    
    private IInstanceManager InstanceManager
    {
        get
        {
            if (_instanceManager == null)
            {
                // Try to get from static wrapper as fallback (shouldn't be needed in DI mode)
                // Access the private field via reflection as a last resort
                var staticType = typeof(Database.InstanceManager);
                var fieldInfo = staticType.GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var staticInstance = fieldInfo?.GetValue(null) as IInstanceManager;
                if (staticInstance != null)
                {
                    _instanceManager = staticInstance;
                    return _instanceManager;
                }
                throw new InvalidOperationException($"InstanceManager not set on ObjectManagerInstance. This should be set by DI container. Check that IInstanceManager factory calls SetInstanceManager().");
            }
            return _instanceManager;
        }
    }
    
    // Cache Management
    public void LoadAllObjectsToCache()
    {
        var allObjects = _dbProvider.FindAll<GameObject>("gameobjects");
        foreach (var obj in allObjects)
        {
            CacheGameObject(obj);
        }
    }

    public void LoadAllObjectClassesToCache()
    {
        var allClasses = _dbProvider.FindAll<ObjectClass>("objectclasses");
        foreach (var objClass in allClasses)
        {
            _objectClassCache[objClass.Id] = objClass;
        }
    }

    public GameObject? CacheGameObject(GameObject obj)
    {
        if (obj == null) return null;
        
        // Convert to subtype before caching
        var converted = ConvertToSubtype(obj);
        
        if (_objectCache.TryGetValue(converted.Id, out var cached))
        {
            // If cached object is already the correct subtype, return it
            if (cached.GetType() == converted.GetType())
            {
                return cached;
            }
            // Otherwise, update cache with converted subtype
        }
        
        _objectCache[converted.Id] = converted;
        return converted;
    }

    public ObjectClass? CacheObjectClass(ObjectClass objClass)
    {
        if (objClass == null) return null;
        if (_objectClassCache.TryGetValue(objClass.Id, out var cached))
            return cached;
        _objectClassCache[objClass.Id] = objClass;
        return objClass;
    }
    
    /// <summary>
    /// Converts a GameObject to its correct subtype based on Properties["classid"]
    /// Returns the original GameObject if no matching subtype is found
    /// </summary>
    private GameObject ConvertToSubtype(GameObject obj)
    {
        if (obj == null) return null!;
        
        // If it's already the correct subtype, return as-is
        // Check by comparing GetType().Name with the stored classid
        var storedClassId = obj.Properties.ContainsKey("classid") 
            ? obj.Properties["classid"].AsString 
            : null;
        
        if (string.IsNullOrEmpty(storedClassId))
        {
            // No classid stored, return as GameObject
            return obj;
        }
        
        // Check if already the correct type
        var currentTypeName = obj.GetType().Name;
        if (string.Equals(currentTypeName, storedClassId, StringComparison.OrdinalIgnoreCase))
        {
            return obj;
        }
        
        // Get the ObjectClass to find the actual class name
        var objectClass = GetClass(storedClassId);
        if (objectClass == null)
        {
            // Class not found, return as GameObject
            return obj;
        }
        
        var className = objectClass.Name;
        
        // Map ObjectClass.Name to C# type and create instance
        GameObject? subtype = className.ToLowerInvariant() switch
        {
            "exit" => CreateExitFromGameObject(obj),
            "room" => CreateRoomFromGameObject(obj),
            "player" => CreatePlayerFromGameObject(obj),
            "item" => CreateItemFromGameObject(obj),
            "container" => CreateContainerFromGameObject(obj),
            _ => null // Unknown class, fall back to GameObject
        };
        
        return subtype ?? obj;
    }

    /// <summary>
    /// Creates an Exit instance from a GameObject, copying all properties
    /// </summary>
    private Exit CreateExitFromGameObject(GameObject obj)
    {
        var exit = new Exit(obj.Id, obj.Name)
        {
            Properties = new BsonDocument(obj.Properties), // Deep copy
            DbRef = obj.DbRef,
            Location = obj.Location,
            Owner = obj.Owner,
            CreatedAt = obj.CreatedAt,
            ModifiedAt = obj.ModifiedAt
        };
        return exit;
    }

    /// <summary>
    /// Creates a Room instance from a GameObject, copying all properties
    /// </summary>
    private Room CreateRoomFromGameObject(GameObject obj)
    {
        var description = obj.Properties.ContainsKey("description") 
            ? obj.Properties["description"].AsString 
            : "";
        var room = new Room(obj.Id, obj.Name, description)
        {
            Properties = new BsonDocument(obj.Properties), // Deep copy
            DbRef = obj.DbRef,
            Location = obj.Location,
            Owner = obj.Owner,
            CreatedAt = obj.CreatedAt,
            ModifiedAt = obj.ModifiedAt
        };
        return room;
    }

    /// <summary>
    /// Creates a Player instance from a GameObject, copying all properties
    /// </summary>
    private Player CreatePlayerFromGameObject(GameObject obj)
    {
        var player = new Player
        {
            Id = obj.Id,
            Name = obj.Name,
            Properties = new BsonDocument(obj.Properties), // Deep copy
            DbRef = obj.DbRef,
            Location = obj.Location,
            CreatedAt = obj.CreatedAt,
            ModifiedAt = obj.ModifiedAt
        };
        
        // Note: Owner is readonly in Player and set in constructor, so we don't copy it here
        
        // Copy PasswordHash if present
        if (obj.Properties.ContainsKey("passwordhash"))
        {
            player.PasswordHash = obj.Properties["passwordhash"].AsString;
        }
        
        return player;
    }

    /// <summary>
    /// Creates an Item instance from a GameObject, copying all properties
    /// </summary>
    private Item CreateItemFromGameObject(GameObject obj)
    {
        var item = new Item(obj.Id, obj.Name)
        {
            Properties = new BsonDocument(obj.Properties), // Deep copy
            DbRef = obj.DbRef,
            Location = obj.Location,
            Owner = obj.Owner,
            CreatedAt = obj.CreatedAt,
            ModifiedAt = obj.ModifiedAt
        };
        
        // Copy Item-specific properties if present
        if (obj.Properties.ContainsKey("description"))
        {
            item.Description = obj.Properties["description"].AsString;
        }
        if (obj.Properties.ContainsKey("weight"))
        {
            item.Weight = obj.Properties["weight"].AsInt32;
        }
        
        return item;
    }

    /// <summary>
    /// Creates a Container instance from a GameObject, copying all properties
    /// </summary>
    private Container CreateContainerFromGameObject(GameObject obj)
    {
        var container = new Container(obj.Id, obj.Name)
        {
            Properties = new BsonDocument(obj.Properties), // Deep copy
            DbRef = obj.DbRef,
            Location = obj.Location,
            Owner = obj.Owner,
            CreatedAt = obj.CreatedAt,
            ModifiedAt = obj.ModifiedAt
        };
        
        // Copy Container-specific properties if present
        if (obj.Properties.ContainsKey("maxcapacity"))
        {
            container.MaxCapacity = obj.Properties["maxcapacity"].AsInt32;
        }
        
        // Note: Items list would need special handling if stored in Properties
        // For now, we'll leave it as an empty list
        
        return container;
    }
    
    // Class Management (delegated to ClassManager)
    public ObjectClass CreateClass(string name, string? parentClassId = null, string description = "")
        => _classManager.CreateClass(name, parentClassId, description);

    public List<ObjectClass> GetInheritanceChain(string classId)
        => _classManager.GetInheritanceChain(classId);

    public bool InheritsFrom(string childClassId, string parentClassId)
        => _classManager.InheritsFrom(childClassId, parentClassId);

    public List<ObjectClass> GetSubclasses(string parentClassId, bool recursive = true)
        => _classManager.GetSubclasses(parentClassId, recursive);

    public bool DeleteClass(string classId, bool deleteSubclasses = false)
        => _classManager.DeleteClass(classId, deleteSubclasses);

    public bool UpdateClass(ObjectClass objectClass)
        => _classManager.UpdateClass(objectClass);

    public List<ObjectClass> FindClassesByName(string name, bool exactMatch = false)
        => _classManager.FindClassesByName(name, exactMatch);

    public ObjectClass? GetClass(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return null;
        if (_objectClassCache.TryGetValue(classId, out var cached))
            return cached;
        var objClass = _dbProvider.FindById<ObjectClass>("objectclasses", classId);
        if (objClass != null)
        {
            _objectClassCache[objClass.Id] = objClass;
        }
        return objClass;
    }
    
    public ObjectClass? GetClassByName(string className)
    {
        if (string.IsNullOrEmpty(className)) return null;
        var objClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
        if (objClass != null)
        {
            _objectClassCache[objClass.Id] = objClass;
        }
        return objClass;
    }
    
    // Instance Management (delegated to InstanceManager)
    public GameObject CreateInstance(string classId, string? location = null)
        => this.InstanceManager.CreateInstance(classId, location);

    public bool DestroyInstance(string objectId)
        => this.InstanceManager.DestroyInstance(objectId);

    public bool MoveObject(string objectId, string? newLocationId)
        => this.InstanceManager.MoveObject(objectId, newLocationId);

    public bool MoveObject(GameObject gameObject, GameObject newLocation)
        => this.InstanceManager.MoveObject(gameObject, newLocation);

    public List<GameObject> GetObjectsInLocation(string? locationId)
        => this.InstanceManager.GetObjectsInLocation(locationId);
        
    public List<GameObject> GetObjectsInLocation(GameObject? location)
        => this.InstanceManager.GetObjectsInLocation(location);

    public List<GameObject> FindObjectsByClass(string classId, bool includeSubclasses = true)
        => this.InstanceManager.FindObjectsByClass(classId, includeSubclasses);

    public GameObject? FindByDbRef(int dbRef)
    {
        // FindByDbRef is handled by GetObjectByDbRef which uses cache and database
        return GetObjectByDbRef(dbRef);
    }

    public Dictionary<string, int> GetObjectStatistics()
        => this.InstanceManager.GetObjectStatistics();

    public GameObject? GetObject(string objectId)
    {
        if (string.IsNullOrEmpty(objectId)) return null;
        
        // Check cache first
        if (_objectCache.TryGetValue(objectId, out var cached))
        {
            // Ensure cached object is the correct subtype
            var converted = ConvertToSubtype(cached);
            if (converted != cached)
            {
                _objectCache[objectId] = converted;
            }
            return converted;
        }
        
        // Get from database
        var obj = _dbProvider.FindById<GameObject>("gameobjects", objectId);
        if (obj != null)
        {
            // Convert to subtype and cache
            var converted = ConvertToSubtype(obj);
            _objectCache[converted.Id] = converted;
            return converted;
        }
        return null;
    }
    
    public T? GetObject<T>(string objectId) where T : GameObject
    {
        if (string.IsNullOrEmpty(objectId)) return default;
        
        // Check cache first
        if (_objectCache.TryGetValue(objectId, out var cachedObj))
        {
            // Convert to subtype if needed
            var converted = ConvertToSubtype(cachedObj);
            if (converted is T cached)
            {
                // Update cache if type changed
                if (converted != cachedObj)
                {
                    _objectCache[objectId] = converted;
                }
                return cached;
            }
        }
        
        // Get from database
        T? obj;
        if (typeof(T) == typeof(Player))
        {
            obj = _dbProvider.FindById<T>("players", objectId);
        }
        else
        {
            obj = _dbProvider.FindById<T>("gameobjects", objectId);
        }

        if (obj != null)
        {
            // Convert to subtype (in case it was deserialized as GameObject)
            var converted = ConvertToSubtype(obj);
            if (converted is T convertedTyped)
            {
                _objectCache[convertedTyped.Id] = convertedTyped;
                return convertedTyped;
            }
            // If conversion failed but we have the original, cache and return it
            _objectCache[obj.Id] = obj;
            return obj;
        }
        
        return default;
    }

    public GameObject? GetObjectByDbRef(int dbRef)
    {
        // First check cache
        var cached = _objectCache.Values.FirstOrDefault(obj => obj.DbRef == dbRef);
        if (cached != null)
        {
            // Ensure cached object is the correct subtype
            var converted = ConvertToSubtype(cached);
            if (converted != cached)
            {
                _objectCache[converted.Id] = converted;
            }
            return converted;
        }

        // Fallback to database
        var obj = _dbProvider.FindOne<GameObject>("gameobjects", o => o.DbRef == dbRef);
        if (obj != null)
        {
            // Convert to subtype and cache
            var converted = ConvertToSubtype(obj);
            _objectCache[converted.Id] = converted;
            return converted;
        }
        return null;
    }

    public List<dynamic> GetAllObjects()
    {
        return _objectCache.Values.ToList().ConvertAll(obj => (dynamic)obj);
    }

    public List<ObjectClass> GetAllObjectClasses()
    {
        return _objectClassCache.Values.ToList();
    }

    public GameObject? ReloadObject(string objectId)
    {
        var obj = _dbProvider.FindById<GameObject>("gameobjects", objectId);
        if (obj != null)
        {
            // Convert to subtype and cache
            var converted = ConvertToSubtype(obj);
            _objectCache[converted.Id] = converted;
            return converted;
        }
        return null;
    }
    
    // Property Management (delegated to PropertyManager)
    public BsonValue? GetProperty(GameObject gameObject, string propertyName)
        => PropertyManager.GetProperty(gameObject, propertyName);

    public void SetProperty(GameObject gameObject, string propertyName, BsonValue value)
    {
        // Always update the cached instance
        if (_objectCache.TryGetValue(gameObject.Id, out var cached))
        {
            PropertyManager.SetProperty(cached, propertyName, value);
            _dbProvider.Update("gameobjects", cached);
        }
        else
        {
            PropertyManager.SetProperty(gameObject, propertyName, value);
            _objectCache[gameObject.Id] = gameObject;
            _dbProvider.Update("gameobjects", gameObject);
        }
    }

    public bool UpdateObject(GameObject gameObject)
    {
        if (gameObject == null || string.IsNullOrEmpty(gameObject.Id))
            return false;

        // Update the cache
        _objectCache[gameObject.Id] = gameObject;

        // Persist to database
        return _dbProvider.Update("gameobjects", gameObject);
    }
    
    public bool RemoveProperty(GameObject gameObject, string propertyName)
    {
        // Update the cache with the latest instance
        if (!_objectCache.ContainsKey(gameObject.Id))
            _objectCache[gameObject.Id] = gameObject;
        return PropertyManager.RemoveProperty(gameObject, propertyName);
    }

    public string[] GetPropertyNames(GameObject gameObject)
    {
        // Update the cache with the latest instance
        if (!_objectCache.ContainsKey(gameObject.Id))
            _objectCache[gameObject.Id] = gameObject;
        return PropertyManager.GetAllPropertyNames(gameObject);
    }

    public bool HasProperty(GameObject gameObject, string propertyName)
        => PropertyManager.HasProperty(gameObject, propertyName);

    public string[] GetAllPropertyNames(GameObject gameObject)
        => PropertyManager.GetAllPropertyNames(gameObject);

    public T? GetPropertyValue<T>(GameObject gameObject, string propertyName, T? defaultValue = default)
        => PropertyManager.GetPropertyValue(gameObject, propertyName, defaultValue);

    public void SetPropertyValue<T>(GameObject gameObject, string propertyName, T value)
        => PropertyManager.SetPropertyValue(gameObject, propertyName, value);
}
