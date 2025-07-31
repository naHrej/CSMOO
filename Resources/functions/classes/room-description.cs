StringBuilder desc = new StringBuilder("");
try
{
    desc.Append($"<section class='room' style='color:red'>");
    desc.Append($"<h3 class='name'>{This.Name}");
    desc.Append($"<span class='dbref' style='color:maroon'> ({This.ClassId.Replace("obj_","")})</span>");
    desc.Append($"</h3>");
    desc.Append($"<p class='description' style='color:#FF6666;'>");
    desc.Append(This.longDescription ?? This.shortDescription ?? "You see nothing special.");
    desc.Append($"</p>");

    dynamic Room = This;
    if (Room.Exits().Count > 0)
    {
        desc.Append("<div class='header'>Exits:</div>");
        desc.Append("<ul>");
        foreach (var exit in Room.Exits())
        {
            desc.Append($"<li>{exit.Name}</li>");
        }
        desc.Append("</ul>");
    }
}
catch(Exception ex)
{}
return desc.ToString();
