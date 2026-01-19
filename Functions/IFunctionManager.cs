using System.Collections.Generic;
using CSMOO.Object;

namespace CSMOO.Functions;

/// <summary>
/// Interface for function management operations
/// </summary>
public interface IFunctionManager
{
    /// <summary>
    /// Creates a new function on an object
    /// </summary>
    Function CreateFunction(GameObject obj, string name, string[] parameterTypes, string[] parameterNames, string returnType = "void", string code = "", string createdBy = "system");
    
    /// <summary>
    /// Updates an existing function
    /// </summary>
    bool UpdateFunction(Function function);
    
    /// <summary>
    /// Deletes a function
    /// </summary>
    bool DeleteFunction(string functionId);
    
    /// <summary>
    /// Deletes all functions on an object
    /// </summary>
    int DeleteFunctionsOnObject(string objectId);
    
    /// <summary>
    /// Gets a function by ID
    /// </summary>
    Function? GetFunction(string functionId);
    
    /// <summary>
    /// Finds a function by name on a specific object
    /// </summary>
    Function? FindFunction(string objectId, string functionName);
    
    /// <summary>
    /// Gets all functions on a specific object (not including inherited)
    /// </summary>
    List<Function> GetFunctionsOnObject(string objectId);
    
    /// <summary>
    /// Gets all functions created by a specific user
    /// </summary>
    List<Function> GetFunctionsByCreator(string createdBy);
    
    /// <summary>
    /// Validates function parameter types
    /// </summary>
    bool IsValidParameterType(string type);
    
    /// <summary>
    /// Validates function return type
    /// </summary>
    bool IsValidReturnType(string type);
    
    /// <summary>
    /// Validates a function name
    /// </summary>
    bool IsValidFunctionName(string name);
    
    /// <summary>
    /// Gets basic statistics about functions in the database
    /// </summary>
    Dictionary<string, int> GetFunctionStatistics();
    
    /// <summary>
    /// Sets function code
    /// </summary>
    bool SetFunctionCode(string functionId, string code);
    
    /// <summary>
    /// Sets function description
    /// </summary>
    bool SetFunctionDescription(string functionId, string description);
    
    /// <summary>
    /// Sets function permissions
    /// </summary>
    bool SetFunctionPermissions(string functionId, List<Keyword> permissions);
}
