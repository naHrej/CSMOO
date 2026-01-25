using System.Text.RegularExpressions;
using LiteDB;
using CSMOO.Database;
using CSMOO.Scripting;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Exceptions;
using CSMOO.Commands;
using System.Collections.Generic;
using System.Linq;

namespace CSMOO.Verbs;

/// <summary>
/// Instance-based verb resolver implementation for dependency injection
/// </summary>
public class VerbResolverInstance : IVerbResolver
{
    private readonly IDbProvider _dbProvider;
    private readonly IObjectManager _objectManager;
    private readonly ILogger _logger;
    private readonly Func<IScriptEngineFactory> _scriptEngineFactoryFactory;
    
    public VerbResolverInstance(IDbProvider dbProvider, IObjectManager objectManager, ILogger logger, Func<IScriptEngineFactory> scriptEngineFactoryFactory)
    {
        _dbProvider = dbProvider;
        _objectManager = objectManager;
        _logger = logger;
        _scriptEngineFactoryFactory = scriptEngineFactoryFactory ?? throw new ArgumentNullException(nameof(scriptEngineFactoryFactory));
    }
    
    /// <summary>
    /// Finds the best matching verb for a command on an object with variable extraction
    /// </summary>
    public VerbMatchResult? FindMatchingVerbWithVariables(string objectId, string[] commandArgs, bool includeSystemVerbs = true)
    {
        if (commandArgs.Length == 0) return null;

        var verbName = commandArgs[0].ToLower();
        var allVerbs = GetVerbsForObject(objectId, includeSystemVerbs);

        // Step 1: Try to find verb by name or alias first
        Verb? foundVerb = null;
        
        // Try exact name match
        foundVerb = allVerbs.FirstOrDefault(v => v.Name?.ToLower() == verbName);
        if (foundVerb == null)
        {
            // Try alias match
            foundVerb = allVerbs.FirstOrDefault(v => 
                !string.IsNullOrEmpty(v.Aliases) && 
                v.Aliases.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                         .Any(alias => alias.ToLower() == verbName));
        }

        // Step 2: If we found a verb, check if it has a pattern
        if (foundVerb != null)
        {
            if (!string.IsNullOrEmpty(foundVerb.Pattern))
            {
                var variables = MatchesPatternWithVariables(commandArgs, foundVerb.Pattern);
                if (variables != null)
                {
                    return new VerbMatchResult(foundVerb, variables);
                }
                else
                {
                    return null; // Pattern didn't match, don't fall back to legacy
                }
            }
            else
            {
                return new VerbMatchResult(foundVerb); // No pattern, use legacy args
            }
        }

        // Step 3: If no verb found by name/alias, return null
        return null;
    }

    /// <summary>
    /// Finds the best matching verb for a command on an object (legacy method)
    /// </summary>
    public Verb? FindMatchingVerb(string objectId, string[] commandArgs, bool includeSystemVerbs = true)
    {
        if (commandArgs.Length == 0) return null;

        var verbName = commandArgs[0].ToLower();
        var allVerbs = GetVerbsForObject(objectId, includeSystemVerbs);

        // First, try exact name match
        var exactMatch = allVerbs.FirstOrDefault(v => v.Name?.ToLower() == verbName);
        if (exactMatch != null) return exactMatch;

        // Then try alias match
        var aliasMatch = allVerbs.FirstOrDefault(v => 
            !string.IsNullOrEmpty(v.Aliases) && 
            v.Aliases.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                     .Any(alias => alias.ToLower() == verbName));
        if (aliasMatch != null) return aliasMatch;

        // Finally, try pattern matching if we have arguments
        if (commandArgs.Length > 1)
        {
            var patternMatch = allVerbs.FirstOrDefault(v => 
                !string.IsNullOrEmpty(v.Pattern) && 
                MatchesPattern(commandArgs, v.Pattern));
            if (patternMatch != null) return patternMatch;
        }

        return null;
    }

    /// <summary>
    /// Gets all verbs available on an object (including inherited and system verbs)
    /// </summary>
    public List<Verb> GetVerbsForObject(string objectId, bool includeSystemVerbs = true)
    {
        var allVerbs = new List<Verb>();
        var instanceVerbs = _dbProvider.Find<Verb>("verbs", v => v.ObjectId == objectId).ToList();
        allVerbs.AddRange(instanceVerbs);

        // Get the GameObject to access its class
        var gameObject = _objectManager.GetObject(objectId);
        if (gameObject != null)
        {
            // Then get verbs from the inheritance chain (classes)
            var inheritanceChain = _objectManager.GetInheritanceChain(gameObject.ClassId);
            foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse()) // Child to parent order for proper override
            {
                var classVerbs = _dbProvider.Find<Verb>("verbs", v => v.ObjectId == objectClass.Id).ToList();
                
                // Add class verbs that aren't already overridden by instance or more specific class
                foreach (var classVerb in classVerbs)
                {
                    if (!allVerbs.Any(existing => existing.Name?.ToLower() == classVerb.Name?.ToLower()))
                    {
                        allVerbs.Add(classVerb);
                    }
                }
            }
        }

        // Add system verbs if requested
        if (includeSystemVerbs)
        {
            var systemVerbs = GetSystemVerbs();
            foreach (var systemVerb in systemVerbs)
            {
                if (!allVerbs.Any(existing => existing.Name?.ToLower() == systemVerb.Name?.ToLower()))
                {
                    allVerbs.Add(systemVerb);
                }
            }
        }

        return allVerbs;
    }

    /// <summary>
    /// Gets all system verbs (global commands)
    /// </summary>
    public List<Verb> GetSystemVerbs()
    {
        var systemObjectId = FindSystemObjectId();
        if (systemObjectId == null) return new List<Verb>();
        return _dbProvider.Find<Verb>("verbs", v => v.ObjectId == systemObjectId).ToList();
    }

    /// <summary>
    /// Checks if command arguments match a verb pattern
    /// </summary>
    private bool MatchesPattern(string[] args, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return true;

        // Simple pattern matching - "*" matches any number of words
        var patternParts = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var argIndex = 1; // Skip the verb name
        var patternIndex = 0;

        while (patternIndex < patternParts.Length && argIndex < args.Length)
        {
            var patternPart = patternParts[patternIndex];
            
            if (patternPart == "*")
            {
                // Wildcard - consume remaining args if this is the last pattern part
                if (patternIndex == patternParts.Length - 1)
                    return true;
                    
                // Otherwise consume one arg
                argIndex++;
            }
            else
            {
                // Exact match required
                if (args[argIndex].ToLower() != patternPart.ToLower())
                    return false;
                argIndex++;
            }
            
            patternIndex++;
        }

        // Pattern matches if we consumed all pattern parts
        return patternIndex == patternParts.Length;
    }

    /// <summary>
    /// Enhanced pattern matching that supports named variables like {varname}
    /// Returns a dictionary of extracted variables if the pattern matches
    /// </summary>
    public Dictionary<string, string>? MatchesPatternWithVariables(string[] args, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return new Dictionary<string, string>();

        // Handle special case: wildcard pattern "*" matches anything
        if (pattern == "*")
        {
            return new Dictionary<string, string>(); // Empty variables dict but successful match
        }

        // Only process patterns that contain {variables}
        if (!pattern.Contains("{") || !pattern.Contains("}"))
        {
            return MatchesPatternLegacy(args, pattern) ? new Dictionary<string, string>() : null;
        }

        // Convert pattern with {varname} syntax to regex
        var regexPattern = ConvertPatternToRegex(pattern);
        if (regexPattern == null) return null;

        // Join all args except the first (verb name) into a single string
        var input = string.Join(" ", args.Skip(1));
        
        var match = Regex.Match(input, regexPattern, RegexOptions.IgnoreCase);
        if (!match.Success) return null;

        // Extract variable names from the original pattern
        var variableNames = ExtractVariableNames(pattern);
        var variables = new Dictionary<string, string>();

        // Map captured groups to variable names
        for (int i = 0; i < variableNames.Count && i + 1 < match.Groups.Count; i++)
        {
            variables[variableNames[i]] = match.Groups[i + 1].Value;
        }

        return variables;
    }

    /// <summary>
    /// Legacy pattern matching for patterns without named variables
    /// </summary>
    private bool MatchesPatternLegacy(string[] args, string pattern)
    {
        // This handles the old-style patterns
        return MatchesPattern(args, pattern);
    }

    /// <summary>
    /// Converts a pattern like "give {item} to {person}" to a regex pattern
    /// </summary>
    private string? ConvertPatternToRegex(string pattern)
    {
        try
        {
            // First, let's extract variables before escaping
            var variablePattern = @"\{(\w+)\}";
            var variables = new List<string>();
            var matches = Regex.Matches(pattern, variablePattern);
            foreach (Match match in matches)
            {
                variables.Add(match.Groups[1].Value);
            }
            
            // Replace variables with a placeholder
            var tempPattern = Regex.Replace(pattern, variablePattern, "VARIABLE_PLACEHOLDER");
            
            // Now escape the pattern (this will escape everything except our placeholder)
            var escaped = Regex.Escape(tempPattern);
            
            // Replace the placeholder with the capture group
            var regexPattern = escaped.Replace("VARIABLE_PLACEHOLDER", @"(\w+)");
            
            // Allow for flexible whitespace
            regexPattern = Regex.Replace(regexPattern, @"\\ ", @"\s+");
            
            var finalPattern = $"^{regexPattern}$";
            
            return finalPattern;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error converting pattern '{pattern}' to regex: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Extracts variable names from a pattern like "give {item} to {person}"
    /// </summary>
    private List<string> ExtractVariableNames(string pattern)
    {
        var variables = new List<string>();
        var matches = Regex.Matches(pattern, @"{(\w+)}");
        
        foreach (Match match in matches)
        {
            variables.Add(match.Groups[1].Value);
        }
        
        return variables;
    }

    /// <summary>
    /// Finds the system object ID for global verbs
    /// </summary>
    private string? FindSystemObjectId()
    {
        var allObjects = _objectManager.GetAllObjects();
        var systemObj = allObjects.OfType<GameObject>().FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isSystemObject") && 
            obj.Properties["isSystemObject"].AsBoolean == true);
        
        return systemObj?.Id;
    }

    /// <summary>
    /// Gets all verb names available to an object (for command completion/help)
    /// </summary>
    public List<string> GetAvailableVerbNames(string objectId, bool includeSystemVerbs = true)
    {
        var verbs = GetVerbsForObject(objectId, includeSystemVerbs);
        var names = new List<string>();

        foreach (var verb in verbs)
        {
            if (!string.IsNullOrEmpty(verb.Name))
                names.Add(verb.Name);
                
            if (!string.IsNullOrEmpty(verb.Aliases))
            {
                var aliases = verb.Aliases.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                names.AddRange(aliases);
            }
        }

        return names.Distinct().OrderBy(name => name).ToList();
    }

    /// <summary>
    /// Checks if a specific verb exists on an object
    /// </summary>
    public bool HasVerb(string objectId, string verbName, bool includeSystemVerbs = true)
    {
        var verbs = GetVerbsForObject(objectId, includeSystemVerbs);
        verbName = verbName.ToLower();

        return verbs.Any(v => 
            v.Name?.ToLower() == verbName ||
            (!string.IsNullOrEmpty(v.Aliases) && 
             v.Aliases.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                      .Any(alias => alias.ToLower() == verbName)));
    }

    /// <summary>
    /// Gets verb information for display/debugging
    /// </summary>
    public Dictionary<string, object> GetVerbInfo(Verb verb)
    {
        var info = new Dictionary<string, object>
        {
            ["Name"] = verb.Name ?? "unnamed",
            ["ObjectId"] = verb.ObjectId,
            ["Pattern"] = verb.Pattern ?? "",
            ["Aliases"] = verb.Aliases ?? "",
            ["Permissions"] = verb.Permissions,
            ["Description"] = verb.Description ?? "",
            ["CreatedBy"] = verb.CreatedBy,
            ["CreatedAt"] = verb.CreatedAt,
            ["ModifiedAt"] = verb.ModifiedAt,
            ["CodeLength"] = verb.Code?.Length ?? 0,
            ["HasCode"] = !string.IsNullOrEmpty(verb.Code)
        };

        return info;
    }

    /// <summary>
    /// Gets all verbs on an object including inherited verbs from classes
    /// </summary>
    public List<(Verb verb, string source)> GetAllVerbsOnObject(string objectId)
    {
        var allVerbs = new List<(Verb verb, string source)>();
        
        // Get the GameObject to access its class
        var gameObject = _objectManager.GetObject(objectId);
        if (gameObject != null)
        {
            // First, get verbs directly on the object instance
            var instanceVerbs = _dbProvider.Find<Verb>("verbs", v => v.ObjectId == objectId).ToList();
            
            foreach (var verb in instanceVerbs)
            {
                allVerbs.Add((verb, "instance"));
            }
            
            // Then get verbs from the inheritance chain (classes)
            var inheritanceChain = _objectManager.GetInheritanceChain(gameObject.ClassId);
            foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse()) // Child to parent order
            {
                var classVerbs = _dbProvider.Find<Verb>("verbs", v => v.ObjectId == objectClass.Id).ToList();
                foreach (var classVerb in classVerbs)
                {
                    // Only add if not already overridden by instance or more specific class
                    if (!allVerbs.Any(existing => existing.verb.Name?.ToLower() == classVerb.Name?.ToLower()))
                    {
                        allVerbs.Add((classVerb, $"class {objectClass.Name}"));
                    }
                }
            }
        }
        else
        {
            // If not a GameObject, might be a class ID itself
            var objectClass = _dbProvider.FindById<ObjectClass>("objectclasses", objectId);
            if (objectClass != null)
            {
                var classVerbs = _dbProvider.Find<Verb>("verbs", v => v.ObjectId == objectId).ToList();
                foreach (var classVerb in classVerbs)
                {
                    allVerbs.Add((classVerb, $"class {objectClass.Name}"));
                }
            }
        }
        
        return allVerbs;
    }

    /// <summary>
    /// Attempts to resolve and execute a command through the verb system
    /// </summary>
    public bool TryExecuteVerb(string input, Player player, CommandProcessor commandProcessor)
    {
        if (string.IsNullOrWhiteSpace(input) || player == null)
            return false;

        // Ensure player has a valid location
        if (string.IsNullOrEmpty(player.Location?.Id))
        {
            // Set default location if none exists
            var defaultRoom = _dbProvider.FindOne<GameObject>("gameobjects", obj => obj.ClassId == "Room");
            if (defaultRoom != null)
            {
                player.Location = defaultRoom;
                _dbProvider.Update("players", player);
            }
            else
            {
                return false; // No rooms exist
            }
        }

        var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        var verb = parts[0].ToLower();

        // Try to find a matching verb in this order:
        // 1. Objects in the room (including the player)
        // 2. The room itself
        // 3. The player
        // 4. Global verbs (on the system object)

        // 1. Check the room itself FIRST (before objects in the room)
        // This ensures room verbs take precedence over object verbs
        var room = player.Location;
        if (room != null)
        {
            var roomVerbResult = FindMatchingVerbWithVariables(room.Id, parts);
            if (roomVerbResult != null)
            {
                var roomName = _objectManager.GetProperty(room, "name")?.AsString ?? room.Id;
                _logger.Info($"[VERB] Executing verb '{roomVerbResult.Verb.Name}' on room '{roomName}' (ID: {room.Id})");
                return ExecuteVerb(roomVerbResult.Verb, input, player, commandProcessor, room.Id, roomVerbResult.Variables);
            }
        }

        // 2. Check objects in the room (excluding exits for certain verbs)
        var roomObjects = _objectManager.GetObjectsInLocation(player.Location);
        var exitClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Exit");
        foreach (var obj in roomObjects)
        {
            // Skip exits for certain global verbs that should be handled by room/system
            // This prevents exits from intercepting commands like "look" when no target is specified
            if (exitClass != null && obj.ClassId == exitClass.Id && parts.Length == 1)
            {
                // For single-word commands like "look", skip exits to allow room/system verbs to handle them
                continue;
            }
            
            var matchResult = FindMatchingVerbWithVariables(obj.Id, parts);
            if (matchResult != null)
            {
                var objName = _objectManager.GetProperty(obj, "name")?.AsString ?? obj.Id;
                _logger.Info($"[VERB] Executing verb '{matchResult.Verb.Name}' on object '{objName}' (ID: {obj.Id})");
                return ExecuteVerb(matchResult.Verb, input, player, commandProcessor, obj.Id, matchResult.Variables);
            }
        }

        // 3. Check the player
        var playerVerbResult = FindMatchingVerbWithVariables(player.Id, parts);
        if (playerVerbResult != null)
        {
            _logger.Info($"[VERB] Executing verb '{playerVerbResult.Verb.Name}' on player '{player.Name}' (ID: {player.Id})");
            return ExecuteVerb(playerVerbResult.Verb, input, player, commandProcessor, player.Id, playerVerbResult.Variables);
        }

        // 4. Check global verbs (we'll use a special "system" object)
        var systemObject = GetOrCreateSystemObject();
        var globalVerbResult = FindMatchingVerbWithVariables(systemObject.Id, parts);
        if (globalVerbResult != null)
        {
            _logger.Info($"[VERB] Executing verb '{globalVerbResult.Verb.Name}' on system object (ID: {systemObject.Id})");
            return ExecuteVerb(globalVerbResult.Verb, input, player, commandProcessor, systemObject.Id, globalVerbResult.Variables);
        }

        // 5. Final fallback: Check for pattern-based verbs with named variables across all objects
        if (parts.Length > 1)
        {
            // Check all objects in order for verbs with named variable patterns
            var allObjectIds = new List<string>();
            
            // Add room objects
            var roomObjectList = _objectManager.GetObjectsInLocation(player.Location);
            allObjectIds.AddRange(roomObjectList.Select(obj => obj.Id));
            
            // Add room
            if (room != null) allObjectIds.Add(room.Id);
            
            // Add player
            allObjectIds.Add(player.Id);
            
            // Add system object
            allObjectIds.Add(systemObject.Id);
            
            foreach (var objectId in allObjectIds)
            {
                var allVerbs = GetVerbsForObject(objectId, objectId == systemObject.Id);
                foreach (var verbItem in allVerbs.Where(v => !string.IsNullOrEmpty(v.Pattern) && v.Pattern.Contains("{") && v.Pattern.Contains("}")))
                {
                    var variables = MatchesPatternWithVariables(parts, verbItem.Pattern);
                    if (variables != null)
                    {
                        var obj = _objectManager.GetObject(objectId);
                        var objName = obj != null ? (_objectManager.GetProperty(obj, "name")?.AsString ?? objectId) : objectId;
                        _logger.Info($"[VERB] Executing verb '{verbItem.Name}' (pattern: '{verbItem.Pattern}') on object '{objName}' (ID: {objectId})");
                        return ExecuteVerb(verbItem, input, player, commandProcessor, objectId, variables);
                    }
                }
            }
        }

        return false; // No verb found
    }

    /// <summary>
    /// Executes a verb with the script engine
    /// </summary>
    private bool ExecuteVerb(Verb verb, string input, Player player, CommandProcessor commandProcessor, string thisObjectId, Dictionary<string, string>? variables = null)
    {
        try
        {
            var scriptEngine = _scriptEngineFactoryFactory().Create();
            var result = scriptEngine.ExecuteVerbWithResult(verb, input, player, commandProcessor, thisObjectId, variables);
            return result.success;
        }
        catch (Exception ex)
        {
            // IMPORTANT: an error during a matched command is still a handled command.
            // Do not allow this to fall through and be reported as "Unknown command".
            var rawCommand = verb.Name ?? "";
            var prettyCommand = rawCommand.Length > 0
                ? char.ToUpperInvariant(rawCommand[0]) + rawCommand[1..].ToLowerInvariant()
                : rawCommand;
            commandProcessor?.SendToPlayer($"<span class='error'>Error in '<span class='command'>{prettyCommand}</span>' command</span>");

            // Check if it's our custom script exception with enhanced error reporting
            if (ex is ScriptExecutionException scriptEx)
            {
                _logger.Error(scriptEx.ToString()); // Plain text for console
                
                // Send the HTML formatted error to the player via command processor
                commandProcessor?.SendToPlayer(scriptEx.ToHtmlString());
            }
            else
            {
                _logger.Error($"Error executing verb '{verb.Name}': {ex.Message}");
                var errorMessage = $"Error executing command \"{verb.Name.ToUpperInvariant()}\":{ex.Message}";
                
                // Send to command processor
                commandProcessor?.SendToPlayer(errorMessage);
                
                // Also send directly to the player's session
                if (player?.SessionGuid.HasValue == true && commandProcessor != null)
                {
                    try
                    {
                        commandProcessor.SendToPlayer(errorMessage, player.SessionGuid.Value);
                    }
                    catch
                    {
                        // Fallback handled by commandProcessor?.SendToPlayer above
                    }
                }
            }
            
            // Clear the script stack trace in case of unhandled errors
            ScriptStackTrace.Clear();
            return true;
        }
    }

    /// <summary>
    /// Gets or creates the system object for global verbs
    /// </summary>
    private GameObject GetOrCreateSystemObject()
    {
        var allObjects = _objectManager.GetAllObjects();
        var systemObject = allObjects.OfType<GameObject>().FirstOrDefault(obj => obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true);
        if (systemObject == null)
        {
            // Create system object
            systemObject = new GameObject
            {
                Id = Guid.NewGuid().ToString(),
                Properties = new BsonDocument
                {
                    ["name"] = "System",
                    ["isSystemObject"] = true,
                    ["description"] = "System object for global verbs"
                }
            };
            _dbProvider.Insert("gameobjects", systemObject);
            _objectManager.CacheGameObject(systemObject);
        }
        return systemObject;
    }
}
