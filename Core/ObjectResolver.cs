using CSMOO.Database;
using Microsoft.CodeAnalysis;
using CSMOO.Object;
using LiteDB;
using CSMOO.Configuration;
using CSMOO.Logging;
using System.Collections.Generic;

namespace CSMOO.Core;

/// <summary>
/// Static wrapper for ObjectResolver (backward compatibility)
/// Delegates to ObjectResolverInstance for dependency injection support
/// </summary>
public static class ObjectResolver
{
    private static IObjectResolver? _instance;
    
    /// <summary>
    /// Sets the object resolver instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IObjectResolver instance)
    {
        _instance = instance;
    }
    
    private static IObjectResolver Instance => _instance ?? throw new InvalidOperationException("ObjectResolver instance not set. Call ObjectResolver.SetInstance() first. Static access is no longer supported - use dependency injection.");
    
    /// <summary>
    /// Returns all GameObjects matching the given name, alias, type, and location, as seen by the looker.
    /// If a location is specified, it will be used as if the looker is in that location even if they are not.
    /// The objectType can be used to filter results by class or type.
    /// </summary>
    public static List<dynamic> ResolveObjects(
        string name,
        GameObject looker,
        GameObject? location = null,
        string? objectType = null)
    {
        return Instance.ResolveObjects(name, looker, location, objectType);
    }
    /// <summary>
    /// Returns the first GameObject matching the given name, alias, type, and location, as seen by the looker.
    /// </summary>
    public static GameObject? ResolveObject(
        string name,
        GameObject looker,
        GameObject? location = null,
        string? objectType = null)
    {
        return Instance.ResolveObject(name, looker, location, objectType);
    }
}

