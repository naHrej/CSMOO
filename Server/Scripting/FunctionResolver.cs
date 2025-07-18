using System;
using System.Collections.Generic;
using System.Linq;
using CSMOO.Server.Database;
using CSMOO.Server.Database.Models;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Resolves and manages function execution for objects
/// </summary>
public static class FunctionResolver
{
    /// <summary>
    /// Finds a function on an object or its inheritance chain
    /// </summary>
    public static Function? FindFunction(string objectId, string functionName)
    {
        var functionCollection = GameDatabase.Instance.GetCollection<Function>("functions");

        // First check for instance-specific function
        var instanceFunction = functionCollection.FindOne(f => f.ObjectId == objectId && f.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
        if (instanceFunction != null)
        {
            return instanceFunction;
        }

        // Check if this is a game object with a class
        var gameObjects = GameDatabase.Instance.GameObjects;
        var gameObject = gameObjects.FindById(objectId);
        
        if (gameObject?.ClassId != null)
        {
            // Get inheritance chain and search from most specific to most general
            var inheritanceChain = ObjectManager.GetInheritanceChain(gameObject.ClassId);
            
            foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse()) // Child to parent order
            {
                var classFunction = functionCollection.FindOne(f => f.ObjectId == objectClass.Id && f.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
                if (classFunction != null)
                {
                    return classFunction;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all functions available on an object (including inherited)
    /// </summary>
    public static List<Function> GetFunctionsForObject(string objectId, bool includeSystemFunctions = true)
    {
        var allFunctions = new List<Function>();
        var functionCollection = GameDatabase.Instance.GetCollection<Function>("functions");

        // Add instance functions first (highest priority)
        var instanceFunctions = functionCollection.Find(f => f.ObjectId == objectId).ToList();
        allFunctions.AddRange(instanceFunctions);

        // Check if this is a game object with class inheritance
        var gameObjects = GameDatabase.Instance.GameObjects;
        var gameObject = gameObjects.FindById(objectId);
        
        if (gameObject?.ClassId != null)
        {
            var inheritanceChain = ObjectManager.GetInheritanceChain(gameObject.ClassId);
            
            // Add class functions (child to parent order for proper override)
            foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse())
            {
                var classFunctions = functionCollection.Find(f => f.ObjectId == objectClass.Id).ToList();
                
                // Add class functions that aren't already overridden by instance or more specific class
                foreach (var classFunction in classFunctions)
                {
                    if (!allFunctions.Any(existing => existing.Name?.ToLower() == classFunction.Name?.ToLower()))
                    {
                        allFunctions.Add(classFunction);
                    }
                }
            }
        }

        // Add system functions if requested
        if (includeSystemFunctions)
        {
            var systemObjectId = GetSystemObjectId();
            if (systemObjectId != null)
            {
                var systemFunctions = functionCollection.Find(f => f.ObjectId == systemObjectId).ToList();
                
                foreach (var systemFunction in systemFunctions)
                {
                    if (!allFunctions.Any(existing => existing.Name?.ToLower() == systemFunction.Name?.ToLower()))
                    {
                        allFunctions.Add(systemFunction);
                    }
                }
            }
        }

        return allFunctions;
    }

    /// <summary>
    /// Gets all functions on an object including inherited functions from classes
    /// </summary>
    public static List<(Function function, string source)> GetAllFunctionsOnObject(string objectId)
    {
        var allFunctions = new List<(Function function, string source)>();
        var functionCollection = GameDatabase.Instance.GetCollection<Function>("functions");

        // Add instance functions
        var instanceFunctions = functionCollection.Find(f => f.ObjectId == objectId).ToList();
        foreach (var func in instanceFunctions)
        {
            allFunctions.Add((func, "instance"));
        }

        // Check if this is a game object with class inheritance
        var gameObjects = GameDatabase.Instance.GameObjects;
        var gameObject = gameObjects.FindById(objectId);
        
        if (gameObject?.ClassId != null)
        {
            var inheritanceChain = ObjectManager.GetInheritanceChain(gameObject.ClassId);
            
            foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse()) // Child to parent order
            {
                var classFunctions = GameDatabase.Instance.GetCollection<Function>("functions")
                    .Find(f => f.ObjectId == objectClass.Id)
                    .ToList();
                
                foreach (var classFunction in classFunctions)
                {
                    // Only add if not already overridden by instance or more specific class
                    if (!allFunctions.Any(existing => existing.function.Name?.ToLower() == classFunction.Name?.ToLower()))
                    {
                        allFunctions.Add((classFunction, $"class {objectClass.Name}"));
                    }
                }
            }
        }
        else
        {
            // This might be a class itself
            var objectClass = GameDatabase.Instance.ObjectClasses.FindById(objectId);
            if (objectClass != null)
            {
                var inheritanceChain = ObjectManager.GetInheritanceChain(objectId);
                
                foreach (var parentClass in inheritanceChain.AsEnumerable().Reverse())
                {
                    var classFunctions = functionCollection.Find(f => f.ObjectId == parentClass.Id).ToList();
                    
                    foreach (var classFunction in classFunctions)
                    {
                        if (!allFunctions.Any(existing => existing.function.Name?.ToLower() == classFunction.Name?.ToLower()))
                        {
                            var source = parentClass.Id == objectId ? "class" : $"parent class {parentClass.Name}";
                            allFunctions.Add((classFunction, source));
                        }
                    }
                }
            }
        }

        return allFunctions;
    }

    /// <summary>
    /// Resolves object references for function calls (e.g., "player", "system", "#123", "class:Name")
    /// </summary>
    public static string? ResolveObjectReference(string objectRef, string currentPlayerId, string currentRoomId)
    {
        if (string.IsNullOrEmpty(objectRef))
            return null;

        var lowerRef = objectRef.ToLower();

        // Handle special references
        switch (lowerRef)
        {
            case "player":
            case "me":
                return currentPlayerId;
                
            case "here":
            case "room":
                return currentRoomId;
                
            case "system":
                return GetSystemObjectId();
        }

        // Handle DBREF format (#123)
        if (objectRef.StartsWith("#") && int.TryParse(objectRef.Substring(1), out var dbref))
        {
            var gameObjects = GameDatabase.Instance.GameObjects;
            var obj = gameObjects.FindOne(o => o.DbRef == dbref);
            return obj?.Id;
        }

        // Handle class references (class:Name)
        if (objectRef.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectRef.Substring(6);
            var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return objectClass?.Id;
        }

        // Try to find by object ID directly
        var gameObjects2 = GameDatabase.Instance.GameObjects;
        if (gameObjects2.Exists(o => o.Id == objectRef))
            return objectRef;

        // Try to find by name
        var namedObject = gameObjects2.FindOne(o => o.Properties.ContainsKey("name") && o.Properties["name"].AsString.Equals(objectRef, StringComparison.OrdinalIgnoreCase));
        return namedObject?.Id;
    }

    /// <summary>
    /// Gets the system object ID
    /// </summary>
    private static string? GetSystemObjectId()
    {
        var gameObjects = GameDatabase.Instance.GameObjects;
        var allObjects = gameObjects.FindAll();
        var systemObject = allObjects.FirstOrDefault(obj => 
            (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
            (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
        return systemObject?.Id;
    }
}
