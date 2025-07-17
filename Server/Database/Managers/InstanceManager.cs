using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Database.Managers;

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
        var objectClass = GameDatabase.Instance.ObjectClasses.FindById(classId);
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

        var gameObject = new GameObject
        {
            Id = Guid.NewGuid().ToString(),
            ClassId = classId,
            Properties = mergedProperties,
            Location = location
        };

        // Assign a DbRef
        gameObject.DbRef = GetNextDbRef();
        
        GameDatabase.Instance.GameObjects.Insert(gameObject);
        Logger.Debug($"Created instance of {objectClass.Name} with ID {gameObject.Id} (#{gameObject.DbRef})");
        
        return gameObject;
    }

    /// <summary>
    /// Destroys an object instance
    /// </summary>
    public static bool DestroyInstance(string objectId)
    {
        var gameObject = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (gameObject == null) return false;

        // Move any contents to nowhere (they become orphaned)
        var contents = GetObjectsInLocation(objectId);
        foreach (var item in contents)
        {
            item.Location = null;
            GameDatabase.Instance.GameObjects.Update(item);
        }

        // Remove from database
        GameDatabase.Instance.GameObjects.Delete(objectId);
        Logger.Debug($"Destroyed object #{gameObject.DbRef} ({objectId})");
        return true;
    }

    /// <summary>
    /// Moves an object to a new location
    /// </summary>
    public static bool MoveObject(string objectId, string? newLocationId)
    {
        var gameObject = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (gameObject == null) return false;

        gameObject.Location = newLocationId;
        return GameDatabase.Instance.GameObjects.Update(gameObject);
    }

    /// <summary>
    /// Moves an object to a new location (alternative signature)
    /// </summary>
    public static bool MoveObject(GameObject gameObject, string? newLocationId)
    {
        gameObject.Location = newLocationId;
        return GameDatabase.Instance.GameObjects.Update(gameObject);
    }

    /// <summary>
    /// Gets all objects in a specific location
    /// </summary>
    public static List<GameObject> GetObjectsInLocation(string? locationId)
    {
        if (locationId == null)
        {
            return GameDatabase.Instance.GameObjects.Find(obj => obj.Location == null).ToList();
        }
        
        return GameDatabase.Instance.GameObjects.Find(obj => obj.Location == locationId).ToList();
    }

    /// <summary>
    /// Gets all objects of a specific class type (including inheritance)
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
    /// Gets the next available DbRef number
    /// </summary>
    private static int GetNextDbRef()
    {
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        return allObjects.Any() ? allObjects.Max(obj => obj.DbRef) + 1 : 1;
    }

    /// <summary>
    /// Migrates objects to have DbRefs if they don't already have them
    /// </summary>
    public static void MigrateDbRefs()
    {
        var objectsWithoutDbRef = GameDatabase.Instance.GameObjects.Find(obj => obj.DbRef == 0).ToList();
        
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
            GameDatabase.Instance.GameObjects.Update(obj);
        }
        
        Logger.Info("DbRef migration completed");
    }

    /// <summary>
    /// Finds an object by its DbRef number
    /// </summary>
    public static GameObject? FindByDbRef(int dbRef)
    {
        return GameDatabase.Instance.GameObjects.FindOne(obj => obj.DbRef == dbRef);
    }

    /// <summary>
    /// Gets basic statistics about objects in the database
    /// </summary>
    public static Dictionary<string, int> GetObjectStatistics()
    {
        var allObjects = GameDatabase.Instance.GameObjects.FindAll().ToList();
        var stats = new Dictionary<string, int>();

        // Count by class
        var classCounts = allObjects
            .GroupBy(obj => obj.ClassId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Convert class IDs to names
        foreach (var kvp in classCounts)
        {
            var objectClass = GameDatabase.Instance.ObjectClasses.FindById(kvp.Key);
            var className = objectClass?.Name ?? "Unknown";
            stats[className] = kvp.Value;
        }

        return stats;
    }
}
