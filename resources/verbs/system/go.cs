// Smart go command - move in any available direction
var currentLocation = Player.Location;
if (currentLocation is null)
{
    notify(Player, "<p class='error' style='color:red'>You are not in any location.</p>");
    return;
}
var exits = Builtins.GetExits(Location);
var exitNames = new List<string>();
foreach (var exit in exits)
{
    var dir = Builtins.GetProperty(exit, "direction");
    if (dir != null)
    {
        exitNames.Add(dir);
    }
}
var availableExits = $"Available exits: <span class='param' style='color:yellow'>{string.Join(", ", exitNames)}</span>";
if (Args.Count == 0)
{
    // Show available exits if no direction given
    if (exits.Count == 0)
    {
        notify(Player, "<p class='error' style='color:red'>There are no exits from here.</p>");
    }
    else
    {
        notify(Player, availableExits);
        notify(Player, "<p class='usage' style='color:green'>Usage: <span class='command' style='color:yellow'>go <span class='param' style='color:gray'>&lt;direction&gt;</span></span></p>");
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
        notify(Player, $"<p class='error' style='color:red'>There is no exit '<span class='param' style='color:yellow'>{chosenDirection}</span>'. {availableExits}</p>");
    }
    else
    {
        notify(Player, "<p class='error' style='color:red'>There are no exits from here.</p>");
    }
    return;
}
dynamic destination = chosenExit.destination;

if (destination == null)
{
    notify(Player, "<p class='error' style='color:red'>That exit doesn't lead anywhere.</p>");
    return;
}
// Move the player
if (Builtins.MoveObject(Player, destination))
{
    notify(Player, $"<p class='success' style='color:dodgerblue'>You go <span class='param' style='color:yellow'>{chosenDirection}</span>.</p>");
    notify(Player, destination.Description());
}
else
{
    notify(Player, "<p class='error' style='color:red'>You can't go that way.</p>");
}
