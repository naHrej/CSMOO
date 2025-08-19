using CSMOO.Parsing;

public class System
{
    [VerbAliases("\"")]
    [VerbDescription("Speak to others in the room")]
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
    [VerbPattern("help *")]
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
            var helpText = GetTopicHelp(searchTerm);
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
        var chosenDirection = String.Join(" ", Args).ToLowerInvariant();
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
    [VerbDescription("Look at the room or a specific object")]
    /// <summary>
    /// Look at the room or a specific object
    /// </summary>
    public verb Look(string target)
    {
        // Look command - shows room or looks at specific object.
        string lookTarget = "";
        if (Args.Count == 0)
        {
            lookTarget = "here";
        }
        else if (Args.Count >= 2 && Args[0].ToLower() == "at")
        {
            // 'look at something'
            lookTarget = string.Join(" ", Args.Skip(1));
        }
        else
        {
            // 'look something'
            lookTarget = string.Join(" ", Args);
        }
        var resolved = (dynamic)(ObjectResolver.ResolveObject(lookTarget, Player));
        if (resolved == null)
        {
            notify(Player, $"You don't see '{lookTarget}' here.");
            return false;
        }
        notify(Player, resolved.Description() ?? $"<h3>{resolved.Name}</h3><p>You see nothing special about this {resolved.ClassId}.</p>");
        return true;
    }

    [VerbAliases("exam x")]
    [VerbDescription("Examine an object in detail")]
    /// <summary>
    /// Examine an object in detail
    /// </summary>
    public verb Examine(string target)
    {
        // Examine command - more detailed look at an object
        if (Args.Count == 0)
        {
            notify(Player, "Examine what?");
            return false;
        }

        var targetName = string.Join(" ", Args);
        var resolved = (dynamic)(ObjectResolver.ResolveObject(targetName, Player));
        if (resolved == null)
        {
            notify(Player, $"You don't see '{targetName}' here.");
            return false;
        }

        // Show detailed information
        var description = resolved.Description() ?? $"<h3>{resolved.Name}</h3><p>You see nothing special about this {resolved.ClassId}.</p>";
        notify(Player, description);

        // Show additional details if available
        notify(Player, $"<p class='debug' style='color:gray'>Class: {resolved.ClassId}, ID: {resolved.Id}</p>");
        return true;
    }

    [VerbAliases("take")]
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

        public string DisplayLogin()
    {
        var banner = new StringBuilder();
        banner.AppendLine("╔══════════════════════════════════════════════════════════════╗");
        banner.AppendLine("║                     Welcome to CSMOO                        ║");
        banner.AppendLine("║              C# Multi-User Object Oriented                  ║");
        banner.AppendLine("║                                                              ║");
        banner.AppendLine("║                    Please log in                            ║");
        banner.AppendLine("╚══════════════════════════════════════════════════════════════╝");
        return banner.ToString();
    }
}
