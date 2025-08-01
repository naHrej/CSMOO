StringBuilder desc = new StringBuilder("");
try
{
    desc.Append($"<section class='player' style='color:green'>");
    desc.Append($"<h3 class='name'>{This.Name}");
    desc.Append($"<span class='dbref' style='color:chartreuse'> ({This.ClassId})</span>");
    desc.Append($"</h3>");
    desc.Append($"<p class='description' style='color:#66FF66;'>");
    desc.Append(This.longDescription ?? This.shortDescription ?? "You see nothing special.");
    desc.Append($"</p>");    
}
catch(Exception ex)
{}
return desc.ToString();