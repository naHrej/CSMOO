using System.Text.RegularExpressions;
using LiteDB;
using CSMOO.Database;
using CSMOO.Scripting;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Exceptions;
using CSMOO.Commands;
using CSMOO.Configuration;
using System.Collections.Generic;

namespace CSMOO.Verbs;

/// <summary>
/// Static wrapper for VerbResolver (backward compatibility)
/// Delegates to VerbResolverInstance for dependency injection support
/// </summary>
public static class VerbResolver
{
    private static IVerbResolver? _instance;
    
    /// <summary>
    /// Sets the verb resolver instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IVerbResolver instance)
    {
        _instance = instance;
    }
    
    private static IVerbResolver Instance => _instance ?? throw new InvalidOperationException("VerbResolver instance not set. Call VerbResolver.SetInstance() first.");
    
    /// <summary>
    /// Ensures an instance exists (creates default if not set)
    /// </summary>
    private static void EnsureInstance()
    {
        if (_instance == null)
        {
            // Create default instances for backward compatibility
            var config = Config.Instance;
            var logger = new LoggerInstance(config);
            var dbProvider = DbProvider.Instance;
            var classManager = new ClassManagerInstance(dbProvider, logger);
            var objectManager = new ObjectManagerInstance(dbProvider, classManager);
            _instance = new VerbResolverInstance(dbProvider, objectManager, logger);
        }
    }
    
    /// <summary>
    /// Finds the best matching verb for a command on an object with variable extraction
    /// </summary>
    public static VerbMatchResult? FindMatchingVerbWithVariables(string objectId, string[] commandArgs, bool includeSystemVerbs = true)
    {
        EnsureInstance();
        return Instance.FindMatchingVerbWithVariables(objectId, commandArgs, includeSystemVerbs);
    }

    /// <summary>
    /// Finds the best matching verb for a command on an object (legacy method)
    /// </summary>
    public static Verb? FindMatchingVerb(string objectId, string[] commandArgs, bool includeSystemVerbs = true)
    {
        EnsureInstance();
        return Instance.FindMatchingVerb(objectId, commandArgs, includeSystemVerbs);
    }

    /// <summary>
    /// Gets all verbs available on an object (including inherited and system verbs)
    /// </summary>
    public static List<Verb> GetVerbsForObject(string objectId, bool includeSystemVerbs = true)
    {
        EnsureInstance();
        return Instance.GetVerbsForObject(objectId, includeSystemVerbs);
    }

    /// <summary>
    /// Gets all system verbs (global commands)
    /// </summary>
    public static List<Verb> GetSystemVerbs()
    {
        EnsureInstance();
        return Instance.GetSystemVerbs();
    }

    /// <summary>
    /// Enhanced pattern matching that supports named variables like {varname}
    /// Returns a dictionary of extracted variables if the pattern matches
    /// </summary>
    public static Dictionary<string, string>? MatchesPatternWithVariables(string[] args, string pattern)
    {
        EnsureInstance();
        return Instance.MatchesPatternWithVariables(args, pattern);
    }

    /// <summary>
    /// Gets all verb names available to an object (for command completion/help)
    /// </summary>
    public static List<string> GetAvailableVerbNames(string objectId, bool includeSystemVerbs = true)
    {
        EnsureInstance();
        return Instance.GetAvailableVerbNames(objectId, includeSystemVerbs);
    }

    /// <summary>
    /// Checks if a specific verb exists on an object
    /// </summary>
    public static bool HasVerb(string objectId, string verbName, bool includeSystemVerbs = true)
    {
        EnsureInstance();
        return Instance.HasVerb(objectId, verbName, includeSystemVerbs);
    }

    /// <summary>
    /// Gets verb information for display/debugging
    /// </summary>
    public static Dictionary<string, object> GetVerbInfo(Verb verb)
    {
        EnsureInstance();
        return Instance.GetVerbInfo(verb);
    }

    /// <summary>
    /// Gets all verbs on an object including inherited verbs from classes
    /// </summary>
    public static List<(Verb verb, string source)> GetAllVerbsOnObject(string objectId)
    {
        EnsureInstance();
        return Instance.GetAllVerbsOnObject(objectId);
    }

    /// <summary>
    /// Attempts to resolve and execute a command through the verb system
    /// </summary>
    public static bool TryExecuteVerb(string input, Player player, Commands.CommandProcessor commandProcessor)
    {
        EnsureInstance();
        return Instance.TryExecuteVerb(input, player, commandProcessor);
    }
}


