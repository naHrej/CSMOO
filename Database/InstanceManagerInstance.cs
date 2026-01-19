using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Instance-based instance manager implementation for dependency injection
/// </summary>
public class InstanceManagerInstance : IInstanceManager
{
    private readonly IDbProvider _dbProvider;
    private readonly IClassManager _classManager;
    private readonly IObjectManager _objectManager;
    private readonly IPropertyManager _propertyManager;
    
    public InstanceManagerInstance(IDbProvider dbProvider, IClassManager classManager, IObjectManager objectManager)
    {
        _dbProvider = dbProvider;
        _classManager = classManager;
        _objectManager = objectManager;
        // For backward compatibility, create PropertyManager instance directly
        _propertyManager = new PropertyManagerInstance(dbProvider, classManager, objectManager);
    }
    
    public InstanceManagerInstance(IDbProvider dbProvider, IClassManager classManager, IObjectManager objectManager, IPropertyManager propertyManager)
    {
        _dbProvider = dbProvider;
        _classManager = classManager;
        _objectManager = objectManager;
        _propertyManager = propertyManager;
    }
    
    /// <summary>
    /// Creates an instance of a class with full inheritance chain
    /// </summary>
    public GameObject CreateInstance(string classId, string? location = null)
    {
        var objectClass = _dbProvider.FindById<ObjectClass>("objectclasses", classId);
        if (objectClass == null)
            throw new ArgumentException($"Class with ID {classId} not found");

        if (objectClass.IsAbstract)
            throw new InvalidOperationException($"Cannot instantiate abstract class {objectClass.Name}");

        // Get the full inheritance chain
        var inheritanceChain = _classManager.GetInheritanceChain(classId);
        
        // Merge properties from the inheritance chain (parent first, child last)
        var mergedProperties = new BsonDocument();
        foreach (var classInChain in inheritanceChain)
        {
            _propertyManager.MergeProperties(mergedProperties, classInChain.Properties);
        }

        mergedProperties["classid"] = classId; // Ensure class ID is set    
        GameObject gameObject;
        var newId = Guid.NewGuid().ToString();
        var name = mergedProperties.ContainsKey("name") ? mergedProperties["name"].AsString : objectClass.Name;
        var description = mergedProperties.ContainsKey("description") ? mergedProperties["description"].AsString : "";

        switch (objectClass.Name.ToLowerInvariant())
        {
            case "room":
                gameObject = new Room(newId, name, description);
                break;
            case "exit":
                gameObject = new Exit(newId, name);
                break;
            case "item":
                gameObject = new Item(newId, name);
                break;
            case "player":
                gameObject = new Player { Id = newId, Name = name };
                break;
            case "container":
                gameObject = new Container(newId, name);
                break;
            default:
                gameObject = new GameObject { Id = newId, Name = name };
                break;
        }

        gameObject.Properties = mergedProperties;
        gameObject.Location = _dbProvider.FindOne<GameObject>("gameobjects", o => o.Properties.ContainsKey("isStartingRoom")) ?? null;
        gameObject.DbRef = GetNextDbRef();

        _dbProvider.Insert("gameobjects", gameObject);
        return gameObject;
    }

    /// <summary>
    /// Destroys an object instance
    /// </summary>
    public bool DestroyInstance(string objectId)
    {
        var gameObject = _objectManager.GetObject(objectId);
        if (gameObject == null) return false;

        // Move any contents to nowhere (they become orphaned)
        var contents = GetObjectsInLocation(objectId);
        foreach (var item in contents)
        {
            item.Location = null;
            _dbProvider.Update("gameobjects", item);
        }

        // Remove from database
        _dbProvider.Delete<GameObject>("gameobjects", objectId);
        return true;
    }

    /// <summary>
    /// Moves an object to a new location
    /// </summary>
    public bool MoveObject(string objectId, string? newLocationId)
    {
        var gameObject = _objectManager.GetObject(objectId);
        if (gameObject == null) return false;
        if (newLocationId == null)
        {
            gameObject.Location = null; // Move to nowhere
        }
        else
        {
            var newLocation = _objectManager.GetObject(newLocationId);
            if (newLocation == null)
                throw new ArgumentException($"Location with ID {newLocationId} not found");
            
            gameObject.Location = newLocation;
        }
        return _dbProvider.Update("gameobjects", gameObject);
    }

    /// <summary>
    /// Moves an object to a new location (alternative signature)
    /// </summary>
    public bool MoveObject(GameObject gameObject, GameObject newLocation)
    {
        if (gameObject == null || newLocation == null) return false;
        gameObject.Location = newLocation;
        return _dbProvider.Update("gameobjects", gameObject);
    }

    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    public List<GameObject> GetObjectsInLocation(string? locationId)
    {
        if (locationId == null)
        {
            return _dbProvider.Find<GameObject>("gameobjects", obj => obj.Location == null).ToList();
        }
        return _dbProvider.Find<GameObject>("gameobjects", obj => obj.Location?.Id == locationId).ToList();
    }
    
    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    public List<GameObject> GetObjectsInLocation(GameObject? location)
    {
        if (location is null)
        {
            return _dbProvider.Find<GameObject>("gameobjects", obj => obj.Location == null).ToList();
        }
        return _dbProvider.Find<GameObject>("gameobjects", obj => obj.Location?.Id == location.Id).ToList();
    }
    
    /// <summary>
    /// Gets all objects of a specific class type (including inheritance)
    /// </summary>
    public List<GameObject> FindObjectsByClass(string classId, bool includeSubclasses = true)
    {
        if (!includeSubclasses)
        {
            return _dbProvider.Find<GameObject>("gameobjects", obj => obj.ClassId == classId).ToList();
        }

        // Find all classes that inherit from the specified class
        var allClasses = _dbProvider.FindAll<ObjectClass>("objectclasses").ToList();
        var targetClassIds = new HashSet<string> { classId };

        bool foundNewClasses;
        do
        {
            foundNewClasses = false;
            foreach (var objectClass in allClasses)
            {
                if (objectClass.ParentClassId != null &&
                    targetClassIds.Contains(objectClass.ParentClassId) &&
                    !targetClassIds.Contains(objectClass.Id))
                {
                    targetClassIds.Add(objectClass.Id);
                    foundNewClasses = true;
                }
            }
        } while (foundNewClasses);

        return _dbProvider.Find<GameObject>("gameobjects", obj => targetClassIds.Contains(obj.ClassId)).ToList();
    }

    /// <summary>
    /// Gets the next available DbRef number
    /// </summary>
    private int GetNextDbRef()
    {
        var allObjects = _dbProvider.FindAll<GameObject>("gameobjects");
        return allObjects.Any() ? allObjects.Max(obj => obj.DbRef) + 1 : 1;
    }

    /// <summary>
    /// Gets basic statistics about objects in the database
    /// </summary>
    public Dictionary<string, int> GetObjectStatistics()
    {
        var allObjects = _dbProvider.FindAll<GameObject>("gameobjects").ToList();
        var stats = new Dictionary<string, int>();

        // Count by class
        var classCounts = allObjects
            .GroupBy(obj => obj.ClassId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Convert class IDs to names
        foreach (var kvp in classCounts)
        {
            var objectClass = _dbProvider.FindById<ObjectClass>("objectclasses", kvp.Key);
            var className = objectClass?.Name ?? "Unknown";
            stats[className] = kvp.Value;
        }

        return stats;
    }
}
