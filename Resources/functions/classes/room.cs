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
        desc.Append($"<h3 style='color:dodgerblue;margin:0;font-weight:bold'>{This.Name}</h3>");
        
        var roomDescription = Builtins.GetProperty(This, "description");
        if (!string.IsNullOrEmpty(roomDescription))
        {
            desc.Append($"<p style='margin:0.5em 0'>{roomDescription}</p>");
        }
        
        // Show exits
        var exits = Exits();
        if (exits.Count > 0)
        {
            var exitNames = new List<string>();
            foreach (var exit in exits)
            {
                var dir = Builtins.GetProperty(exit, "direction");
                if (dir != null)
                {
                    exitNames.Add($"<span class='exit' style='color:yellow'>{dir}</span>");
                }
            }
            if (exitNames.Count > 0)
            {
                desc.Append($"<p style='margin:0.5em 0'>Exits: {string.Join(", ", exitNames)}</p>");
            }
        }
        
        // Show contents (objects and players)
        var contents = Contents();
        var players = Players();
        
        if (contents.Count > 0)
        {
            var itemNames = new List<string>();
            foreach (var item in contents)
            {
                itemNames.Add($"<span class='object' style='color:lightgreen'>{item.Name}</span>");
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
                playerNames.Add($"<span class='player' style='color:lightblue'>{player.Name}</span>");
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
