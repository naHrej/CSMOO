using LiteDB;
using CSMOO.Database;
using CSMOO.Object;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CSMOO.Verbs;

/// <summary>
/// Instance-based verb manager implementation for dependency injection
/// </summary>
public class VerbManagerInstance : IVerbManager
{
    private readonly IDbProvider _dbProvider;
    
    public VerbManagerInstance(IDbProvider dbProvider)
    {
        _dbProvider = dbProvider;
    }
    
    /// <summary>
    /// Creates a new verb on an object
    /// </summary>
    public Verb CreateVerb(string objectId, string name, string pattern = "", string code = "", string createdBy = "system")
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

        _dbProvider.Insert("verbs", verb);
        
        return verb;
    }

    /// <summary>
    /// Updates an existing verb
    /// </summary>
    public bool UpdateVerb(Verb verb)
    {
        verb.ModifiedAt = DateTime.UtcNow;
        var result = _dbProvider.Update("verbs", verb);
        
        return result;
    }

    /// <summary>
    /// Deletes a verb
    /// </summary>
    public bool DeleteVerb(string verbId)
    {
        var verb = _dbProvider.FindById<Verb>("verbs", verbId);
        if (verb == null) return false;
        var result = _dbProvider.Delete<Verb>("verbs", verbId);

        return result;
    }

    /// <summary>
    /// Deletes all verbs on an object
    /// </summary>
    public int DeleteVerbsOnObject(string objectId)
    {
        var verbsToDelete = _dbProvider.Find<Verb>("verbs", v => v.ObjectId == objectId).ToList();
        int deletedCount = 0;
        foreach (var verb in verbsToDelete)
        {
            if (_dbProvider.Delete<Verb>("verbs", verb.Id))
                deletedCount++;
        }

        return deletedCount;
    }

    /// <summary>
    /// Gets a verb by ID
    /// </summary>
    public Verb? GetVerb(string verbId)
    {
        return _dbProvider.FindById<Verb>("verbs", verbId);
    }

    /// <summary>
    /// Finds a verb by name on a specific object
    /// </summary>
    public Verb? FindVerb(string objectId, string verbName)
    {
        return _dbProvider.FindOne<Verb>("verbs", v => v.ObjectId == objectId && v.Name == verbName);
    }

    /// <summary>
    /// Gets all verbs on a specific object (not including inherited)
    /// </summary>
    public List<Verb> GetVerbsOnObject(string objectId)
    {
        return _dbProvider.Find<Verb>("verbs", v => v.ObjectId == objectId).ToList();
    }

    /// <summary>
    /// Gets all verbs created by a specific user
    /// </summary>
    public List<Verb> GetVerbsByCreator(string createdBy)
    {
        return _dbProvider.Find<Verb>("verbs", v => v.CreatedBy == createdBy).ToList();
    }

    /// <summary>
    /// Copies a verb from one object to another
    /// </summary>
    public Verb? CopyVerb(string sourceVerbId, string targetObjectId, string copiedBy = "system")
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

        _dbProvider.Insert("verbs", newVerb);
        
        return newVerb;
    }

    /// <summary>
    /// Moves a verb from one object to another
    /// </summary>
    public bool MoveVerb(string verbId, string newObjectId)
    {
        var verb = GetVerb(verbId);
        if (verb == null) return false;

        var oldObjectId = verb.ObjectId;
        verb.ObjectId = newObjectId;
        verb.ModifiedAt = DateTime.UtcNow;
        
        var result = UpdateVerb(verb);

        return result;
    }

    /// <summary>
    /// Sets verb aliases
    /// </summary>
    public bool SetVerbAliases(string verbId, string aliases)
    {
        var verb = GetVerb(verbId);
        if (verb == null) return false;

        verb.Aliases = aliases;
        return UpdateVerb(verb);
    }

    /// <summary>
    /// Sets verb pattern
    /// </summary>
    public bool SetVerbPattern(string verbId, string pattern)
    {
        var verb = GetVerb(verbId);
        if (verb == null) return false;

        verb.Pattern = pattern;
        return UpdateVerb(verb);
    }

    /// <summary>
    /// Sets verb permissions
    /// </summary>
    public bool SetVerbPermissions(string verbId, string permissions)
    {
        var verb = GetVerb(verbId);
        if (verb == null) return false;

        verb.Permissions = permissions;
        return UpdateVerb(verb);
    }

    /// <summary>
    /// Sets verb description
    /// </summary>
    public bool SetVerbDescription(string verbId, string description)
    {
        var verb = GetVerb(verbId);
        if (verb == null) return false;

        verb.Description = description;
        return UpdateVerb(verb);
    }

    /// <summary>
    /// Sets verb code
    /// </summary>
    public bool SetVerbCode(string verbId, string code)
    {
        var verb = GetVerb(verbId);
        if (verb == null) return false;

        verb.Code = code;
        return UpdateVerb(verb);
    }

    /// <summary>
    /// Validates a verb name
    /// </summary>
    public bool IsValidVerbName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Length > 50) return false;
        
        // Allow letters, numbers, underscore, hyphen
        return Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9_-]*$");
    }

    /// <summary>
    /// Gets basic statistics about verbs in the database
    /// </summary>
    public Dictionary<string, int> GetVerbStatistics()
    {
        var allVerbs = _dbProvider.FindAll<Verb>("verbs").ToList();

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
    public List<Verb> SearchVerbs(string namePattern, bool useRegex = false)
    {
        var allVerbs = _dbProvider.FindAll<Verb>("verbs").ToList();

        if (useRegex)
        {
            try
            {
                var regex = new Regex(namePattern, RegexOptions.IgnoreCase);
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
    public bool UpdateVerbCode(string verbId, string code)
    {
        var verb = _dbProvider.FindById<Verb>("verbs", verbId);
        if (verb == null)
            return false;
        verb.Code = code;
        verb.ModifiedAt = DateTime.UtcNow;
        var result = _dbProvider.Update("verbs", verb);

        return result;
    }

    /// <summary>
    /// Gets all verbs from the database
    /// </summary>
    public List<Verb> GetAllVerbs()
    {
        return _dbProvider.FindAll<Verb>("verbs").ToList();
    }
}
