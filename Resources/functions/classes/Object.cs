public class Object
{
    /// <summary>
    /// Returns a formatted description of any object
    /// </summary>
    public string Description()
    {
        var desc = new StringBuilder();
        desc.Append($"<section class='Object'>");
        
        // Object name
        var objectName = Builtins.GetProperty(This, "name", "") ?? "something";
        desc.Append($"<h3 class='Name'>{objectName}</h3>");
        
        // Object description
        var objectDescription = Builtins.GetProperty(This, "description", "");
        if (!string.IsNullOrEmpty(objectDescription))
        {
            desc.Append($"<p class='Description'>{objectDescription}</p>");
        }
        else
        {
            desc.Append($"<p class='Description'>You see nothing special about the {objectName}.</p>");
        }
        
        // Show object class info
        var classId = Builtins.GetProperty(This, "classId", "") ?? "unknown";
        desc.Append($"<p class='Type'>Type: {classId}</p>");
        
        desc.Append("</section>");
        return desc.ToString();
    }
}
