using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Database;

/// <summary>
/// Manages the object-oriented inheritance system and object lifecycle
/// </summary>
public static class ObjectManager
{
    /// <summary>
    /// Creates a new object class definition
    /// </summary>
    public static ObjectClass CreateClass(string name, string? parentClassId = null, string description = "")
    {
        var objectClass = new ObjectClass
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            ParentClassId = parentClassId,
            Description = description,
            Properties = new BsonDocument(),
            Methods = new BsonDocument()
        };

        GameDatabase.Instance.ObjectClasses.Insert(objectClass);
        return objectClass;
    }

    /// <summary>
    /// Creates an instance of a class with full inheritance chain
    /// </summary>
    public static GameObject CreateInstance(string classId, string? location = null)
    {
        var objectClass = GameDatabase.Instance.ObjectClasses.FindById(classId);
        if (objectClass == null)
            throw new ArgumentException($"Class with ID {classId} not found");

        if (objectClass.IsAbstract)
            throw new InvalidOperationException($"Cannot instantiate abstract class {objectClass.Name}");

        // Get the full inheritance chain
        var inheritanceChain = GetInheritanceChain(classId);
        
        // Merge properties from the inheritance chain (parent first, child last)
        var mergedProperties = new BsonDocument();
        foreach (var classInChain in inheritanceChain)
        {
            MergeProperties(mergedProperties, classInChain.Properties);
        }

        var gameObject = new GameObject
        {
            Id = Guid.NewGuid().ToString(),
            DbRef = GetNextDbRef(),
            ClassId = classId,
            Properties = mergedProperties,
            Location = location
        };

        GameDatabase.Instance.GameObjects.Insert(gameObject);
        
        // If placed in a location, update the container's contents
        if (location != null)
        {
            AddToLocation(gameObject.Id, location);
        }

        return gameObject;
    }

    /// <summary>
    /// Gets the full inheritance chain for a class (from root to target class)
    /// </summary>
    public static List<ObjectClass> GetInheritanceChain(string classId)
    {
        var chain = new List<ObjectClass>();
        var currentClass = GameDatabase.Instance.ObjectClasses.FindById(classId);
        
        while (currentClass != null)
        {
            chain.Insert(0, currentClass); // Insert at beginning to maintain parent->child order
            
            if (currentClass.ParentClassId == null)
                break;
                
            currentClass = GameDatabase.Instance.ObjectClasses.FindById(currentClass.ParentClassId);
        }

        return chain;
    }

    /// <summary>
    /// Gets a property value from an object, checking inheritance chain if not found on instance
    /// </summary>
    public static BsonValue? GetProperty(GameObject gameObject, string propertyName)
    {
        // First check the instance properties
        if (gameObject.Properties.ContainsKey(propertyName))
            return gameObject.Properties[propertyName];

        // Then check the class inheritance chain
        var inheritanceChain = GetInheritanceChain(gameObject.ClassId);
        foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse()) // Child to parent order
        {
            if (objectClass.Properties.ContainsKey(propertyName))
                return objectClass.Properties[propertyName];
        }

        return null;
    }

    /// <summary>
    /// Sets a property on an object instance
    /// </summary>
    public static void SetProperty(GameObject gameObject, string propertyName, BsonValue value)
    {
        gameObject.Properties[propertyName] = value;
        gameObject.ModifiedAt = DateTime.UtcNow;
        GameDatabase.Instance.GameObjects.Update(gameObject);
    }

    /// <summary>
    /// Moves an object to a new location
    /// </summary>
    public static void MoveObject(string objectId, string? newLocation)
    {
        var gameObject = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (gameObject == null)
            throw new ArgumentException($"Object with ID {objectId} not found");

        // Remove from old location
        if (gameObject.Location != null)
        {
            RemoveFromLocation(objectId, gameObject.Location);
        }

        // Add to new location
        if (newLocation != null)
        {
            AddToLocation(objectId, newLocation);
        }

        // Update the object
        gameObject.Location = newLocation;
        gameObject.ModifiedAt = DateTime.UtcNow;
        GameDatabase.Instance.GameObjects.Update(gameObject);
    }

    /// <summary>
    /// Gets all objects in a location
    /// </summary>
    public static List<GameObject> GetObjectsInLocation(string locationId)
    {
        return GameDatabase.Instance.GameObjects
            .Find(obj => obj.Location == locationId)
            .ToList();
    }

    /// <summary>
    /// Finds objects by class type (including inheritance)
    /// </summary>
    public static List<GameObject> FindObjectsByClass(string classId, bool includeSubclasses = true)
    {
        if (!includeSubclasses)
        {
            return GameDatabase.Instance.GameObjects
                .Find(obj => obj.ClassId == classId)
                .ToList();
        }

        // Find all classes that inherit from the specified class
        var allClasses = GameDatabase.Instance.ObjectClasses.FindAll().ToList();
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

        return GameDatabase.Instance.GameObjects
            .Find(obj => targetClassIds.Contains(obj.ClassId))
            .ToList();
    }

    /// <summary>
    /// Gets the next available DBREF number
    /// </summary>
    private static int GetNextDbRef()
    {
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        if (allObjects.Any())
        {
            return allObjects.Max(o => o.DbRef) + 1;
        }
        return 1; // Start at 1 like traditional MUDs
    }

    /// <summary>
    /// Assigns DBREFs to objects that don't have them (for migration)
    /// </summary>
    public static void MigrateDbRefs()
    {
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        var objectsNeedingDbRef = allObjects.Where(o => o.DbRef == 0).ToList();
        
        if (objectsNeedingDbRef.Any())
        {
            Logger.Info($"Migrating {objectsNeedingDbRef.Count} objects to have DBREFs...");
            int nextDbRef = allObjects.Where(o => o.DbRef > 0).Any() ? allObjects.Max(o => o.DbRef) + 1 : 1;
            
            foreach (var obj in objectsNeedingDbRef)
            {
                obj.DbRef = nextDbRef++;
                GameDatabase.Instance.GameObjects.Update(obj);
            }
            
            Logger.Info($"Migration complete. Next DBREF will be #{nextDbRef}");
        }
    }

    private static void MergeProperties(BsonDocument target, BsonDocument source)
    {
        foreach (var kvp in source)
        {
            target[kvp.Key] = kvp.Value;
        }
    }

    private static void AddToLocation(string objectId, string locationId)
    {
        var location = GameDatabase.Instance.GameObjects.FindById(locationId);
        if (location != null)
        {
            if (!location.Contents.Contains(objectId))
            {
                location.Contents.Add(objectId);
                location.ModifiedAt = DateTime.UtcNow;
                GameDatabase.Instance.GameObjects.Update(location);
            }
        }
    }

    private static void RemoveFromLocation(string objectId, string locationId)
    {
        var location = GameDatabase.Instance.GameObjects.FindById(locationId);
        if (location != null)
        {
            location.Contents.Remove(objectId);
            location.ModifiedAt = DateTime.UtcNow;
            GameDatabase.Instance.GameObjects.Update(location);
        }
    }
}
