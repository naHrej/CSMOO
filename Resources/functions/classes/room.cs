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
        
        // Show exits
        try
        {
            var exits = This.Exits();
            if (exits != null && exits.Count > 0)
            {
                var exitNames = new List<string>();
                foreach (var exit in exits)
                {
                    try
                    {
                        var dir = Builtins.GetProperty(exit, "direction", "");
                        if (!string.IsNullOrEmpty(dir))
                        {
                            exitNames.Add($"<span class='Exit'>{dir}</span>");
                        }
                    }
                    catch
                    {
                        // Skip this exit if we can't get its direction
                    }
                }
                if (exitNames.Count > 0)
                {
                    desc.Append($"<p class='Exits'>Exits: {string.Join(", ", exitNames)}</p>");
                }
            }
        }
        catch
        {
            // If Exits() fails, just skip showing exits
        }
        
        // Show contents (objects and players)
        List<GameObject> contents = new List<GameObject>();
        List<GameObject> players = new List<GameObject>();
        try
        {
            contents = This.Contents();
        }
        catch
        {
            // If Contents() fails, use empty list
        }
        try
        {
            players = This.Players();
        }
        catch
        {
            // If Players() fails, use empty list
        }
        
        if (contents.Count > 0)
        {
            var itemNames = new List<string>();
            foreach (var item in contents)
            {
                var itemName = Builtins.GetProperty(item, "name", "") ?? "something";
                itemNames.Add($"<span class='Object'>{itemName}</span>");
            }
            if (itemNames.Count > 0)
            {
                desc.Append($"<p class='Contents'>You see: {string.Join(", ", itemNames)}</p>");
            }
        }
        
        if (players.Count > 0)
        {
            var playerNames = new List<string>();
            foreach (var player in players)
            {
                var playerName = Builtins.GetProperty(player, "name", "") ?? "someone";
                playerNames.Add($"<span class='Player'>{playerName}</span>");
            }
            if (playerNames.Count > 0)
            {
                desc.Append($"<p class='Players'>Players here: {string.Join(", ", playerNames)}</p>");
            }
        }
        
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
        return Builtins.GetObjectsInRoom(This).Cast<GameObject>().ToList();
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
        return Builtins.GetExits(This).Cast<GameObject>().ToList();
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
        return Builtins.GetPlayersInRoom(This).Cast<GameObject>().ToList();
    }
}
