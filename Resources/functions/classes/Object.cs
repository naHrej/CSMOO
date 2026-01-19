public class Object
{
    /// <summary>
    /// Returns a formatted description of any object
    /// </summary>
    public string Description()
    {
        var desc = new StringBuilder();
        desc.Append($"<section class='object' style='margin:0'>");
        
        // Object name
        var objectName = Builtins.GetProperty(This, "name", "") ?? "something";
        desc.Append($"<h3 style='color:lightgreen;margin:0;font-weight:bold'>{objectName}</h3>");
        
        // Object description
        var objectDescription = Builtins.GetProperty(This, "description", "");
        if (!string.IsNullOrEmpty(objectDescription))
        {
            desc.Append($"<p style='margin:0.5em 0'>{objectDescription}</p>");
        }
        else
        {
            desc.Append($"<p style='margin:0.5em 0'>You see nothing special about the {objectName}.</p>");
        }
        
        // Show object class info
        var classId = Builtins.GetProperty(This, "classId", "") ?? "unknown";
        desc.Append($"<p style='margin:0.5em 0;color:gray;font-size:0.9em'>Type: {classId}</p>");
        
        desc.Append("</section>");
        return desc.ToString();
    }
}
