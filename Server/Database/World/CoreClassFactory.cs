using System;
using LiteDB;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Database.World;

/// <summary>
/// Factory for creating core object classes that form the foundation of the object system
/// </summary>
public static class CoreClassFactory
{
    /// <summary>
    /// Creates the fundamental object classes that everything inherits from
    /// </summary>
    public static void CreateCoreClasses()
    {
        // Check if core classes already exist
        if (GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Object") != null)
        {
            Logger.Debug("Core classes already exist, skipping creation");
            return;
        }

        Logger.Info("Creating core object classes...");

        CreateBaseObjectClass();
        CreateRoomClass();
        CreateExitClass();
        CreateContainerClass();
        CreateItemClass();
        CreatePlayerClass();

        Logger.Info("Core object classes created successfully");
    }

    /// <summary>
    /// Creates the base Object class that everything inherits from
    /// </summary>
    private static ObjectClass CreateBaseObjectClass()
    {
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
        Logger.Debug("Created base Object class");
        return baseObjectClass;
    }

    /// <summary>
    /// Creates the Room class for locations
    /// </summary>
    private static ObjectClass CreateRoomClass()
    {
        var baseObjectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Object");
        
        var roomClass = new ObjectClass
        {
            Id = "obj_room",
            Name = "Room",
            ParentClassId = baseObjectClass?.Id,
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
        Logger.Debug("Created Room class");
        return roomClass;
    }

    /// <summary>
    /// Creates the Exit class for connections between rooms
    /// </summary>
    private static ObjectClass CreateExitClass()
    {
        var baseObjectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Object");
        
        var exitClass = new ObjectClass
        {
            Id = "obj_exit",
            Name = "Exit",
            ParentClassId = baseObjectClass?.Id,
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
        Logger.Debug("Created Exit class");
        return exitClass;
    }

    /// <summary>
    /// Creates the Container class for objects that can hold other objects
    /// </summary>
    private static ObjectClass CreateContainerClass()
    {
        var baseObjectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Object");
        
        var containerClass = new ObjectClass
        {
            Id = "obj_container",
            Name = "Container",
            ParentClassId = baseObjectClass?.Id,
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
        Logger.Debug("Created Container class");
        return containerClass;
    }

    /// <summary>
    /// Creates the Item class for portable objects
    /// </summary>
    private static ObjectClass CreateItemClass()
    {
        var baseObjectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Object");
        
        var itemClass = new ObjectClass
        {
            Id = "obj_item",
            Name = "Item",
            ParentClassId = baseObjectClass?.Id,
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
        Logger.Debug("Created Item class");
        return itemClass;
    }

    /// <summary>
    /// Creates the Player class for player characters
    /// </summary>
    private static ObjectClass CreatePlayerClass()
    {
        // Check if PlayerManager already created this
        var existingPlayerClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Player");
        if (existingPlayerClass != null)
        {
            Logger.Debug("Player class already exists, skipping creation");
            return existingPlayerClass;
        }

        var baseObjectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Object");
        
        var playerClass = new ObjectClass
        {
            Id = "obj_player",
            Name = "Player",
            ParentClassId = baseObjectClass?.Id,
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
        Logger.Debug("Created Player class");
        return playerClass;
    }

    /// <summary>
    /// Gets the base Object class
    /// </summary>
    public static ObjectClass? GetBaseObjectClass()
    {
        return GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Object");
    }

    /// <summary>
    /// Gets a core class by name
    /// </summary>
    public static ObjectClass? GetCoreClass(string className)
    {
        return GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == className);
    }
}
