using CSMOO.Parsing;

public class System
{


    public string[] less = LoadFile("stylesheet.less");

    public int test = 1;
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
        notify(Player, $"You say, \"{fullMessage}\"");

        // Send to other players in the room
        var players = Builtins.GetPlayersInRoom(Player.Location);
        foreach (var plr in players)
        {
            if (plr.Id != Player.Id)
                notify(plr, $"{Player.Name} says, \"{fullMessage}\"");
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
    [VerbPattern("{topic}")]
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
            output.AppendLine("=== Help Categories ===");
            foreach (var cat in helpCategories.Keys.OrderBy(x => x))
            {
                output.AppendLine($"<span class='command' style='color:yellow'>{cat}</span>: {string.Join(", ", helpCategories[cat])}");
            }
            output.AppendLine();
            output.AppendLine("Use: <span class='command' style='color:yellow'>help <span class='param' style='color:gray'>&lt;category&gt;</span></span> or <span class='command' style='color:yellow'>help <span class='param' style='color:gray'>&lt;topic&gt;</span></span>");
            notify(Player, output.ToString());
            return;
        }

        var searchTerm = string.Join(" ", Args).ToLower();

        // Check if it's a category
        if (helpCategories.ContainsKey(searchTerm))
        {
            var output = new StringBuilder();
            output.AppendLine($"=== {char.ToUpper(searchTerm[0])}{searchTerm.Substring(1)} Help ===");
            foreach (var topic in helpCategories[searchTerm])
            {
                output.AppendLine($"• <span class='command' style='color:yellow'>{topic}</span>");
            }
            output.AppendLine();
            output.AppendLine($"Use: <span class='command' style='color:yellow'>help <span class='param' style='color:gray'>&lt;topic&gt;</span></span> for specific help");
            notify(Player, output.ToString());
            return;
        }

        // Check if it's a specific topic
        var foundInCategory = helpCategories.FirstOrDefault(kvp =>
            kvp.Value.Any(topic => topic.Equals(searchTerm, StringComparison.OrdinalIgnoreCase)));

        if (!foundInCategory.Equals(default(KeyValuePair<string, List<string>>)))
        {
            // Provide specific help for the topic
            var helpText = his.GetTopicHelp(searchTerm);
            notify(Player, helpText);
        }
        else
        {
            notify(Player, $"No help found for '{searchTerm}'. Try <span class='command' style='color:yellow'>help</span> for available categories.");
        }
    }

    private string GetTopicHelp(string topic)
    {
        return topic.ToLower() switch
        {
            "movement" => "Movement commands: <span class='command' style='color:yellow'>go</span>, <span class='command' style='color:yellow'>north</span>, <span class='command' style='color:yellow'>south</span>, <span class='command' style='color:yellow'>east</span>, <span class='command' style='color:yellow'>west</span>, etc.\nUse <span class='command' style='color:yellow'>look</span> to see available exits.",
            "communication" => "Communication: <span class='command' style='color:yellow'>say</span> (or <span class='command' style='color:yellow'>\"</span>), <span class='command' style='color:yellow'>tell</span>, <span class='command' style='color:yellow'>ooc</span>",
            "objects" => "Object commands: <span class='command' style='color:yellow'>look</span>, <span class='command' style='color:yellow'>examine</span>, <span class='command' style='color:yellow'>get</span>, <span class='command' style='color:yellow'>drop</span>, <span class='command' style='color:yellow'>inventory</span>",
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
            notify(Player, "<p class='error' style='color:red'>You are not in any location.</p>");
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
        var availableExits = $"Available exits: <span class='param' style='color:yellow'>{string.Join(", ", exitNames)}</span>";
        if (Args.Count == 0)
        {
            // Show available exits if no direction given
            if (exits.Count == 0)
            {
                notify(Player, "<p class='error' style='color:red'>There are no exits from here.</p>");
            }
            else
            {
                notify(Player, availableExits);
                notify(Player, "<p class='usage' style='color:green'>Usage: <span class='command' style='color:yellow'>go <span class='param' style='color:gray'>&lt;direction&gt;</span></span></p>");
            }
            return true; // Successfully handled the command (showed help)
        }
        var chosenDirection = direction;
        dynamic chosenExit = null;
        foreach (var exit in exits)
        {
            var exitDirection = Builtins.GetProperty(exit, "direction")?.ToString().ToLowerInvariant();
            if (exitDirection == chosenDirection || exitDirection == $"\"{chosenDirection}\"") // for some reason, property values are coming back quoted
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
            notify(Player, "<p class='error' style='color:red'>That exit doesn't lead anywhere.</p>");
            return true; // Exit exists but broken - we handled the command
        }
        // Move the player
        if (Builtins.MoveObject(Player, destination))
        {
            notify(Player, $"<p class='success' style='color:dodgerblue'>You go <span class='param' style='color:yellow'>{chosenDirection}</span>.</p>");
            notify(Player, destination.Description());
            return true; // Successfully moved
        }
        else
        {
            notify(Player, "<p class='error' style='color:red'>You can't go that way.</p>");
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
        // This is a test
        var target = "";
        if (Args.Count == 0)
        {
            target = "here";
        }
        else if (Args.Count >= 2 && Args[0].ToLower() == "at")
        {
            // 'look at something'
            target = string.Join(" ", Args.Skip(1));
        }
        else
        {
            // 'look something'
            target = string.Join(" ", Args);
        }
        var resolved = (dynamic)(ObjectResolver.ResolveObject(target, Player));
        if (resolved == null)
        {
            notify(player, $"You don't see '{target}' here.");
            return false;
        }
        notify(player, resolved.Description() ?? $"<h3>{resolved.Name}</h3><p>You see nothing special about this {resolved.ClassId}.</p>");
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
        var target = (GameObject)null;
        var targetPlayer = (Player)null;
        var targetId = "";
        var objectClass = (ObjectClass)null;

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
            notify(Player, $"<hr/><p>Examining {target.Properties["name"]} '{targetName}' ({target.Id})</p>");

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
            if (objectClass.Properties?.Any() == true)
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
            if (instances.Any() && (Builtins.IsAdmin(Player) || Builtins.IsModerator(Player)))
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
                output.AppendLine($"Object ID: {target.Id}");
                output.AppendLine($"DB Reference: #{target.DbRef}");
                output.AppendLine($"Created: {target.CreatedAt}");
                output.AppendLine($"Modified: {target.ModifiedAt}");

                // Show player flags if examining a player
                if (targetPlayer != null)
                {
                    var flags = Builtins.GetPlayerFlags(targetPlayer);
                    if (flags.Any())
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
                if (verbs.Any())
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
                var allVerbsWithSource = VerbResolver.GetAllVerbsOnObject(targetId);
                var instanceVerbs = allVerbsWithSource.Where(v => v.source == "instance").ToList();
                var inheritedVerbs = allVerbsWithSource.Where(v => v.source != "instance").ToList();

                // Show instance-specific verbs
                if (instanceVerbs.Any())
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
                if (inheritedVerbs.Any())
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
                if (functions.Any())
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
                var allFunctionsWithSource = FunctionResolver.GetAllFunctionsOnObject(targetId);
                var instanceFunctions = allFunctionsWithSource.Where(f => f.source == "instance").ToList();
                var inheritedFunctions = allFunctionsWithSource.Where(f => f.source != "instance").ToList();

                // Show instance-specific functions
                if (instanceFunctions.Any())
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
                if (inheritedFunctions.Any())
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

                if (target.Properties?.Any() == true)
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
                        if (classInChain.Properties?.Any() == true)
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
                if (instanceProperties.Any())
                {
                    output.AppendLine("Instance Properties:");
                    foreach (var prop in instanceProperties.OrderBy(p => p.Key))
                    {
                        var value = prop.Value?.ToString() ?? "null";
                        if (value.Length > 50)
                        {
                            value = value.Substring(0, 47) + "...";
                        }
                        output.AppendLine($"  {prop.Key}: {value}");
                    }
                }
                else
                {
                    output.AppendLine("Instance Properties: none");
                }

                // Show inherited properties
                if (inheritedProperties.Any())
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
}
