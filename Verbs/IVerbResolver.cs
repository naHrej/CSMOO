using System.Collections.Generic;
using CSMOO.Object;
using CSMOO.Commands;

namespace CSMOO.Verbs;

/// <summary>
/// Interface for verb resolution and execution operations
/// </summary>
public interface IVerbResolver
{
    /// <summary>
    /// Finds the best matching verb for a command on an object with variable extraction
    /// </summary>
    VerbMatchResult? FindMatchingVerbWithVariables(string objectId, string[] commandArgs, bool includeSystemVerbs = true);
    
    /// <summary>
    /// Finds the best matching verb for a command on an object (legacy method)
    /// </summary>
    Verb? FindMatchingVerb(string objectId, string[] commandArgs, bool includeSystemVerbs = true);
    
    /// <summary>
    /// Gets all verbs available on an object (including inherited and system verbs)
    /// </summary>
    List<Verb> GetVerbsForObject(string objectId, bool includeSystemVerbs = true);
    
    /// <summary>
    /// Gets all system verbs (global commands)
    /// </summary>
    List<Verb> GetSystemVerbs();
    
    /// <summary>
    /// Enhanced pattern matching that supports named variables like {varname}
    /// Returns a dictionary of extracted variables if the pattern matches
    /// </summary>
    Dictionary<string, string>? MatchesPatternWithVariables(string[] args, string pattern);
    
    /// <summary>
    /// Gets all verb names available to an object (for command completion/help)
    /// </summary>
    List<string> GetAvailableVerbNames(string objectId, bool includeSystemVerbs = true);
    
    /// <summary>
    /// Checks if a specific verb exists on an object
    /// </summary>
    bool HasVerb(string objectId, string verbName, bool includeSystemVerbs = true);
    
    /// <summary>
    /// Gets verb information for display/debugging
    /// </summary>
    Dictionary<string, object> GetVerbInfo(Verb verb);
    
    /// <summary>
    /// Gets all verbs on an object including inherited verbs from classes
    /// </summary>
    List<(Verb verb, string source)> GetAllVerbsOnObject(string objectId);
    
    /// <summary>
    /// Attempts to resolve and execute a command through the verb system
    /// </summary>
    bool TryExecuteVerb(string input, Player player, CommandProcessor commandProcessor);
}
