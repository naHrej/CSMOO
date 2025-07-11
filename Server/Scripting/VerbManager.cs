using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LiteDB;
using CSMOO.Server.Database;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Represents a verb (command) that can be executed on an object
/// </summary>
public class Verb
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// The object this verb is defined on
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the verb (e.g., "look", "get", "open")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Aliases for this verb (space-separated, e.g., "l examine")
    /// </summary>
    public string Aliases { get; set; } = string.Empty;
    
    /// <summary>
    /// Pattern that this verb matches (e.g., "* at *", "* from *")
    /// </summary>
    public string Pattern { get; set; } = string.Empty;
    
    /// <summary>
    /// The C# code to execute when this verb is called
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Who can execute this verb (public, owner, wizard)
    /// </summary>
    public string Permissions { get; set; } = "public";
    
    /// <summary>
    /// Description of what this verb does
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Creator of this verb
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Represents a function that can be called with typed arguments
/// </summary>
public class GameFunction
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// The object this function is defined on
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the function
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameter definitions (name:type pairs)
    /// </summary>
    public List<FunctionParameter> Parameters { get; set; } = new List<FunctionParameter>();
    
    /// <summary>
    /// Return type of the function
    /// </summary>
    public string ReturnType { get; set; } = "void";
    
    /// <summary>
    /// The C# code to execute
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Who can call this function
    /// </summary>
    public string Permissions { get; set; } = "public";
    
    /// <summary>
    /// Description of what this function does
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Creator of this function
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}

public class FunctionParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "object";
    public object? DefaultValue { get; set; }
}

/// <summary>
/// Manages verb resolution and execution in the LambdaMOO style
/// </summary>
public static class VerbManager
{
    /// <summary>
    /// Attempts to resolve and execute a command through the verb system
    /// </summary>
    public static bool TryExecuteVerb(string input, Player player, Commands.CommandProcessor commandProcessor)
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
        var args = string.Join(" ", parts.Skip(1));

        // Try to find a matching verb in this order:
        // 1. Objects in the room (including the player)
        // 2. The room itself
        // 3. The player
        // 4. Global verbs (on the system object)

        // 1. Check objects in the room
        var roomObjects = ObjectManager.GetObjectsInLocation(player.Location);
        foreach (var obj in roomObjects)
        {
            var matchedVerb = FindMatchingVerb(obj.Id, verb, input);
            if (matchedVerb != null)
            {
                return ExecuteVerb(matchedVerb, input, player, commandProcessor, obj.Id);
            }
        }

        // 2. Check the room itself
        var room = GameDatabase.Instance.GameObjects.FindById(player.Location);
        if (room != null)
        {
            var roomVerb = FindMatchingVerb(room.Id, verb, input);
            if (roomVerb != null)
            {
                return ExecuteVerb(roomVerb, input, player, commandProcessor, room.Id);
            }
        }

        // 3. Check the player
        var playerVerb = FindMatchingVerb(player.Id, verb, input);
        if (playerVerb != null)
        {
            return ExecuteVerb(playerVerb, input, player, commandProcessor, player.Id);
        }

        // 4. Check global verbs (we'll use a special "system" object)
        var systemObject = GetOrCreateSystemObject();
        var globalVerb = FindMatchingVerb(systemObject.Id, verb, input);
        if (globalVerb != null)
        {
            return ExecuteVerb(globalVerb, input, player, commandProcessor, systemObject.Id);
        }

        return false; // No verb found
    }

    /// <summary>
    /// Finds a matching verb on an object, checking inheritance chain
    /// </summary>
    private static Verb? FindMatchingVerb(string objectId, string verb, string fullInput)
    {
        var allVerbs = new List<Verb>();
        
        // Get the GameObject to access its class
        var gameObject = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (gameObject != null)
        {
            // First, get verbs directly on the object instance
            var instanceVerbs = GameDatabase.Instance.GetCollection<Verb>("verbs")
                .Find(v => v.ObjectId == objectId)
                .ToList();
            allVerbs.AddRange(instanceVerbs);
            
            // Then get verbs from the inheritance chain (classes)
            var inheritanceChain = Database.ObjectManager.GetInheritanceChain(gameObject.ClassId);
            foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse()) // Child to parent order for proper override
            {
                var classVerbs = GameDatabase.Instance.GetCollection<Verb>("verbs")
                    .Find(v => v.ObjectId == objectClass.Id)
                    .ToList();
                
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
        else
        {
            // If not a GameObject, just get verbs directly on the object (for system objects, etc.)
            allVerbs = GameDatabase.Instance.GetCollection<Verb>("verbs")
                .Find(v => v.ObjectId == objectId)
                .ToList();
        }

        Console.WriteLine($"FindMatchingVerb: Looking for '{verb}' on object {objectId}");
        Console.WriteLine($"Found {allVerbs.Count} total verbs (including inheritance):");
        
        foreach (var v in allVerbs)
        {
            Console.WriteLine($"  Verb: ID={v.Id}, Name='{v.Name}', CodeLength={v.Code?.Length ?? 0}");
            if (!string.IsNullOrEmpty(v.Code) && v.Code.Length > 0)
            {
                Console.WriteLine($"    Code preview: {v.Code.Substring(0, Math.Min(50, v.Code.Length))}...");
            }
        }

        // First, collect ALL matching verbs (by name or alias)
        var matchingVerbs = new List<Verb>();
        
        foreach (var v in allVerbs)
        {
            // Check if verb name matches
            if (!string.IsNullOrEmpty(v.Name) && v.Name.ToLower() == verb)
            {
                matchingVerbs.Add(v);
                continue;
            }

            // Check aliases
            if (!string.IsNullOrEmpty(v.Aliases))
            {
                var aliases = v.Aliases.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (aliases.Any(alias => alias.ToLower() == verb))
                {
                    matchingVerbs.Add(v);
                    continue;
                }
            }
        }

        if (matchingVerbs.Count > 1)
        {
            Console.WriteLine($"⚠️  WARNING: Found {matchingVerbs.Count} verbs matching '{verb}':");
            foreach (var v in matchingVerbs)
            {
                Console.WriteLine($"    ID={v.Id}, Name='{v.Name}', CodeLength={v.Code?.Length ?? 0}");
            }
            
            // Prioritize verbs that have actual code
            var verbWithCode = matchingVerbs.FirstOrDefault(v => !string.IsNullOrEmpty(v.Code) && v.Code.Length > 0);
            if (verbWithCode != null)
            {
                Console.WriteLine($"  -> CHOOSING verb with code: ID={verbWithCode.Id}, CodeLength={verbWithCode.Code.Length}");
                return verbWithCode;
            }
            
            Console.WriteLine($"  -> No verbs have code, using first match: ID={matchingVerbs[0].Id}");
            return matchingVerbs[0];
        }
        else if (matchingVerbs.Count == 1)
        {
            Console.WriteLine($"  -> MATCH: {matchingVerbs[0].Name} (CodeLength: {matchingVerbs[0].Code?.Length ?? 0})");
            return matchingVerbs[0];
        }

        Console.WriteLine($"  -> NO MATCH found for '{verb}' on object {objectId}");
        return null;
    }

    /// <summary>
    /// Executes a verb with the given context
    /// </summary>
    private static bool ExecuteVerb(Verb verb, string input, Player player, Commands.CommandProcessor commandProcessor, string thisObjectId)
    {
        try
        {
            Console.WriteLine($"Executing verb '{verb.Name}' with code length: {verb.Code?.Length ?? 0}");
            Console.WriteLine($"Verb code: '{verb.Code ?? "(null)"}'");
            
            var scriptEngine = new VerbScriptEngine();
            var result = scriptEngine.ExecuteVerb(verb, input, player, commandProcessor, thisObjectId);
            
            if (!string.IsNullOrEmpty(result))
            {
                commandProcessor.SendToPlayer(result);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            commandProcessor.SendToPlayer($"Error executing verb '{verb.Name}': {ex.Message}");
            Console.WriteLine($"Error executing verb '{verb.Name}': {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return true; // We handled it, even if it errored
        }
    }

    /// <summary>
    /// Creates a verb on an object
    /// </summary>
    public static Verb CreateVerb(string objectId, string name, string pattern = "", string code = "", string createdBy = "")
    {
        var verb = new Verb
        {
            ObjectId = objectId,
            Name = name,
            Pattern = pattern,
            Code = code,
            CreatedBy = createdBy
        };

        GameDatabase.Instance.GetCollection<Verb>("verbs").Insert(verb);
        return verb;
    }

    /// <summary>
    /// Updates a verb's code
    /// </summary>
    public static void UpdateVerbCode(string verbId, string code)
    {
        Console.WriteLine($"UpdateVerbCode called with verbId: '{verbId}', code length: {code?.Length ?? 0}");
        
        var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");
        var verb = verbCollection.FindById(verbId);
        
        if (verb != null)
        {
            Console.WriteLine($"Found verb: {verb.Name}, current code length: {verb.Code?.Length ?? 0}");
            verb.Code = code ?? string.Empty;
            verb.ModifiedAt = DateTime.UtcNow;
            
            try
            {
                verbCollection.Update(verb);
                Console.WriteLine($"Verb updated successfully. New code length: {verb.Code?.Length ?? 0}");
                
                // Verify the update worked
                var verifyVerb = verbCollection.FindById(verbId);
                if (verifyVerb != null)
                {
                    Console.WriteLine($"Verification: Code in database is now {verifyVerb.Code?.Length ?? 0} characters");
                }
                else
                {
                    Console.WriteLine("ERROR: Could not find verb after update!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR updating verb: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"ERROR: Verb with ID '{verbId}' not found!");
        }
    }

    /// <summary>
    /// Gets all verbs on an object
    /// </summary>
    public static List<Verb> GetVerbsOnObject(string objectId)
    {
        return GameDatabase.Instance.GetCollection<Verb>("verbs")
            .Find(v => v.ObjectId == objectId)
            .ToList();
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
            var inheritanceChain = Database.ObjectManager.GetInheritanceChain(gameObject.ClassId);
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
                
                foreach (var verb in classVerbs)
                {
                    allVerbs.Add((verb, $"class {objectClass.Name}"));
                }
            }
            else
            {
                // Just get verbs directly on the object (for system objects, etc.)
                var directVerbs = GameDatabase.Instance.GetCollection<Verb>("verbs")
                    .Find(v => v.ObjectId == objectId)
                    .ToList();
                
                foreach (var verb in directVerbs)
                {
                    allVerbs.Add((verb, "direct"));
                }
            }
        }
        
        return allVerbs;
    }

    /// <summary>
    /// Gets or creates the system object for global verbs
    /// </summary>
    private static GameObject GetOrCreateSystemObject()
    {
        // Get all objects and filter in memory (LiteDB doesn't support ContainsKey in expressions)
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        var systemObj = allObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true);

        if (systemObj == null)
        {
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
            }
        }

        return systemObj ?? throw new InvalidOperationException("Could not create system object");
    }
}

/// <summary>
/// Script engine specifically for executing verbs with additional context
/// </summary>
public class VerbScriptEngine
{
    private readonly Microsoft.CodeAnalysis.Scripting.ScriptOptions _scriptOptions;

    public VerbScriptEngine()
    {
        // Set up script options with necessary references
        _scriptOptions = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
            .WithReferences(
                typeof(object).Assembly,                    // System.Object
                typeof(Console).Assembly,                   // System.Console
                typeof(Enumerable).Assembly,                // System.Linq
                typeof(GameObject).Assembly,                // Our game objects
                typeof(ObjectManager).Assembly,             // Our managers
                System.Reflection.Assembly.GetExecutingAssembly()             // Current assembly
            )
            .WithImports(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "CSMOO.Server.Database",
                "CSMOO.Server.Commands"
            );
    }
    /// <summary>
    /// Executes a verb with verb-specific context variables
    /// </summary>
    public string ExecuteVerb(Verb verb, string input, Player player, Commands.CommandProcessor commandProcessor, string thisObjectId)
    {
        try
        {
            // Check if the verb has any code
            if (string.IsNullOrWhiteSpace(verb.Code))
            {
                return "Verb has no code defined. Use @program to add code to this verb.";
            }

            // Create enhanced globals for verb execution
            var globals = new VerbScriptGlobals
            {
                Player = player,
                CommandProcessor = commandProcessor,
                ObjectManager = new ScriptObjectManager(),
                WorldManager = new ScriptWorldManager(),
                PlayerManager = new ScriptPlayerManager(),
                
                // Verb-specific variables
                ThisObject = thisObjectId,
                Input = input,
                Args = ParseArguments(input),
                Verb = verb.Name
            };

            var script = Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create(
                verb.Code, 
                _scriptOptions, 
                typeof(VerbScriptGlobals));
            
            var result = script.RunAsync(globals).GetAwaiter().GetResult();
            return result.ReturnValue?.ToString() ?? "";
        }
        catch (Exception ex)
        {
            return $"Verb execution error: {ex.Message}";
        }
    }

    private List<string> ParseArguments(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Skip(1).ToList(); // Skip the verb itself
    }
}

/// <summary>
/// Enhanced script globals for verb execution
/// </summary>
public class VerbScriptGlobals : ScriptGlobals
{
    /// <summary>
    /// The object this verb is running on
    /// </summary>
    public string ThisObject { get; set; } = string.Empty;
    
    /// <summary>
    /// The complete input string that triggered this verb
    /// </summary>
    public string Input { get; set; } = string.Empty;
    
    /// <summary>
    /// Parsed arguments from the input
    /// </summary>
    public List<string> Args { get; set; } = new List<string>();
    
    /// <summary>
    /// The name of the verb being executed
    /// </summary>
    public string Verb { get; set; } = string.Empty;

    /// <summary>
    /// Get a property from the current object (this)
    /// </summary>
    public object? GetThisProperty(string propertyName)
    {
        return GetProperty(ThisObject, propertyName);
    }

    /// <summary>
    /// Set a property on the current object (this)
    /// </summary>
    public void SetThisProperty(string propertyName, object value)
    {
        SetProperty(ThisObject, propertyName, value);
    }

    /// <summary>
    /// Send a message to all players in the same room as the current player
    /// </summary>
    public void SayToRoom(string message, bool includePlayer = false)
    {
        if (Player?.Location == null) return;

        var playersInRoom = Database.PlayerManager.GetOnlinePlayers()
            .Where(p => p.Location == Player.Location)
            .ToList();

        foreach (var otherPlayer in playersInRoom)
        {
            if (!includePlayer && otherPlayer.Id == Player.Id)
                continue;

            CommandProcessor?.SendToPlayer(message, otherPlayer.SessionGuid);
        }
    }

    /// <summary>
    /// Find an object in the current room by name
    /// </summary>
    public string? FindObjectInRoom(string name)
    {
        if (Player?.Location == null) return null;

        var objects = Database.ObjectManager.GetObjectsInLocation(Player.Location);
        var targetObject = objects.FirstOrDefault(obj =>
        {
            var objName = Database.ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = Database.ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            name = name.ToLower();
            return objName?.Contains(name) == true || shortDesc?.Contains(name) == true;
        });

        return targetObject?.Id;
    }

    /// <summary>
    /// Call a verb on an object from within another verb
    /// </summary>
    public object? CallVerb(string objectRef, string verbName, params object[] args)
    {
        try
        {
            // Resolve the object reference (supports class:Object syntax, #123 DBREFs, etc.)
            var objectId = ResolveObjectFromScript(objectRef);
            if (objectId == null)
            {
                throw new ArgumentException($"Object '{objectRef}' not found");
            }

            // Find the verb on the object (with inheritance)
            var allVerbsOnObject = VerbManager.GetAllVerbsOnObject(objectId);
            var verbMatch = allVerbsOnObject.FirstOrDefault(v => 
                v.verb.Name.Equals(verbName, StringComparison.OrdinalIgnoreCase));

            if (verbMatch.verb == null)
            {
                throw new ArgumentException($"Verb '{verbName}' not found on object '{objectRef}'");
            }

            // Execute the verb with the provided arguments
            var scriptEngine = new VerbScriptEngine();
            
            // Build input string from arguments
            var inputArgs = args.Select(a => a?.ToString() ?? "").ToArray();
            var input = verbName + (inputArgs.Length > 0 ? " " + string.Join(" ", inputArgs) : "");
            
            if (Player == null || CommandProcessor == null)
            {
                throw new InvalidOperationException("Cannot call verb without valid player and command processor context");
            }
            
            var result = scriptEngine.ExecuteVerb(verbMatch.verb, input, Player, CommandProcessor, objectId);
            
            // Try to parse result as different types
            if (string.IsNullOrEmpty(result)) return null;
            if (bool.TryParse(result, out bool boolVal)) return boolVal;
            if (int.TryParse(result, out int intVal)) return intVal;
            if (double.TryParse(result, out double doubleVal)) return doubleVal;
            return result; // Return as string if no other type matches
        }
        catch (Exception ex)
        {
            Say($"Error calling {objectRef}:{verbName}() - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Resolve object reference from script context (supports all the same syntax as ResolveObject)
    /// </summary>
    private string? ResolveObjectFromScript(string objectRef)
    {
        // Handle special keywords
        switch (objectRef.ToLower())
        {
            case "this":
                return ThisObject;
            case "me":
                return Player?.Id;
            case "here":
                return Player?.Location;
            case "system":
                return GetSystemObjectId();
        }
        
        // Check if it's a DBREF (starts with # followed by digits)
        if (objectRef.StartsWith("#") && int.TryParse(objectRef.Substring(1), out int dbref))
        {
            var obj = GameDatabase.Instance.GameObjects.FindOne(o => o.DbRef == dbref);
            return obj?.Id;
        }
        
        // Check if it's a class reference
        if (objectRef.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectRef.Substring(6);
            var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return objectClass?.Id;
        }
        
        if (objectRef.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectRef.Substring(0, objectRef.Length - 6);
            var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return objectClass?.Id;
        }
        
        // Try to find by name (simplified version for script context)
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        var match = allObjects.FirstOrDefault(obj =>
        {
            var objName = Database.ObjectManager.GetProperty(obj, "name")?.AsString;
            return objName?.Equals(objectRef, StringComparison.OrdinalIgnoreCase) == true;
        });
        
        if (match != null) return match.Id;
        
        // Try as class name
        var objectClass2 = GameDatabase.Instance.ObjectClasses.FindOne(c => 
            c.Name.Equals(objectRef, StringComparison.OrdinalIgnoreCase));
        return objectClass2?.Id;
    }

    /// <summary>
    /// Get system object ID (helper method)
    /// </summary>
    private string GetSystemObjectId()
    {
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        var systemObj = allObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true);
        return systemObj?.Id ?? "";
    }

    /// <summary>
    /// Call a verb on the current object (this)
    /// </summary>
    public object? This(string verbName, params object[] args)
    {
        return CallVerb("this", verbName, args);
    }

    /// <summary>
    /// Call a verb on the player object (me)
    /// </summary>
    public object? Me(string verbName, params object[] args)
    {
        return CallVerb("me", verbName, args);
    }

    /// <summary>
    /// Call a verb on the current room (here)
    /// </summary>
    public object? Here(string verbName, params object[] args)
    {
        return CallVerb("here", verbName, args);
    }

    /// <summary>
    /// Call a verb on the system object
    /// </summary>
    public object? System(string verbName, params object[] args)
    {
        return CallVerb("system", verbName, args);
    }

    /// <summary>
    /// Call a verb on an object by DBREF
    /// </summary>
    public object? Object(int dbref, string verbName, params object[] args)
    {
        return CallVerb($"#{dbref}", verbName, args);
    }

    /// <summary>
    /// Call a verb on a class
    /// </summary>
    public object? Class(string className, string verbName, params object[] args)
    {
        return CallVerb($"class:{className}", verbName, args);
    }
}
