using CSMOO.Parsing;

public class System
{


    public string[] less = LoadFile("stylesheet.less");

    public int test = 1;

    [adminonly]
    private readonly int protectedValue = 1;


    [VerbAliases("\"")]
    [VerbDescription("Speak to others in the room")]
    [VerbPattern("*")]
    /// <summary>
    /// Speak to others in the room
    /// </summary>

    public verb Say(string message)
    {
        // Say command - speak to others in the room
        if (Args.Count == 0)
        {
            notify(Player, "Say what?");
            return;
        }

        var fullMessage = string.Join(" ", Args);
        notify(Player, $"<section class='Speech'>You say, \"<span class='text'>{fullMessage}</span>\"</section>");

        // Send to other players in the room
        var players = Builtins.GetPlayersInRoom(Player.Location);
        foreach (var plr in players)
        {
            if (plr.Id != Player.Id)
                notify(plr, $"<section class='Speech'><span class='actor'>{Player.Name}</span> says, \"<span class='text'>{fullMessage}</span>\"</section>");
        }
    }

    [VerbDescription("List all online players")]
    /// <summary>
    /// List all online players
    /// </summary>
    public verb Who()
    {
        // Who command - list online players
        var onlinePlayers = Builtins.GetOnlinePlayers();
        notify(Player, "Online players:");
        foreach (var onlinePlayer in onlinePlayers)
        {
            notify(Player, $"  {onlinePlayer.Name}");
        }
    }

    [VerbAliases("i inv")]
    [VerbDescription("Show what you are carrying")]
    /// <summary>
    /// Show what you are carrying
    /// </summary>
    public verb Inventory()
    {
        // Inventory command - show what the player is carrying
        Builtins.ShowInventory();
    }

    [VerbAliases("?")]
    [VerbDescription("Show help categories and topics")]
    [VerbPattern("*")]
    /// <summary>
    /// Show help categories and topics
    /// </summary>
    public verb Help(string topic)
    {
        // Help categories and topics (expand as needed)
        var helpCategories = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "basics", new List<string> { "movement", "communication", "objects" } },
            { "building", new List<string> { "rooms", "exits", "properties" } },
            { "programming", new List<string> { "verbs", "flags", "permissions" } }
        };

        if (Args.Count == 0)
        {
            var output = new StringBuilder();
            output.AppendLine("<section class='Help'>");
            output.AppendLine("<h3>Help Categories</h3>");
            foreach (var cat in helpCategories.Keys.OrderBy(x => x))
            {
                var topics = string.Join(", ", helpCategories[cat].Select(t => $"<span class='topic'>{t}</span>"));
                output.AppendLine($"<div><span class='category'>{cat}</span>: {topics}</div>");
            }
            output.AppendLine("<div><span class='usage'>Use: '<span class='command'>help <span class='param'>&lt;category&gt;</span></span>" +
            "' or '<span class='command'>help <span class='param'>&lt;topic&gt;</span></span>'</span></div>");
            output.AppendLine("</section>");
            notify(Player, output.ToString());
            return;
        }

        var searchTerm = string.Join(" ", Args).ToLower();

        // Check if it's a category
        if (helpCategories.ContainsKey(searchTerm))
        {
            var output = new StringBuilder();
            var prettyCategory = char.ToUpper(searchTerm[0]) + searchTerm.Substring(1);
            output.AppendLine("<section class='Help'>");
            output.AppendLine($"<h3>{prettyCategory} Help</h3>");
            foreach (var topic in helpCategories[searchTerm])
            {
                output.AppendLine($"<div><span class='topic'>{topic}</span></div>");
            }
            output.AppendLine("<div><span class='usage'>Use: '<span class='command'>help <span class='param'>&lt;topic&gt;</span></span>' for specific help</span></div>");
            output.AppendLine("</section>");
            notify(Player, output.ToString());
            return;
        }

        // Check if it's a specific topic
        var foundInCategory = helpCategories.FirstOrDefault(kvp =>
            kvp.Value.Any(topic => topic.Equals(searchTerm, StringComparison.OrdinalIgnoreCase)));

        if (!foundInCategory.Equals(default(KeyValuePair<string, List<string>>)))
        {
            // Provide specific help for the topic
            // NOTE: verb scripts run in Roslyn scripting context (not inside a C# instance method),
            // so `this` isn't available here. Keep the topic help inline.
            var helpText = searchTerm switch
            {
                "movement" => "<section class='Help'><h3>Movement</h3><div>Movement commands: <span class='command'>go</span>, <span class='command'>north</span>, <span class='command'>south</span>, <span class='command'>east</span>, <span class='command'>west</span>, etc.</div><div>Use <span class='command'>look</span> to see available exits.</div></section>",
                "communication" => "<section class='Help'><h3>Communication</h3><div>Communication: <span class='command'>say</span> (or <span class='command'>\"</span>), <span class='command'>tell</span>, <span class='command'>ooc</span></div></section>",
                "objects" => "<section class='Help'><h3>Objects</h3><div>Object commands: <span class='command'>look</span>, <span class='command'>examine</span>, <span class='command'>get</span>, <span class='command'>drop</span>, <span class='command'>inventory</span></div></section>",
                _ => $"Help for '{searchTerm}' is not yet available."
            };
            notify(Player, helpText);
        }
        else
        {
            notify(Player, $"<section class='Help'><div>No help found for '{searchTerm}'. Try <span class='command'>help</span> for available categories.</div></section>");
        }
    }

    private string GetTopicHelp(string topic)
    {
        return topic.ToLower() switch
        {
            "movement" => "Movement commands: <span class='command'>go</span>, <span class='command'>north</span>, <span class='command'>south</span>, <span class='command'>east</span>, <span class='command'>west</span>, etc.\nUse <span class='command'>look</span> to see available exits.",
            "communication" => "Communication: <span class='command'>say</span> (or <span class='command'>\"</span>), <span class='command'>tell</span>, <span class='command'>ooc</span>",
            "objects" => "Object commands: <span class='command'>look</span>, <span class='command'>examine</span>, <span class='command'>get</span>, <span class='command'>drop</span>, <span class='command'>inventory</span>",
            _ => $"Help for '{topic}' is not yet available."
        };
    }

    [VerbAliases("move walk travel head")]
    [VerbPattern("{direction}")]
    [VerbDescription("Smart go command - move in any available direction")]
    /// <summary>
    /// Smart go command - move in any available direction
    /// </summary>
    public verb Go(string direction)
    {
        // Smart go command - move in any available direction.
        var currentLocation = Player.Location;
        if (currentLocation is null)
        {
            notify(Player, "<p class='error'>You are not in any location.</p>");
            return false;
        }
        var exits = Builtins.GetExits(Location);
        var exitNames = new List<string>();
        foreach (var exit in exits)
        {
            var dir = Builtins.GetProperty(exit, "direction");
            if (dir != null)
            {
                exitNames.Add(dir);
            }
        }
        var availableExits = $"Available exits: <span class='param'>{string.Join(", ", exitNames)}</span>";
        if (Args.Count == 0)
        {
            // Show available exits if no direction given
            if (exits.Count == 0)
            {
                notify(Player, "<p class='error'>There are no exits from here.</p>");
            }
            else
            {
                notify(Player, availableExits);
                notify(Player, "<p class='usage'>Usage: <span class='command'>go <span class='param'>&lt;direction&gt;</span></span></p>");
            }
            return true; // Successfully handled the command (showed help)
        }
        var chosenDirection = direction.ToLowerInvariant();
        dynamic chosenExit = null;
        
        // Helper function to extract abbreviation (capitalized letters and numbers)
        string ExtractAbbreviation(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var result = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (char.IsUpper(c) || char.IsDigit(c))
                {
                    result.Append(c);
                }
            }
            return result.ToString().ToUpperInvariant();
        }
        
        var chosenAbbreviation = ExtractAbbreviation(direction).ToUpperInvariant();
        
        foreach (var exit in exits)
        {
            var exitDirectionProp = Builtins.GetProperty(exit, "direction");
            if (exitDirectionProp == null) continue;
            
            var exitDirection = exitDirectionProp.ToString();
            // Remove quotes if present
            if (exitDirection.StartsWith("\"") && exitDirection.EndsWith("\""))
            {
                exitDirection = exitDirection.Substring(1, exitDirection.Length - 2);
            }
            
            var exitDirectionLower = exitDirection.ToLowerInvariant();
            
            // Check exact name match (case-insensitive)
            if (exitDirectionLower == chosenDirection)
            {
                chosenExit = exit as dynamic;
                break;
            }
            
            // Check abbreviation match
            var exitAbbreviation = ExtractAbbreviation(exitDirection).ToUpperInvariant();
            if (!string.IsNullOrEmpty(exitAbbreviation) && 
                exitAbbreviation == chosenAbbreviation)
            {
                chosenExit = exit as dynamic;
                break;
            }
        }
        if (chosenExit == null)
        {
            return false; // Direction not recognized - let other command processing handle it
        }
        dynamic destination = chosenExit.destination;

        if (destination == null)
        {
            notify(Player, "<p class='error'>That exit doesn't lead anywhere.</p>");
            return true; // Exit exists but broken - we handled the command
        }
        // Move the player
        if (Builtins.MoveObject(Player, destination))
        {
            notify(Player, $"<p class='success'>You go <span class='param'>{chosenDirection}</span>.</p>");
            notify(Player, destination.Description());
            return true; // Successfully moved
        }
        else
        {
            notify(Player, "<p class='error'>You can't go that way.</p>");
            return true; // We handled the command, but movement failed
        }
    }

    [VerbAliases("l")]
    [VerbPattern("*")]
    [VerbDescription("Look at the room or a specific object")]
    /// <summary>
    /// Look at the room or a specific object
    /// </summary>
    public verb Look(string target)
    {
        // Look command - shows room or looks at specific object.
        var targetText = "";
        if (Args.Count == 0)
        {
            targetText = "here";
        }
        else if (Args.Count >= 2 && Args[0].ToLower() == "at")
        {
            // 'look at something'
            targetText = string.Join(" ", Args.Skip(1));
        }
        else
        {
            // 'look something'
            targetText = string.Join(" ", Args);
        }

        var resolution = ObjectResolver.ResolveUnique(targetText, Player);
        if (resolution.Ambiguous)
        {
            notify(Player, $"<section class='error'>I don't know which '<span class='param'>{targetText}</span>' you mean.</section>");
            return true;
        }
        if (resolution.Match == null)
        {
            notify(Player, $"<section class='error'>You don't see '<span class='param'>{targetText}</span>' here.</section>");
            return true;
        }
        var resolved = resolution.Match;
        
        // Try to call Description() function, but fall back to property access if object has no owner
        string description;
        try
        {
            // Cast to dynamic to call Description() method
            description = ((dynamic)resolved).Description();
        }
        catch (Exception ex)
        {
            // Object has no owner or Description() function failed, use property access instead
            try
            {
                var name = Builtins.GetProperty(resolved, "name", "") ?? "something";
                var longDesc = Builtins.GetProperty(resolved, "longDescription", "") ?? Builtins.GetProperty(resolved, "description", "") ?? "";
                var shortDesc = Builtins.GetProperty(resolved, "shortDescription", "") ?? "";
                var classId = Builtins.GetProperty(resolved, "classId", "") ?? "object";
                
                if (!string.IsNullOrEmpty(longDesc))
                {
                    description = $"<h3>{name}</h3><p>{longDesc}</p>";
                }
                else if (!string.IsNullOrEmpty(shortDesc))
                {
                    description = $"<h3>{name}</h3><p>{shortDesc}</p>";
                }
                else
                {
                    description = $"<h3>{name}</h3><p>You see nothing special about this {classId}.</p>";
                }
            }
            catch
            {
                // If even property access fails, show a generic message
                description = $"<p>You see something, but can't make out any details.</p>";
            }
        }
        
        notify(Player, description);
        return true;
    }

    [VerbAliases("exam x ex")]
    [VerbPattern("{targetName}")]
    [VerbDescription("Examine an object in detail")]
    /// <summary>
    /// Examine an object in detail
    /// </summary>
    public verb Examine(string targetName)
    {
        // First, try to resolve as a class name
        var classTarget = Builtins.GetClassByName(targetName);
        var isExaminingClass = classTarget != null;
        dynamic target = null;
        dynamic targetPlayer = null;
        var targetId = "";
        ObjectClass objectClass = null;
        if (isExaminingClass)
        {
            // Examining a class definition
            objectClass = classTarget;
            targetId = classTarget.Id;
            notify(Player, $"<hr/><p>Examining class '{classTarget.Name}' ({classTarget.Id})</p>");
        }
        else
        {
            // Try to resolve as an object instance
            target = ObjectResolver.ResolveObject(targetName, Player);
            if (target is null)
            {
                notify(Player, $"You can't see '{targetName}' here.");
                return true;
            }
            targetId = target.Id;
            objectClass = Builtins.GetObjectClass(target);
            notify(Player, $"<hr/><p>Examining {target.Properties["name"].AsString} '{targetName}' ({target.Id})</p>");
            // Check if target is a player (only for object instances)
            if (Builtins.IsPlayerObject(target))
            {
                targetPlayer = (Player)target;
            }
        }
        // Get basic properties using different approaches for classes vs instances
        string name, shortDesc, longDesc;
        if (isExaminingClass)
        {
            // For class definitions, use class properties
            name = objectClass.Name;
            shortDesc = objectClass.Description;
            longDesc = objectClass.Description;
        }
        else
        {
            // For object instances, use dynamic access
            name = target.Name ?? Builtins.GetObjectName(target);
            shortDesc = target.shortDescription ?? "";
            longDesc = target.longDescription ?? target.description ?? "";
        }
        // Build the examination output
        var output = new StringBuilder();
        // Object name and short description
        if (!string.IsNullOrEmpty(shortDesc))
        {
            output.AppendLine($"{name} ({shortDesc})");
        }
        else
        {
            output.AppendLine(name);
        }
        // Long description
        if (!string.IsNullOrEmpty(longDesc))
        {
            output.AppendLine(longDesc);
        }
        else
        {
            output.AppendLine("You see nothing special.");
        }
        // Show contents if it's a container (only for object instances)
        if (!isExaminingClass)
        {
            var contents = Helpers.GetObjectsInLocation(targetId);
            if (contents.Any())
            {
                output.AppendLine();
                output.AppendLine("Contents:");
                foreach (var item in contents)
                {
                    var itemName = item.Name ?? "unknown object";
                    var itemShort = item.shortDescription ?? "";
                    var displayName = !string.IsNullOrEmpty(itemShort) ? $"{itemName} ({itemShort})" : itemName;
                    output.AppendLine($"  {displayName}");
                }
            }
        }
        // For class definitions, show default properties and instances
        if (isExaminingClass)
        {
            output.AppendLine();
            output.AppendLine("=== Class Information ===");
            // Show inheritance
            if (!string.IsNullOrEmpty(objectClass.ParentClassId))
            {
                var parentClass = Builtins.GetClass(objectClass.ParentClassId);
                output.AppendLine($"Inherits from: {parentClass?.Name ?? objectClass.ParentClassId}");
            }
            else
            {
                output.AppendLine("Inherits from: (none - root class)");
            }
            // Show if it's abstract
            if (objectClass.IsAbstract)
            {
                output.AppendLine("Type: Abstract class (cannot be instantiated)");
            }
            else
            {
                output.AppendLine("Type: Concrete class");
            }
            // Show default properties
            if (objectClass.Properties?.Count > 0)
            {
                output.AppendLine();
                output.AppendLine("Default Properties:");
                foreach (var prop in objectClass.Properties)
                {
                    var value = prop.Value?.ToString() ?? "null";
                    if (value.Length > 50)
                    {
                        value = value.Substring(0, 47) + "...";
                    }
                    output.AppendLine($"  {prop.Key}: {value}");
                }
            }
            // Show instances of this class
            var instances = Builtins.GetObjectsByClass(objectClass.Id);
            output.AppendLine();
            output.AppendLine($"Instances: {instances.Count()}");
            if (instances.Count > 0 && (Builtins.IsAdmin(Player) || Builtins.IsModerator(Player)))
            {
                var limitedInstances = instances.Take(10);
                foreach (var instance in limitedInstances)
                {
                    var instName = instance.Name ?? "unnamed";
                    output.AppendLine($"  #{instance.DbRef}: {instName}");
                }
                if (instances.Count() > 10)
                {
                    output.AppendLine($"  ... and {instances.Count() - 10} more");
                }
            }
        }
        // Administrative information for Admin/Moderator users
        if (Builtins.IsAdmin(Player) || Builtins.IsModerator(Player))
        {
            output.AppendLine();
            output.AppendLine("=== Administrative Information ===");
            if (isExaminingClass)
            {
                // Administrative info for class definitions
                output.AppendLine($"Class ID: {objectClass.Id}");
                output.AppendLine($"Class Name: {objectClass.Name}");
                output.AppendLine($"Created: {objectClass.CreatedAt}");
                output.AppendLine($"Modified: {objectClass.ModifiedAt}");
            }
            else
            {
                // Administrative info for object instances
                output.AppendLine($"Owner: {target.Owner}(#{target.DbRef})");
                output.AppendLine($"Object ID: {target.Id}");
                output.AppendLine($"DB Reference: #{target.DbRef}");
                output.AppendLine($"Created: {target.CreatedAt}");
                output.AppendLine($"Modified: {target.ModifiedAt}");
                // Show player flags if examining a player
                if (targetPlayer != null)
                {
                    var flags = Builtins.GetPlayerFlags(targetPlayer);
                    if (flags.Count > 0)
                    {
                        output.AppendLine($"Player Flags: {string.Join(", ", flags)}");
                    }
                    else
                    {
                        output.AppendLine("Player Flags: none");
                    }
                }
            }
            // Show object class for both cases
            if (objectClass != null)
            {
                output.AppendLine($"Class: {objectClass.Name}");
            }
            // Show verbs (different approach for classes vs instances)
            if (isExaminingClass)
            {
                var verbs = Builtins.GetVerbsOnClass(objectClass.Id);
                if (verbs.Count > 0)
                {
                    output.AppendLine("Class Verbs:");
                    foreach (var verb in verbs.OrderBy(v => v.Name))
                    {
                        var verbInfo = $"  {verb.Name}";
                        if (!string.IsNullOrEmpty(verb.Aliases))
                        {
                            verbInfo += $" ({verb.Aliases})";
                        }
                        if (!string.IsNullOrEmpty(verb.Pattern))
                        {
                            verbInfo += $" [{verb.Pattern}]";
                        }
                        output.AppendLine(verbInfo);
                    }
                }
                else
                {
                    output.AppendLine("Class Verbs: none");
                }
            }
            else
            {
                // Get all verbs with source information
                var allVerbsWithSource = Builtins.GetVerbsOnObject(targetId);
                var instanceVerbs = allVerbsWithSource.Where(v => v.source == "instance").ToList();
                var inheritedVerbs = allVerbsWithSource.Where(v => v.source != "instance").ToList();
                // Show instance-specific verbs
                if (instanceVerbs.Count > 0)
                {
                    output.AppendLine("Instance Verbs:");
                    foreach (var (verb, source) in instanceVerbs.OrderBy(v => v.verb.Name))
                    {
                        var verbInfo = $"  {verb.Name}";
                        if (!string.IsNullOrEmpty(verb.Aliases))
                        {
                            verbInfo += $" ({verb.Aliases})";
                        }
                        if (!string.IsNullOrEmpty(verb.Pattern))
                        {
                            verbInfo += $" [{verb.Pattern}]";
                        }
                        output.AppendLine(verbInfo);
                    }
                }
                else
                {
                    output.AppendLine("Instance Verbs: none");
                }
                // Show inherited verbs
                if (inheritedVerbs.Count > 0)
                {
                    output.AppendLine("Inherited Verbs:");
                    foreach (var (verb, source) in inheritedVerbs.OrderBy(v => v.verb.Name))
                    {
                        var verbInfo = $"  {verb.Name}";
                        if (!string.IsNullOrEmpty(verb.Aliases))
                        {
                            verbInfo += $" ({verb.Aliases})";
                        }
                        if (!string.IsNullOrEmpty(verb.Pattern))
                        {
                            verbInfo += $" [{verb.Pattern}]";
                        }
                        verbInfo += $" (from {source})";
                        output.AppendLine(verbInfo);
                    }
                }
                else
                {
                    output.AppendLine("Inherited Verbs: none");
                }
            }
            // Show functions (different approach for classes vs instances)
            if (isExaminingClass)
            {
                var functions = Builtins.GetFunctionsOnClass(objectClass.Id);
                if (functions.Count > 0)
                {
                    output.AppendLine("Class Functions:");
                    foreach (var func in functions.OrderBy(f => f.Name))
                    {
                        var paramString = string.Join(", ", func.ParameterTypes.Zip(func.ParameterNames, (type, name) => $"{type} {name}"));
                        var funcInfo = $"  {func.ReturnType} {func.Name}({paramString})";
                        if (!string.IsNullOrEmpty(func.Description))
                        {
                            funcInfo += $" - {func.Description}";
                        }
                        output.AppendLine(funcInfo);
                    }
                }
                else
                {
                    output.AppendLine("Class Functions: none");
                }
            }
            else
            {
                // Get all functions with source information
                var allFunctionsWithSource = Builtins.GetFunctionsOnObject(targetId);
                var instanceFunctions = allFunctionsWithSource.Where(f => f.source == "instance").ToList();
                var inheritedFunctions = allFunctionsWithSource.Where(f => f.source != "instance").ToList();
                // Show instance-specific functions
                if (instanceFunctions.Count > 0)
                {
                    output.AppendLine("Instance Functions:");
                    foreach (var (func, source) in instanceFunctions.OrderBy(f => f.function.Name))
                    {
                        var paramString = string.Join(", ", func.ParameterTypes.Zip(func.ParameterNames, (type, name) => $"{type} {name}"));
                        var funcInfo = $"  {func.ReturnType} {func.Name}({paramString})";
                        if (!string.IsNullOrEmpty(func.Description))
                        {
                            funcInfo += $" - {func.Description}";
                        }
                        output.AppendLine(funcInfo);
                    }
                }
                else
                {
                    output.AppendLine("Instance Functions: none");
                }
                // Show inherited functions
                if (inheritedFunctions.Count > 0)
                {
                    output.AppendLine("Inherited Functions:");
                    foreach (var (func, source) in inheritedFunctions.OrderBy(f => f.function.Name))
                    {
                        var paramString = string.Join(", ", func.ParameterTypes.Zip(func.ParameterNames, (type, name) => $"{type} {name}"));
                        var funcInfo = $"  {func.ReturnType} {func.Name}({paramString})";
                        if (!string.IsNullOrEmpty(func.Description))
                        {
                            funcInfo += $" - {func.Description}";
                        }
                        funcInfo += $" (from {source})";
                        output.AppendLine(funcInfo);
                    }
                }
                else
                {
                    output.AppendLine("Inherited Functions: none");
                }
            }
            // Show properties (only for object instances, classes already show default properties above)
            if (!isExaminingClass)
            {
                // For properties, we need to separate instance vs inherited differently
                // Instance properties are those directly in target.Properties
                // Inherited properties come from the class hierarchy
                var instanceProperties = new List<KeyValuePair<string, object>>();
                var inheritedProperties = new List<KeyValuePair<string, object>>();
                if (target.Properties?.Count > 0)
                {
                    // Convert instance properties to list
                    foreach (var prop in target.Properties)
                    {
                        instanceProperties.Add(new KeyValuePair<string, object>(prop.Key, prop.Value));
                    }
                }
                // Get properties from class hierarchy (excluding those overridden in instance)
                if (objectClass != null)
                {
                    var inheritanceChain = CSMOO.Object.ObjectManager.GetInheritanceChain(objectClass.Id);
                    foreach (var classInChain in inheritanceChain)
                    {
                        if (classInChain.Properties?.Count > 0)
                        {
                            foreach (var classProp in classInChain.Properties)
                            {
                                // Only add if not already in instance properties
                                if (!instanceProperties.Any(ip => ip.Key == classProp.Key))
                                {
                                    inheritedProperties.Add(new KeyValuePair<string, object>(classProp.Key, classProp.Value));
                                }
                            }
                        }
                    }
                }
                // Show instance properties
                if (instanceProperties.Count > 0)
                {
                    output.AppendLine("Instance Properties:");
                    foreach (var prop in instanceProperties.OrderBy(p => p.Key))
                    {
                        var value = prop.Value?.ToString() ?? "null";
                        if (value.Length > 50)
                        {
                            value = value.Substring(0, 47) + "...";
                        }
                        if (target.PropAccessors.ContainsKey(prop.Key))
                        {
                            List<Keyword> accList = target.PropAccessors[prop.Key];
                            string accessor = string.Join(" ", accList.Select(k => k.ToString()));
                            accessor = accessor.TrimEnd();
                            output.AppendLine($"  {prop.Key} [{accessor}]: {value}");
                        }
                        else
                        {
                            output.AppendLine($"  {prop.Key} [{"Public"}]: {value}");
                        }
                    }
                }
                else
                {
                    output.AppendLine("Instance Properties: none");
                }
                // Show inherited properties
                if (inheritedProperties.Count > 0)
                {
                    output.AppendLine("Inherited Properties:");
                    foreach (var prop in inheritedProperties.OrderBy(p => p.Key))
                    {
                        var value = prop.Value?.ToString() ?? "null";
                        if (value.Length > 50)
                        {
                            value = value.Substring(0, 47) + "...";
                        }
                        output.AppendLine($"  {prop.Key}: {value} (from class)");
                    }
                }
                else
                {
                    output.AppendLine("Inherited Properties: none");
                }
            }
        }
        notify(Player, output.ToString().TrimEnd());



    }

    [VerbAliases("take")]
    [VerbPattern("get {item}")]
    [VerbDescription("Pick up an object")]
    /// <summary>
    /// Pick up an object
    /// </summary>
    public verb Get(string target)
    {
        if (Args.Count == 0)
        {
            notify(Player, "Get what?");
            return false;
        }

        var targetName = string.Join(" ", Args);
        var resolved = ObjectResolver.ResolveObject(targetName, Player);
        if (resolved == null)
        {
            notify(Player, $"You don't see '{targetName}' here.");
            return false;
        }

        if (Builtins.MoveObject(resolved, Player))
        {
            notify(Player, $"You pick up the {resolved.Name}.");
            return true;
        }
        else
        {
            notify(Player, $"You can't pick up the {resolved.Name}.");
            return false;
        }
    }

    [VerbDescription("Drop an object from inventory")]
    /// <summary>
    /// Drop an object from inventory
    /// </summary>
    public verb Drop(string target)
    {
        if (Args.Count == 0)
        {
            notify(Player, "Drop what?");
            return false;
        }

        var targetName = string.Join(" ", Args);
        var resolved = ObjectResolver.ResolveObject(targetName, Player);
        if (resolved == null)
        {
            notify(Player, $"You don't have '{targetName}'.");
            return false;
        }

        if (Builtins.MoveObject(resolved, Player.Location))
        {
            notify(Player, $"You drop the {resolved.Name}.");
            return true;
        }
        else
        {
            notify(Player, $"You can't drop the {resolved.Name}.");
            return false;
        }
    }

    public string display_login()
    {
        // Dynamic HTML login banner using LESS CSS functionality
        // Define theme variables for consistent styling
        var theme = new Dictionary<string, string>
        {
            ["primary"] = "#00ff88",
            ["secondary"] = "#88ddff",
            ["accent"] = "#ffaa00",
            ["text-light"] = "#cccccc",
            ["text-white"] = "#ffffff",
            ["text-dim"] = "#888888",
            ["background-dark"] = "#000000",
            ["glow-shadow"] = "0 0 30px #00ff88",
            ["border-primary"] = "2px solid #00ff88",
            ["border-accent"] = "1px solid #ffaa00",
            ["spacing-small"] = "10px",
            ["spacing-medium"] = "20px",
            ["spacing-large"] = "30px",
            ["spacing-xlarge"] = "40px",
            ["border-radius"] = "15px",
            ["border-radius-pill"] = "25px"
        };

        // Create the main container with cyberpunk styling
        var container = Html.CyberpunkContainer()
            .WithLessStyle("margin:0; padding:@spacing-medium; min-height:100vh; display:flex; flex-direction:column; align-items:center; justify-content:center;", theme);

        // Create the inner content box
        var contentBox = Html.Div()
            .WithLessStyle("max-width:800px; text-align:center; padding:@spacing-xlarge; border:@border-primary; border-radius:@border-radius; background:@background-dark; box-shadow:@glow-shadow;", theme);

        // Add title
        var title = Html.GlowTitle("CSMOO");

        // Add subtitle
        var subtitle = Html.Div("Multi-User Object Oriented Server")
            .WithLessStyle("font-size:1.2rem; color:@secondary; margin-bottom:@spacing-large; letter-spacing:2px;", theme);

        // Add version badge
        var version = Html.Div("Version 1.0.0 (Dynamic!)")
            .WithLessStyle("font-size:1rem; color:@accent; margin-bottom:@spacing-xlarge; padding:@spacing-small @spacing-medium; border:@border-accent; border-radius:@border-radius; display:inline-block;", theme);

        // Add welcome message
        var welcome = Html.Div("*** Welcome to CSMOO! ***")
            .WithLessStyle("font-size:1.3rem; margin-bottom:@spacing-large; color:@text-white;", theme);

        // Add instructions container
        var instructions = Html.Div()
            .WithLessStyle("font-size:1.1rem; line-height:1.8; color:@text-light; margin-bottom:@spacing-medium;", theme);

        // Add instruction paragraphs
        var intro = Html.P("*** Enter the world of collaborative object-oriented programming and exploration! ***");
        var loginInstr = Html.P()
          .AppendChild(Html.Element("span", "> To connect: "))
          .AppendChild(Html.Command("login <username> <password>"));
        var createInstr = Html.P()
          .AppendChild(Html.Element("span", "> New player: "))
          .AppendChild(Html.Command("create player <username> <password>"));

        // Add footer
        var footer = Html.Div(">> Powered by C# and imagination • Built with ❤️ for MUD enthusiasts! <<")
            .WithLessStyle("margin-top:@spacing-xlarge; font-size:0.9rem; color:@text-dim; font-style:italic;", theme);

        // Assemble the complete structure
        instructions.AppendChildren(intro, loginInstr, createInstr);
        contentBox.AppendChildren(title, subtitle, version, welcome, instructions, footer);
        container.AppendChild(contentBox);

        // Return as single line for MUD client compatibility
        return container.ToSingleLine();
    }
    [VerbAliases("th think ;")]
    public verb Script()
    {
        // Check if any code was provided
        if (Args.Count == 0 || string.Join(" ", Args).Trim().Length == 0)
        {
            notify(Player, "Usage: script { C# code here }");
            notify(Player, "Aliases: ;, th, think");
            notify(Player, "Available variables: Player, This, Args, Input, Verb");
            notify(Player, "All Builtins methods accept either objectId strings or dynamic objects");
            notify(Player, "Example: ; notify(Player, $\"Hello {Player.Name}!\"); ");
            notify(Player, "Example: ; SetProperty(This, \"test\", \"value\"); ");
            return;
        }

        // Join all arguments to reconstruct the script code
        var scriptCode = string.Join(" ", Args);
        var result = Builtins.ExecuteScript(scriptCode, Player, CommandProcessor, This, Input);

        // Only show non-null, non-empty results
        if (!string.IsNullOrEmpty(result) && result != "null")
        {
            Builtins.Log($"[SCRIPT RESULT] Player '{Player.Name}' (ID: {Player.Id}): Script result: {result}");
            notify(Player, $"Script result: {result}");
        }
        else
        {
            Builtins.Log($"[SCRIPT RESULT] Player '{Player.Name}' (ID: {Player.Id}): Script executed successfully (no result)");
            notify(Player, "Script executed successfully.");
        }
    }
}
