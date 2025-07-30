// Look command - shows room or looks at specific object.
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
GameObject resolved = ObjectResolver.ResolveObject(target, Player);
if (resolved == null)
{
    notify(player, $"You don't see '{target}' here.");
    return false;
}

var name = resolved.Name;
if(string.IsNullOrEmpty(name))
{
    name = resolved.Properties["name"];
}
var desc = resolved.Description();
notify(player, desc);
return true;
