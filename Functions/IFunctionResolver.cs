using System.Collections.Generic;
using CSMOO.Object;

namespace CSMOO.Functions;

/// <summary>
/// Interface for function resolution operations
/// </summary>
public interface IFunctionResolver
{
    /// <summary>
    /// Finds a function on an object or its inheritance chain
    /// </summary>
    Function? FindFunction(string objectId, string functionName);
    
    /// <summary>
    /// Gets all functions available on an object (including inherited)
    /// </summary>
    List<Function> GetFunctionsForObject(string objectId, bool includeSystemFunctions = true);
    
    /// <summary>
    /// Gets all functions on an object including inherited functions from classes
    /// </summary>
    List<(Function function, string source)> GetAllFunctionsOnObject(string objectId);
    
    /// <summary>
    /// Resolves object references for function calls (e.g., "player", "system", "#123", "class:Name")
    /// </summary>
    string? ResolveObjectReference(string objectRef, string currentPlayerId, string currentRoomId);
}
