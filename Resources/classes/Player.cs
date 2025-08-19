public class Player
{
    /// <summary>
    /// Returns a formatted description of any player
    /// </summary>
    public string Description()
    {
        var desc = new StringBuilder();
        desc.Append($"<section class='player' style='margin:0'>");
        
        // Player name
        desc.Append($"<h3 style='color:lightblue;margin:0;font-weight:bold'>{This.Name}</h3>");
        
        // Player description
        var playerDescription = Builtins.GetProperty(This, "description");
        if (!string.IsNullOrEmpty(playerDescription))
        {
            desc.Append($"<p style='margin:0.5em 0'>{playerDescription}</p>");
        }
        else
        {
            desc.Append($"<p style='margin:0.5em 0'>You see nothing special about {This.Name}.</p>");
        }
        
        // Show what they're carrying (if visible)
        var inventory = Builtins.GetInventory(This);
        if (inventory.Count > 0)
        {
            var itemNames = new List<string>();
            foreach (var item in inventory)
            {
                itemNames.Add($"<span class='object' style='color:lightgreen'>{item.Name}</span>");
            }
            if (itemNames.Count > 0)
            {
                desc.Append($"<p style='margin:0.5em 0'>{This.Name} is carrying: {string.Join(", ", itemNames)}</p>");
            }
        }
        
        desc.Append("</section>");
        return desc.ToString();
    }
}
