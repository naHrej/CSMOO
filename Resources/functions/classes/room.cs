public class Room
{
    /// <summary>
    /// Returns a formatted description of any Room
    /// </summary>
    public string Description()
    {
        if (This is null)
        {
            return "";
        }
        
        var desc = new StringBuilder();
        desc.Append($"<section class='Room'>");
        
        // Room name and description
        var roomName = Builtins.GetProperty(This, "name", "") ?? "a room";
        desc.Append($"<h3 class='Name'>{roomName}</h3>");
        
        var roomDescription = Builtins.GetProperty(This, "longDescription", "")
        ?? Builtins.GetProperty(This, "Description", "") ??
         "You see nothing special.";
        if (!string.IsNullOrEmpty(roomDescription))
        {
            desc.Append($"<p class='Description'>{roomDescription}</p>");
        }
        
        // Two-column layout: Contents (1/3) and Exits (2/3)
        desc.Append("<div class='RoomColumns'>");
        
        // Left column: Contents (1/3 width)
        desc.Append("<div class='RoomColumn ContentsColumn'>");
        desc.Append("<h4 class='ColumnHeader'>Contents</h4>");
        
        // Get players (GetPlayersInRoom already filters to online players only)
        List<GameObject> players = new List<GameObject>();
        try
        {
            players = This.Players();
        }
        catch
        {
            // If Players() fails, use empty list
        }
        
        // Get contents and filter out players
        List<GameObject> contents = new List<GameObject>();
        try
        {
            contents = This.Contents();
        }
        catch
        {
            // If Contents() fails, use empty list
        }
        
        // Separate players from objects by checking if item is in players list
        var playerIds = new HashSet<string>();
        foreach (var player in players)
        {
            try
            {
                var playerId = Builtins.GetProperty(player, "id", "");
                if (!string.IsNullOrEmpty(playerId))
                {
                    playerIds.Add(playerId);
                }
            }
            catch
            {
                // Skip if we can't get ID
            }
        }
        
        var objects = new List<GameObject>();
        foreach (var item in contents)
        {
            try
            {
                var itemId = Builtins.GetProperty(item, "id", "");
                // Only add if it's not a player
                if (!string.IsNullOrEmpty(itemId) && !playerIds.Contains(itemId))
                {
                    objects.Add(item);
                }
            }
            catch
            {
                // If we can't check, assume it's an object
                objects.Add(item);
            }
        }
        
        // Display: players first, then objects
        if (players.Count == 0 && objects.Count == 0)
        {
            desc.Append("<div>No contents</div>");
        }
        else
        {
            // Display players first
            foreach (var player in players)
            {
                try
                {
                    var playerName = Builtins.GetProperty(player, "name", "") ?? "someone";
                    desc.Append($"<div><span class='Player'>{playerName}</span></div>");
                }
                catch
                {
                    // Skip if we can't get name
                }
            }
            
            // Display objects
            foreach (var item in objects)
            {
                try
                {
                    var itemName = Builtins.GetProperty(item, "name", "") ?? "something";
                    desc.Append($"<div><span class='Object'>{itemName}</span></div>");
                }
                catch
                {
                    // Skip if we can't get name
                }
            }
        }
        
        desc.Append("</div>"); // End ContentsColumn
        
        // Right column: Exits (2/3 width)
        desc.Append("<div class='RoomColumn ExitsColumn'>");
        desc.Append("<h4 class='ColumnHeader'>Exits</h4>");
        
        try
        {
            var exits = This.Exits();
            if (exits == null || exits.Count == 0)
            {
                desc.Append("<div>No Exits</div>");
            }
            else
            {
                foreach (var exit in exits)
                {
                    try
                    {
                        var exitName = Builtins.GetProperty(exit, "direction", "") 
                            ?? Builtins.GetProperty(exit, "name", "") 
                            ?? "an exit";
                        var isClosed = Builtins.GetBoolProperty(exit, "closed", false);
                        var isLocked = Builtins.GetBoolProperty(exit, "locked", false);
                        
                        // Format exit name by highlighting capital letters and digits (abbreviation characters)
                        string formattedExitName = exitName ?? "";
                        try
                        {
                            if (!string.IsNullOrEmpty(exitName))
                            {
                                var result = new StringBuilder();
                                var currentAbbrev = new StringBuilder();
                                var currentText = new StringBuilder();
                                
                                foreach (var ch in exitName)
                                {
                                    if (char.IsUpper(ch) || char.IsDigit(ch))
                                    {
                                        // If we have accumulated non-abbrev text, output it first
                                        if (currentText.Length > 0)
                                        {
                                            result.Append(currentText.ToString());
                                            currentText.Clear();
                                        }
                                        // Accumulate abbreviation characters
                                        currentAbbrev.Append(ch.ToString());
                                    }
                                    else
                                    {
                                        // If we have accumulated abbrev characters, wrap and output them
                                        if (currentAbbrev.Length > 0)
                                        {
                                            result.Append($"<span class='abbreviation'>{currentAbbrev.ToString()}</span>");
                                            currentAbbrev.Clear();
                                        }
                                        // Accumulate non-abbrev text
                                        currentText.Append(ch.ToString());
                                    }
                                }
                                
                                // Output any remaining accumulated text
                                if (currentAbbrev.Length > 0)
                                {
                                    result.Append($"<span class='abbreviation'>{currentAbbrev.ToString()}</span>");
                                }
                                if (currentText.Length > 0)
                                {
                                    result.Append(currentText.ToString());
                                }
                                
                                formattedExitName = result.ToString();
                            }
                        }
                        catch
                        {
                            // If formatting fails, just use the plain exit name
                            formattedExitName = exitName ?? "";
                        }
                        
                        // Only open the div after we've successfully formatted the exit name
                        desc.Append("<div class='ExitItem'>");
                        desc.Append($"<span class='Exit'>{formattedExitName}</span>");
                        
                        if (!isClosed)
                        {
                            // Show destination if not closed
                            var destinationId = Builtins.GetProperty(exit, "destination", "");
                            if (!string.IsNullOrEmpty(destinationId))
                            {
                                try
                                {
                                    var destination = Builtins.FindObject(destinationId);
                                    if (destination != null)
                                    {
                                        var destName = Builtins.GetProperty(destination, "name", "") ?? destinationId;
                                        desc.Append($" → <span class='Destination'>{destName}</span>");
                                    }
                                }
                                catch
                                {
                                    // Skip destination if we can't resolve it
                                }
                            }
                        }
                        else
                        {
                            // Show closed/locked status
                            if (isLocked)
                            {
                                desc.Append(" <span class='Locked'>(locked)</span>");
                            }
                            else
                            {
                                desc.Append(" <span class='Closed'>(closed)</span>");
                            }
                        }
                        
                        desc.Append("</div>");
                    }
                    catch
                    {
                        // Log the error and skip this exit - ensure we don't leave unclosed divs
                        // Skip this exit if we can't process it
                    }
                }
            }
        }
        catch
        {
            // If Exits() fails, show "No Exits"
            desc.Append("<div>No Exits</div>");
        }
        
        desc.Append("</div>"); // End ExitsColumn
        desc.Append("</div>"); // End RoomColumns
        
        desc.Append("</section>");
        return desc.ToString();
    }

    /// <summary>
    /// Returns a list of Contents in the room
    /// </summary>
    public List<GameObject> Contents()
    {
        if (This is null)
        {
            return new List<GameObject>();
        }
        // Get objects as dynamic list and convert each item to GameObject
        var objectsDynamic = Builtins.GetObjectsInRoom(This);
        var objects = new List<GameObject>();
        foreach (var obj in objectsDynamic)
        {
            if (obj is GameObject gameObject)
            {
                objects.Add(gameObject);
            }
        }
        return objects;
    }

    /// <summary>
    /// Returns a list of Exits from the room
    /// </summary>
    public List<GameObject> Exits()
    {
        if (This is null)
        {
            return new List<GameObject>();
        }
        // Get exits as dynamic list and convert each item to GameObject
        var exitsDynamic = Builtins.GetExits(This);
        var exits = new List<GameObject>();
        foreach (var exit in exitsDynamic)
        {
            if (exit is GameObject gameObject)
            {
                exits.Add(gameObject);
            }
        }
        return exits;
    }

    /// <summary>
    /// Returns a list of Players in the room
    /// </summary>
    public List<GameObject> Players()
    {
        if (This is null)
        {
            return new List<GameObject>();
        }
        // Get players as dynamic list and convert each item to GameObject
        var playersDynamic = Builtins.GetPlayersInRoom(This);
        var players = new List<GameObject>();
        foreach (var player in playersDynamic)
        {
            if (player is GameObject gameObject)
            {
                players.Add(gameObject);
            }
        }
        return players;
    }
}
