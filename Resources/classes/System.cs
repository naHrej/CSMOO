using CSMOO.Parsing;

public class System
{


    public string[] less = LoadFile("../stylesheet.less");

    public int test = 1;

    [adminonly]
    private readonly int protectedValue = 1;


    [VerbAliases("\"")]
    [VerbDescription("Speak to others in the room")]
    [VerbPattern("*")]
    /// <summary>
    /// Speak to others in the room
    /// </summary>
    /// <category>basics</category>
    /// <category>communication</category>
    /// <topic>social</topic>
    /// <usage>say &lt;message&gt;</usage>
    /// <usage>"&lt;message&gt;</usage>
    /// <help>
    /// The say command allows you to speak to other players in the same room.
    /// You can use either say &lt;message&gt; or just "&lt;message&gt; (quotes).
    /// Your message will be visible to all players in the room.
    /// </help>
    public verb Say(string message)
    {
        // Say command - speak to others in the room
        if (Args.Count == 0)
        {
            notify(Player, "<section class='InCharacter'>Say what?</section>");
            return;
        }

        var fullMessage = string.Join(" ", Args);
        notify(Player, $"<section class='Speech'>You say, \"<span class='text'>{fullMessage}</span>\"</section>");

        // Send to other players in the room
        if (Player.Location is null)
        {
            return;
        }
        // Use pattern matching to ensure compiler recognizes location as non-null
        if (Player.Location is GameObject location)
        {
            var players = Builtins.GetPlayersInRoom(location);
            foreach (var plr in players)
            {
                if (plr is Player && plr.Id != Player.Id)
                  notify(plr, $"<section class='Speech'><span class='actor'>{Player.Name}</span> says, \"<span class='text'>{fullMessage}</span>\"</section>");
            }
        }
    }

    [VerbDescription("List all online players")]
    /// <summary>
    /// List all online players
    /// </summary>
    public verb Who()
    {
        var output = new StringBuilder();
        output.Append("<section class='Who'>");
        output.Append("<h3>Online Players</h3>");
        output.Append("<ul>");  
        // Who command - list online players
        var onlinePlayers = Builtins.GetOnlinePlayers();
        foreach (var onlinePlayer in onlinePlayers)
        {
            output.Append($"<li class='player'>{onlinePlayer.Name}</li>");
        }
        output.Append("</ul>");
        output.Append("</section>");
        notify(Player, output.ToString());
    }

    [VerbAliases("i inv")]
    [VerbDescription("Show what you are carrying")]
    /// <summary>
    /// Show what you are carrying
    /// </summary>
    public verb Inventory()
    {
        // Inventory command - show what the player is carrying
        var playerGameObject = ObjectResolver.ResolveObject("me", Player) ?? ObjectResolver.ResolveObject(Player.Id, Player);
        if (playerGameObject == null)
        {
            notify(Player, "<section class='Error'>Unable to find player object.</section>");
            return;
        }

        if (playerGameObject.Contents == null || playerGameObject.Contents.Count == 0)
        {
            notify(Player, "<section class='InCharacter'>You are carrying nothing.</section>");
            return;
        }

        var output = new StringBuilder();
        output.Append("<section class='InCharacter'>You are carrying:</section>");
        output.Append("<section class='InCharacter'>");
        foreach (var itemId in playerGameObject.Contents)
        {
            var item = ObjectResolver.ResolveObject(itemId, Player);
            if (item != null)
            {
                var name = Builtins.GetProperty(item, "shortDescription")?.AsString ?? item.Name ?? "something";
                output.Append($"<div>  {name}</div>");
            }
        }
        output.Append("</section>");
        notify(Player, output.ToString());
    }

    [VerbAliases("?")]
    [VerbDescription("Show help categories and topics")]
    [VerbPattern("*")]
    /// <summary>
    /// Show help categories and topics
    /// </summary>
    public verb Help(string topic)
    {
        // Get all verbs and functions to build dynamic help
        var allVerbs = Builtins.GetAllVerbs();
        var allFunctions = Builtins.GetAllFunctions();
        
        // Debug: Check if we're getting verbs and if they have categories
        var verbsWithCategories = allVerbs.Where(v => !string.IsNullOrEmpty(v.Categories)).ToList();
        var verbsWithTopics = allVerbs.Where(v => !string.IsNullOrEmpty(v.Topics)).ToList();
        
        // Build categories and topics dynamically from verb and function metadata
        var helpCategories = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var topicToVerbs = new Dictionary<string, List<Verb>>(StringComparer.OrdinalIgnoreCase);
        var topicToFunctions = new Dictionary<string, List<Function>>(StringComparer.OrdinalIgnoreCase);
        var categoryToVerbs = new Dictionary<string, List<Verb>>(StringComparer.OrdinalIgnoreCase);
        var categoryToFunctions = new Dictionary<string, List<Function>>(StringComparer.OrdinalIgnoreCase);
        
        // Helper function to format Usage strings - decodes HTML entities for display
        // Usage strings are now automatically styled during parsing, so we just need to decode entities
        string FormatUsage(string? usage)
        {
            if (string.IsNullOrEmpty(usage)) return "";
            
            // Usage strings are stored with HTML entities escaped (e.g., &lt;span class='param'&gt;&amp;lt;message&amp;gt;&lt;/span&gt;)
            // We need to decode them for display (e.g., <span class='param'>&lt;message&gt;</span>)
            var result = usage
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&amp;", "&")
                .Replace("&quot;", "\"")
                .Replace("&apos;", "'");
            
            return result;
        }
        
        foreach (var verb in allVerbs)
        {
            // Parse categories (stored as comma-separated string)
            if (!string.IsNullOrEmpty(verb.Categories))
            {
                var categoryParts = verb.Categories.Split(',');
                foreach (var catRaw in categoryParts)
                {
                    var cat = catRaw?.Trim();
                    if (!string.IsNullOrWhiteSpace(cat))
                    {
                        if (!helpCategories.ContainsKey(cat))
                        {
                            helpCategories[cat] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            categoryToVerbs[cat] = new List<Verb>();
                        }
                        categoryToVerbs[cat].Add(verb);
                    }
                }
            }
            
            // Parse topics (stored as comma-separated string)
            if (!string.IsNullOrEmpty(verb.Topics))
            {
                var topicParts = verb.Topics.Split(',');
                foreach (var topRaw in topicParts)
                {
                    var top = topRaw?.Trim();
                    if (!string.IsNullOrWhiteSpace(top))
                    {
                        if (!topicToVerbs.ContainsKey(top))
                        {
                            topicToVerbs[top] = new List<Verb>();
                        }
                        topicToVerbs[top].Add(verb);
                        
                        // Add topics to their categories
                        if (!string.IsNullOrEmpty(verb.Categories))
                        {
                            var categoryParts2 = verb.Categories.Split(',');
                            foreach (var catRaw in categoryParts2)
                            {
                                var cat = catRaw?.Trim();
                                if (!string.IsNullOrWhiteSpace(cat) && helpCategories.ContainsKey(cat))
                                {
                                    helpCategories[cat].Add(top);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Process functions similarly
        foreach (var func in allFunctions)
        {
            // Parse categories (stored as comma-separated string)
            if (!string.IsNullOrEmpty(func.Categories))
            {
                var categoryParts = func.Categories.Split(',');
                foreach (var catRaw in categoryParts)
                {
                    var cat = catRaw?.Trim();
                    if (!string.IsNullOrWhiteSpace(cat))
                    {
                        if (!helpCategories.ContainsKey(cat))
                        {
                            helpCategories[cat] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            categoryToVerbs[cat] = new List<Verb>();
                            categoryToFunctions[cat] = new List<Function>();
                        }
                        categoryToFunctions[cat].Add(func);
                    }
                }
            }
            
            // Parse topics (stored as comma-separated string)
            if (!string.IsNullOrEmpty(func.Topics))
            {
                var topicParts = func.Topics.Split(',');
                foreach (var topRaw in topicParts)
                {
                    var top = topRaw?.Trim();
                    if (!string.IsNullOrWhiteSpace(top))
                    {
                        if (!topicToFunctions.ContainsKey(top))
                        {
                            topicToFunctions[top] = new List<Function>();
                        }
                        topicToFunctions[top].Add(func);
                        
                        // Add topics to their categories
                        if (!string.IsNullOrEmpty(func.Categories))
                        {
                            var categoryParts2 = func.Categories.Split(',');
                            foreach (var catRaw in categoryParts2)
                            {
                                var cat = catRaw?.Trim();
                                if (!string.IsNullOrWhiteSpace(cat) && helpCategories.ContainsKey(cat))
                                {
                                    helpCategories[cat].Add(top);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Add automatic "Verbs" category - all verbs belong to it
        if (!helpCategories.ContainsKey("verbs"))
        {
            helpCategories["verbs"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            categoryToVerbs["verbs"] = new List<Verb>();
        }
        foreach (var verb in allVerbs)
        {
            if (!categoryToVerbs["verbs"].Contains(verb))
            {
                categoryToVerbs["verbs"].Add(verb);
            }
        }
        
        // Add automatic "Functions" category - all functions belong to it
        if (!helpCategories.ContainsKey("functions"))
        {
            helpCategories["functions"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            categoryToFunctions["functions"] = new List<Function>();
        }
        foreach (var func in allFunctions)
        {
            if (!categoryToFunctions["functions"].Contains(func))
            {
                categoryToFunctions["functions"].Add(func);
            }
        }

        if (Args.Count == 0)
        {
            // Show help preamble and horizontal list of categories
            var output = new StringBuilder();
            output.Append("<section class='Help'>");
            output.Append("<h3>Help</h3>");
            
            // Display help preamble if available
            var preamble = Builtins.GetHelpPreamble();
            if (!string.IsNullOrEmpty(preamble))
            {
                output.Append($"<div class='description'>{preamble}</div>");
            }
            
            // Display horizontal list of categories
            if (helpCategories.Count > 0)
            {
                var categoryList = helpCategories.Keys.OrderBy(x => x)
                    .Select(cat => $"<li><span class='category'>{cat}</span></li>");
                output.Append("<ul class='help-list'>");
                output.Append(string.Join("", categoryList));
                output.Append("</ul>");
            }
            else
            {
                output.Append("<div>No help categories available.</div>");
            }
            
            output.Append("</section>");
            notify(Player, output.ToString());
            return;
        }

        var searchTerm = string.Join(" ", Args).ToLower();
        
        // Parse search term for object prefix (e.g., "room.description()" or "room.description")
        string? objectPrefix = null;
        string functionSearchTerm = searchTerm;
        var isFunctionLookup = searchTerm.Contains("(") || searchTerm.Contains(")");
        
        // Check for object prefix (e.g., "room.description()" or "exit.description")
        var dotIndex = searchTerm.IndexOf('.');
        if (dotIndex > 0)
        {
            objectPrefix = searchTerm.Substring(0, dotIndex).Trim();
            functionSearchTerm = searchTerm.Substring(dotIndex + 1).Trim();
        }
        
        // Remove parentheses from function search term
        if (isFunctionLookup || functionSearchTerm.Contains("(") || functionSearchTerm.Contains(")"))
        {
            isFunctionLookup = true;
            functionSearchTerm = functionSearchTerm.Replace("(", "").Replace(")", "").Trim();
        }

        // Check if it's a category
        if (categoryToVerbs.ContainsKey(searchTerm) || categoryToFunctions.ContainsKey(searchTerm))
        {
            var output = new StringBuilder();
            var prettyCategory = char.ToUpper(searchTerm[0]) + searchTerm.Substring(1);
            output.Append("<section class='Help'>");
            output.Append($"<h3>{prettyCategory}</h3>");
            
            // Display category description if available
            var (description, summary) = Builtins.GetHelpMetadata(searchTerm);
            if (!string.IsNullOrEmpty(description))
            {
                output.Append($"<div class='description'>{description}</div>");
            }
            else if (!string.IsNullOrEmpty(summary))
            {
                output.Append($"<div class='description'>{summary}</div>");
            }
            
            // Build separate lists for topics, verbs, and functions
            var topicItems = new List<string>();
            var verbItems = new List<string>();
            var functionItems = new List<string>();
            
            // Add topics
            if (helpCategories.ContainsKey(searchTerm))
            {
                foreach (var topic in helpCategories[searchTerm].OrderBy(t => t))
                {
                    topicItems.Add($"<span class='topic'>{topic}</span>");
                }
            }
            
            // Add verbs
            if (categoryToVerbs.ContainsKey(searchTerm))
            {
                foreach (var verb in categoryToVerbs[searchTerm].OrderBy(v => v.Name))
                {
                    verbItems.Add($"<span class='command'>{verb.Name}</span>");
                }
            }
            
            // Add functions with class name
            if (categoryToFunctions.ContainsKey(searchTerm))
            {
                foreach (var func in categoryToFunctions[searchTerm].OrderBy(f => f.Name))
                {
                    var className = Builtins.GetFunctionClassName(func.ObjectId);
                    if (!string.IsNullOrEmpty(className))
                    {
                        var trimmedClassName = className.Trim().ToLower();
                        functionItems.Add($"<span class='param'>{trimmedClassName}</span>.<span class='command'>{func.Name}</span>()");
                    }
                    else
                    {
                        functionItems.Add($"<span class='command'>{func.Name}</span>()");
                    }
                }
            }
            
            if (topicItems.Count > 0)
            {
                output.Append("<div><h4>Topics:</h4> <ul class='help-list'>");
                output.Append(string.Join("", topicItems.Select(item => $"<li>{item}</li>")));
                output.Append("</ul></div>");
            }
            
            if (verbItems.Count > 0)
            {
                output.Append("<div><h4>Verbs:</h4> <ul class='help-list'>");
                output.Append(string.Join("", verbItems.Select(item => $"<li>{item}</li>")));
                output.Append("</ul></div>");
            }
            
            if (functionItems.Count > 0)
            {
                output.Append("<div><h4>Functions:</h4> <ul class='help-list'>");
                output.Append(string.Join("", functionItems.Select(item => $"<li>{item}</li>")));
                output.Append("</ul></div>");
            }
            
            output.Append("</section>");
            notify(Player, output.ToString());
            return;
        }

        // Check if it's a topic
        if (topicToVerbs.ContainsKey(searchTerm) || topicToFunctions.ContainsKey(searchTerm))
        {
            var output = new StringBuilder();
            var prettyTopic = char.ToUpper(searchTerm[0]) + searchTerm.Substring(1);
            output.Append("<section class='Help'>");
            output.Append($"<h3>{prettyTopic}</h3>");
            
            // Display topic description if available
            var (description, summary) = Builtins.GetHelpMetadata(searchTerm);
            if (!string.IsNullOrEmpty(description))
            {
                output.Append($"<div class='description'>{description}</div>");
            }
            else if (!string.IsNullOrEmpty(summary))
            {
                output.Append($"<div class='description'>{summary}</div>");
            }
            
            // Build separate lists for verbs and functions
            var verbItems = new List<string>();
            var functionItems = new List<string>();
            
            // Add verbs
            if (topicToVerbs.ContainsKey(searchTerm))
            {
                foreach (var verb in topicToVerbs[searchTerm].OrderBy(v => v.Name))
                {
                    verbItems.Add($"<span class='command'>{verb.Name}</span>");
                }
            }
            
            // Add functions with class name
            if (topicToFunctions.ContainsKey(searchTerm))
            {
                foreach (var func in topicToFunctions[searchTerm].OrderBy(f => f.Name))
                {
                    var className = Builtins.GetFunctionClassName(func.ObjectId);
                    if (!string.IsNullOrEmpty(className))
                    {
                        var trimmedClassName = className.Trim().ToLower();
                        functionItems.Add($"<span class='param'>{trimmedClassName}</span>.<span class='command'>{func.Name}</span>()");
                    }
                    else
                    {
                        functionItems.Add($"<span class='command'>{func.Name}</span>()");
                    }
                }
            }
            
            if (verbItems.Count > 0)
            {
                output.Append("<div class='help-list'><h4>Verbs:</h4> ");
                output.Append(string.Join(" ", verbItems));
                output.Append("</div>");
            }
            
            if (functionItems.Count > 0)
            {
                output.Append("<div class='help-list'><h4>Functions:</h4> ");
                output.Append(string.Join(" ", functionItems));
                output.Append("</div>");
            }
            
            // Find associated categories and display them horizontally
            var associatedCategories = new List<string>();
            foreach (var verb in allVerbs)
            {
                if (!string.IsNullOrEmpty(verb.Topics) && verb.Topics.Split(',').Any(t => t.Trim().Equals(searchTerm, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!string.IsNullOrEmpty(verb.Categories))
                    {
                        foreach (var cat in verb.Categories.Split(',').Select(c => c?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c)))
                        {
                            if (!associatedCategories.Contains(cat, StringComparer.OrdinalIgnoreCase))
                            {
                                associatedCategories.Add(cat);
                            }
                        }
                    }
                }
            }
            foreach (var func in allFunctions)
            {
                if (!string.IsNullOrEmpty(func.Topics) && func.Topics.Split(',').Any(t => t.Trim().Equals(searchTerm, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!string.IsNullOrEmpty(func.Categories))
                    {
                        foreach (var cat in func.Categories.Split(',').Select(c => c?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c)))
                        {
                            if (!associatedCategories.Contains(cat, StringComparer.OrdinalIgnoreCase))
                            {
                                associatedCategories.Add(cat);
                            }
                        }
                    }
                }
            }
            
            if (associatedCategories.Count > 0)
            {
                output.Append("<div style='margin-top: 0.5em;'><h4>Categories:</h4> <ul class='help-list'>");
                output.Append(string.Join("", associatedCategories.OrderBy(c => c).Select(c => $"<li><span class='category'>{c}</span></li>")));
                output.Append("</ul></div>");
            }
            
            output.Append("</section>");
            notify(Player, output.ToString());
            return;
        }

        // Helper function to get function display name
        string GetFunctionDisplayName(Function func)
        {
            var className = Builtins.GetFunctionClassName(func.ObjectId);
            if (!string.IsNullOrEmpty(className))
            {
                return $"<span class='param'>{className.Trim().ToLower()}</span>.<span class='command'>{func.Name}</span>()";
            }
            return $"<span class='command'>{func.Name}</span>()";
        }
        
        // Check if it's a function or verb
        // Priority: If object prefix or parentheses present, prefer function; otherwise prefer verb, then function as fallback
        List<Function> matchingFunctions = new List<Function>();
        Verb? matchingVerb = null;
        
        if (isFunctionLookup || !string.IsNullOrEmpty(objectPrefix))
        {
            // Function lookup - search with object prefix if specified
            if (!string.IsNullOrEmpty(objectPrefix))
            {
                // Search functions on specific object type
                matchingFunctions = allFunctions.Where(f =>
                {
                    var className = Builtins.GetFunctionClassName(f.ObjectId);
                    if (string.IsNullOrEmpty(className)) return false;
                    var normalizedClassName = className.Trim().ToLower();
                    return normalizedClassName.Equals(objectPrefix, StringComparison.OrdinalIgnoreCase) &&
                           (f.Name.Equals(functionSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                            f.Name.StartsWith(functionSearchTerm, StringComparison.OrdinalIgnoreCase));
                }).ToList();
            }
            else
            {
                // Search all functions with partial matching
                matchingFunctions = allFunctions.Where(f =>
                    f.Name.Equals(functionSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    f.Name.StartsWith(functionSearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }
        else
        {
            // No parentheses or object prefix - prefer verb first
            matchingVerb = allVerbs.FirstOrDefault(v =>
                v.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(v.Aliases) && v.Aliases.Split(' ')
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Any(a => a.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))));
            
            // If no verb found, try function with partial matching
            if (matchingVerb == null)
            {
                matchingFunctions = allFunctions.Where(f =>
                    f.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    f.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }
        
        // If multiple functions match, show list
        if (matchingFunctions.Count > 1)
        {
            var output = new StringBuilder();
            output.Append("<section class='Help'>");
            output.Append($"<h3>Help {searchTerm}</h3>");
            output.Append("<div><h4>Functions:</h4> <ul class='help-list'>");
            foreach (var func in matchingFunctions.OrderBy(f => Builtins.GetFunctionClassName(f.ObjectId) ?? "").ThenBy(f => f.Name))
            {
                output.Append($"<li>{GetFunctionDisplayName(func)}</li>");
            }
            output.Append("</ul></div>");
            output.Append("</section>");
            notify(Player, output.ToString());
            return;
        }
        
        // Single function match
        Function? matchingFunction = matchingFunctions.FirstOrDefault();

        // Display function help if found
        if (matchingFunction != null)
        {
            var output = new StringBuilder();
            var funcDisplayName = GetFunctionDisplayName(matchingFunction);
            
            output.Append("<section class='Help'>");
            output.Append($"<h3>Help for {funcDisplayName}</h3>");
            
            // Display help text (description, usage, helpText)
            if (!string.IsNullOrEmpty(matchingFunction.HelpText))
            {
                output.Append($"<div class='description'>{matchingFunction.HelpText}</div>");
            }
            else if (!string.IsNullOrEmpty(matchingFunction.Description))
            {
                output.Append($"<div class='description'>{matchingFunction.Description}</div>");
            }
            
            if (!string.IsNullOrEmpty(matchingFunction.Usage))
            {
                output.Append($"<div><span class='usage'>Usage: <span class='command'>{FormatUsage(matchingFunction.Usage)}</span></span></div>");
            }
            
            // Display horizontal lists of topics and categories
            var topicList = new List<string>();
            var categoryList = new List<string>();
            
            if (!string.IsNullOrEmpty(matchingFunction.Topics))
            {
                var topicParts = matchingFunction.Topics.Split(',');
                topicList = topicParts.Select(t => t?.Trim()).Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => $"<span class='topic'>{t}</span>").ToList();
            }
            
            if (!string.IsNullOrEmpty(matchingFunction.Categories))
            {
                var categoryParts = matchingFunction.Categories.Split(',');
                categoryList = categoryParts.Select(c => c?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => $"<span class='category'>{c}</span>").ToList();
            }
            
            if (topicList.Count > 0)
            {
                output.Append("<div><h4>Topics:</h4> <ul class='help-list'>");
                output.Append(string.Join("", topicList.Select(item => $"<li>{item}</li>")));
                output.Append("</ul></div>");
            }
            
            if (categoryList.Count > 0)
            {
                output.Append("<div><h4>Categories:</h4> <ul class='help-list'>");
                output.Append(string.Join("", categoryList.Select(item => $"<li>{item}</li>")));
                output.Append("</ul></div>");
            }
            
            output.Append("</section>");
            notify(Player, output.ToString());
            return;
        }

        if (matchingVerb != null)
        {
            var output = new StringBuilder();
            output.Append("<section class='Help'>");
            output.Append($"<h3>Help for <span class='command'>{matchingVerb.Name}</span></h3>");
            
            // Display help text (helpText, description, usage, aliases)
            if (!string.IsNullOrEmpty(matchingVerb.HelpText))
            {
                output.Append($"<div class='description'>{matchingVerb.HelpText}</div>");
            }
            else if (!string.IsNullOrEmpty(matchingVerb.Description))
            {
                output.Append($"<div class='description'>{matchingVerb.Description}</div>");
            }
            
            if (!string.IsNullOrEmpty(matchingVerb.Usage))
            {
                output.Append($"<div><span class='usage'>Usage: <span class='command'>{FormatUsage(matchingVerb.Usage)}</span></span></div>");
            }
            
            if (!string.IsNullOrEmpty(matchingVerb.Aliases))
            {
                var aliasParts = matchingVerb.Aliases.Split(' ');
                var aliases = aliasParts.Where(a => !string.IsNullOrWhiteSpace(a));
                output.Append($"<div><span class='usage'>Aliases: {string.Join(", ", aliases.Select(a => $"<span class='command'>{a}</span>"))}</span></div>");
            }
            
            // Display horizontal lists of topics and categories
            var topicList = new List<string>();
            var categoryList = new List<string>();
            
            if (!string.IsNullOrEmpty(matchingVerb.Topics))
            {
                var topicParts = matchingVerb.Topics.Split(',');
                topicList = topicParts.Select(t => t?.Trim()).Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => $"<span class='topic'>{t}</span>").ToList();
            }
            
            if (!string.IsNullOrEmpty(matchingVerb.Categories))
            {
                var categoryParts = matchingVerb.Categories.Split(',');
                categoryList = categoryParts.Select(c => c?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => $"<span class='category'>{c}</span>").ToList();
            }
            
            if (topicList.Count > 0)
            {
                output.Append("<div><h4>Topics:</h4> <ul class='help-list'>");
                output.Append(string.Join("", topicList.Select(item => $"<li>{item}</li>")));
                output.Append("</ul></div>");
            }
            
            if (categoryList.Count > 0)
            {
                output.Append("<div><h4>Categories:</h4> <ul class='help-list'>");
                output.Append(string.Join("", categoryList.Select(item => $"<li>{item}</li>")));
                output.Append("</ul></div>");
            }
            
            output.Append("</section>");
            notify(Player, output.ToString());
            return;
        }

        // No match found
        notify(Player, $"<section class='Help'><div>No help found for '<span class='param'>{searchTerm}</span>'. Try <span class='command'>help</span> for available categories.</div></section>");
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
    /// <category>basics</category>
    /// <category>movement</category>
    /// <topic>navigation</topic>
    /// <usage>go &lt;direction&gt;</usage>
    /// <usage>&lt;direction&gt;</usage>
    /// <help>
    /// The go command allows you to move between rooms using exits.
    /// You can use "go &lt;direction&gt;" or just type the direction name (north, south, east, west, etc.).
    /// Use the look command to see available exits from your current location.
    /// </help>
    public verb Go(string direction)
    {
        // Smart go command - move in any available direction.
        if (Player is null)
        {
            return false;
        }
        var location = Player.Location;
        if (location is null)
        {
            notify(Player, "<p class='error'>You are not in any location.</p>");
            return false;
        }
        // GetExits requires a Room, so cast only when needed
        if (location is not Room currentRoom)
        {
            notify(Player, "<p class='error' style='color:red'>You cannot go anywhere from here.</p>");
            return false;
        }
        var exits = Builtins.GetExits(currentRoom);
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
                notify(Player, $"<section class='Info'><div>{availableExits}</div></section>");
                notify(Player, "<p class='usage'>Usage: <span class='command'>go <span class='param'>&lt;direction&gt;</span></span></p>");
            }
            return true; // Successfully handled the command (showed help)
        }
        var chosenDirection = direction.ToLowerInvariant();
        GameObject? chosenExit = null;
        
        // Extract abbreviation (capitalized letters and numbers) - inlined to avoid local function issues
        string ExtractAbbreviation(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var result = new global::System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (global::System.Char.IsUpper(c) || global::System.Char.IsDigit(c))
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
                chosenExit = exit;
                break;
            }
            
            // Check abbreviation match
            var exitAbbreviation = ExtractAbbreviation(exitDirection).ToUpperInvariant();
            if (!string.IsNullOrEmpty(exitAbbreviation) && 
                exitAbbreviation == chosenAbbreviation)
            {
                chosenExit = exit;
                break;
            }
        }
        if (chosenExit == null)
        {
            return false; // Direction not recognized - let other command processing handle it
        }
        // Get destination from exit properties and resolve to GameObject
        var destinationId = Builtins.GetProperty(chosenExit, "destination", "");
        GameObject? destination = null;
        if (!string.IsNullOrEmpty(destinationId))
        {
            destination = ObjectResolver.ResolveObject(destinationId, Player);
        }

        if (destination == null)
        {
            notify(Player, "<p class='error'>That exit doesn't lead anywhere.</p>");
            return true; // Exit exists but broken - we handled the command
        }
        // Move the player
        if (Builtins.MoveObject(Player, destination))
        {
            notify(Player, $"<section class='InCharacter'>You go <span class='object'>{chosenDirection}</span>.</section>");
            // Call Description() function if available, otherwise use property
            try
            {
                var descResult = CallFunctionOnObject(destination, "Description");
                var description = descResult?.ToString() ?? "";
                if (!string.IsNullOrEmpty(description))
                {
                    // Description should already be HTML from Room.Description(), but ensure it's wrapped if needed
                    if (!description.TrimStart().StartsWith("<"))
                    {
                        notify(Player, $"<section class='Room'>{description}</section>");
                    }
                    else
                    {
                        notify(Player, description);
                    }
                }
            }
            catch
            {
                // Fallback to property access if function call fails
                var description = Builtins.GetProperty(destination, "longDescription", "") ?? Builtins.GetProperty(destination, "description", "") ?? "You see nothing special.";
                notify(Player, $"<section class='Room'><div class='description'>{description}</div></section>");
            }
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
    /// <category>basics</category>
    /// <category>objects</category>
    /// <topic>examination</topic>
    /// <topic>navigation</topic>
    /// <usage>look</usage>
    /// <usage>look &lt;object&gt;</usage>
    /// <alias>l</alias>
    /// <help>
    /// The look command shows you information about your current location or a specific object.
    /// Use "look" to see the room description, exits, and contents.
    /// Use "look &lt;object&gt;" to examine a specific object in detail.
    /// </help>
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
        string lookDescHtml = "";
        try
        {
            // Use CallFunctionOnObject to call Description() function on the object
            var descResult = CallFunctionOnObject(resolved, "Description");
            lookDescHtml = descResult?.ToString() ?? "";
        }
        catch (Exception)
        {
            // Object has no owner or Description() function failed, use typed property access
            try
            {
                // Use typed access where possible - resolved is GameObject?, can access properties directly
                var name = resolved.Name ?? Builtins.GetProperty(resolved, "name", "") ?? "something";
                var longDesc = Builtins.GetProperty(resolved, "longDescription", "") ?? Builtins.GetProperty(resolved, "description", "") ?? "";
                var shortDesc = Builtins.GetProperty(resolved, "shortDescription", "") ?? "";
                var classId = resolved.ClassId ?? "object";
                
                if (!string.IsNullOrEmpty(longDesc))
                {
                    lookDescHtml = $"<h3>{name}</h3><p>{longDesc}</p>";
                }
                else if (!string.IsNullOrEmpty(shortDesc))
                {
                    lookDescHtml = $"<h3>{name}</h3><p>{shortDesc}</p>";
                }
                else
                {
                    lookDescHtml = $"<h3>{name}</h3><p>You see nothing special about this {classId}.</p>";
                }
            }
            catch
            {
                // If even property access fails, show a generic message
                lookDescHtml = $"<p>You see something, but can't make out any details.</p>";
            }
        }
        if(!string.IsNullOrEmpty(lookDescHtml))
        {
            // Description should already be HTML from Room.Description(), but ensure it's wrapped if needed
            if (!lookDescHtml.TrimStart().StartsWith("<"))
            {
                notify(Player, $"<section class='Room'>{lookDescHtml}</section>");
            }
            else
            {
                notify(Player, lookDescHtml);
            }
        }
        return true;
    }

    [VerbAliases("exam x ex")]
    [VerbPattern("{targetName}")]
    [VerbDescription("Examine an object in detail")]
    /// <summary>
    /// Examine an object in detail
    /// </summary>
    /// <category>basics</category>
    /// <category>objects</category>
    /// <topic>examination</topic>
    /// <usage>examine &lt;object&gt;</usage>
    /// <alias>ex</alias>
    /// <help>
    /// The examine command provides detailed information about an object.
    /// This is more detailed than the look command and shows object properties, class information, and other metadata.
    /// </help>
    public verb Examine(string targetName)
    {
        // First, try to resolve as a class name
        var classTarget = Builtins.GetClassByName(targetName);
        var isExaminingClass = classTarget != null;
        GameObject? target = null;
        Player? targetPlayer = null;
        var targetId = "";
        ObjectClass? objectClass = null;
        if (isExaminingClass && classTarget is not null)
        {
            // Examining a class definition
            objectClass = classTarget;
            targetId = classTarget.Id;
            notify(Player, $"<section class='Examine'><div class='header'>Examining class '<span class='name'>{classTarget.Name}</span>' ({classTarget.Id})</div></section>");
        }
        else
        {
            // Try to resolve as an object instance
            target = ObjectResolver.ResolveObject(targetName, Player);
            if (target is null)
            {
                notify(Player, $"<section class='InCharacter'>You can't see '<span class='object'>{targetName}</span>' here.</section>");
                return true;
            }
            targetId = target.Id;
            objectClass = Builtins.GetObjectClass(target);
            var targetNameProp = Builtins.GetProperty(target, "name", "") ?? target.Name ?? "something";
            notify(Player, $"<section class='Examine'><div class='header'>Examining {targetNameProp} '<span class='name'>{targetName}</span>' ({target.Id})</div></section>");
            // Check if target is a player (only for object instances)
            if (Builtins.IsPlayerObject(target))
            {
                targetPlayer = (Player)target;
            }
        }
        // Get basic properties using different approaches for classes vs instances
        string name, shortDesc, longDesc;
        if (isExaminingClass && objectClass is not null)
        {
            // For class definitions, use class properties
            name = objectClass.Name;
            shortDesc = objectClass.Description;
            longDesc = objectClass.Description;
        }
        else
        {
            // For object instances, use typed property access
            // target is already checked for null in the else block above
            if (target is null)
            {
                notify(Player, "<section class='error'><div>Error: Object information not available.</div></section>");
                return false;
            }
            name = target.Name ?? Builtins.GetObjectName(target);
            shortDesc = Builtins.GetProperty(target, "shortDescription", "") ?? "";
            longDesc = Builtins.GetProperty(target, "longDescription", "") ?? Builtins.GetProperty(target, "description", "") ?? "";
        }
        // Build the examination output
        var output = new StringBuilder();
        output.Append("<section class='Examine'>");
        // Object name and short description
        if (!string.IsNullOrEmpty(shortDesc))
        {
            output.Append($"<div class='name'>{name} ({shortDesc})</div>");
        }
        else
        {
            output.Append($"<div class='name'>{name}</div>");
        }
        // Long description
        if (!string.IsNullOrEmpty(longDesc))
        {
            output.Append($"<div class='description'>{longDesc}</div>");
        }
        else
        {
            output.Append("<div class='description'>You see nothing special.</div>");
        }
        // Show contents if it's a container (only for object instances)
        if (!isExaminingClass)
        {
            var contents = Helpers.GetObjectsInLocation(targetId);
            if (contents.Any())
            {
                output.Append("<p>");
                output.Append("<span class='header'>Contents:</span>");
                foreach (var item in contents)
                {
                    var itemName = item.Name ?? "unknown object";
                    var itemShort = item.shortDescription ?? "";
                    var displayName = !string.IsNullOrEmpty(itemShort) ? $"{itemName} ({itemShort})" : itemName;
                    output.Append($"<div>  {displayName}</div>");
                }
                output.Append("</p>");
            }
        }
        // For class definitions, show default properties and instances
        if (isExaminingClass && objectClass is not null)
        {
            output.Append("<div class='class-info'><h4>Class Information</h4>");
            // Show inheritance
            if (!string.IsNullOrEmpty(objectClass.ParentClassId))
            {
                var parentClass = Builtins.GetClass(objectClass.ParentClassId);
                output.Append($"<div>Inherits from: {parentClass?.Name ?? objectClass.ParentClassId}</div>");
            }
            else
            {
                output.Append("<div>Inherits from: (none - root class)</div>");
            }
            // Show if it's abstract
            if (objectClass.IsAbstract)
            {
                output.Append("<div>Type: Abstract class (cannot be instantiated)</div>");
            }
            else
            {
                output.Append("<div>Type: Concrete class</div>");
            }
            // Show default properties
            if (objectClass.Properties?.Count > 0)
            {
                output.Append("<div><strong>Default Properties:</strong>");
                foreach (var prop in objectClass.Properties)
                {
                    var value = prop.Value?.ToString() ?? "null";
                    if (value.Length > 50)
                    {
                        value = value.Substring(0, 47) + "...";
                    }
                    output.Append($"<div>  {prop.Key}: {value}</div>");
                }
                output.Append("</div>");
            }
            // Show instances of this class
            var instances = Builtins.GetObjectsByClass(objectClass.Id);
            output.Append($"<div>Instances: {instances.Count()}");
            if (instances.Count > 0 && (Builtins.IsAdmin(Player) || Builtins.IsModerator(Player)))
            {
                var limitedInstances = instances.Take(10);
                foreach (var instance in limitedInstances)
                {
                    var instName = instance.Name ?? "unnamed";
                    output.Append($"<div>  #{instance.DbRef}: {instName}</div>");
                }
                if (instances.Count() > 10)
                {
                    output.Append($"<div>  ... and {instances.Count() - 10} more</div>");
                }
            }
            output.Append("</div></div>");
        }
        // Administrative information for Admin/Moderator users
        if (Builtins.IsAdmin(Player) || Builtins.IsModerator(Player))
        {
            output.Append("<div class='admin-info'><h4>Administrative Information</h4>");
            if (isExaminingClass && objectClass is not null)
            {
                // Administrative info for class definitions
                output.Append($"<div>Class ID: {objectClass.Id}</div>");
                output.Append($"<div>Class Name: {objectClass.Name}</div>");
                output.Append($"<div>Created: {objectClass.CreatedAt}</div>");
                output.Append($"<div>Modified: {objectClass.ModifiedAt}</div>");
            }
            else if (!isExaminingClass && target is not null)
            {
                // Administrative info for object instances
                output.Append($"<div>Owner: {target.Owner}(#{target.DbRef})</div>");
                output.Append($"<div>Object ID: {target.Id}</div>");
                output.Append($"<div>DB Reference: #{target.DbRef}</div>");
                output.Append($"<div>Created: {target.CreatedAt}</div>");
                output.Append($"<div>Modified: {target.ModifiedAt}</div>");
                // Show player flags if examining a player
                if (targetPlayer != null)
                {
                    var flags = Builtins.GetPlayerFlags(targetPlayer);
                    if (flags.Count > 0)
                    {
                        output.Append($"<div>Player Flags: {string.Join(", ", flags)}</div>");
                    }
                    else
                    {
                        output.Append("<div>Player Flags: none</div>");
                    }
                }
            }
            // Show object class for both cases
            if (objectClass != null)
            {
                output.Append($"<div>Class: {objectClass.Name}</div>");
            }
            // Show verbs (different approach for classes vs instances)
            if (isExaminingClass && objectClass is not null)
            {
                var verbs = Builtins.GetVerbsOnClass(objectClass.Id);
                if (verbs.Count > 0)
                {
                    output.Append("<div><strong>Class Verbs:</strong>");
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
                        output.Append($"<div>{verbInfo}</div>");
                    }
                    output.Append("</div>");
                }
                else
                {
                    output.Append("<div>Class Verbs: none</div>");
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
                    output.Append("<div><strong>Instance Verbs:</strong>");
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
                        output.Append($"<div>{verbInfo}</div>");
                    }
                    output.Append("</div>");
                }
                else
                {
                    output.Append("<div>Instance Verbs: none</div>");
                }
                // Show inherited verbs
                if (inheritedVerbs.Count > 0)
                {
                    output.Append("<div><strong>Inherited Verbs:</strong>");
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
                        output.Append($"<div>{verbInfo}</div>");
                    }
                    output.Append("</div>");
                }
                else
                {
                    output.Append("<div>Inherited Verbs: none</div>");
                }
            }
            // Show functions (different approach for classes vs instances)
            if (isExaminingClass && objectClass is not null)
            {
                var functions = Builtins.GetFunctionsOnClass(objectClass.Id);
                if (functions.Count > 0)
                {
                    output.Append("<div><strong>Class Functions:</strong>");
                    foreach (var func in functions.OrderBy(f => f.Name))
                    {
                        var paramString = string.Join(", ", func.ParameterTypes.Zip(func.ParameterNames, (type, name) => $"{type} {name}"));
                        var funcInfo = $"  {func.ReturnType} {func.Name}({paramString})";
                        if (!string.IsNullOrEmpty(func.Description))
                        {
                            funcInfo += $" - {func.Description}";
                        }
                        output.Append($"<div>{funcInfo}</div>");
                    }
                    output.Append("</div>");
                }
                else
                {
                    output.Append("<div>Class Functions: none</div>");
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
                    output.Append("<div><strong>Instance Functions:</strong>");
                    foreach (var (func, source) in instanceFunctions.OrderBy(f => f.function.Name))
                    {
                        var paramString = string.Join(", ", func.ParameterTypes.Zip(func.ParameterNames, (type, name) => $"{type} {name}"));
                        var funcInfo = $"  {func.ReturnType} {func.Name}({paramString})";
                        if (!string.IsNullOrEmpty(func.Description))
                        {
                            funcInfo += $" - {func.Description}";
                        }
                        output.Append($"<div>{funcInfo}</div>");
                    }
                    output.Append("</div>");
                }
                else
                {
                    output.Append("<div>Instance Functions: none</div>");
                }
                // Show inherited functions
                if (inheritedFunctions.Count > 0)
                {
                    output.Append("<div><strong>Inherited Functions:</strong>");
                    foreach (var (func, source) in inheritedFunctions.OrderBy(f => f.function.Name))
                    {
                        var paramString = string.Join(", ", func.ParameterTypes.Zip(func.ParameterNames, (type, name) => $"{type} {name}"));
                        var funcInfo = $"  {func.ReturnType} {func.Name}({paramString})";
                        if (!string.IsNullOrEmpty(func.Description))
                        {
                            funcInfo += $" - {func.Description}";
                        }
                        funcInfo += $" (from {source})";
                        output.Append($"<div>{funcInfo}</div>");
                    }
                    output.Append("</div>");
                }
                else
                {
                    output.Append("<div>Inherited Functions: none</div>");
                }
            }
            // Show properties (only for object instances, classes already show default properties above)
            if (!isExaminingClass && target is not null)
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
                    // Use Builtins wrapper to avoid namespace auto-resolution issues
                    var inheritanceChain = Builtins.GetInheritanceChain(objectClass.Id);
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
                    output.Append("<div><strong>Instance Properties:</strong>");
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
                            output.Append($"<div>  {prop.Key} [{accessor}]: {value}</div>");
                        }
                        else
                        {
                            output.Append($"<div>  {prop.Key} [{"Public"}]: {value}</div>");
                        }
                    }
                    output.Append("</div>");
                }
                else
                {
                    output.Append("<div>Instance Properties: none</div>");
                }
                // Show inherited properties
                if (inheritedProperties.Count > 0)
                {
                    output.Append("<div><strong>Inherited Properties:</strong>");
                    foreach (var prop in inheritedProperties.OrderBy(p => p.Key))
                    {
                        var value = prop.Value?.ToString() ?? "null";
                        if (value.Length > 50)
                        {
                            value = value.Substring(0, 47) + "...";
                        }
                        output.Append($"<div>  {prop.Key}: {value} (from class)</div>");
                    }
                    output.Append("</div>");
                }
                else
                {
                    output.Append("<div>Inherited Properties: none</div>");
                }
            }
            output.Append("</div>"); // Close admin-info div
        }
        output.Append("</section>"); // Close Examine section
        notify(Player, output.ToString());



    }

    [VerbAliases("take")]
    [VerbPattern("get {item}")]
    [VerbDescription("Pick up an object")]
    /// <summary>
    /// Pick up an object
    /// </summary>
    /// <category>basics</category>
    /// <category>objects</category>
    /// <topic>inventory</topic>
    /// <usage>get &lt;object&gt;</usage>
    /// <usage>take &lt;object&gt;</usage>
    /// <help>
    /// The get command allows you to pick up objects from your current location and add them to your inventory.
    /// You can only get objects that are in the same room as you.
    /// Use the inventory command to see what you're carrying.
    /// </help>
    public verb Get(string target)
    {
        if (Args.Count == 0)
        {
            notify(Player, "<section class='InCharacter'>Get what?</section>");
            return false;
        }

        var targetName = string.Join(" ", Args);
        var resolved = ObjectResolver.ResolveObject(targetName, Player);
        if (resolved is null)
        {
            notify(Player, $"<section class='InCharacter'>You don't see '<span class='object'>{targetName}</span>' here.</section>");
            return false;
        }

        if (Builtins.MoveObject(resolved, Player))
        {
            notify(Player, $"<section class='InCharacter'>You pick up the <span class='object'>{resolved.Name}</span>.</section>");
            return true;
        }
        else
        {
            notify(Player, $"<section class='InCharacter'>You can't pick up the <span class='object'>{resolved.Name}</span>.</section>");
            return false;
        }
    }

    [VerbDescription("Drop an object from inventory")]
    /// <summary>
    /// Drop an object from inventory
    /// </summary>
    /// <category>basics</category>
    /// <category>objects</category>
    /// <topic>inventory</topic>
    /// <usage>drop &lt;object&gt;</usage>
    /// <help>
    /// The drop command allows you to remove an object from your inventory and place it in your current location.
    /// The object will be visible to other players in the room after you drop it.
    /// </help>
    public verb Drop(string target)
    {
        if (Args.Count == 0)
        {
            notify(Player, "<section class='InCharacter'>Drop what?</section>");
            return false;
        }

        var targetName = string.Join(" ", Args);
        var resolved = ObjectResolver.ResolveObject(targetName, Player);
        if (resolved is null)
        {
            notify(Player, $"<section class='InCharacter'>You don't have '<span class='object'>{targetName}</span>'.</section>");
            return false;
        }

        if (Player.Location is null)
        {
            notify(Player, "<section class='InCharacter'>You are not in any location.</section>");
            return false;
        }
        // Use pattern matching to ensure compiler recognizes location as non-null
        if (Player.Location is GameObject location)
        {
            if (Builtins.MoveObject(resolved, location))
            {
                notify(Player, $"<section class='InCharacter'>You drop the <span class='object'>{resolved.Name}</span>.</section>");
                return true;
            }
            else
            {
                notify(Player, $"<section class='InCharacter'>You can't drop the <span class='object'>{resolved.Name}</span>.</section>");
                return false;
            }
        }
        notify(Player, "<section class='InCharacter'>You are not in any location.</section>");
        return false;
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
            var output = new StringBuilder();
            output.Append("<section class='Help'><h3>Script Command</h3>");
            output.Append("<div><strong>Usage:</strong> <span class='command'>script</span> { C# code here }</div>");
            output.Append("<div><strong>Aliases:</strong> <span class='command'>;</span>, <span class='command'>th</span>, <span class='command'>think</span></div>");
            output.Append("<div><strong>Available variables:</strong> <span class='code'>Player</span>, <span class='code'>This</span>, <span class='code'>Args</span>, <span class='code'>Input</span>, <span class='code'>Verb</span></div>");
            output.Append("<div>All Builtins methods accept either objectId strings or dynamic objects</div>");
            output.Append("<div><strong>Examples:</strong></div>");
            output.Append("<div><span class='code'>; notify(Player, $\"Hello {Player.Name}!\");</span></div>");
            output.Append("<div><span class='code'>; SetProperty(This, \"test\", \"value\");</span></div>");
            output.Append("</section>");
            notify(Player, output.ToString());
            return;
        }

        // Join all arguments to reconstruct the script code
        var scriptCode = string.Join(" ", Args);
        var result = Builtins.ExecuteScript(scriptCode, Player, CommandProcessor, This, Input);

        // Only show non-null, non-empty results
        if (!string.IsNullOrEmpty(result) && result != "null")
        {
            Builtins.Log($"[SCRIPT RESULT] Player '{Player.Name}' (ID: {Player.Id}): Script result: {result}");
            // Wrap result in HTML - check if it's already HTML
            if (result.TrimStart().StartsWith("<"))
            {
                notify(Player, $"<section class='ScriptResult'><div><strong>Script result:</strong> {result}</div></section>");
            }
            else
            {
                notify(Player, $"<section class='ScriptResult'><div><strong>Script result:</strong> <span class='code'>{result}</span></div></section>");
            }
        }
        else
        {
            Builtins.Log($"[SCRIPT RESULT] Player '{Player.Name}' (ID: {Player.Id}): Script executed successfully (no result)");
            notify(Player, "<section class='Success'>Script executed successfully.</section>");
        }
    }
}
