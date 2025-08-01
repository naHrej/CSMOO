using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Manages object class definitions and inheritance relationships
/// </summary>
public static class ClassManager
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

        DbProvider.Instance.Insert("objectclasses", objectClass);
        return objectClass;
    }

    /// <summary>
    /// Gets the full inheritance chain for a class (from root to target class)
    /// </summary>
    public static List<ObjectClass> GetInheritanceChain(string classId)
    {
        var chain = new List<ObjectClass>();
        var currentClass = DbProvider.Instance.FindById<ObjectClass>("objectclasses", classId);
        
        while (currentClass != null)
        {
            chain.Insert(0, currentClass); // Insert at beginning to maintain parent->child order
            
            if (currentClass.ParentClassId == null)
                break;
                
            currentClass = DbProvider.Instance.FindById<ObjectClass>("objectclasses", currentClass.ParentClassId);
        }

        return chain;
    }

    /// <summary>
    /// Checks if a class inherits from another class (directly or indirectly)
    /// </summary>
    public static bool InheritsFrom(string childClassId, string parentClassId)
    {
        var chain = GetInheritanceChain(childClassId);
        return chain.Any(c => c.Id == parentClassId);
    }

    /// <summary>
    /// Gets all classes that inherit from a given class
    /// </summary>
    public static List<ObjectClass> GetSubclasses(string parentClassId, bool recursive = true)
    {
        var allClasses = DbProvider.Instance.FindAll<ObjectClass>("objectclasses").ToList();
        var subclasses = new List<ObjectClass>();

        if (!recursive)
        {
            return allClasses.Where(c => c.ParentClassId == parentClassId).ToList();
        }

        // Recursive: find all descendants
        var toProcess = new Queue<string>();
        toProcess.Enqueue(parentClassId);
        var processed = new HashSet<string>();

        while (toProcess.Count > 0)
        {
            var currentClassId = toProcess.Dequeue();
            if (processed.Contains(currentClassId)) continue;
            processed.Add(currentClassId);

            var directChildren = allClasses.Where(c => c.ParentClassId == currentClassId);
            foreach (var child in directChildren)
            {
                subclasses.Add(child);
                toProcess.Enqueue(child.Id);
            }
        }

        return subclasses;
    }

    /// <summary>
    /// Deletes a class and optionally its subclasses
    /// </summary>
    public static bool DeleteClass(string classId, bool deleteSubclasses = false)
    {
        var objectClass = DbProvider.Instance.FindById<ObjectClass>("objectclasses", classId);
        if (objectClass == null) return false;

        // Check if there are any instances of this class
        var instances = DbProvider.Instance.Find<GameObject>("gameobjects", obj => obj.ClassId == classId);
        if (instances.Any())
        {
            Logger.Warning($"Cannot delete class {objectClass.Name} - it has {instances.Count()} instances");
            return false;
        }

        // Handle subclasses
        var subclasses = GetSubclasses(classId, false);
        if (subclasses.Any() && !deleteSubclasses)
        {
            Logger.Warning($"Cannot delete class {objectClass.Name} - it has subclasses. Use deleteSubclasses=true to force deletion");
            return false;
        }

        if (deleteSubclasses)
        {
            foreach (var subclass in GetSubclasses(classId, true))
            {
                DeleteClass(subclass.Id, true);
            }
        }

        DbProvider.Instance.Delete<ObjectClass>("objectclasses", classId);
        Logger.Info($"Deleted class {objectClass.Name}");
        return true;
    }

    /// <summary>
    /// Updates a class definition
    /// </summary>
    public static bool UpdateClass(ObjectClass objectClass)
    {
        objectClass.ModifiedAt = DateTime.UtcNow;
        return DbProvider.Instance.Update("objectclasses", objectClass);
    }

    /// <summary>
    /// Finds classes by name (case-insensitive)
    /// </summary>
    public static List<ObjectClass> FindClassesByName(string name, bool exactMatch = false)
    {
        var allClasses = DbProvider.Instance.FindAll<ObjectClass>("objectclasses");
        
        if (exactMatch)
        {
            return allClasses.Where(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        else
        {
            return allClasses.Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}



