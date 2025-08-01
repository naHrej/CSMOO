// Generic description function for all objects
var objectName = This.Name ?? "an object";
var shortDesc = This.shortDescription ?? objectName;
var longDesc = This.longDescription ?? This.description ?? "You see nothing special about it.";

// Check if this is a Room (for special formatting)
if (This.ClassId == "Room" || (This.Properties.ContainsKey("isRoom") && This.Properties["isRoom"]))
{
    return $"=== {objectName.ToUpper()} ===\n{longDesc}\n=== END ===";
}

// For players and other living beings
if (This.ClassId == "Player" || This.ClassId?.StartsWith("Player") == true)
{
    var level = This.Properties.ContainsKey("level") ? $" (Level {This.Properties["level"]})" : "";
    return $"{objectName}{level}\n{longDesc}";
}

// For containers
if (This.ClassId == "Container" || (This.Properties.ContainsKey("capacity") && This.Properties["capacity"] > 0))
{
    var status = "";
    if (This.Properties.ContainsKey("closed") && This.Properties["closed"])
    {
        status = This.Properties.ContainsKey("locked") && This.Properties["locked"] ? " (closed and locked)" : " (closed)";
    }
    else if (This.Properties.ContainsKey("closed"))
    {
        status = " (open)";
    }
    return $"{objectName}{status}\n{longDesc}";
}

// For exits
if (This.ClassId == "Exit")
{
    var direction = This.Properties.ContainsKey("direction") ? $" leading {This.Properties["direction"]}" : "";
    return $"{objectName}{direction}\n{longDesc}";
}

// Default for all other objects (items, etc.)
var weight = This.Properties.ContainsKey("weight") ? $" (Weight: {This.Properties["weight"]})" : "";
var value = This.Properties.ContainsKey("value") && This.Properties["value"] > 0 ? $" (Value: {This.Properties["value"]} coins)" : "";
return $"{objectName}{weight}{value}\n{longDesc}";
