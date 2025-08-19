public class GameObject
{
    /// <summary>
    /// Returns a formatted description of any object
    /// </summary>
    public string Description()
    {
        var desc = new StringBuilder();
        desc.Append($"<section class='object' style='margin:0'>");
        
        // Object name
        desc.Append($"<h3 style='color:lightgreen;margin:0;font-weight:bold'>{This.Name}</h3>");
        
        // Object description
        var objectDescription = Builtins.GetProperty(This, "description");
        if (!string.IsNullOrEmpty(objectDescription))
        {
            desc.Append($"<p style='margin:0.5em 0'>{objectDescription}</p>");
        }
        else
        {
            desc.Append($"<p style='margin:0.5em 0'>You see nothing special about the {This.Name}.</p>");
        }
        
        // Show object class info
        desc.Append($"<p style='margin:0.5em 0;color:gray;font-size:0.9em'>Type: {This.ClassId}</p>");
        
        desc.Append("</section>");
        return desc.ToString();
    }
}
