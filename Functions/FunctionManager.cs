using LiteDB;
using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Configuration;
using System.Collections.Generic;

namespace CSMOO.Functions;

/// <summary>
/// Static wrapper for FunctionManager (backward compatibility)
/// Delegates to FunctionManagerInstance for dependency injection support
/// </summary>
public static class FunctionManager
{
    private static IFunctionManager? _instance;
    
    /// <summary>
    /// Sets the function manager instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IFunctionManager instance)
    {
        _instance = instance;
    }
    
    private static IFunctionManager Instance => _instance ?? throw new InvalidOperationException("FunctionManager instance not set. Call FunctionManager.SetInstance() first.");
    
    /// <summary>
    /// Ensures an instance exists (creates default if not set)
    /// </summary>
    private static void EnsureInstance()
    {
        if (_instance == null)
        {
            // Use existing GameDatabase instance if available to avoid connection conflicts
            // GameDatabase.Instance will reuse existing instance or create one if needed
            var gameDatabase = GameDatabase.Instance;
            _instance = new FunctionManagerInstance(gameDatabase);
        }
    }

    /// <summary>
    /// Creates a new function on an object
    /// </summary>
    public static Function CreateFunction(GameObject obj, string name, string[] parameterTypes, string[] parameterNames, string returnType = "void", string code = "", string createdBy = "system")
    {
        EnsureInstance();
        return Instance.CreateFunction(obj, name, parameterTypes, parameterNames, returnType, code, createdBy);
    }
    /// <summary>
    /// Updates an existing function
    /// </summary>
    public static bool UpdateFunction(Function function)
    {
        EnsureInstance();
        return Instance.UpdateFunction(function);
    }

    /// <summary>
    /// Deletes a function
    /// </summary>
    public static bool DeleteFunction(string functionId)
    {
        EnsureInstance();
        return Instance.DeleteFunction(functionId);
    }

    /// <summary>
    /// Deletes all functions on an object
    /// </summary>
    public static int DeleteFunctionsOnObject(string objectId)
    {
        EnsureInstance();
        return Instance.DeleteFunctionsOnObject(objectId);
    }

    /// <summary>
    /// Gets a function by ID
    /// </summary>
    public static Function? GetFunction(string functionId)
    {
        EnsureInstance();
        return Instance.GetFunction(functionId);
    }

    /// <summary>
    /// Finds a function by name on a specific object
    /// </summary>
    public static Function? FindFunction(string objectId, string functionName)
    {
        EnsureInstance();
        return Instance.FindFunction(objectId, functionName);
    }

    /// <summary>
    /// Gets all functions on a specific object (not including inherited)
    /// </summary>
    public static List<Function> GetFunctionsOnObject(string objectId)
    {
        EnsureInstance();
        return Instance.GetFunctionsOnObject(objectId);
    }

    /// <summary>
    /// Gets all functions created by a specific user
    /// </summary>
    public static List<Function> GetFunctionsByCreator(string createdBy)
    {
        EnsureInstance();
        return Instance.GetFunctionsByCreator(createdBy);
    }

    /// <summary>
    /// Validates function parameter types
    /// </summary>
    public static bool IsValidParameterType(string type)
    {
        EnsureInstance();
        return Instance.IsValidParameterType(type);
    }

    /// <summary>
    /// Validates function return type
    /// </summary>
    public static bool IsValidReturnType(string type)
    {
        EnsureInstance();
        return Instance.IsValidReturnType(type);
    }

    /// <summary>
    /// Validates a function name
    /// </summary>
    public static bool IsValidFunctionName(string name)
    {
        EnsureInstance();
        return Instance.IsValidFunctionName(name);
    }

    /// <summary>
    /// Gets basic statistics about functions in the database
    /// </summary>
    public static Dictionary<string, int> GetFunctionStatistics()
    {
        EnsureInstance();
        return Instance.GetFunctionStatistics();
    }

    /// <summary>
    /// Sets function code
    /// </summary>
    public static bool SetFunctionCode(string functionId, string code)
    {
        EnsureInstance();
        return Instance.SetFunctionCode(functionId, code);
    }

    /// <summary>
    /// Sets function description
    /// </summary>
    public static bool SetFunctionDescription(string functionId, string description)
    {
        EnsureInstance();
        return Instance.SetFunctionDescription(functionId, description);
    }

    /// <summary>
    /// Sets function permissions
    /// </summary>
    public static bool SetFunctionPermissions(string functionId, List<Keyword> permissions)
    {
        EnsureInstance();
        return Instance.SetFunctionPermissions(functionId, permissions);
    }

    /// <summary>
    /// Gets all functions from the database (for help system)
    /// </summary>
    public static List<Function> GetAllFunctions()
    {
        // Use DbProvider directly to avoid creating new database connections
        // This prevents "file is being used by another process" errors
        return DbProvider.Instance.FindAll<Function>("functions").ToList();
    }
}

