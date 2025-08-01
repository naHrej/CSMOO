using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Manages object instances and their lifecycle
/// </summary>
public static class InstanceManager
{
    /// <summary>
    /// Creates an instance of a class with full inheritance chain
    /// </summary>
    public static GameObject CreateInstance(string classId, string? location = null)
    {
        var objectClass = DbProvider.Instance.FindById<ObjectClass>("objectclasses", classId);
        if (objectClass == null)
            throw new ArgumentException($"Class with ID {classId} not found");

        if (objectClass.IsAbstract)
            throw new InvalidOperationException($"Cannot instantiate abstract class {objectClass.Name}");

        // Get the full inheritance chain
        var inheritanceChain = ClassManager.GetInheritanceChain(classId);
        
        // Merge properties from the inheritance chain (parent first, child last)
        var mergedProperties = new BsonDocument();
        foreach (var classInChain in inheritanceChain)
        {
            PropertyManager.MergeProperties(mergedProperties, classInChain.Properties);
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
            default:
                gameObject = new GameObject { Id = newId, Name = name };
                break;
        }

        gameObject.Properties = mergedProperties;
        gameObject.Location = DbProvider.Instance.FindOne<GameObject>("gameobjects", o => o.Properties.ContainsKey("isStartingRoom")) ?? null;
        gameObject.DbRef = GetNextDbRef();

        DbProvider.Instance.Insert("gameobjects", gameObject);
        Logger.Debug($"Created instance of {objectClass.Name} with ID {gameObject.Id} (#{gameObject.DbRef})");
        return gameObject;
    }

    /// <summary>
    /// Destroys an object instance
    /// </summary>
    public static bool DestroyInstance(string objectId)
    {
        var gameObject = ObjectManager.GetObject(objectId);
        if (gameObject == null) return false;

        // Move any contents to nowhere (they become orphaned)
        var contents = GetObjectsInLocation(objectId);
        foreach (var item in contents)
        {
            item.Location = null;
            DbProvider.Instance.Update("gameobjects", item);
        }

        // Remove from database
        DbProvider.Instance.Delete<GameObject>("gameobjects", objectId);
        Logger.Debug($"Destroyed object #{gameObject.DbRef} ({objectId})");
        return true;
    }

    /// <summary>
    /// Moves an object to a new location
    /// </summary>
    public static bool MoveObject(string objectId, string? newLocationId)
    {
        var gameObject = ObjectManager.GetObject(objectId);
        if (gameObject == null) return false;
        if (newLocationId == null)
        {
            gameObject.Location = null; // Move to nowhere
        }
        else
        {
            var newLocation = ObjectManager.GetObject(newLocationId);
            if (newLocation == null)
                throw new ArgumentException($"Location with ID {newLocationId} not found");
            
            gameObject.Location = newLocation;
        }
        return DbProvider.Instance.Update("gameobjects", gameObject);
    }

    /// <summary>
    /// Moves an object to a new location (alternative signature)
    /// </summary>
    public static bool MoveObject(GameObject gameObject, GameObject newLocation)
    {
        if (gameObject == null || newLocation == null) return false;
        gameObject.Location = newLocation;
        return DbProvider.Instance.Update("gameobjects", gameObject);
    }

    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    public static List<GameObject> GetObjectsInLocation(string? locationId)
    {
        if (locationId == null)
        {
            return DbProvider.Instance.Find<GameObject>("gameobjects", obj => obj.Location == null).ToList();
        }
        return DbProvider.Instance.Find<GameObject>("gameobjects", obj => obj.Location?.Id == locationId).ToList();
    }
    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    public static List<GameObject> GetObjectsInLocation(GameObject? location)
    {
        if (location is null)
        {
            return DbProvider.Instance.Find<GameObject>("gameobjects", obj => obj.Location == null).ToList();
        }
        return DbProvider.Instance.Find<GameObject>("gameobjects", obj => obj.Location?.Id == location.Id).ToList();
    }
    
    /// <summary>
    /// Gets all objects of a specific class type (including inheritance)
    /// </summary>
    public static List<GameObject> FindObjectsByClass(string classId, bool includeSubclasses = true)
    {
        if (!includeSubclasses)
        {
            return DbProvider.Instance.Find<GameObject>("gameobjects", obj => obj.ClassId == classId).ToList();
        }

        // Find all classes that inherit from the specified class
        var allClasses = DbProvider.Instance.FindAll<ObjectClass>("objectclasses").ToList();
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

        return DbProvider.Instance.Find<GameObject>("gameobjects", obj => targetClassIds.Contains(obj.ClassId)).ToList();
    }

    /// <summary>
    /// Gets the next available DbRef number
    /// </summary>
    private static int GetNextDbRef()
    {
        var allObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects");
        return allObjects.Any() ? allObjects.Max(obj => obj.DbRef) + 1 : 1;
    }

    /// <summary>
    /// Migrates objects to have DbRefs if they don't already have them
    /// </summary>
    public static void MigrateDbRefs()
    {
        var objectsWithoutDbRef = DbProvider.Instance.Find<GameObject>("gameobjects", obj => obj.DbRef == 0).ToList();
        
        if (!objectsWithoutDbRef.Any())
        {
            Logger.Debug("All objects already have DbRefs");
            return;
        }

        Logger.Info($"Migrating {objectsWithoutDbRef.Count} objects to have DbRefs...");
        
        var nextDbRef = GetNextDbRef();
        foreach (var obj in objectsWithoutDbRef)
        {
            obj.DbRef = nextDbRef++;
            DbProvider.Instance.Update("gameobjects", obj);
        }
        
        Logger.Info("DbRef migration completed");
    }

    /// <summary>
    /// Finds an object by its DbRef number
    /// </summary>
    public static GameObject? FindByDbRef(int dbRef)
    {
        return DbProvider.Instance.FindOne<GameObject>("gameobjects", obj => obj.DbRef == dbRef);
    }

    /// <summary>
    /// Gets basic statistics about objects in the database
    /// </summary>
    public static Dictionary<string, int> GetObjectStatistics()
    {
        var allObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects").ToList();
        var stats = new Dictionary<string, int>();

        // Count by class
        var classCounts = allObjects
            .GroupBy(obj => obj.ClassId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Convert class IDs to names
        foreach (var kvp in classCounts)
        {
        var objectClass = DbProvider.Instance.FindById<ObjectClass>("objectclasses", kvp.Key);
            var className = objectClass?.Name ?? "Unknown";
            stats[className] = kvp.Value;
        }

        return stats;
    }
}



