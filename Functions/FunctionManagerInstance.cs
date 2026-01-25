using CSMOO.Database;
using CSMOO.Object;
using System.Collections.Generic;
using System.Linq;

namespace CSMOO.Functions;

/// <summary>
/// Instance-based function manager implementation for dependency injection
/// </summary>
public class FunctionManagerInstance : IFunctionManager
{
    private readonly IDbProvider _dbProvider;
    
    public FunctionManagerInstance(IDbProvider dbProvider)
    {
        _dbProvider = dbProvider;
    }
    
    /// <summary>
    /// Creates a new function on an object
    /// </summary>
    public Function CreateFunction(GameObject obj, string name, string[] parameterTypes, string[] parameterNames, string returnType = "void", string code = "", string createdBy = "system")
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

        _dbProvider.Insert("functions", function);

        return function;
    }
    
    /// <summary>
    /// Updates an existing function
    /// </summary>
    public bool UpdateFunction(Function function)
    {
        function.ModifiedAt = DateTime.UtcNow;
        return _dbProvider.Update("functions", function);
    }

    /// <summary>
    /// Deletes a function
    /// </summary>
    public bool DeleteFunction(string functionId)
    {
        var function = _dbProvider.FindById<Function>("functions", functionId);
        
        if (function == null) return false;
        
        return _dbProvider.Delete<Function>("functions", functionId);
    }

    /// <summary>
    /// Deletes all functions on an object
    /// </summary>
    public int DeleteFunctionsOnObject(string objectId)
    {
        var functions = _dbProvider.Find<Function>("functions", f => f.ObjectId == objectId).ToList();
        
        foreach (var function in functions)
        {
            _dbProvider.Delete<Function>("functions", function.Id);
        }
        
        return functions.Count;
    }

    /// <summary>
    /// Gets a function by ID
    /// </summary>
    public Function? GetFunction(string functionId)
    {
        return _dbProvider.FindById<Function>("functions", functionId);
    }

    /// <summary>
    /// Finds a function by name on a specific object
    /// </summary>
    public Function? FindFunction(string objectId, string functionName)
    {
        return _dbProvider.FindOne<Function>("functions", f => f.ObjectId == objectId && f.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all functions on a specific object (not including inherited)
    /// </summary>
    public List<Function> GetFunctionsOnObject(string objectId)
    {
        return _dbProvider.Find<Function>("functions", f => f.ObjectId == objectId).ToList();
    }

    /// <summary>
    /// Gets all functions created by a specific user
    /// </summary>
    public List<Function> GetFunctionsByCreator(string createdBy)
    {
        return _dbProvider.Find<Function>("functions", f => f.CreatedBy == createdBy).ToList();
    }

    /// <summary>
    /// Validates function parameter types
    /// </summary>
    public bool IsValidParameterType(string type)
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
    public bool IsValidReturnType(string type)
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
    public bool IsValidFunctionName(string name)
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
    public Dictionary<string, int> GetFunctionStatistics()
    {
        var allFunctions = _dbProvider.FindAll<Function>("functions").ToList();

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
    public bool SetFunctionCode(string functionId, string code)
    {
        var function = GetFunction(functionId);
        if (function == null) return false;

        function.Code = code;
        return UpdateFunction(function);
    }

    /// <summary>
    /// Sets function description
    /// </summary>
    public bool SetFunctionDescription(string functionId, string description)
    {
        var function = GetFunction(functionId);
        if (function == null) return false;

        function.Description = description;
        return UpdateFunction(function);
    }

    /// <summary>
    /// Sets function permissions
    /// </summary>
    public bool SetFunctionPermissions(string functionId, List<Keyword> permissions)
    {
        var function = GetFunction(functionId);
        if (function == null) return false;

        function.AccessModifiers = permissions;
        return UpdateFunction(function);
    }

    /// <summary>
    /// Gets all functions from the database
    /// </summary>
    public List<Function> GetAllFunctions()
    {
        return _dbProvider.FindAll<Function>("functions").ToList();
    }
}
