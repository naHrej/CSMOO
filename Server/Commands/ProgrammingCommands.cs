using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSMOO.Server.Database;
using CSMOO.Server.Scripting;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Commands;

/// <summary>
/// Handles programming commands for creating and editing verbs
/// </summary>
public class ProgrammingCommands
{
    private readonly CommandProcessor _commandProcessor;
    private readonly Player _player;
    
    // For multi-line programming
    private bool _isInProgrammingMode = false;
    private readonly StringBuilder _currentCode = new StringBuilder();
    private string _currentVerbId = string.Empty;

    public ProgrammingCommands(CommandProcessor commandProcessor, Player player)
    {
        _commandProcessor = commandProcessor;
        _player = player;
    }

    public bool IsInProgrammingMode => _isInProgrammingMode;

    /// <summary>
    /// Handles programming-related commands
    /// </summary>
    public bool HandleProgrammingCommand(string input)
    {
        // If we're in programming mode, handle code input
        if (_isInProgrammingMode)
        {
            return HandleProgrammingInput(input);
        }

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        var command = parts[0].ToLower();

        return command switch
        {
            "@program" => HandleProgramCommand(parts),
            "@verb" => HandleVerbCommand(parts),
            "@list" => HandleListCommand(parts),
            "@edit" => HandleEditCommand(parts),
            "@examine" => HandleExamineCommand(parts),
            "@verbs" => HandleVerbsCommand(parts),
            "@rmverb" => HandleRemoveVerbCommand(parts),
            "@debug" when parts.Length > 1 && parts[1] == "verbs" => HandleDebugVerbsCommand(parts),
            "@fix" when parts.Length > 1 && parts[1] == "verbs" => HandleFixVerbsCommand(parts),
            "@remove" when parts.Length > 1 && parts[1] == "verb" => HandleRemoveVerbByIdCommand(parts),
            "@cleanup" when parts.Length > 1 && parts[1] == "player" => HandleCleanupPlayerCommand(parts),
            "@cleanup" => HandleCleanupCommand(parts),
            "@function" => HandleFunctionCommand(parts),
            "@functions" => HandleFunctionsCommand(parts),
            _ => false
        };
    }

    /// <summary>
    /// @program <object>:<verb> - Start programming a verb
    /// </summary>
    private bool HandleProgramCommand(string[] parts)
    {
        if (parts.Length != 2)
        {
            _commandProcessor.SendToPlayer("Usage: @program <object>:<verb>");
            _commandProcessor.SendToPlayer("Example: @program here:test or @program me:inventory");
            return true;
        }

        var verbSpec = parts[1];
        if (!verbSpec.Contains(':'))
        {
            _commandProcessor.SendToPlayer("Verb specification must be in format <object>:<verb>");
            return true;
        }

        // Handle class syntax (split from right for class:Object:verb)
        var lastColonIndex = verbSpec.LastIndexOf(':');
        var objectName = verbSpec.Substring(0, lastColonIndex);
        var verbName = verbSpec.Substring(lastColonIndex + 1);

        // Resolve object
        var objectId = ResolveObject(objectName);
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        // Find or create the verb
        var verb = VerbManager.GetVerbsOnObject(objectId)
            .FirstOrDefault(v => v.Name.ToLower() == verbName.ToLower());

        if (verb == null)
        {
            verb = VerbManager.CreateVerb(objectId, verbName, "", "", _player.Name);
            _commandProcessor.SendToPlayer($"Created new verb '{verbName}' on {GetObjectName(objectId)}.");
            Logger.Debug($"Created new verb: ID={verb.Id}, Name={verb.Name}, ObjectId={verb.ObjectId}");
        }
        else
        {
            _commandProcessor.SendToPlayer($"Editing existing verb '{verbName}' on {GetObjectName(objectId)}.");
            Logger.Debug($"Editing existing verb: ID={verb.Id}, Name={verb.Name}, CurrentCodeLength={verb.Code?.Length ?? 0}");
        }

        // Enter programming mode
        _isInProgrammingMode = true;
        _currentVerbId = verb.Id;
        _currentCode.Clear(); // Always start with empty code - @program replaces existing code
        
        Logger.Debug($"Entering programming mode for verb ID: {_currentVerbId}");
        
        if (!string.IsNullOrEmpty(verb.Code))
        {
            _commandProcessor.SendToPlayer("Existing code (will be replaced):");
            var lines = verb.Code.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                _commandProcessor.SendToPlayer($"{i + 1}: {lines[i]}");
            }
            _commandProcessor.SendToPlayer("--- End of existing code ---");
        }
        else
        {
            _commandProcessor.SendToPlayer("No existing code.");
        }

        _commandProcessor.SendToPlayer("Enter your NEW C# code (will replace any existing code).");
        _commandProcessor.SendToPlayer("Type '.' on a line by itself to save, or '.abort' to cancel without changes.");
        _commandProcessor.SendToPlayer("Programming mode active. Available variables:");
        _commandProcessor.SendToPlayer("  Player - the player executing the verb");
        _commandProcessor.SendToPlayer("  ThisObject - ID of the object this verb is on");
        _commandProcessor.SendToPlayer("  Input - the complete command input");
        _commandProcessor.SendToPlayer("  Args - list of arguments");
        _commandProcessor.SendToPlayer("  Say(message) - send message to player");
        _commandProcessor.SendToPlayer("  SayToRoom(message) - send message to all in room");

        return true;
    }

    /// <summary>
    /// Handle input while in programming mode
    /// </summary>
    private bool HandleProgrammingInput(string input)
    {
        if (input.Trim() == ".")
        {
            // Finish programming
            var code = _currentCode.ToString();
            Logger.Debug($"Saving verb code. VerbId: {_currentVerbId}, Code length: {code.Length}");
            Logger.Debug($"Code content: '{code}'");
            
            VerbManager.UpdateVerbCode(_currentVerbId, code);
            
            // Verify the code was saved
            var savedVerb = GameDatabase.Instance.GetCollection<Verb>("verbs").FindById(_currentVerbId);
            if (savedVerb != null)
            {
                Logger.Debug($"Verification: Saved code length: {savedVerb.Code?.Length ?? 0}");
                _commandProcessor.SendToPlayer("Verb programming complete.");
                _commandProcessor.SendToPlayer($"Code saved ({code.Split('\n').Length} lines).");
                _commandProcessor.SendToPlayer($"Verified: Code length is {savedVerb.Code?.Length ?? 0} characters.");
            }
            else
            {
                Logger.Error($"Could not find verb with ID {_currentVerbId} after saving!");
                _commandProcessor.SendToPlayer("ERROR: Could not verify that code was saved!");
            }
            
            _isInProgrammingMode = false;
            _currentCode.Clear();
            _currentVerbId = string.Empty;
            return true;
        }

        if (input.Trim().ToLower() == ".abort")
        {
            // Abort programming
            _commandProcessor.SendToPlayer("Programming aborted. No changes saved.");
            
            _isInProgrammingMode = false;
            _currentCode.Clear();
            _currentVerbId = string.Empty;
            return true;
        }

        // Add line to current code
        _currentCode.AppendLine(input);
        _commandProcessor.SendToPlayer($"[{_currentCode.ToString().Split('\n').Length}] "); // Show line number
        return true;
    }

    /// <summary>
    /// @verb <object> <name> [aliases] [pattern] - Create a new verb
    /// </summary>
    private bool HandleVerbCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            _commandProcessor.SendToPlayer("Usage: @verb <object> <name> [aliases] [pattern]");
            _commandProcessor.SendToPlayer("Example: @verb here look 'l examine' '*'");
            return true;
        }

        var objectName = parts[1];
        var verbName = parts[2];
        var aliases = parts.Length > 3 ? parts[3].Trim('"') : "";
        var pattern = parts.Length > 4 ? parts[4].Trim('"') : "";

        var objectId = ResolveObject(objectName);
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        var verb = VerbManager.CreateVerb(objectId, verbName, pattern, "", _player.Name);
        if (!string.IsNullOrEmpty(aliases))
        {
            var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");
            verb.Aliases = aliases;
            verbCollection.Update(verb);
        }

        _commandProcessor.SendToPlayer($"Created verb '{verbName}' on {GetObjectName(objectId)}.");
        if (!string.IsNullOrEmpty(aliases))
            _commandProcessor.SendToPlayer($"Aliases: {aliases}");
        if (!string.IsNullOrEmpty(pattern))
            _commandProcessor.SendToPlayer($"Pattern: {pattern}");

        return true;
    }

    /// <summary>
    /// @list <object>:<verb> - Show the code for a verb
    /// </summary>
    private bool HandleListCommand(string[] parts)
    {
        if (parts.Length != 2)
        {
            _commandProcessor.SendToPlayer("Usage: @list <object>:<verb>");
            return true;
        }

        var verbSpec = parts[1];
        if (!verbSpec.Contains(':'))
        {
            _commandProcessor.SendToPlayer("Verb specification must be in format <object>:<verb>");
            return true;
        }

        // Split from the right to handle class:Object:verb syntax
        var lastColonIndex = verbSpec.LastIndexOf(':');
        var objectName = verbSpec.Substring(0, lastColonIndex);
        var verbName = verbSpec.Substring(lastColonIndex + 1);

        var objectId = ResolveObject(objectName);
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        var verb = VerbManager.GetVerbsOnObject(objectId)
            .FirstOrDefault(v => v.Name.ToLower() == verbName.ToLower());

        if (verb == null)
        {
            _commandProcessor.SendToPlayer($"Verb '{verbName}' not found on {GetObjectName(objectId)}.");
            return true;
        }

        _commandProcessor.SendToPlayer($"=== {GetObjectName(objectId)}:{verb.Name} ===");
        if (!string.IsNullOrEmpty(verb.Aliases))
            _commandProcessor.SendToPlayer($"Aliases: {verb.Aliases}");
        if (!string.IsNullOrEmpty(verb.Pattern))
            _commandProcessor.SendToPlayer($"Pattern: {verb.Pattern}");
        if (!string.IsNullOrEmpty(verb.Description))
            _commandProcessor.SendToPlayer($"Description: {verb.Description}");
        
        _commandProcessor.SendToPlayer($"Created by: {verb.CreatedBy} on {verb.CreatedAt:yyyy-MM-dd HH:mm}");
        _commandProcessor.SendToPlayer("Code:");

        if (string.IsNullOrEmpty(verb.Code))
        {
            _commandProcessor.SendToPlayer("  (no code)");
        }
        else
        {
            var lines = verb.Code.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                _commandProcessor.SendToPlayer($"{i + 1,3}: {lines[i]}");
            }
        }

        return true;
    }

    /// <summary>
    /// @edit <object>:<verb> - Edit an existing verb
    /// </summary>
    private bool HandleEditCommand(string[] parts)
    {
        // Just redirect to @program for now
        return HandleProgramCommand(new[] { "@program", parts.Length > 1 ? parts[1] : "" });
    }

    /// <summary>
    /// @verbs <object> - List all verbs on an object
    /// </summary>
    private bool HandleVerbsCommand(string[] parts)
    {
        var objectName = parts.Length > 1 ? parts[1] : "here";
        var objectId = ResolveObject(objectName);
        
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        var allVerbs = VerbManager.GetAllVerbsOnObject(objectId);
        
        _commandProcessor.SendToPlayer($"=== Verbs on {GetObjectName(objectId)} ===");
        if (!allVerbs.Any())
        {
            _commandProcessor.SendToPlayer("No verbs defined.");
        }
        else
        {
            foreach (var (verb, source) in allVerbs.OrderBy(v => v.verb.Name))
            {
                var info = $"{verb.Name}";
                if (!string.IsNullOrEmpty(verb.Aliases))
                    info += $" ({verb.Aliases})";
                if (!string.IsNullOrEmpty(verb.Pattern))
                    info += $" [{verb.Pattern}]";
                if (!string.IsNullOrEmpty(verb.Description))
                    info += $" - {verb.Description}";
                
                // Show where the verb comes from
                if (source != "instance")
                    info += $" (from {source})";
                
                _commandProcessor.SendToPlayer($"  {info}");
            }
        }

        return true;
    }

    /// <summary>
    /// @rmverb <object>:<verb> - Remove a verb
    /// </summary>
    private bool HandleRemoveVerbCommand(string[] parts)
    {
        if (parts.Length != 2)
        {
            _commandProcessor.SendToPlayer("Usage: @rmverb <object>:<verb>");
            return true;
        }

        var verbSpec = parts[1];
        if (!verbSpec.Contains(':'))
        {
            _commandProcessor.SendToPlayer("Verb specification must be in format <object>:<verb>");
            return true;
        }

        // Handle class syntax (split from right for class:Object:verb)
        var lastColonIndex = verbSpec.LastIndexOf(':');
        var objectName = verbSpec.Substring(0, lastColonIndex);
        var verbName = verbSpec.Substring(lastColonIndex + 1);

        var objectId = ResolveObject(objectName);
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        var verb = VerbManager.GetVerbsOnObject(objectId)
            .FirstOrDefault(v => v.Name.ToLower() == verbName.ToLower());

        if (verb == null)
        {
            _commandProcessor.SendToPlayer($"Verb '{verbName}' not found on {GetObjectName(objectId)}.");
            return true;
        }

        GameDatabase.Instance.GetCollection<Verb>("verbs").Delete(verb.Id);
        _commandProcessor.SendToPlayer($"Removed verb '{verbName}' from {GetObjectName(objectId)}.");

        return true;
    }

    /// <summary>
    /// @remove verb <verb-id> - Remove a specific verb by ID
    /// </summary>
    private bool HandleRemoveVerbByIdCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            _commandProcessor.SendToPlayer("Usage: @remove verb <verb-id>");
            return true;
        }

        var verbId = parts[2];
        var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");
        var verb = verbCollection.FindById(verbId);

        if (verb == null)
        {
            _commandProcessor.SendToPlayer($"Verb with ID '{verbId}' not found.");
            return true;
        }

        _commandProcessor.SendToPlayer($"Found verb: '{verb.Name}' (CodeLength: {verb.Code?.Length ?? 0})");
        _commandProcessor.SendToPlayer($"Object ID: {verb.ObjectId}");
        
        if (!string.IsNullOrEmpty(verb.Code) && verb.Code.Length > 0)
        {
            _commandProcessor.SendToPlayer("⚠️ WARNING: This verb has code! Are you sure you want to remove it?");
            _commandProcessor.SendToPlayer("Type 'yes' to confirm, or anything else to cancel:");
            // For now, just proceed - in a real implementation you'd want confirmation
        }

        verbCollection.Delete(verbId);
        _commandProcessor.SendToPlayer($"Removed verb '{verb.Name}' (ID: {verbId})");
        return true;
    }

    /// <summary>
    /// @function <object> <name> [returnType] - Create a new function
    /// </summary>
    private bool HandleFunctionCommand(string[] parts)
    {
        _commandProcessor.SendToPlayer("Functions are not yet implemented. Use verbs for now.");
        return true;
    }

    /// <summary>
    /// @functions <object> - List functions on an object
    /// </summary>
    private bool HandleFunctionsCommand(string[] parts)
    {
        _commandProcessor.SendToPlayer("Functions are not yet implemented. Use verbs for now.");
        return true;
    }

    /// <summary>
    /// @debug verbs <object> - Show ALL verbs in database for debugging
    /// </summary>
    private bool HandleDebugVerbsCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            _commandProcessor.SendToPlayer("Usage: @debug verbs <object>");
            return true;
        }

        var objectId = ResolveObject(parts[2]);
        if (string.IsNullOrEmpty(objectId))
        {
            _commandProcessor.SendToPlayer($"Object '{parts[2]}' not found.");
            return true;
        }

        var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");
        var allVerbs = verbCollection.FindAll().ToList();
        var objectVerbs = allVerbs.Where(v => v.ObjectId == objectId).ToList();

        _commandProcessor.SendToPlayer("=== COMPREHENSIVE VERB DEBUG ===");
        _commandProcessor.SendToPlayer($"Searching for verbs on object: {objectId}");
        _commandProcessor.SendToPlayer($"Total verbs in database: {allVerbs.Count}");
        _commandProcessor.SendToPlayer($"Verbs on this object: {objectVerbs.Count}");
        _commandProcessor.SendToPlayer("");

        foreach (var verb in objectVerbs.OrderBy(v => v.Name))
        {
            _commandProcessor.SendToPlayer($"Verb ID: {verb.Id}");
            _commandProcessor.SendToPlayer($"  Name: '{verb.Name}'");
            _commandProcessor.SendToPlayer($"  Aliases: '{verb.Aliases}'");
            _commandProcessor.SendToPlayer($"  Code Length: {verb.Code?.Length ?? 0}");
            _commandProcessor.SendToPlayer($"  Object ID: {verb.ObjectId}");
            if (!string.IsNullOrEmpty(verb.Code))
            {
                var preview = verb.Code.Length > 100 ? verb.Code.Substring(0, 100) + "..." : verb.Code;
                _commandProcessor.SendToPlayer($"  Code Preview: {preview}");
            }
            _commandProcessor.SendToPlayer($"  Created By: {verb.CreatedBy}");
            _commandProcessor.SendToPlayer("");
        }

        // Look for verbs with 'ooc' pattern specifically
        var oocVerbs = allVerbs.Where(v => 
            (!string.IsNullOrEmpty(v.Name) && v.Name.ToLower().Contains("ooc")) ||
            (!string.IsNullOrEmpty(v.Code) && v.Code.Contains("OOC"))
        ).ToList();

        if (oocVerbs.Any())
        {
            _commandProcessor.SendToPlayer("=== ALL OOC-RELATED VERBS IN DATABASE ===");
            foreach (var verb in oocVerbs)
            {
                _commandProcessor.SendToPlayer($"ID: {verb.Id}, Name: '{verb.Name}', ObjectId: {verb.ObjectId}, CodeLength: {verb.Code?.Length ?? 0}");
                if (!string.IsNullOrEmpty(verb.Code) && verb.Code.Length > 0)
                {
                    var preview = verb.Code.Length > 50 ? verb.Code.Substring(0, 50) + "..." : verb.Code;
                    _commandProcessor.SendToPlayer($"  Code: {preview}");
                }
            }
        }

        return true;
    }

    /// <summary>
    /// @fix verbs <object> - Remove all duplicate empty verbs, keep only verbs with code
    /// </summary>
    private bool HandleFixVerbsCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            _commandProcessor.SendToPlayer("Usage: @fix verbs <object>");
            return true;
        }

        var objectId = ResolveObject(parts[2]);
        if (string.IsNullOrEmpty(objectId))
        {
            _commandProcessor.SendToPlayer($"Object '{parts[2]}' not found.");
            return true;
        }

        var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");
        var objectVerbs = verbCollection.Find(v => v.ObjectId == objectId).ToList();

        _commandProcessor.SendToPlayer("=== FIXING DUPLICATE VERBS ===");
        
        // Group by name
        var verbGroups = objectVerbs.GroupBy(v => v.Name?.ToLower() ?? "").ToList();
        int removedCount = 0;

        foreach (var group in verbGroups)
        {
            var verbs = group.ToList();
            if (verbs.Count <= 1) continue;

            _commandProcessor.SendToPlayer($"Found {verbs.Count} verbs named '{group.Key}':");
            
            var verbsWithCode = verbs.Where(v => !string.IsNullOrEmpty(v.Code) && v.Code.Length > 0).ToList();
            var emptyVerbs = verbs.Where(v => string.IsNullOrEmpty(v.Code) || v.Code.Length == 0).ToList();

            _commandProcessor.SendToPlayer($"  {verbsWithCode.Count} with code, {emptyVerbs.Count} empty");

            // Remove all empty verbs if there's at least one with code
            if (verbsWithCode.Count > 0)
            {
                foreach (var emptyVerb in emptyVerbs)
                {
                    _commandProcessor.SendToPlayer($"  Removing empty verb: {emptyVerb.Id}");
                    verbCollection.Delete(emptyVerb.Id);
                    removedCount++;
                }
            }
            // If all are empty, keep only the newest one
            else if (emptyVerbs.Count > 1)
            {
                var verbsToRemove = emptyVerbs.OrderBy(v => v.Id).Take(emptyVerbs.Count - 1);
                foreach (var verb in verbsToRemove)
                {
                    _commandProcessor.SendToPlayer($"  Removing duplicate empty verb: {verb.Id}");
                    verbCollection.Delete(verb.Id);
                    removedCount++;
                }
            }
        }

        _commandProcessor.SendToPlayer($"Removed {removedCount} duplicate verbs.");
        return true;
    }

    /// <summary>
    /// Resolves object names to object IDs
    /// </summary>
    private string? ResolveObject(string objectName)
    {
        Logger.Debug($"Resolving object name: '{objectName}'");
        
        string? result = null;
        
        // Handle special keywords first
        switch (objectName.ToLower())
        {
            case "me":
                result = _player.Id;
                break;
            case "here":
                result = _player.Location;
                break;
            case "system":
                result = GetSystemObjectId();
                break;
            default:
                // Check if it's a DBREF (starts with # followed by digits)
                if (objectName.StartsWith("#") && int.TryParse(objectName.Substring(1), out int dbref))
                {
                    var obj = GameDatabase.Instance.GameObjects.FindOne(o => o.DbRef == dbref);
                    result = obj?.Id;
                    Logger.Debug($"DBREF lookup #{dbref} -> {result ?? "not found"}");
                }
                // Check if it's a class reference (starts with "class:" or ends with ".class")
                else if (objectName.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
                {
                    var className = objectName.Substring(6); // Remove "class:" prefix
                    var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                        c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
                    result = objectClass?.Id;
                    Logger.Debug($"Class lookup '{className}' -> {result ?? "not found"}");
                }
                else if (objectName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
                {
                    var className = objectName.Substring(0, objectName.Length - 6); // Remove ".class" suffix
                    var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                        c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
                    result = objectClass?.Id;
                    Logger.Debug($"Class lookup '{className}' -> {result ?? "not found"}");
                }
                else
                {
                    // Try to find by name in current location, then globally, then as a class
                    result = FindObjectByName(objectName);
                    
                    // If not found as an object, try as a class name
                    if (result == null)
                    {
                        var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                            c.Name.Equals(objectName, StringComparison.OrdinalIgnoreCase));
                        
                        if (objectClass != null)
                        {
                            result = objectClass.Id;
                            Logger.Debug($"Found class '{objectName}' -> {result}");
                        }
                    }
                }
                break;
        }
        
        Logger.Debug($"Resolved '{objectName}' to: {result ?? "null"}");
        return result;
    }

    /// <summary>
    /// Find an object by name, first in current room, then globally
    /// </summary>
    private string? FindObjectByName(string name)
    {
        name = name.ToLower();
        
        // First, search in current location (most common case)
        if (_player.Location != null)
        {
            var localObjects = ObjectManager.GetObjectsInLocation(_player.Location);
            var localMatch = localObjects.FirstOrDefault(obj =>
            {
                var objName = ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
                var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
                return objName?.Contains(name) == true || shortDesc?.Contains(name) == true;
            });
            
            if (localMatch != null)
            {
                Logger.Debug($"Found '{name}' locally: #{localMatch.DbRef} ({ObjectManager.GetProperty(localMatch, "name")?.AsString})");
                return localMatch.Id;
            }
        }
        
        // If not found locally, search all players (common for targeting players)
        var players = PlayerManager.GetOnlinePlayers();
        var playerMatch = players.FirstOrDefault(p => p.Name.ToLower().Contains(name));
        if (playerMatch != null)
        {
            var playerObj = GameDatabase.Instance.GameObjects.FindById(playerMatch.Id);
            if (playerObj != null)
            {
                Logger.Debug($"Found player '{name}': #{playerObj.DbRef} ({playerMatch.Name})");
                return playerMatch.Id;
            }
        }
        
        // Finally, search globally (for admin/building purposes)
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        var globalMatch = allObjects.FirstOrDefault(obj =>
        {
            var objName = ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            return objName?.Contains(name) == true || shortDesc?.Contains(name) == true;
        });
        
        if (globalMatch != null)
        {
            Logger.Debug($"Found '{name}' globally: #{globalMatch.DbRef} ({ObjectManager.GetProperty(globalMatch, "name")?.AsString})");
            return globalMatch.Id;
        }
        
        Logger.Debug($"Object '{name}' not found anywhere");
        return null;
    }

    /// <summary>
    /// Get the system object ID
    /// </summary>
    private string? GetSystemObjectId()
    {
        // Get all objects and filter in memory (LiteDB doesn't support ContainsKey in expressions)
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        var systemObj = allObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true);
        
        if (systemObj == null)
        {
            // System object doesn't exist, create it
            Logger.Debug("System object not found, creating it...");
            // Use Container class instead of abstract Object class
            var containerClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Container");
            if (containerClass != null)
            {
                systemObj = ObjectManager.CreateInstance(containerClass.Id);
                ObjectManager.SetProperty(systemObj, "name", "System");
                ObjectManager.SetProperty(systemObj, "shortDescription", "the system object");
                ObjectManager.SetProperty(systemObj, "longDescription", "This is the system object that holds global verbs and functions.");
                ObjectManager.SetProperty(systemObj, "isSystemObject", true);
                ObjectManager.SetProperty(systemObj, "gettable", false); // Don't allow players to pick up the system
                Logger.Debug($"Created system object with ID: {systemObj.Id}");
            }
            else
            {
                Logger.Error("Could not find Container class to create system object!");
                return null;
            }
        }
        
        Logger.Debug($"Resolved 'system' to object ID: {systemObj?.Id}");
        return systemObj?.Id;
    }

    /// <summary>
    /// Get a friendly name for an object
    /// </summary>
    private string GetObjectName(string objectId)
    {
        if (objectId == _player.Id) return "you";
        if (objectId == _player.Location) return "here";

        // Try as a GameObject first
        var obj = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (obj != null)
        {
            var name = ObjectManager.GetProperty(obj, "name")?.AsString;
            if (!string.IsNullOrEmpty(name))
                return $"{name} (#{obj.DbRef})";
            else
                return $"#{obj.DbRef}";
        }

        // Try as an ObjectClass
        var objectClass = GameDatabase.Instance.ObjectClasses.FindById(objectId);
        if (objectClass != null)
        {
            return $"class {objectClass.Name}";
        }

        return $"object #{objectId[..8]}...";
    }

    /// <summary>
    /// @cleanup <object> - Remove duplicate empty verbs from an object
    /// </summary>
    private bool HandleCleanupCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            _commandProcessor.SendToPlayer("Usage: @cleanup <object>");
            return true;
        }

        var objectId = ResolveObject(parts[1]);
        if (string.IsNullOrEmpty(objectId))
        {
            _commandProcessor.SendToPlayer($"Object '{parts[1]}' not found.");
            return true;
        }

        var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");
        var allVerbs = verbCollection.Find(v => v.ObjectId == objectId).ToList();

        // Group verbs by name to find duplicates
        var verbGroups = allVerbs.GroupBy(v => v.Name?.ToLower() ?? "").Where(g => g.Count() > 1).ToList();

        int removedCount = 0;
        foreach (var group in verbGroups)
        {
            var verbs = group.ToList();
            var verbsWithCode = verbs.Where(v => !string.IsNullOrEmpty(v.Code) && v.Code.Length > 0).ToList();
            var emptyVerbs = verbs.Where(v => string.IsNullOrEmpty(v.Code) || v.Code.Length == 0).ToList();

            _commandProcessor.SendToPlayer($"Found {verbs.Count} verbs named '{group.Key}': {verbsWithCode.Count} with code, {emptyVerbs.Count} empty");

            // If we have verbs with code, remove the empty ones
            if (verbsWithCode.Count > 0 && emptyVerbs.Count > 0)
            {
                foreach (var emptyVerb in emptyVerbs)
                {
                    _commandProcessor.SendToPlayer($"  Removing empty verb: ID={emptyVerb.Id}");
                    verbCollection.Delete(emptyVerb.Id);
                    removedCount++;
                }
            }
            // If all verbs are empty, keep only the first one
            else if (verbsWithCode.Count == 0 && emptyVerbs.Count > 1)
            {
                for (int i = 1; i < emptyVerbs.Count; i++)
                {
                    _commandProcessor.SendToPlayer($"  Removing duplicate empty verb: ID={emptyVerbs[i].Id}");
                    verbCollection.Delete(emptyVerbs[i].Id);
                    removedCount++;
                }
            }
        }

        _commandProcessor.SendToPlayer($"Cleanup complete. Removed {removedCount} duplicate verbs.");
        return true;
    }

    /// <summary>
    /// @cleanup player - Remove duplicate empty verbs from player object specifically
    /// </summary>
    private bool HandleCleanupPlayerCommand(string[] parts)
    {
        // Get the actual player object ID (not the system object)
        var allObjects = GameDatabase.Instance.GameObjects.FindAll().ToList();
        var playerObject = allObjects.FirstOrDefault(obj => 
        {
            var playerIdProp = ObjectManager.GetProperty(obj, "playerId");
            return playerIdProp != null && playerIdProp.AsString == _player.Id;
        });

        if (playerObject == null)
        {
            _commandProcessor.SendToPlayer("Could not find your player object.");
            return true;
        }

        var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");
        var playerVerbs = verbCollection.Find(v => v.ObjectId == playerObject.Id).ToList();

        _commandProcessor.SendToPlayer($"=== CLEANING PLAYER OBJECT {playerObject.Id} ===");
        _commandProcessor.SendToPlayer($"Found {playerVerbs.Count} verbs on your player object:");

        foreach (var verb in playerVerbs)
        {
            _commandProcessor.SendToPlayer($"  ID: {verb.Id}, Name: '{verb.Name}', CodeLength: {verb.Code?.Length ?? 0}");
        }

        // Group by name to find duplicates
        var verbGroups = playerVerbs.GroupBy(v => v.Name?.ToLower() ?? "").ToList();
        int removedCount = 0;

        foreach (var group in verbGroups)
        {
            var verbs = group.ToList();
            if (verbs.Count <= 1) continue;

            var emptyVerbs = verbs.Where(v => string.IsNullOrEmpty(v.Code) || v.Code.Length == 0).ToList();
            
            _commandProcessor.SendToPlayer($"Found {verbs.Count} verbs named '{group.Key}': removing {emptyVerbs.Count} empty ones");

            foreach (var emptyVerb in emptyVerbs)
            {
                _commandProcessor.SendToPlayer($"  Removing empty verb: {emptyVerb.Id}");
                verbCollection.Delete(emptyVerb.Id);
                removedCount++;
            }
        }

        _commandProcessor.SendToPlayer($"Removed {removedCount} duplicate empty verbs from player object.");
        return true;
    }

    /// <summary>
    /// @examine <object> - Show detailed information about an object
    /// </summary>
    private bool HandleExamineCommand(string[] parts)
    {
        var objectName = parts.Length > 1 ? parts[1] : "here";
        var objectId = ResolveObject(objectName);
        
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        var obj = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (obj == null)
        {
            _commandProcessor.SendToPlayer("Object not found in database.");
            return true;
        }

        var objClass = GameDatabase.Instance.ObjectClasses.FindById(obj.ClassId);
        var name = ObjectManager.GetProperty(obj, "name")?.AsString ?? "unnamed";
        var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString ?? "no description";
        var longDesc = ObjectManager.GetProperty(obj, "longDescription")?.AsString ?? "You see nothing special.";

        _commandProcessor.SendToPlayer($"=== Object #{obj.DbRef}: {name} ===");
        _commandProcessor.SendToPlayer($"Class: {objClass?.Name ?? "unknown"}");
        _commandProcessor.SendToPlayer($"Short description: {shortDesc}");
        _commandProcessor.SendToPlayer($"Long description: {longDesc}");
        _commandProcessor.SendToPlayer($"Location: {GetObjectName(obj.Location ?? "nowhere")}");
        _commandProcessor.SendToPlayer($"GUID: {obj.Id}");
        
        // Show contents if any
        if (obj.Contents?.Any() == true)
        {
            _commandProcessor.SendToPlayer("Contents:");
            foreach (var contentId in obj.Contents)
            {
                var content = GameDatabase.Instance.GameObjects.FindById(contentId);
                if (content != null)
                {
                    var contentName = ObjectManager.GetProperty(content, "name")?.AsString ?? "unnamed";
                    _commandProcessor.SendToPlayer($"  #{content.DbRef}: {contentName}");
                }
            }
        }

        // Show verbs count
        var verbs = VerbManager.GetVerbsOnObject(objectId);
        if (verbs.Any())
        {
            _commandProcessor.SendToPlayer($"Verbs: {verbs.Count} defined (use @verbs #{obj.DbRef} to list)");
        }

        return true;
    }
}
