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
        var direction = Builtins.GetProperty(This, "direction");
        if (!string.IsNullOrEmpty(direction))
        {
            desc.Append($"<h3 style='color:yellow;margin:0;font-weight:bold'>Exit: {direction}</h3>");
        }
        else
        {
            desc.Append($"<h3 style='color:yellow;margin:0;font-weight:bold'>{This.Name}</h3>");
        }
        
        // Exit description
        var exitDescription = Builtins.GetProperty(This, "description");
        if (!string.IsNullOrEmpty(exitDescription))
        {
            desc.Append($"<p style='margin:0.5em 0'>{exitDescription}</p>");
        }
        
        // Show destination if visible
        var destination = Builtins.GetProperty(This, "destination");
        if (destination != null)
        {
            desc.Append($"<p style='margin:0.5em 0'>This exit leads to: <span class='destination' style='color:lightblue'>{destination.Name}</span></p>");
        }
        
        desc.Append("</section>");
        return desc.ToString();
    }
}
