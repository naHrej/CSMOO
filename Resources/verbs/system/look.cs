// Look command - shows room or looks at specific object.
// This is a test
string target = "";
if (Args.Count == 0)
{
    target = "here";
}
else if (Args.Count >= 2 && Args[0].ToLower() == "at")
{
    // 'look at something'
    target = string.Join(" ", Args.Skip(1));
}
else
{
    // 'look something'
    target = string.Join(" ", Args);
}
var resolved = (dynamic)(ObjectResolver.ResolveObject(target, Player));
if (resolved == null)
{
    notify(player, $"You don't see '{target}' here.");
    return false;
}
notify(player, resolved.Description() ?? $"<h3>{resolved.Name}</h3><p>You see nothing special about this {resolved.ClassId}.</p>");
return true;
