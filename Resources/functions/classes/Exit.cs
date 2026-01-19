public class Exit
{
    /// <summary>
    /// Returns a formatted description of any exit
    /// </summary>
    public string Description()
    {
        var desc = new StringBuilder();
        desc.Append($"<section class='exit' style='margin:0'>");
        
        // Exit direction
        var direction = Builtins.GetProperty(This, "direction", "");
        var exitName = !string.IsNullOrEmpty(direction) ? direction : Builtins.GetProperty(This, "name", "") ?? "an exit";
        if (!string.IsNullOrEmpty(direction))
        {
            desc.Append($"<h3 style='color:yellow;margin:0;font-weight:bold'>Exit: {direction}</h3>");
        }
        else
        {
            var name = Builtins.GetProperty(This, "name", "") ?? "an exit";
            desc.Append($"<h3 style='color:yellow;margin:0;font-weight:bold'>{name}</h3>");
        }
        
        // Exit description (what the exit looks like)
        var exitDescription = Builtins.GetProperty(This, "description", "");
        if (!string.IsNullOrEmpty(exitDescription))
        {
            desc.Append($"<p style='margin:0.5em 0'>{exitDescription}</p>");
        }
        
        // Check if exit is closed
        var isClosed = Builtins.GetBoolProperty(This, "closed", false);
        
        if (isClosed)
        {
            // Exit is closed
            desc.Append($"<p style='margin:0.5em 0;color:red'>The {exitName} exit is closed.</p>");
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
                        desc.Append($"<p style='margin:0.5em 0'>{roomDescription}</p>");
                    }
                    catch
                    {
                        // If Description() fails, show basic info
                        var roomName = Builtins.GetProperty(destinationRoom, "name", "") ?? destinationId;
                        desc.Append($"<p style='margin:0.5em 0'>This exit leads to: <span class='destination' style='color:lightblue'>{roomName}</span></p>");
                    }
                }
            }
        }
        
        desc.Append("</section>");
        return desc.ToString();
    }
}
