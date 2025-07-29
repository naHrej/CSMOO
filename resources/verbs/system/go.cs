// Smart go command - move in any available direction
var currentLocation = Player.Location;
if (currentLocation is null)
{
notify(Player, "<p class='error'>You are not in any location.</p>");
return;
}
var exits = Builtins.GetExitsFromRoom(currentLocation);
var exitNames = new List<string>();
foreach (var exit in exits)
{
var dir = Builtins.GetProperty(exit, "direction");
if (dir != null)
{
exitNames.Add(dir);
}
}
var availableExits = $"Available exits: {string.Join(", ", exitNames)}</p>";
if (Args.Count == 0)
{
// Show available exits if no direction given
if (exits.Count == 0)
{
notify(Player, "<p class='error'>There are no exits from here.</p>");
}
else
{
notify(Player, availableExits);
notify(Player, "Usage: go &lt;direction&gt;");
}
return;
}
var chosenDirection = String.Join(" ",Args).ToLowerInvariant();
dynamic chosenExit = null;
foreach (var exit in exits)
{
var exitDirection = Builtins.GetProperty(exit, "direction")?.ToString().ToLowerInvariant();
if (exitDirection == chosenDirection || exitDirection == $"\"{chosenDirection}\"") // for some reason, property values are coming back quoted
{
chosenExit = exit as dynamic;
break;
}
}
if (chosenExit == null)
{
// Show available exits
if (exitNames.Count > 0)
{
notify(Player, $"<p class='error'>There is no exit '{chosenDirection}'. {availableExits}</p>");
}
else
{
notify(Player, "<p class='error'>There are no exits from here.</p>");
}
return;
}
dynamic destination = chosenExit.destination;
notify(player, destination.Name);
if (destination == null)
{
notify(Player, "<p class='error'>That exit doesn't lead anywhere.</p>");
return;
}
// Move the player
if (Builtins.MoveObject(Player, destination))
{
notify(Player, $"<p class='success'>You go <span class='param'>{chosenDirection}</span>.</p>");
    notify(Player, destination.longDescription);
}
else
{
notify(Player, "<p class='error'>You can't go that way.</p>");
}
