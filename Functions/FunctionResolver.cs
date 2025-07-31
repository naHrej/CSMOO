using System;
using System.Collections.Generic;
using System.Linq;
using CSMOO.Database;
using CSMOO.Database.Models;
using CSMOO.Logging;

namespace CSMOO.Functions;

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
        // First check for instance-specific function
        var instanceFunction = DbProvider.Instance.FindFunctionsByObjectId(objectId)
            .FirstOrDefault(f => f.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
        if (instanceFunction != null)
        {
            return instanceFunction;
        }

        // Check if this is a game object with a class
        var gameObject = ObjectManager.GetObject(objectId);
        if (gameObject?.ClassId != null)
        {
            // Get inheritance chain and search from most specific to most general
            var inheritanceChain = ObjectManager.GetInheritanceChain(gameObject.ClassId);
            foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse()) // Child to parent order
            {
                var classFunction = DbProvider.Instance.FindFunctionsByObjectId(objectClass.Id)
                    .FirstOrDefault(f => f.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
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
        // Add instance functions first (highest priority)
        var instanceFunctions = DbProvider.Instance.FindFunctionsByObjectId(objectId).ToList();
        allFunctions.AddRange(instanceFunctions);

        // Check if this is a game object with class inheritance
        var gameObject = ObjectManager.GetObject(objectId);
        if (gameObject?.ClassId != null)
        {
            var inheritanceChain = ObjectManager.GetInheritanceChain(gameObject.ClassId);
            // Add class functions (child to parent order for proper override)
            foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse())
            {
                var classFunctions = DbProvider.Instance.FindFunctionsByObjectId(objectClass.Id).ToList();
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
            var systemFunctions = DbProvider.Instance.FindFunctionsByObjectId(systemObjectId).ToList();
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
        // Add instance functions
        var instanceFunctions = DbProvider.Instance.FindFunctionsByObjectId(objectId).ToList();
        foreach (var func in instanceFunctions)
        {
            allFunctions.Add((func, "instance"));
        }

        // Check if this is a game object with class inheritance
        var gameObject = ObjectManager.GetObject(objectId);

        if (gameObject?.ClassId != null)
        {
            var inheritanceChain = ObjectManager.GetInheritanceChain(gameObject.ClassId);
            
            foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse()) // Child to parent order
            {
                var classFunctions = DbProvider.Instance.FindFunctionsByObjectId(objectClass.Id).ToList();
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
            var objectClass = DbProvider.Instance.FindById<ObjectClass>("objectclasses", objectId);
            if (objectClass != null)
            {
                var inheritanceChain = ObjectManager.GetInheritanceChain(objectId);
                
                foreach (var parentClass in inheritanceChain.AsEnumerable().Reverse())
                {
                    var classFunctions = DbProvider.Instance.FindFunctionsByObjectId(parentClass.Id).ToList();
                    foreach (Function classFunction in classFunctions)
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
            var obj = DbProvider.Instance.FindOne<GameObject>("gameobjects", o => o.DbRef == dbref);
            return obj?.Id;
        }

        // Handle class references (class:Name)
        if (objectRef.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectRef.Substring(6);
            var objectClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => 
                c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return objectClass?.Id;
        }

        // Check if it's a direct class ID (like "obj_room", "obj_exit", etc.)
        var classById = DbProvider.Instance.FindById<ObjectClass>("objectclasses", objectRef);
        if (classById != null)
        {
            return classById.Id;
        }

        // Try to find by object ID directly
        var allGameObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects");
        if (allGameObjects.Any(o => o.Id == objectRef))
            return objectRef;

        // Try to find by name
        var namedObject = DbProvider.Instance.FindOne<GameObject>("gameobjects", o => o.Properties.ContainsKey("name") && o.Properties["name"].AsString.Equals(objectRef, StringComparison.OrdinalIgnoreCase));
        if (namedObject != null) return namedObject.Id;

        // Try as class name
        var classByName = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => 
            c.Name.Equals(objectRef, StringComparison.OrdinalIgnoreCase));
        return classByName?.Id;
    }

    /// <summary>
    /// Gets the system object ID
    /// </summary>
    private static string? GetSystemObjectId()
    {
        var allObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects");
        var systemObject = allObjects.FirstOrDefault(obj => 
            (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
            (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
        return systemObject?.Id;
    }
}

