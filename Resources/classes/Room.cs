public class Room
{
    /// <summary>
    /// Returns a formatted description of any Room
    /// </summary>
    public string Description()
    {
        StringBuilder desc = new StringBuilder("");
        try
        {
            desc.Append($"<section class='room' style='color:red;margin:0'>");
            desc.Append($"<h3 class='name' style='margin:0'>{This.Name}");
            desc.Append($"<span class='dbref' style='color:maroon'> ({This.ClassId})</span>");
            desc.Append($"</h3>");
            desc.Append($"<p class='description' style='color:#FF6666;margin:0'>");
            desc.Append(This.longDescription ?? This.shortDescription ?? "You see nothing special.");
            desc.Append($"</p>");
            var Room = This;
            if (Room.Exits().Count > 0)
            {
                desc.Append("<div class='header'>Exits:</div>");
                desc.Append("<ul style='margin:0'>");
                foreach (var exit in Room.Exits())
                {
                    desc.Append($"<li style='color:yellow'>{exit.Name}</li>");
                }
                desc.Append("</ul>");
            }
            if (Room.Players().Count > 0)
            {
                desc.Append("<div class='header'>Players:</div>");
                desc.Append("<ul style='margin:0'>");
                foreach (var plyr in Room.Players())
                {
                    desc.Append($"<li style='color:yellow'>{plyr.Name}</li>");
                }
                desc.Append("</ul>");
            }
            if (Room.Contents().Count > 0)
            {
                desc.Append("<div class='header'>Contents:</div>");
                desc.Append("<ul style='margin:0'>");
                foreach (var item in Room.Contents())
                {
                    desc.Append($"<li style='color:yellow'>{item.Name}</li>");
                }
                desc.Append("</ul>");
            }
        }
        catch (Exception ex)
        {

        }
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
