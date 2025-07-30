using System.Text;
var desc = new System.Text.StringBuilder();
desc.Append($"<section class='room'>");
desc.Append($"<h3 class='name'>{this.Name}<span class='dbref'>(#{this.DbRef})</span></h3>");
desc.Append($"<p class='description'>{this.longDescription ?? this.shortDescription ?? "You see nothing special."}</p>");
if (this.Exits()?.Count >0)
{
  desc.append("<h4 class='header'>Exits</h4>");
  foreach (var exit in this.Exits())
  {
    desc.Append($"<div class='exit'>{exit.Name}</div>");
  }
}
return desc;
