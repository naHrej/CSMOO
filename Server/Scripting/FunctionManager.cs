using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using CSMOO.Server.Database;
using CSMOO.Server.Database.Models;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Manages function creation, modification, and deletion
/// </summary>
public static class FunctionManager
{

    /// <summary>
    /// Creates a new function on an object
    /// </summary>
    public static Function CreateFunction(GameObject obj, string name, string[] parameterTypes, string[] parameterNames, string returnType = "void", string code = "", string createdBy = "system")
    {
        var function = new Function
        {
            ObjectId = obj.Id,
            Name = name,
            ParameterTypes = parameterTypes,
            ParameterNames = parameterNames,
            ReturnType = returnType,
            Code = code,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        var functionCollection = GameDatabase.Instance.GetCollection<Function>("functions");
        functionCollection.Insert(function);

        Logger.Debug($"Created function '{name}' on object {obj.Id} by {createdBy}");
        return function;
    }
    /// <summary>
    /// Updates an existing function
    /// </summary>
    public static bool UpdateFunction(Function function)
    {
        function.ModifiedAt = DateTime.UtcNow;
        var functionCollection = GameDatabase.Instance.GetCollection<Function>("functions");
        var result = functionCollection.Update(function);
        
        if (result)
        {
            Logger.Debug($"Updated function '{function.Name}' on object {function.ObjectId}");
        }
        
        return result;
    }

    /// <summary>
    /// Deletes a function
    /// </summary>
    public static bool DeleteFunction(string functionId)
    {
        var functionCollection = GameDatabase.Instance.GetCollection<Function>("functions");
        var function = functionCollection.FindById(functionId);
        
        if (function == null) return false;
        
        var result = functionCollection.Delete(functionId);
        if (result)
        {
            Logger.Debug($"Deleted function '{function.Name}' from object {function.ObjectId}");
        }
        
        return result;
    }

    /// <summary>
    /// Deletes all functions on an object
    /// </summary>
    public static int DeleteFunctionsOnObject(string objectId)
    {
        var functionCollection = GameDatabase.Instance.GetCollection<Function>("functions");
        var functions = functionCollection.Find(f => f.ObjectId == objectId).ToList();
        
        foreach (var function in functions)
        {
            functionCollection.Delete(function.Id);
        }
        
        Logger.Debug($"Deleted {functions.Count} functions from object {objectId}");
        return functions.Count;
    }

    /// <summary>
    /// Gets a function by ID
    /// </summary>
    public static Function? GetFunction(string functionId)
    {
        var functionCollection = GameDatabase.Instance.GetCollection<Function>("functions");
        return functionCollection.FindById(functionId);
    }

    /// <summary>
    /// Finds a function by name on a specific object
    /// </summary>
    public static Function? FindFunction(string objectId, string functionName)
    {
        var functionCollection = GameDatabase.Instance.GetCollection<Function>("functions");
        return functionCollection.FindOne(f => f.ObjectId == objectId && f.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all functions on a specific object (not including inherited)
    /// </summary>
    public static List<Function> GetFunctionsOnObject(string objectId)
    {
        var functionCollection = GameDatabase.Instance.GetCollection<Function>("functions");
        return functionCollection.Find(f => f.ObjectId == objectId).ToList();
    }

    /// <summary>
    /// Gets all functions created by a specific user
    /// </summary>
    public static List<Function> GetFunctionsByCreator(string createdBy)
    {
        var functionCollection = GameDatabase.Instance.GetCollection<Function>("functions");
        return functionCollection.Find(f => f.CreatedBy == createdBy).ToList();
    }

    /// <summary>
    /// Validates function parameter types
    /// </summary>
    public static bool IsValidParameterType(string type)
    {
        var validTypes = new HashSet<string>
        {
            "string", "int", "bool", "float", "double", "decimal",
            "object", "Player", "GameObject", "ObjectClass",
            "List<dynamic>", "List<GameObject>", "List<Player>", "List<string>", "List<int>",
            "IEnumerable<dynamic>", "IEnumerable<GameObject>", "IEnumerable<Player>"
        };
        
        return validTypes.Contains(type);
    }

    /// <summary>
    /// Validates function return type
    /// </summary>
    public static bool IsValidReturnType(string type)
    {
        var validTypes = new HashSet<string>
        {
            "void", "string", "int", "bool", "float", "double", "decimal",
            "object", "Player", "GameObject", "ObjectClass",
            "List<dynamic>", "List<GameObject>", "List<Player>", "List<string>", "List<int>",
            "IEnumerable<dynamic>", "IEnumerable<GameObject>", "IEnumerable<Player>"
        };
        
        return validTypes.Contains(type);
    }

    /// <summary>
    /// Validates a function name
    /// </summary>
    public static bool IsValidFunctionName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
            
        // Function names must start with a letter and contain only letters, numbers, and underscores
        if (!char.IsLetter(name[0]))
            return false;
            
        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    /// <summary>
    /// Gets basic statistics about functions in the database
    /// </summary>
    public static Dictionary<string, int> GetFunctionStatistics()
    {
        var functionCollection = GameDatabase.Instance.GetCollection<Function>("functions");
        var allFunctions = functionCollection.FindAll().ToList();

        var stats = new Dictionary<string, int>
        {
            ["TotalFunctions"] = allFunctions.Count,
            ["FunctionsWithCode"] = allFunctions.Count(f => !string.IsNullOrEmpty(f.Code)),
            ["SystemFunctions"] = allFunctions.Count(f => f.CreatedBy == "system"),
            ["UserFunctions"] = allFunctions.Count(f => f.CreatedBy != "system"),
            ["FunctionsWithParameters"] = allFunctions.Count(f => f.ParameterTypes.Length > 0),
            ["VoidFunctions"] = allFunctions.Count(f => f.ReturnType == "void")
        };

        return stats;
    }

    /// <summary>
    /// Sets function code
    /// </summary>
    public static bool SetFunctionCode(string functionId, string code)
    {
        var function = GetFunction(functionId);
        if (function == null) return false;

        function.Code = code;
        return UpdateFunction(function);
    }

    /// <summary>
    /// Sets function description
    /// </summary>
    public static bool SetFunctionDescription(string functionId, string description)
    {
        var function = GetFunction(functionId);
        if (function == null) return false;

        function.Description = description;
        return UpdateFunction(function);
    }

    /// <summary>
    /// Sets function permissions
    /// </summary>
    public static bool SetFunctionPermissions(string functionId, string permissions)
    {
        var function = GetFunction(functionId);
        if (function == null) return false;

        function.Permissions = permissions;
        return UpdateFunction(function);
    }
}
