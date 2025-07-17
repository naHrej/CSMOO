using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LiteDB;
using CSMOO.Server.Database;
using CSMOO.Server.Database.Models;
using CSMOO.Server.Database.Managers;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Resolves and manages verb execution for objects
/// </summary>
public static class VerbResolver
{
    /// <summary>
    /// Finds the best matching verb for a command on an object
    /// </summary>
    public static Verb? FindMatchingVerb(string objectId, string[] commandArgs, bool includeSystemVerbs = true)
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
    public static List<Verb> GetVerbsForObject(string objectId, bool includeSystemVerbs = true)
    {
        var allVerbs = new List<Verb>();
        var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");

        // Get instance-specific verbs first (highest priority)
        var instanceVerbs = verbCollection.Find(v => v.ObjectId == objectId).ToList();
        allVerbs.AddRange(instanceVerbs);

        // Get the GameObject to access its class
        var gameObject = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (gameObject != null)
        {
            // Then get verbs from the inheritance chain (classes)
            var inheritanceChain = ObjectManager.GetInheritanceChain(gameObject.ClassId);
            foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse()) // Child to parent order for proper override
            {
                var classVerbs = verbCollection.Find(v => v.ObjectId == objectClass.Id).ToList();
                
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
    public static List<Verb> GetSystemVerbs()
    {
        var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");
        var systemObjectId = FindSystemObjectId();
        
        if (systemObjectId == null) return new List<Verb>();
        
        return verbCollection.Find(v => v.ObjectId == systemObjectId).ToList();
    }

    /// <summary>
    /// Checks if command arguments match a verb pattern
    /// </summary>
    private static bool MatchesPattern(string[] args, string pattern)
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
    /// Finds the system object ID for global verbs
    /// </summary>
    private static string? FindSystemObjectId()
    {
        var allObjects = GameDatabase.Instance.GameObjects.FindAll().ToList();
        var systemObj = allObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isSystemObject") && 
            obj.Properties["isSystemObject"].AsBoolean == true);
        
        return systemObj?.Id;
    }

    /// <summary>
    /// Gets all verb names available to an object (for command completion/help)
    /// </summary>
    public static List<string> GetAvailableVerbNames(string objectId, bool includeSystemVerbs = true)
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
    public static bool HasVerb(string objectId, string verbName, bool includeSystemVerbs = true)
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
    public static Dictionary<string, object> GetVerbInfo(Verb verb)
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
    public static List<(Verb verb, string source)> GetAllVerbsOnObject(string objectId)
    {
        var allVerbs = new List<(Verb verb, string source)>();
        
        // Get the GameObject to access its class
        var gameObject = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (gameObject != null)
        {
            // First, get verbs directly on the object instance
            var instanceVerbs = GameDatabase.Instance.GetCollection<Verb>("verbs")
                .Find(v => v.ObjectId == objectId)
                .ToList();
            
            foreach (var verb in instanceVerbs)
            {
                allVerbs.Add((verb, "instance"));
            }
            
            // Then get verbs from the inheritance chain (classes)
            var inheritanceChain = ObjectManager.GetInheritanceChain(gameObject.ClassId);
            foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse()) // Child to parent order
            {
                var classVerbs = GameDatabase.Instance.GetCollection<Verb>("verbs")
                    .Find(v => v.ObjectId == objectClass.Id)
                    .ToList();
                
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
            var objectClass = GameDatabase.Instance.ObjectClasses.FindById(objectId);
            if (objectClass != null)
            {
                var classVerbs = GameDatabase.Instance.GetCollection<Verb>("verbs")
                    .Find(v => v.ObjectId == objectId)
                    .ToList();
                
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
    public static bool TryExecuteVerb(string input, Database.Player player, Commands.CommandProcessor commandProcessor)
    {
        if (string.IsNullOrWhiteSpace(input) || player == null)
            return false;

        // Ensure player has a valid location
        if (string.IsNullOrEmpty(player.Location))
        {
            // Set default location if none exists
            var defaultRoom = GameDatabase.Instance.GameObjects.FindOne(obj => obj.ClassId == "Room");
            if (defaultRoom != null)
            {
                player.Location = defaultRoom.Id;
                GameDatabase.Instance.Players.Update(player);
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

        // 1. Check objects in the room
        var roomObjects = ObjectManager.GetObjectsInLocation(player.Location);
        foreach (var obj in roomObjects)
        {
            var matchedVerb = FindMatchingVerb(obj.Id, parts);
            if (matchedVerb != null)
            {
                return ExecuteVerb(matchedVerb, input, player, commandProcessor, obj.Id);
            }
        }

        // 2. Check the room itself
        var room = GameDatabase.Instance.GameObjects.FindById(player.Location);
        if (room != null)
        {
            var roomVerb = FindMatchingVerb(room.Id, parts);
            if (roomVerb != null)
            {
                return ExecuteVerb(roomVerb, input, player, commandProcessor, room.Id);
            }
        }

        // 3. Check the player
        var playerVerb = FindMatchingVerb(player.Id, parts);
        if (playerVerb != null)
        {
            return ExecuteVerb(playerVerb, input, player, commandProcessor, player.Id);
        }

        // 4. Check global verbs (we'll use a special "system" object)
        var systemObject = GetOrCreateSystemObject();
        var globalVerb = FindMatchingVerb(systemObject.Id, parts);
        if (globalVerb != null)
        {
            return ExecuteVerb(globalVerb, input, player, commandProcessor, systemObject.Id);
        }

        // 5. Fallback: Check if this is a movement command (single word that matches an exit)
        if (parts.Length == 1)
        {
            var direction = parts[0].ToLower();
            if (TryExecuteMovementCommand(direction, player, commandProcessor))
            {
                return true;
            }
        }

        return false; // No verb found
    }

    /// <summary>
    /// Executes a verb with the script engine
    /// </summary>
    private static bool ExecuteVerb(Verb verb, string input, Database.Player player, Commands.CommandProcessor commandProcessor, string thisObjectId)
    {
        try
        {
            var scriptEngine = new VerbScriptEngine();
            var result = scriptEngine.ExecuteVerb(verb, input, player, commandProcessor, thisObjectId);
            
            // Log successful execution
            Logger.Debug($"Executed verb '{verb.Name}' on object {thisObjectId} for player {player.Name}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error executing verb '{verb.Name}': {ex.Message}");
            commandProcessor?.SendToPlayer($"Error executing command: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tries to execute a movement command by calling the 'go' verb with the direction
    /// </summary>
    private static bool TryExecuteMovementCommand(string direction, Database.Player player, Commands.CommandProcessor commandProcessor)
    {
        // Get current room
        var room = GameDatabase.Instance.GameObjects.FindById(player.Location);
        if (room == null) return false;

        // Get exits from current room to check if the direction is valid
        var exitClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Exit");
        if (exitClass == null) return false;

        var exits = ObjectManager.GetObjectsInLocation(room.Id)
            .Where(obj => obj.ClassId == exitClass.Id)
            .ToList();

        if (exits.Count == 0) return false;

        // Common direction mappings
        var directionMap = new Dictionary<string, string> {
            {"n", "north"}, {"s", "south"}, {"e", "east"}, {"w", "west"},
            {"ne", "northeast"}, {"nw", "northwest"}, {"se", "southeast"}, {"sw", "southwest"},
            {"u", "up"}, {"d", "down"}
        };

        // Normalize direction (convert abbreviations to full names)
        var normalizedDirection = directionMap.ContainsKey(direction) ? directionMap[direction] : direction;

        // Check if the word matches any exit direction or alias
        bool hasMatchingExit = false;
        foreach (var exit in exits)
        {
            var exitDirection = exit.Properties.ContainsKey("direction") ? exit.Properties["direction"].AsString?.ToLower() : null;
            var aliases = exit.Properties.ContainsKey("aliases") ? exit.Properties["aliases"].AsString?.ToLower() : null;
            
            if (exitDirection == direction || exitDirection == normalizedDirection)
            {
                hasMatchingExit = true;
                break;
            }

            // Check aliases
            if (!string.IsNullOrEmpty(aliases))
            {
                var aliasArray = aliases.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (aliasArray.Any(alias => alias == direction || alias == normalizedDirection))
                {
                    hasMatchingExit = true;
                    break;
                }
            }
        }

        if (!hasMatchingExit) return false;

        // Found a matching exit, call the 'go' verb with this direction
        var systemObject = GetOrCreateSystemObject();
        var goVerb = GetSystemVerbs().FirstOrDefault(v => v.Name?.ToLower() == "go");
        
        if (goVerb != null)
        {
            // Execute the 'go' verb with the direction as argument
            var goInput = $"go {normalizedDirection}";
            return ExecuteVerb(goVerb, goInput, player, commandProcessor, systemObject.Id);
        }

        return false;
    }

    /// <summary>
    /// Gets or creates the system object for global verbs
    /// </summary>
    private static Database.GameObject GetOrCreateSystemObject()
    {
        var systemObject = GameDatabase.Instance.GameObjects.Query()
            .ToList()
            .FirstOrDefault(obj => obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true);
        
        if (systemObject == null)
        {
            // Create system object
            systemObject = new Database.GameObject
            {
                Id = Guid.NewGuid().ToString(),
                ClassId = "Object", // Base object class
                Properties = new BsonDocument
                {
                    ["name"] = "System",
                    ["isSystemObject"] = true,
                    ["description"] = "System object for global verbs"
                }
            };
            GameDatabase.Instance.GameObjects.Insert(systemObject);
        }
        
        return systemObject;
    }
}
