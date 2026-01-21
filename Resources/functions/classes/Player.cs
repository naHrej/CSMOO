public class Player
{
    /// <summary>
    /// Returns a formatted description of any player
    /// </summary>
    public string Description()
    {
        var desc = new StringBuilder();
        desc.Append($"<section class='Player'>");
        
        // Player name
        var playerName = Builtins.GetProperty(This, "name", "") ?? "someone";
        desc.Append($"<h3 class='Name'>{playerName}</h3>");
        
        // Player description
        var playerDescription = Builtins.GetProperty(This, "description", "");
        if (!string.IsNullOrEmpty(playerDescription))
        {
            desc.Append($"<p class='Description'>{playerDescription}</p>");
        }
        else
        {
            desc.Append($"<p class='Description'>You see nothing special about {playerName}.</p>");
        }
        
        // Show what they're carrying (if visible)
        var inventory = Builtins.GetInventory(This);
        if (inventory.Count > 0)
        {
            var itemNames = new List<string>();
            foreach (var item in inventory)
            {
                var itemName = Builtins.GetProperty(item, "name", "") ?? "something";
                itemNames.Add($"<span class='Object'>{itemName}</span>");
            }
            if (itemNames.Count > 0)
            {
                desc.Append($"<p class='Inventory'>{playerName} is carrying: {string.Join(", ", itemNames)}</p>");
            }
        }
        
        desc.Append("</section>");
        return desc.ToString();
    }
}
