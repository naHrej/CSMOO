public class Room
{
    /// <summary>
    /// Returns a formatted description of any Room
    /// </summary>
    public string Description()
    {
        var desc = new StringBuilder();
        desc.Append($"<section class='room' style='color:red;margin:0'>");
        
        // Room name and description
        var roomName = Builtins.GetProperty(This, "name", "") ?? "a room";
        desc.Append($"<h3 style='color:dodgerblue;margin:0;font-weight:bold'>{roomName}</h3>");
        
        var roomDescription = Builtins.GetProperty(This, "longDescription", "")
        ?? Builtins.GetProperty(This, "Description", "") ??
         "You see nothing special.";
        if (!string.IsNullOrEmpty(roomDescription))
        {
            desc.Append($"<p style='margin:0.5em 0'>{roomDescription}</p>");
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
                            exitNames.Add($"<span class='exit' style='color:yellow'>{dir}</span>");
                        }
                    }
                    catch
                    {
                        // Skip this exit if we can't get its direction
                    }
                }
                if (exitNames.Count > 0)
                {
                    desc.Append($"<p style='margin:0.5em 0'>Exits: {string.Join(", ", exitNames)}</p>");
                }
            }
        }
        catch
        {
            // If Exits() fails, just skip showing exits
        }
        
        // Show contents (objects and players)
        List<dynamic> contents = new List<dynamic>();
        List<dynamic> players = new List<dynamic>();
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
                itemNames.Add($"<span class='object' style='color:lightgreen'>{itemName}</span>");
            }
            if (itemNames.Count > 0)
            {
                desc.Append($"<p style='margin:0.5em 0'>You see: {string.Join(", ", itemNames)}</p>");
            }
        }
        
        if (players.Count > 0)
        {
            var playerNames = new List<string>();
            foreach (var player in players)
            {
                var playerName = Builtins.GetProperty(player, "name", "") ?? "someone";
                playerNames.Add($"<span class='player' style='color:lightblue'>{playerName}</span>");
            }
            if (playerNames.Count > 0)
            {
                desc.Append($"<p style='margin:0.5em 0'>Players here: {string.Join(", ", playerNames)}</p>");
            }
        }
        
        desc.Append("</section>");
        return desc.ToString();
    }

    /// <summary>
    /// Returns a list of Contents in the room
    /// </summary>
    public List<dynamic> Contents()
    {
        return Builtins.GetObjectsInRoom(This);
    }

    /// <summary>
    /// Returns a list of Exits from the room
    /// </summary>
    public List<dynamic> Exits()
    {
        return Builtins.GetExits(This);
    }

    /// <summary>
    /// Returns a list of Players in the room
    /// </summary>
    public List<dynamic> Players()
    {
        return Builtins.GetPlayersInRoom(This);
    }
}
