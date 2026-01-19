using LiteDB;
using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Configuration;
using System.Collections.Generic;

namespace CSMOO.Verbs;

/// <summary>
/// Static wrapper for VerbManager (backward compatibility)
/// Delegates to VerbManagerInstance for dependency injection support
/// </summary>
public static class VerbManager
{
    private static IVerbManager? _instance;
    
    /// <summary>
    /// Sets the verb manager instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IVerbManager instance)
    {
        _instance = instance;
    }
    
    private static IVerbManager Instance => _instance ?? throw new InvalidOperationException("VerbManager instance not set. Call VerbManager.SetInstance() first.");
    
    /// <summary>
    /// Ensures an instance exists (creates default if not set)
    /// </summary>
    private static void EnsureInstance()
    {
        if (_instance == null)
        {
            // Create default instances for backward compatibility
            var dbProvider = DbProvider.Instance;
            _instance = new VerbManagerInstance(dbProvider);
        }
    }
    
    /// <summary>
    /// Creates a new verb on an object
    /// </summary>
    public static Verb CreateVerb(string objectId, string name, string pattern = "", string code = "", string createdBy = "system")
    {
        EnsureInstance();
        return Instance.CreateVerb(objectId, name, pattern, code, createdBy);
    }

    /// <summary>
    /// Updates an existing verb
    /// </summary>
    public static bool UpdateVerb(Verb verb)
    {
        EnsureInstance();
        return Instance.UpdateVerb(verb);
    }

    /// <summary>
    /// Deletes a verb
    /// </summary>
    public static bool DeleteVerb(string verbId)
    {
        EnsureInstance();
        return Instance.DeleteVerb(verbId);
    }

    /// <summary>
    /// Deletes all verbs on an object
    /// </summary>
    public static int DeleteVerbsOnObject(string objectId)
    {
        EnsureInstance();
        return Instance.DeleteVerbsOnObject(objectId);
    }

    /// <summary>
    /// Gets a verb by ID
    /// </summary>
    public static Verb? GetVerb(string verbId)
    {
        EnsureInstance();
        return Instance.GetVerb(verbId);
    }

    /// <summary>
    /// Finds a verb by name on a specific object
    /// </summary>
    public static Verb? FindVerb(string objectId, string verbName)
    {
        EnsureInstance();
        return Instance.FindVerb(objectId, verbName);
    }

    /// <summary>
    /// Gets all verbs on a specific object (not including inherited)
    /// </summary>
    public static List<Verb> GetVerbsOnObject(string objectId)
    {
        EnsureInstance();
        return Instance.GetVerbsOnObject(objectId);
    }

    /// <summary>
    /// Gets all verbs created by a specific user
    /// </summary>
    public static List<Verb> GetVerbsByCreator(string createdBy)
    {
        EnsureInstance();
        return Instance.GetVerbsByCreator(createdBy);
    }

    /// <summary>
    /// Copies a verb from one object to another
    /// </summary>
    public static Verb? CopyVerb(string sourceVerbId, string targetObjectId, string copiedBy = "system")
    {
        EnsureInstance();
        return Instance.CopyVerb(sourceVerbId, targetObjectId, copiedBy);
    }

    /// <summary>
    /// Moves a verb from one object to another
    /// </summary>
    public static bool MoveVerb(string verbId, string newObjectId)
    {
        EnsureInstance();
        return Instance.MoveVerb(verbId, newObjectId);
    }

    /// <summary>
    /// Sets verb aliases
    /// </summary>
    public static bool SetVerbAliases(string verbId, string aliases)
    {
        EnsureInstance();
        return Instance.SetVerbAliases(verbId, aliases);
    }

    /// <summary>
    /// Sets verb pattern
    /// </summary>
    public static bool SetVerbPattern(string verbId, string pattern)
    {
        EnsureInstance();
        return Instance.SetVerbPattern(verbId, pattern);
    }

    /// <summary>
    /// Sets verb permissions
    /// </summary>
    public static bool SetVerbPermissions(string verbId, string permissions)
    {
        EnsureInstance();
        return Instance.SetVerbPermissions(verbId, permissions);
    }

    /// <summary>
    /// Sets verb description
    /// </summary>
    public static bool SetVerbDescription(string verbId, string description)
    {
        EnsureInstance();
        return Instance.SetVerbDescription(verbId, description);
    }

    /// <summary>
    /// Sets verb code
    /// </summary>
    public static bool SetVerbCode(string verbId, string code)
    {
        EnsureInstance();
        return Instance.SetVerbCode(verbId, code);
    }

    /// <summary>
    /// Validates a verb name
    /// </summary>
    public static bool IsValidVerbName(string name)
    {
        EnsureInstance();
        return Instance.IsValidVerbName(name);
    }

    /// <summary>
    /// Gets basic statistics about verbs in the database
    /// </summary>
    public static Dictionary<string, int> GetVerbStatistics()
    {
        EnsureInstance();
        return Instance.GetVerbStatistics();
    }

    /// <summary>
    /// Searches for verbs by name pattern
    /// </summary>
    public static List<Verb> SearchVerbs(string namePattern, bool useRegex = false)
    {
        EnsureInstance();
        return Instance.SearchVerbs(namePattern, useRegex);
    }

    /// <summary>
    /// Updates the code of an existing verb
    /// </summary>
    public static bool UpdateVerbCode(string verbId, string code)
    {
        EnsureInstance();
        return Instance.UpdateVerbCode(verbId, code);
    }
}


