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
var name = ObjectManager.GetProperty(ThisObject, ""name"")?.ToString() ?? ""something"";
var desc = ObjectManager.GetProperty(ThisObject, ""longDescription"")?.ToString() ?? ""You see nothing special."";
Say(desc);", "system");
        }

        // Default "inventory" verb on Player class
        if (playerClass != null && !existingVerbs.Exists(v => v.ObjectId == playerClass.Id && v.Name == "inventory"))
        {
            Scripting.VerbManager.CreateVerb(playerClass.Id, "inventory", "", 
                @"// Show player's inventory
var items = ObjectManager.GetObjectsInLocation(ThisObject);
if (items.Count == 0)
{
    Say(""You are not carrying anything."");
}
else
{
    Say(""You are carrying:"");
    foreach (var itemId in items)
    {
        var name = ObjectManager.GetProperty(itemId, ""name"")?.ToString() ?? ""something"";
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
var gettable = ObjectManager.GetProperty(ThisObject, ""gettable"")?.AsBoolean ?? false;
if (!gettable)
{
    Say(""You can't take that."");
    return;
}

var name = ObjectManager.GetProperty(ThisObject, ""name"")?.ToString() ?? ""something"";
ObjectManager.MoveObject(ThisObject, Player.Id);
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
var items = ObjectManager.GetObjectsInLocation(Player.Id);
string foundItemId = null;

foreach (var itemId in items)
{
    var name = ObjectManager.GetProperty(itemId, ""name"")?.ToString()?.ToLower();
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

var itemFullName = ObjectManager.GetProperty(foundItemId, ""name"")?.ToString() ?? ""something"";
ObjectManager.MoveObject(foundItemId, Player.Location);
Say($""You drop the {itemFullName}."");", "system");
        }
    }

    /// <summary>
    /// Updates existing verbs to fix the 'this.id' syntax error
    /// </summary>
    private static void MigrateVerbSyntax()
    {
        var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");
        var allVerbs = verbCollection.FindAll().ToList();
        
        bool anyUpdated = false;
        
        foreach (var verb in allVerbs)
        {
            if (!string.IsNullOrEmpty(verb.Code) && verb.Code.Contains("this.id"))
            {
                var oldCode = verb.Code;
                verb.Code = verb.Code.Replace("this.id", "ThisObject");
                verbCollection.Update(verb);
                anyUpdated = true;
                
                Logger.Debug($"Updated verb '{verb.Name}' - replaced 'this.id' with 'ThisObject'");
            }
        }
        
        if (anyUpdated)
        {
            Logger.Info("Verb syntax migration completed.");
        }
    }
}
