using System.Collections.Generic;
using CSMOO.Object;

namespace CSMOO.Verbs;

/// <summary>
/// Interface for verb management operations
/// </summary>
public interface IVerbManager
{
    /// <summary>
    /// Creates a new verb on an object
    /// </summary>
    Verb CreateVerb(string objectId, string name, string pattern = "", string code = "", string createdBy = "system");
    
    /// <summary>
    /// Updates an existing verb
    /// </summary>
    bool UpdateVerb(Verb verb);
    
    /// <summary>
    /// Deletes a verb
    /// </summary>
    bool DeleteVerb(string verbId);
    
    /// <summary>
    /// Deletes all verbs on an object
    /// </summary>
    int DeleteVerbsOnObject(string objectId);
    
    /// <summary>
    /// Gets a verb by ID
    /// </summary>
    Verb? GetVerb(string verbId);
    
    /// <summary>
    /// Finds a verb by name on a specific object
    /// </summary>
    Verb? FindVerb(string objectId, string verbName);
    
    /// <summary>
    /// Gets all verbs on a specific object (not including inherited)
    /// </summary>
    List<Verb> GetVerbsOnObject(string objectId);
    
    /// <summary>
    /// Gets all verbs created by a specific user
    /// </summary>
    List<Verb> GetVerbsByCreator(string createdBy);
    
    /// <summary>
    /// Copies a verb from one object to another
    /// </summary>
    Verb? CopyVerb(string sourceVerbId, string targetObjectId, string copiedBy = "system");
    
    /// <summary>
    /// Moves a verb from one object to another
    /// </summary>
    bool MoveVerb(string verbId, string newObjectId);
    
    /// <summary>
    /// Sets verb aliases
    /// </summary>
    bool SetVerbAliases(string verbId, string aliases);
    
    /// <summary>
    /// Sets verb pattern
    /// </summary>
    bool SetVerbPattern(string verbId, string pattern);
    
    /// <summary>
    /// Sets verb permissions
    /// </summary>
    bool SetVerbPermissions(string verbId, string permissions);
    
    /// <summary>
    /// Sets verb description
    /// </summary>
    bool SetVerbDescription(string verbId, string description);
    
    /// <summary>
    /// Sets verb code
    /// </summary>
    bool SetVerbCode(string verbId, string code);
    
    /// <summary>
    /// Validates a verb name
    /// </summary>
    bool IsValidVerbName(string name);
    
    /// <summary>
    /// Gets basic statistics about verbs in the database
    /// </summary>
    Dictionary<string, int> GetVerbStatistics();
    
    /// <summary>
    /// Searches for verbs by name pattern
    /// </summary>
    List<Verb> SearchVerbs(string namePattern, bool useRegex = false);
    
    /// <summary>
    /// Updates the code of an existing verb
    /// </summary>
    bool UpdateVerbCode(string verbId, string code);
    
    /// <summary>
    /// Gets all verbs from the database
    /// </summary>
    List<Verb> GetAllVerbs();
}
