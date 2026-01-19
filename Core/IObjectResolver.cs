using System.Collections.Generic;
using CSMOO.Object;

namespace CSMOO.Core;

/// <summary>
/// Interface for object resolution operations
/// </summary>
public interface IObjectResolver
{
    /// <summary>
    /// Returns all GameObjects matching the given name, alias, type, and location, as seen by the looker.
    /// If a location is specified, it will be used as if the looker is in that location even if they are not.
    /// The objectType can be used to filter results by class or type.
    /// </summary>
    List<dynamic> ResolveObjects(
        string name,
        GameObject looker,
        GameObject? location = null,
        string? objectType = null);
    
    /// <summary>
    /// Returns the first GameObject matching the given name, alias, type, and location, as seen by the looker.
    /// </summary>
    GameObject? ResolveObject(
        string name,
        GameObject looker,
        GameObject? location = null,
        string? objectType = null);
}
