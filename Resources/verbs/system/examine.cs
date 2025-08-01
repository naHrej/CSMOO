var targetName = Args.FirstOrDefault() ?? "here";

// First, try to resolve as a class name
var classTarget = Builtins.GetClassByName(targetName);
var isExaminingClass = classTarget != null;
var target = (GameObject)null;
var targetPlayer = (Player)null;
var targetId = "";
var objectClass = (ObjectClass)null;

if (isExaminingClass)
{
    // Examining a class definition
    objectClass = classTarget;
    targetId = classTarget.Id;
    notify(Player,$"<hr/><p>Examining class '{classTarget.Name}' ({classTarget.Id})</p>");
}
else
{
    // Try to resolve as an object instance
    target = ObjectResolver.ResolveObject(targetName, Player);
    if (target is null)
    {
        notify(Player, $"You can't see '{targetName}' here.");
        return true;
    }
    
    targetId = target.Id;
    objectClass = Builtins.GetObjectClass(target);
    notify(Player,$"<hr/><p>Examining {target.Properties["name"]} '{targetName}' ({target.Id})</p>");
    
    // Check if target is a player (only for object instances)
    if (Builtins.IsPlayerObject(target))
    {
        targetPlayer = (Player)target;
    }
}

// Get basic properties using different approaches for classes vs instances
string name, shortDesc, longDesc;

if (isExaminingClass)
{
    // For class definitions, use class properties
    name = objectClass.Name;
    shortDesc = objectClass.Description;
    longDesc = objectClass.Description;
}
else
{
    // For object instances, use dynamic access
    name = target.Name ?? Builtins.GetObjectName(target);
    shortDesc = target.shortDescription ?? "";
    longDesc = target.longDescription ?? target.description ?? "";
}

// Build the examination output
var output = new StringBuilder();

// Object name and short description
if (!string.IsNullOrEmpty(shortDesc))
{
    output.AppendLine($"{name} ({shortDesc})");
}
else
{
    output.AppendLine(name);
}

// Long description
if (!string.IsNullOrEmpty(longDesc))
{
    output.AppendLine(longDesc);
}
else
{
    output.AppendLine("You see nothing special.");
}

// Show contents if it's a container (only for object instances)
if (!isExaminingClass)
{
    var contents = Helpers.GetObjectsInLocation(targetId);
    if (contents.Any())
    {
        output.AppendLine();
        output.AppendLine("Contents:");
        foreach (var item in contents)
        {
            var itemName = item.Name ?? "unknown object";
            var itemShort = item.shortDescription ?? "";
            var displayName = !string.IsNullOrEmpty(itemShort) ? $"{itemName} ({itemShort})" : itemName;
            output.AppendLine($"  {displayName}");
        }
    }
}

// For class definitions, show default properties and instances
if (isExaminingClass)
{
    output.AppendLine();
    output.AppendLine("=== Class Information ===");
    
    // Show inheritance
    if (!string.IsNullOrEmpty(objectClass.ParentClassId))
    {
        var parentClass = Builtins.GetClass(objectClass.ParentClassId);
        output.AppendLine($"Inherits from: {parentClass?.Name ?? objectClass.ParentClassId}");
    }
    else
    {
        output.AppendLine("Inherits from: (none - root class)");
    }
    
    // Show if it's abstract
    if (objectClass.IsAbstract)
    {
        output.AppendLine("Type: Abstract class (cannot be instantiated)");
    }
    else
    {
        output.AppendLine("Type: Concrete class");
    }
    
    // Show default properties
    if (objectClass.Properties?.Any() == true)
    {
        output.AppendLine();
        output.AppendLine("Default Properties:");
        foreach (var prop in objectClass.Properties)
        {
            var value = prop.Value?.ToString() ?? "null";
            if (value.Length > 50)
            {
                value = value.Substring(0, 47) + "...";
            }
            output.AppendLine($"  {prop.Key}: {value}");
        }
    }
    
    // Show instances of this class
    var instances = Builtins.GetObjectsByClass(objectClass.Id);
    output.AppendLine();
    output.AppendLine($"Instances: {instances.Count()}");
    if (instances.Any() && (Builtins.IsAdmin(Player) || Builtins.IsModerator(Player)))
    {
        var limitedInstances = instances.Take(10);
        foreach (var instance in limitedInstances)
        {
            var instName = instance.Name ?? "unnamed";
            output.AppendLine($"  #{instance.DbRef}: {instName}");
        }
        if (instances.Count() > 10)
        {
            output.AppendLine($"  ... and {instances.Count() - 10} more");
        }
    }
}

// Administrative information for Admin/Moderator users
if (Builtins.IsAdmin(Player) || Builtins.IsModerator(Player))
{
    output.AppendLine();
    output.AppendLine("=== Administrative Information ===");
    
    if (isExaminingClass)
    {
        // Administrative info for class definitions
        output.AppendLine($"Class ID: {objectClass.Id}");
        output.AppendLine($"Class Name: {objectClass.Name}");
        output.AppendLine($"Created: {objectClass.CreatedAt}");
        output.AppendLine($"Modified: {objectClass.ModifiedAt}");
    }
    else
    {
        // Administrative info for object instances
        output.AppendLine($"Object ID: {target.Id}");
        output.AppendLine($"DB Reference: #{target.DbRef}");
        output.AppendLine($"Created: {target.CreatedAt}");
        output.AppendLine($"Modified: {target.ModifiedAt}");

        // Show player flags if examining a player
        if (targetPlayer != null)
        {
            var flags = Builtins.GetPlayerFlags(targetPlayer);
            if (flags.Any())
            {
                output.AppendLine($"Player Flags: {string.Join(", ", flags)}");
            }
            else
            {
                output.AppendLine("Player Flags: none");
            }
        }
    }

    // Show object class for both cases
    if (objectClass != null)
    {
        output.AppendLine($"Class: {objectClass.Name}");
    }

    // Show verbs (different approach for classes vs instances)
    if (isExaminingClass)
    {
        var verbs = Builtins.GetVerbsOnClass(objectClass.Id);
        if (verbs.Any())
        {
            output.AppendLine("Class Verbs:");
            foreach (var verb in verbs.OrderBy(v => v.Name))
            {
                var verbInfo = $"  {verb.Name}";
                if (!string.IsNullOrEmpty(verb.Aliases))
                {
                    verbInfo += $" ({verb.Aliases})";
                }
                if (!string.IsNullOrEmpty(verb.Pattern))
                {
                    verbInfo += $" [{verb.Pattern}]";
                }
                output.AppendLine(verbInfo);
            }
        }
        else
        {
            output.AppendLine("Class Verbs: none");
        }
    }
    else
    {
        var verbs = Builtins.GetVerbsOnObject(targetId);
        if (verbs.Any())
        {
            output.AppendLine("Verbs:");
            foreach (var verb in verbs.OrderBy(v => v.Name))
            {
                var verbInfo = $"  {verb.Name}";
                if (!string.IsNullOrEmpty(verb.Aliases))
                {
                    verbInfo += $" ({verb.Aliases})";
                }
                if (!string.IsNullOrEmpty(verb.Pattern))
                {
                    verbInfo += $" [{verb.Pattern}]";
                }
                output.AppendLine(verbInfo);
            }
        }
        else
        {
            output.AppendLine("Verbs: none");
        }
    }

    // Show functions (different approach for classes vs instances)
    if (isExaminingClass)
    {
        var functions = Builtins.GetFunctionsOnClass(objectClass.Id);
        if (functions.Any())
        {
            output.AppendLine("Class Functions:");
            foreach (var func in functions.OrderBy(f => f.Name))
            {
                var paramString = string.Join(", ", func.ParameterTypes.Zip(func.ParameterNames, (type, name) => $"{type} {name}"));
                var funcInfo = $"  {func.ReturnType} {func.Name}({paramString})";
                if (!string.IsNullOrEmpty(func.Description))
                {
                    funcInfo += $" - {func.Description}";
                }
                output.AppendLine(funcInfo);
            }
        }
        else
        {
            output.AppendLine("Class Functions: none");
        }
    }
    else
    {
        var functions = Builtins.GetFunctionsOnObject(targetId);
        if (functions.Any())
        {
            output.AppendLine("Functions:");
            foreach (var func in functions.OrderBy(f => f.Name))
            {
                var paramString = string.Join(", ", func.ParameterTypes.Zip(func.ParameterNames, (type, name) => $"{type} {name}"));
                var funcInfo = $"  {func.ReturnType} {func.Name}({paramString})";
                if (!string.IsNullOrEmpty(func.Description))
                {
                    funcInfo += $" - {func.Description}";
                }
                output.AppendLine(funcInfo);
            }
        }
        else
        {
            output.AppendLine("Functions: none");
        }
    }

    // Show properties (only for object instances, classes already show default properties above)
    if (!isExaminingClass)
    {
        if (target.Properties?.Any() == true)
        {
            output.AppendLine("Instance Properties:");
            // Convert to list to avoid dynamic lambda dispatch issue
            var propertiesList = new List<KeyValuePair<string, object>>();
            foreach (var prop in target.Properties)
            {
                propertiesList.Add(new KeyValuePair<string, object>(prop.Key, prop.Value));
            }
            // Now we can safely use OrderBy on the concrete list
            foreach (var prop in propertiesList.OrderBy(p => p.Key))
            {
                var value = prop.Value?.ToString() ?? "null";
                if (value.Length > 50)
                {
                    value = value.Substring(0, 47) + "...";
                }
                output.AppendLine($"  {prop.Key}: {value}");
            }
        }
        else
        {
            output.AppendLine("Instance Properties: none");
        }
    }
}

notify(Player, output.ToString().TrimEnd());
