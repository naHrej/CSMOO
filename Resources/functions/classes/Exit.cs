public class Exit
{
    /// <summary>
    /// Returns a formatted description of any exit
    /// </summary>
    public string Description()
    {
        if (This is null)
        {
            return "";
        }
        
        var desc = new StringBuilder();
        desc.Append($"<section class='Exit'>");
        
        // Exit direction
        var direction = Builtins.GetProperty(This, "direction", "");
        var exitName = !string.IsNullOrEmpty(direction) ? direction : Builtins.GetProperty(This, "name", "") ?? "an exit";
        if (!string.IsNullOrEmpty(direction))
        {
            desc.Append($"<h3 class='Name'>Exit: {direction}</h3>");
        }
        else
        {
            var name = Builtins.GetProperty(This, "name", "") ?? "an exit";
            desc.Append($"<h3 class='Name'>{name}</h3>");
        }
        
        // Exit description (what the exit looks like)
        var exitDescription = Builtins.GetProperty(This, "description", "");
        if (!string.IsNullOrEmpty(exitDescription))
        {
            desc.Append($"<p class='Description'>{exitDescription}</p>");
        }
        
        // Check if exit is closed
        var isClosed = Builtins.GetBoolProperty(This, "closed", false);
        
        if (isClosed)
        {
            // Exit is closed
            desc.Append($"<p class='Closed'>The {exitName} exit is closed.</p>");
        }
        else
        {
            // Exit is open - show destination room description
            var destinationId = Builtins.GetProperty(This, "destination", "");
            if (!string.IsNullOrEmpty(destinationId))
            {
                var destinationRoom = Builtins.FindObject(destinationId);
                if (destinationRoom != null)
                {
                    try
                    {
                        var roomDescription = destinationRoom.Description();
                        desc.Append($"<p class='Description'>{roomDescription}</p>");
                    }
                    catch
                    {
                        // If Description() fails, show basic info
                        var roomName = Builtins.GetProperty(destinationRoom, "name", "") ?? destinationId;
                        desc.Append($"<p class='Description'>This exit leads to: <span class='Destination'>{roomName}</span></p>");
                    }
                }
            }
        }
        
        desc.Append("</section>");
        return desc.ToString();
    }
}
