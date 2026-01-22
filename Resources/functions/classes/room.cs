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
        
        // Get all objects in the room (excluding exits)
        List<GameObject> allObjects = new List<GameObject>();
        try
        {
            // Get all objects in location
            var allObjectsDynamic = Builtins.GetObjectsInLocation(This.Id);
            foreach (var obj in allObjectsDynamic)
            {
                try
                {
                    GameObject? gameObject = obj as GameObject;
                    if (gameObject == null && obj != null)
                    {
                        var id = obj.Id;
                        if (id != null)
                        {
                            gameObject = Builtins.FindObject(id.ToString()) as GameObject;
                        }
                    }
                    if (gameObject != null)
                    {
                        allObjects.Add(gameObject);
                    }
                }
                catch
                {
                    // Skip if conversion fails
                }
            }
        }
        catch
        {
            // If GetObjectsInLocation fails, use empty list
        }
        
        // Separate players from objects by checking if item is in players list
        var playerIds = new HashSet<string>();
        foreach (var player in players)
        {
            try
            {
                var playerId = player?.Id ?? "";
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
        
        // Filter out players and exits from all objects
        var exitClassId = Builtins.GetClassByName("Exit")?.Id ?? "";
        var objects = new List<GameObject>();
        foreach (var item in allObjects)
        {
            try
            {
                if (item == null) continue;
                
                var itemId = item.Id ?? "";
                var itemClassId = item.ClassId ?? "";
                
                // Skip if it's a player
                if (!string.IsNullOrEmpty(itemId) && playerIds.Contains(itemId))
                {
                    continue;
                }
                
                // Skip if it's an exit
                if (!string.IsNullOrEmpty(itemClassId) && itemClassId == exitClassId)
                {
                    continue;
                }
                
                // Add all other objects (items, etc.)
                objects.Add(item);
            }
            catch (Exception ex)
            {
                // If we can't check, try to add it anyway (might be an object)
                try
                {
                    if (item != null)
                    {
                        objects.Add(item);
                    }
                }
                catch
                {
                    // Skip if we can't add it
                }
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
                    catch (Exception ex)
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
        // Get all objects in location (includes players, items, exits)
        var allObjectsDynamic = Builtins.GetObjectsInLocation(This.Id);
        var objects = new List<GameObject>();
        foreach (var obj in allObjectsDynamic)
        {
            // The dynamic objects are actually GameObjects, try to cast directly
            try
            {
                GameObject? gameObject = obj as GameObject;
                if (gameObject == null && obj != null)
                {
                    // Try accessing Id property to verify it's a GameObject
                    var id = obj.Id;
                    if (id != null)
                    {
                        gameObject = Builtins.FindObject(id.ToString()) as GameObject;
                    }
                }
                if (gameObject != null)
                {
                    objects.Add(gameObject);
                }
            }
            catch
            {
                // Skip if conversion fails
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
            // The dynamic objects are actually GameObjects, try to cast directly
            try
            {
                GameObject? gameObject = player as GameObject;
                if (gameObject == null && player != null)
                {
                    // Try accessing Id property to verify it's a GameObject
                    var id = player.Id;
                    if (id != null)
                    {
                        gameObject = Builtins.FindObject(id.ToString()) as GameObject;
                    }
                }
                if (gameObject != null)
                {
                    players.Add(gameObject);
                }
            }
            catch
            {
                // Skip if conversion fails
            }
        }
        return players;
    }
}
