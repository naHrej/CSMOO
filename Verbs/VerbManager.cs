using LiteDB;
using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Object;

namespace CSMOO.Verbs;

/// <summary>
/// Manages verb creation, modification, and deletion
/// </summary>
public static class VerbManager
{
    /// <summary>
    /// Creates a new verb on an object
    /// </summary>
    public static Verb CreateVerb(string objectId, string name, string pattern = "", string code = "", string createdBy = "system")
    {
        var verb = new Verb
        {
            ObjectId = objectId,
            Name = name,
            Pattern = pattern,
            Code = code,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        DbProvider.Instance.Insert("verbs", verb);
        
        Logger.Debug($"Created verb '{name}' on object {objectId} by {createdBy}");
        return verb;
    }

    /// <summary>
    /// Updates an existing verb
    /// </summary>
    public static bool UpdateVerb(Verb verb)
    {
        verb.ModifiedAt = DateTime.UtcNow;
        var result = DbProvider.Instance.Update("verbs", verb);
        
        if (result)
        {
            Logger.Debug($"Updated verb '{verb.Name}' on object {verb.ObjectId}");
        }
        
        return result;
    }

    /// <summary>
    /// Deletes a verb
    /// </summary>
    public static bool DeleteVerb(string verbId)
    {
        var verb = DbProvider.Instance.FindById<Verb>("verbs", verbId);
        if (verb == null) return false;
        var result = DbProvider.Instance.Delete<Verb>("verbs", verbId);
        if (result)
        {
            Logger.Debug($"Deleted verb '{verb.Name}' from object {verb.ObjectId}");
        }
        return result;
    }

    /// <summary>
    /// Deletes all verbs on an object
    /// </summary>
    public static int DeleteVerbsOnObject(string objectId)
    {
        var verbsToDelete = DbProvider.Instance.Find<Verb>("verbs", v => v.ObjectId == objectId).ToList();
        int deletedCount = 0;
        foreach (var verb in verbsToDelete)
        {
            if (DbProvider.Instance.Delete<Verb>("verbs", verb.Id))
                deletedCount++;
        }
        if (deletedCount > 0)
        {
            Logger.Debug($"Deleted {deletedCount} verbs from object {objectId}");
        }
        return deletedCount;
    }

    /// <summary>
    /// Gets a verb by ID
    /// </summary>
    public static Verb? GetVerb(string verbId)
    {
        return DbProvider.Instance.FindById<Verb>("verbs", verbId);
    }

    /// <summary>
    /// Finds a verb by name on a specific object
    /// </summary>
    public static Verb? FindVerb(string objectId, string verbName)
    {
        return DbProvider.Instance.FindOne<Verb>("verbs", v => v.ObjectId == objectId && v.Name == verbName);
    }

    /// <summary>
    /// Gets all verbs on a specific object (not including inherited)
    /// </summary>
    public static List<Verb> GetVerbsOnObject(string objectId)
    {
        return DbProvider.Instance.Find<Verb>("verbs", v => v.ObjectId == objectId).ToList();
    }

    /// <summary>
    /// Gets all verbs created by a specific user
    /// </summary>
    public static List<Verb> GetVerbsByCreator(string createdBy)
    {
        return DbProvider.Instance.Find<Verb>("verbs", v => v.CreatedBy == createdBy).ToList();
    }

    /// <summary>
    /// Copies a verb from one object to another
    /// </summary>
    public static Verb? CopyVerb(string sourceVerbId, string targetObjectId, string copiedBy = "system")
    {
        var sourceVerb = GetVerb(sourceVerbId);
        if (sourceVerb == null) return null;

        var newVerb = new Verb
        {
            ObjectId = targetObjectId,
            Name = sourceVerb.Name,
            Aliases = sourceVerb.Aliases,
            Pattern = sourceVerb.Pattern,
            Code = sourceVerb.Code,
            Permissions = sourceVerb.Permissions,
            Description = sourceVerb.Description + $" (copied from {sourceVerb.ObjectId})",
            CreatedBy = copiedBy,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        DbProvider.Instance.Insert("verbs", newVerb);
        
        Logger.Debug($"Copied verb '{sourceVerb.Name}' from {sourceVerb.ObjectId} to {targetObjectId}");
        return newVerb;
    }

    /// <summary>
    /// Moves a verb from one object to another
    /// </summary>
    public static bool MoveVerb(string verbId, string newObjectId)
    {
        var verb = GetVerb(verbId);
        if (verb == null) return false;

        var oldObjectId = verb.ObjectId;
        verb.ObjectId = newObjectId;
        verb.ModifiedAt = DateTime.UtcNow;
        
        var result = UpdateVerb(verb);
        if (result)
        {
            Logger.Debug($"Moved verb '{verb.Name}' from {oldObjectId} to {newObjectId}");
        }
        
        return result;
    }

    /// <summary>
    /// Sets verb aliases
    /// </summary>
    public static bool SetVerbAliases(string verbId, string aliases)
    {
        var verb = GetVerb(verbId);
        if (verb == null) return false;

        verb.Aliases = aliases;
        return UpdateVerb(verb);
    }

    /// <summary>
    /// Sets verb pattern
    /// </summary>
    public static bool SetVerbPattern(string verbId, string pattern)
    {
        var verb = GetVerb(verbId);
        if (verb == null) return false;

        verb.Pattern = pattern;
        return UpdateVerb(verb);
    }

    /// <summary>
    /// Sets verb permissions
    /// </summary>
    public static bool SetVerbPermissions(string verbId, string permissions)
    {
        var verb = GetVerb(verbId);
        if (verb == null) return false;

        verb.Permissions = permissions;
        return UpdateVerb(verb);
    }

    /// <summary>
    /// Sets verb description
    /// </summary>
    public static bool SetVerbDescription(string verbId, string description)
    {
        var verb = GetVerb(verbId);
        if (verb == null) return false;

        verb.Description = description;
        return UpdateVerb(verb);
    }

    /// <summary>
    /// Sets verb code
    /// </summary>
    public static bool SetVerbCode(string verbId, string code)
    {
        var verb = GetVerb(verbId);
        if (verb == null) return false;

        verb.Code = code;
        return UpdateVerb(verb);
    }

    /// <summary>
    /// Validates a verb name
    /// </summary>
    public static bool IsValidVerbName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Length > 50) return false;
        
        // Allow letters, numbers, underscore, hyphen
        return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9_-]*$");
    }

    /// <summary>
    /// Gets basic statistics about verbs in the database
    /// </summary>
    public static Dictionary<string, int> GetVerbStatistics()
    {
        var allVerbs = DbProvider.Instance.FindAll<Verb>("verbs").ToList();

        var stats = new Dictionary<string, int>
        {
            ["TotalVerbs"] = allVerbs.Count,
            ["VerbsWithCode"] = allVerbs.Count(v => !string.IsNullOrEmpty(v.Code)),
            ["VerbsWithAliases"] = allVerbs.Count(v => !string.IsNullOrEmpty(v.Aliases)),
            ["VerbsWithPatterns"] = allVerbs.Count(v => !string.IsNullOrEmpty(v.Pattern)),
            ["SystemVerbs"] = allVerbs.Count(v => v.CreatedBy == "system"),
            ["UserVerbs"] = allVerbs.Count(v => v.CreatedBy != "system")
        };

        return stats;
    }

    /// <summary>
    /// Searches for verbs by name pattern
    /// </summary>
    public static List<Verb> SearchVerbs(string namePattern, bool useRegex = false)
    {
        var allVerbs = DbProvider.Instance.FindAll<Verb>("verbs").ToList();

        if (useRegex)
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(namePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return allVerbs.Where(v => !string.IsNullOrEmpty(v.Name) && regex.IsMatch(v.Name)).ToList();
            }
            catch
            {
                return new List<Verb>();
            }
        }
        else
        {
            return allVerbs.Where(v => 
                !string.IsNullOrEmpty(v.Name) && 
                v.Name.Contains(namePattern, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }
    }

    /// <summary>
    /// Updates the code of an existing verb
    /// </summary>
    public static bool UpdateVerbCode(string verbId, string code)
    {
        var verb = DbProvider.Instance.FindById<Verb>("verbs", verbId);
        if (verb == null)
            return false;
        verb.Code = code;
        verb.ModifiedAt = DateTime.UtcNow;
        var result = DbProvider.Instance.Update("verbs", verb);
        if (result)
        {
            Logger.Debug($"Updated code for verb '{verb.Name}' (ID: {verbId})");
        }
        return result;
    }
}


