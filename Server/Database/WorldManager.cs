using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using CSMOO.Server.Scripting;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Database;

/// <summary>
/// Manages world creation and basic game world setup
/// </summary>
public static class WorldManager
{
    /// <summary>
    /// Initializes the basic world structure with core classes
    /// </summary>
    public static void InitializeWorld()
    {
        CreateCoreClasses();
        CreateDefaultVerbs();
        CreateSystemCommands(); // Add system commands as verbs
        MigrateVerbSyntax();  // Fix any existing verbs with broken syntax
        CreateStartingRoom();
    }

    /// <summary>
    /// Creates the fundamental object classes that everything inherits from
    /// </summary>
    private static void CreateCoreClasses()
    {
        // Check if core classes already exist
        if (GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Object") != null)
            return;

        // Base Object class - everything inherits from this
        var baseObjectClass = new ObjectClass
        {
            Id = "obj_base_object",
            Name = "Object",
            Description = "The fundamental base class for all objects in the game",
            Properties = new BsonDocument
            {
                ["name"] = "an object",
                ["shortDescription"] = "an object",
                ["longDescription"] = "This is a basic object.",
                ["visible"] = true,
                ["gettable"] = false,
                ["weight"] = 0,
                ["value"] = 0
            },
            IsAbstract = true
        };
        GameDatabase.Instance.ObjectClasses.Insert(baseObjectClass);

        // Room class
        var roomClass = new ObjectClass
        {
            Id = "obj_room",
            Name = "Room",
            ParentClassId = baseObjectClass.Id,
            Description = "A location that can contain objects and players",
            Properties = new BsonDocument
            {
                ["name"] = "a room",
                ["shortDescription"] = "a room",
                ["longDescription"] = "This is an empty room.",
                ["gettable"] = false,
                ["capacity"] = 1000,
                ["light"] = true
            }
        };
        GameDatabase.Instance.ObjectClasses.Insert(roomClass);

        // Exit class
        var exitClass = new ObjectClass
        {
            Id = "obj_exit",
            Name = "Exit",
            ParentClassId = baseObjectClass.Id,
            Description = "A connection between rooms",
            Properties = new BsonDocument
            {
                ["name"] = "an exit",
                ["shortDescription"] = "an exit",
                ["longDescription"] = "This is an exit leading somewhere.",
                ["destination"] = "",
                ["direction"] = "",
                ["visible"] = true,
                ["gettable"] = false,
                ["locked"] = false,
                ["hidden"] = false
            }
        };
        GameDatabase.Instance.ObjectClasses.Insert(exitClass);

        // Container class
        var containerClass = new ObjectClass
        {
            Id = "obj_container",
            Name = "Container",
            ParentClassId = baseObjectClass.Id,
            Description = "An object that can hold other objects",
            Properties = new BsonDocument
            {
                ["name"] = "a container",
                ["shortDescription"] = "a container",
                ["longDescription"] = "This is a container that can hold things.",
                ["gettable"] = true,
                ["capacity"] = 100,
                ["closed"] = false,
                ["locked"] = false,
                ["weight"] = 5
            }
        };
        GameDatabase.Instance.ObjectClasses.Insert(containerClass);

        // Item class
        var itemClass = new ObjectClass
        {
            Id = "obj_item",
            Name = "Item",
            ParentClassId = baseObjectClass.Id,
            Description = "A basic item that can be picked up",
            Properties = new BsonDocument
            {
                ["name"] = "an item",
                ["shortDescription"] = "an item",
                ["longDescription"] = "This is a basic item.",
                ["gettable"] = true,
                ["weight"] = 1,
                ["value"] = 1
            }
        };
        GameDatabase.Instance.ObjectClasses.Insert(itemClass);

        // Player class (if not already created by PlayerManager)
        if (GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Player") == null)
        {
            var playerClass = new ObjectClass
            {
                Id = "obj_player",
                Name = "Player",
                ParentClassId = baseObjectClass.Id,
                Description = "A player character",
                Properties = new BsonDocument
                {
                    ["name"] = "a player",
                    ["shortDescription"] = "a player",
                    ["longDescription"] = "This is a player character.",
                    ["gettable"] = false,
                    ["maxHealth"] = 100,
                    ["health"] = 100,
                    ["level"] = 1,
                    ["experience"] = 0,
                    ["strength"] = 10,
                    ["dexterity"] = 10,
                    ["intelligence"] = 10,
                    ["carryCapacity"] = 50
                }
            };
            GameDatabase.Instance.ObjectClasses.Insert(playerClass);
        }
    }

    /// <summary>
    /// Creates the starting room and some basic areas
    /// </summary>
    private static void CreateStartingRoom()
    {
        // Check if starting room already exists
        var allGameObjects = GameDatabase.Instance.GameObjects.FindAll();
        var existingStartingRoom = allGameObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isStartingRoom") && obj.Properties["isStartingRoom"].AsBoolean == true);
        if (existingStartingRoom != null)
            return;

        var roomClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Room");
        if (roomClass == null) return;

        // Create the starting room
        var startingRoom = ObjectManager.CreateInstance(roomClass.Id);
        ObjectManager.SetProperty(startingRoom, "name", "The Nexus");
        ObjectManager.SetProperty(startingRoom, "shortDescription", "the Nexus");
        ObjectManager.SetProperty(startingRoom, "longDescription", 
            "This is the central hub of the CSMOO world. A shimmering portal of energy " +
            "connects this place to all other realms. New adventurers often find themselves " +
            "here when they first enter the world.");
        ObjectManager.SetProperty(startingRoom, "isStartingRoom", true);

        // Create a simple connected room
        var secondRoom = ObjectManager.CreateInstance(roomClass.Id);
        ObjectManager.SetProperty(secondRoom, "name", "A Peaceful Grove");
        ObjectManager.SetProperty(secondRoom, "shortDescription", "a peaceful grove");
        ObjectManager.SetProperty(secondRoom, "longDescription",
            "A tranquil grove surrounded by ancient oak trees. Sunlight filters through " +
            "the canopy above, creating dancing patterns on the soft grass below. " +
            "A gentle breeze carries the scent of wildflowers.");

        // Create exits between the rooms
        CreateExit(startingRoom.Id, secondRoom.Id, "north", "south");

        // Create a simple item in the grove
        CreateSimpleItem("A Wooden Staff", "a wooden staff", 
            "A simple wooden staff, worn smooth by countless hands. It radiates a faint magical aura.",
            secondRoom.Id);
    }

    /// <summary>
    /// Creates bidirectional exits between two rooms
    /// </summary>
    public static void CreateExit(string fromRoomId, string toRoomId, string direction, string returnDirection)
    {
        var exitClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Exit");
        if (exitClass == null) return;

        // Create the forward exit
        var forwardExit = ObjectManager.CreateInstance(exitClass.Id, fromRoomId);
        ObjectManager.SetProperty(forwardExit, "name", direction);
        ObjectManager.SetProperty(forwardExit, "shortDescription", direction);
        ObjectManager.SetProperty(forwardExit, "longDescription", $"An exit leading {direction}.");
        ObjectManager.SetProperty(forwardExit, "direction", direction);
        ObjectManager.SetProperty(forwardExit, "destination", toRoomId);

        // Create the return exit
        var returnExit = ObjectManager.CreateInstance(exitClass.Id, toRoomId);
        ObjectManager.SetProperty(returnExit, "name", returnDirection);
        ObjectManager.SetProperty(returnExit, "shortDescription", returnDirection);
        ObjectManager.SetProperty(returnExit, "longDescription", $"An exit leading {returnDirection}.");
        ObjectManager.SetProperty(returnExit, "direction", returnDirection);
        ObjectManager.SetProperty(returnExit, "destination", fromRoomId);
    }

    /// <summary>
    /// Creates a simple item in the world
    /// </summary>
    public static GameObject CreateSimpleItem(string name, string shortDesc, string longDesc, string? locationId = null)
    {
        var itemClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Item");
        if (itemClass == null)
            throw new InvalidOperationException("Item class not found. Call InitializeWorld first.");

        var item = ObjectManager.CreateInstance(itemClass.Id, locationId);
        ObjectManager.SetProperty(item, "name", name);
        ObjectManager.SetProperty(item, "shortDescription", shortDesc);
        ObjectManager.SetProperty(item, "longDescription", longDesc);

        return item;
    }

    /// <summary>
    /// Gets the default starting room for new players
    /// </summary>
    public static GameObject? GetStartingRoom()
    {
        var allGameObjects = GameDatabase.Instance.GameObjects.FindAll();
        return allGameObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isStartingRoom") && obj.Properties["isStartingRoom"].AsBoolean == true);
    }

    /// <summary>
    /// Gets all rooms in the world
    /// </summary>
    public static List<GameObject> GetAllRooms()
    {
        var roomClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Room");
        if (roomClass == null) return new List<GameObject>();

        return ObjectManager.FindObjectsByClass(roomClass.Id);
    }

    /// <summary>
    /// Gets all exits from a room
    /// </summary>
    public static List<GameObject> GetExitsFromRoom(string roomId)
    {
        var exitClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Exit");
        if (exitClass == null) return new List<GameObject>();

        return ObjectManager.GetObjectsInLocation(roomId)
            .Where(obj => obj.ClassId == exitClass.Id)
            .ToList();
    }

    /// <summary>
    /// Creates default verbs on the built-in classes
    /// </summary>
    private static void CreateDefaultVerbs()
    {
        // Find the classes
        var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Object");
        var playerClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Player");
        var roomClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Room");
        var itemClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Item");
        var containerClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Container");

        if (objectClass == null) return; // Classes not set up yet

        // Check if verbs already exist to avoid duplicates
        var existingVerbs = GameDatabase.Instance.GetCollection<Verb>("verbs");
        
        // Default "look" verb on Object class (inherited by all objects)
        if (!existingVerbs.Exists(v => v.ObjectId == objectClass.Id && v.Name == "look"))
        {
            Scripting.VerbManager.CreateVerb(objectClass.Id, "look", "*", 
                @"// Default look verb
var name = me.name?.ToString() ?? ""something"";
var desc = me.longDescription?.ToString() ?? ""You see nothing special."";
Say(desc);", "system");
        }

        // Default "inventory" verb on Player class
        if (playerClass != null && !existingVerbs.Exists(v => v.ObjectId == playerClass.Id && v.Name == "inventory"))
        {
            Scripting.VerbManager.CreateVerb(playerClass.Id, "inventory", "", 
                @"// Show player's inventory
var items = ObjectManager.GetObjectsInLocation(me);
if (items.Count == 0)
{
    Say(""You are not carrying anything."");
}
else
{
    Say(""You are carrying:"");
    foreach (var itemId in items)
    {
        var itemObj = ObjectManager.GetObject(itemId);
        var name = itemObj.name?.ToString() ?? ""something"";
        Say($""  {name}"");
    }
}", "system");
            
            // Add alias for inventory
            var inventoryVerb = existingVerbs.FindOne(v => v.ObjectId == playerClass.Id && v.Name == "inventory");
            if (inventoryVerb != null)
            {
                inventoryVerb.Aliases = "i inv";
                existingVerbs.Update(inventoryVerb);
            }
        }

        // Default "get" verb on Item class
        if (itemClass != null && !existingVerbs.Exists(v => v.ObjectId == itemClass.Id && v.Name == "get"))
        {
            Scripting.VerbManager.CreateVerb(itemClass.Id, "get", "*", 
                @"// Default get verb for items
var gettable = me.gettable?.AsBoolean ?? false;
if (!gettable)
{
    Say(""You can't take that."");
    return;
}

var name = me.name?.ToString() ?? ""something"";
ObjectManager.MoveObject(me, player);
Say($""You take the {name}."");", "system");
            
            // Add aliases for get
            var getVerb = existingVerbs.FindOne(v => v.ObjectId == itemClass.Id && v.Name == "get");
            if (getVerb != null)
            {
                getVerb.Aliases = "take grab";
                existingVerbs.Update(getVerb);
            }
        }

        // Default "drop" verb on Player class for dropping items
        if (playerClass != null && !existingVerbs.Exists(v => v.ObjectId == playerClass.Id && v.Name == "drop"))
        {
            Scripting.VerbManager.CreateVerb(playerClass.Id, "drop", "*", 
                @"// Drop an item from inventory
if (Args.Count == 0)
{
    Say(""Drop what?"");
    return;
}

var itemName = string.Join("" "", Args).ToLower();
var items = ObjectManager.GetObjectsInLocation(player);
string foundItemId = null;

foreach (var itemId in items)
{
    var itemObj = ObjectManager.GetObject(itemId);
    var name = itemObj.name?.ToString()?.ToLower();
    if (name != null && name.Contains(itemName))
    {
        foundItemId = itemId;
        break;
    }
}

if (foundItemId == null)
{
    Say($""You don't have a '{itemName}'."");
    return;
}

var foundItem = ObjectManager.GetObject(foundItemId);
var itemFullName = foundItem.name?.ToString() ?? ""something"";
ObjectManager.MoveObject(foundItemId, player.Location);
Say($""You drop the {itemFullName}."");", "system");
        }
    }

    /// <summary>
    /// Creates system commands as verbs using the new ScriptHelpers
    /// </summary>
    private static void CreateSystemCommands()
    {
        // Get or create the system object
        var systemObjectId = GetOrCreateSystemObject();
        if (systemObjectId == null)
        {
            Logger.Error("Failed to create system object for system commands");
            return;
        }

        var existingVerbs = GameDatabase.Instance.GetCollection<Verb>("verbs");

        // Look command
        if (!existingVerbs.Exists(v => v.ObjectId == systemObjectId && v.Name == "look"))
        {
            Scripting.VerbManager.CreateVerb(systemObjectId, "look", "*", @"
// Look command - shows room or looks at specific object
if (Args.Count == 0)
{
    // Just 'look' - show the room
    Helpers.ShowRoom();
}
else if (Args.Count >= 2 && Args[0].ToLower() == ""at"")
{
    // 'look at something'
    var target = string.Join("" "", Args.Skip(1));
    Helpers.LookAtObject(target);
}
else
{
    // 'look something'
    var target = string.Join("" "", Args);
    Helpers.LookAtObject(target);
}
", "system");

            // Add 'l' as an alias for look
            var lookVerb = existingVerbs.FindOne(v => v.ObjectId == systemObjectId && v.Name == "look");
            if (lookVerb != null)
            {
                lookVerb.Aliases = "l";
                existingVerbs.Update(lookVerb);
            }
        }

        // Go command
        if (!existingVerbs.Exists(v => v.ObjectId == systemObjectId && v.Name == "go"))
        {
            Scripting.VerbManager.CreateVerb(systemObjectId, "go", "*", @"
// Go command - move in a direction
if (Args.Count != 1)
{
    Say(""Usage: go <direction>"");
    return;
}

var direction = Args[0].ToLower();
var currentLocation = here;
if (currentLocation == null)
{
    Say(""You are not in any location."");
    return;
}

var exits = Helpers.GetExitsFromRoom(currentLocation);
var exit = exits.FirstOrDefault(e => 
    e.direction?.AsString?.ToLower() == direction);

if (exit == null)
{
    Say($""There is no exit {direction}."");
    return;
}

var destination = exit.destination?.AsString;
if (destination == null)
{
    Say(""That exit doesn't lead anywhere."");
    return;
}

// Move the player
if (Helpers.MoveObject(player, destination))
{
    player.Location = destination;
    GameDatabase.Instance.Players.Update(player);
    Say($""You go {direction}."");
    Helpers.ShowRoom();
}
else
{
    Say(""You can't go that way."");
}
", "system");
        }

        // Add direction shortcuts
        var directions = new[] { "north", "south", "east", "west", "n", "s", "e", "w", "northeast", "northwest", "southeast", "southwest", "ne", "nw", "se", "sw", "up", "down", "u", "d" };
        foreach (var dir in directions)
        {
            if (!existingVerbs.Exists(v => v.ObjectId == systemObjectId && v.Name == dir))
            {
                var fullDirection = dir switch
                {
                    "n" => "north",
                    "s" => "south", 
                    "e" => "east",
                    "w" => "west",
                    "ne" => "northeast",
                    "nw" => "northwest", 
                    "se" => "southeast",
                    "sw" => "southwest",
                    "u" => "up",
                    "d" => "down",
                    _ => dir
                };

                Scripting.VerbManager.CreateVerb(systemObjectId, dir, "", $@"
// Direction shortcut for {fullDirection}
var currentLocation = here;
if (currentLocation == null)
{{
    Say(""You are not in any location."");
    return;
}}

var exits = Helpers.GetExitsFromRoom(currentLocation);
var exit = exits.FirstOrDefault(e => 
    e.direction?.AsString?.ToLower() == ""{fullDirection}"");

if (exit == null)
{{
    Say($""There is no exit {fullDirection}."");
    return;
}}

var destination = exit.destination?.AsString;
if (destination == null)
{{
    Say(""That exit doesn't lead anywhere."");
    return;
}}

// Move the player
if (Helpers.MoveObject(player, destination))
{{
    player.Location = destination;
    GameDatabase.Instance.Players.Update(player);
    Say($""You go {fullDirection}."");
    Helpers.ShowRoom();
}}
else
{{
    Say(""You can't go that way."");
}}
", "system");
            }
        }

        // Inventory command
        if (!existingVerbs.Exists(v => v.ObjectId == systemObjectId && v.Name == "inventory"))
        {
            Scripting.VerbManager.CreateVerb(systemObjectId, "inventory", "", @"
// Inventory command - show what the player is carrying
Helpers.ShowInventory();
", "system");

            // Add aliases for inventory
            var invVerb = existingVerbs.FindOne(v => v.ObjectId == systemObjectId && v.Name == "inventory");
            if (invVerb != null)
            {
                invVerb.Aliases = "i inv";
                existingVerbs.Update(invVerb);
            }
        }

        // Get command
        if (!existingVerbs.Exists(v => v.ObjectId == systemObjectId && v.Name == "get"))
        {
            Scripting.VerbManager.CreateVerb(systemObjectId, "get", "*", @"
// Get command - pick up an item
if (Args.Count == 0)
{
    Say(""Get what?"");
    return;
}

var itemName = string.Join("" "", Args);
var item = Helpers.FindItemInRoom(itemName);

if (item == null)
{
    Say(""There is no such item here."");
    return;
}

var gettable = item.gettable?.AsBoolean ?? false;
if (!gettable)
{
    Say(""You can't take that."");
    return;
}

// Move item to player's inventory
if (Helpers.MoveObject(item, player))
{
    var itemDesc = item.shortDescription?.AsString ?? ""something"";
    Say($""You take {itemDesc}."");
}
else
{
    Say(""You can't take that."");
}
", "system");

            // Add aliases for get
            var getVerb = existingVerbs.FindOne(v => v.ObjectId == systemObjectId && v.Name == "get");
            if (getVerb != null)
            {
                getVerb.Aliases = "take grab";
                existingVerbs.Update(getVerb);
            }
        }

        // Drop command
        if (!existingVerbs.Exists(v => v.ObjectId == systemObjectId && v.Name == "drop"))
        {
            Scripting.VerbManager.CreateVerb(systemObjectId, "drop", "*", @"
// Drop command - drop an item from inventory
if (Args.Count == 0)
{
    Say(""Drop what?"");
    return;
}

var itemName = string.Join("" "", Args);
var item = Helpers.FindItemInInventory(itemName);

if (item == null)
{
    Say(""You don't have that item."");
    return;
}

var currentLocation = here;
if (currentLocation == null)
{
    Say(""You are nowhere - you can't drop anything."");
    return;
}

// Move item to current room
if (Helpers.MoveObject(item, currentLocation))
{
    var itemDesc = item.shortDescription?.AsString ?? ""something"";
    Say($""You drop {itemDesc}."");
}
else
{
    Say(""You can't drop that."");
}
", "system");
        }

        // Say command
        if (!existingVerbs.Exists(v => v.ObjectId == systemObjectId && v.Name == "say"))
        {
            Scripting.VerbManager.CreateVerb(systemObjectId, "say", "*", @"
// Say command - speak to others in the room
if (Args.Count == 0)
{
    Say(""Say what?"");
    return;
}

var message = string.Join("" "", Args);
Say($""You say, \""{message}\"""");

// Send to other players in the room
Helpers.SayToRoom($""{player.Name} says, \""{message}\"""", true);
", "system");
        }

        // Who command
        if (!existingVerbs.Exists(v => v.ObjectId == systemObjectId && v.Name == "who"))
        {
            Scripting.VerbManager.CreateVerb(systemObjectId, "who", "", @"
// Who command - list online players
var onlinePlayers = Helpers.GetOnlinePlayers();
Say(""Online players:"");
foreach (var player in onlinePlayers)
{
    Say($""  {player.Name}"");
}
", "system");
        }

        // Tell command (like 'tell player message')
        if (!existingVerbs.Exists(v => v.ObjectId == systemObjectId && v.Name == "tell"))
        {
            Scripting.VerbManager.CreateVerb(systemObjectId, "tell", "* *", @"
// Tell command - send private message to another player
if (Args.Count < 2)
{
    Say(""Usage: tell <player> <message>"");
    return;
}

var targetPlayerName = Args[0];
var message = string.Join("" "", Args.Skip(1));

var targetPlayer = Helpers.FindPlayerByName(targetPlayerName);
if (targetPlayer == null)
{
    Say($""Player '{targetPlayerName}' is not online."");
    return;
}

if (targetPlayer == player)
{
    Say(""You can't tell yourself."");
    return;
}

// Send the message
Helpers.SendToPlayer($""{player.Name} tells you, \""{message}\"""", targetPlayer);
Say($""You tell {targetPlayer.Name}, \""{message}\"""");
", "system");
        }

        // OOC command (Out of Character chat)
        if (!existingVerbs.Exists(v => v.ObjectId == systemObjectId && v.Name == "ooc"))
        {
            Scripting.VerbManager.CreateVerb(systemObjectId, "ooc", "*", @"
// OOC command - out of character chat to all online players
if (Args.Count == 0)
{
    Say(""Say what OOC?"");
    return;
}

var message = string.Join("" "", Args);
var onlinePlayers = Helpers.GetOnlinePlayers();

foreach (var onlinePlayer in onlinePlayers)
{
    if (onlinePlayer == player)
    {
        Helpers.SendToPlayer($""[OOC] You say, \""{message}\"""", onlinePlayer);
    }
    else
    {
        Helpers.SendToPlayer($""[OOC] {player.Name} says, \""{message}\"""", onlinePlayer);
    }
}
", "system");
        }

        // List command (replaces @list built-in command)
        if (!existingVerbs.Exists(v => v.ObjectId == systemObjectId && v.Name == "list"))
        {
            Scripting.VerbManager.CreateVerb(systemObjectId, "list", "*", @"
// List command - show verb code (@list <object>:<verb>)
if (Args.Count != 1)
{
    Say(""Usage: list <object>:<verb>"");
    return;
}

var verbSpec = Args[0];
if (!verbSpec.Contains(':'))
{
    Say(""Verb specification must be in format <object>:<verb>"");
    return;
}

// Split from the right to handle class:Object:verb syntax
var lastColonIndex = verbSpec.LastIndexOf(':');
var objectName = verbSpec.Substring(0, lastColonIndex);
var verbName = verbSpec.Substring(lastColonIndex + 1);

var verbInfo = Helpers.GetVerbInfo(objectName, verbName);
if (verbInfo == null)
{
    Say($""Object '{objectName}' or verb '{verbName}' not found."");
    return;
}

// Display verb information
Say($""=== {verbInfo.ObjectName}:{verbInfo.VerbName} ==="");
if (!string.IsNullOrEmpty(verbInfo.Aliases))
    Say($""Aliases: {verbInfo.Aliases}"");
if (!string.IsNullOrEmpty(verbInfo.Pattern))
    Say($""Pattern: {verbInfo.Pattern}"");
if (!string.IsNullOrEmpty(verbInfo.Description))
    Say($""Description: {verbInfo.Description}"");

Say($""Created by: {verbInfo.CreatedBy} on {verbInfo.CreatedAt:yyyy-MM-dd HH:mm}"");
Say(""Code:"");

if (verbInfo.CodeLines.Length == 0)
{
    Say(""  (no code)"");
}
else
{
    for (int i = 0; i < verbInfo.CodeLines.Length; i++)
    {
        Say($""{i + 1,3}: {verbInfo.CodeLines[i]}"");
    }
}
", "system");

            // Add @list as an alias for backwards compatibility
            var listVerb = existingVerbs.FindOne(v => v.ObjectId == systemObjectId && v.Name == "list");
            if (listVerb != null)
            {
                listVerb.Aliases = "@list";
                existingVerbs.Update(listVerb);
            }
        }

        Logger.Info("System commands created as verbs");
    }

    /// <summary>
    /// Gets or creates the system object for holding global verbs
    /// </summary>
    private static string? GetOrCreateSystemObject()
    {
        // Get all objects and filter in memory (LiteDB doesn't support ContainsKey in expressions)
        var allObjects = GameDatabase.Instance.GameObjects.FindAll();
        var systemObj = allObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true);
        
        if (systemObj == null)
        {
            // System object doesn't exist, create it
            Logger.Debug("System object not found, creating it...");
            // Use Container class instead of abstract Object class
            var containerClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Container");
            if (containerClass != null)
            {
                systemObj = ObjectManager.CreateInstance(containerClass.Id);
                ObjectManager.SetProperty(systemObj, "name", "System");
                ObjectManager.SetProperty(systemObj, "shortDescription", "the system object");
                ObjectManager.SetProperty(systemObj, "longDescription", "This is the system object that holds global verbs and functions.");
                ObjectManager.SetProperty(systemObj, "isSystemObject", true);
                ObjectManager.SetProperty(systemObj, "gettable", false); // Don't allow players to pick up the system
                Logger.Debug($"Created system object with ID: {systemObj.Id}");
            }
            else
            {
                Logger.Error("Could not find Container class to create system object!");
                return null;
            }
        }
        
        Logger.Debug($"System object ID: {systemObj?.Id}");
        return systemObj?.Id;
    }

    /// <summary>
    /// Updates existing verbs to use the new natural object-oriented syntax
    /// </summary>
    private static void MigrateVerbSyntax()
    {
        var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");
        var allVerbs = verbCollection.FindAll().ToList();
        
        bool anyUpdated = false;
        
        foreach (var verb in allVerbs)
        {
            if (string.IsNullOrEmpty(verb.Code)) continue;
            
            var oldCode = verb.Code;
            var newCode = verb.Code;
            
            // Legacy migrations
            if (newCode.Contains("this.id"))
            {
                newCode = newCode.Replace("this.id", "ThisObject");
                anyUpdated = true;
            }
            
            // Migrate to new natural syntax
            if (newCode.Contains("ObjectManager.GetProperty(ThisObject,"))
            {
                newCode = System.Text.RegularExpressions.Regex.Replace(
                    newCode, 
                    @"ObjectManager\.GetProperty\(ThisObject,\s*""([^""]+)""\)",
                    "me.$1");
                anyUpdated = true;
            }
            
            if (newCode.Contains("ObjectManager.SetProperty(ThisObject,"))
            {
                newCode = System.Text.RegularExpressions.Regex.Replace(
                    newCode,
                    @"ObjectManager\.SetProperty\(ThisObject,\s*""([^""]+)"",\s*([^)]+)\)",
                    "me.$1 = $2");
                anyUpdated = true;
            }
            
            // Remove .Id references - treat objects as pure references
            if (newCode.Contains("player.Id"))
            {
                newCode = newCode.Replace("player.Id", "player");
                anyUpdated = true;
            }
            
            if (newCode.Contains("me.Id"))
            {
                newCode = newCode.Replace("me.Id", "me");
                anyUpdated = true;
            }
            
            if (newCode.Contains("here.Id"))
            {
                newCode = newCode.Replace("here.Id", "here");
                anyUpdated = true;
            }
            
            if (newCode.Contains("this.Id"))
            {
                newCode = newCode.Replace("this.Id", "this");
                anyUpdated = true;
            }
            
            if (newCode.Contains("Player.Id"))
            {
                newCode = newCode.Replace("Player.Id", "player");
                anyUpdated = true;
            }
            
            if (newCode.Contains("Player.Name"))
            {
                newCode = newCode.Replace("Player.Name", "player.Name");
                anyUpdated = true;
            }
            
            if (newCode.Contains("Player.Location"))
            {
                newCode = newCode.Replace("Player.Location", "player.Location");
                anyUpdated = true;
            }
            
            if (newCode != oldCode)
            {
                verb.Code = newCode;
                verbCollection.Update(verb);
                Logger.Debug($"Updated verb '{verb.Name}' to use natural syntax");
            }
        }
        
        if (anyUpdated)
        {
            Logger.Info("Verb syntax migration to natural OOP syntax completed.");
        }
    }
}
