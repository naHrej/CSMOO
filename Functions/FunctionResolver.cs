using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Configuration;
using CSMOO.Logging;
using System.Collections.Generic;

namespace CSMOO.Functions;

/// <summary>
/// Static wrapper for FunctionResolver (backward compatibility)
/// Delegates to FunctionResolverInstance for dependency injection support
/// </summary>
public static class FunctionResolver
{
    private static IFunctionResolver? _instance;
    
    /// <summary>
    /// Sets the function resolver instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IFunctionResolver instance)
    {
        _instance = instance;
    }
    
    private static IFunctionResolver Instance => _instance ?? throw new InvalidOperationException("FunctionResolver instance not set. Call FunctionResolver.SetInstance() first.");
    
    /// <summary>
    /// Ensures an instance exists (creates default if not set)
    /// </summary>
    private static void EnsureInstance()
    {
        if (_instance == null)
        {
            // Create default instances for backward compatibility
            var dbProvider = DbProvider.Instance;
            var logger = new LoggerInstance(Config.Instance);
            var classManager = new ClassManagerInstance(dbProvider, logger);
            var objectManager = new ObjectManagerInstance(dbProvider, classManager);
            _instance = new FunctionResolverInstance(dbProvider, objectManager);
        }
    }
    
    /// <summary>
    /// Finds a function on an object or its inheritance chain
    /// </summary>
    public static Function? FindFunction(string objectId, string functionName)
    {
        EnsureInstance();
        return Instance.FindFunction(objectId, functionName);
    }

    /// <summary>
    /// Gets all functions available on an object (including inherited)
    /// </summary>
    public static List<Function> GetFunctionsForObject(string objectId, bool includeSystemFunctions = true)
    {
        EnsureInstance();
        return Instance.GetFunctionsForObject(objectId, includeSystemFunctions);
    }

    /// <summary>
    /// Gets all functions on an object including inherited functions from classes
    /// </summary>
    public static List<(Function function, string source)> GetAllFunctionsOnObject(string objectId)
    {
        EnsureInstance();
        return Instance.GetAllFunctionsOnObject(objectId);
    }

    /// <summary>
    /// Resolves object references for function calls (e.g., "player", "system", "#123", "class:Name")
    /// </summary>
    public static string? ResolveObjectReference(string objectRef, string currentPlayerId, string currentRoomId)
    {
        EnsureInstance();
        return Instance.ResolveObjectReference(objectRef, currentPlayerId, currentRoomId);
    }
}

