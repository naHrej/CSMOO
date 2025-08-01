using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;

namespace CSMOO.Database;

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
        if (DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject") != null)
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
        // Check if GameObject class already exists
        var existingGameObjectClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
        if (existingGameObjectClass != null)
        {
            Logger.Debug("GameObject class already exists, skipping creation");
            return existingGameObjectClass;
        }

        var baseObjectClass = new ObjectClass
        {
            Id = "GameObject",
            Name = "GameObject",
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
        
        DbProvider.Instance.Insert("objectclasses", baseObjectClass);
        Logger.Debug("Created base Object class");
        return baseObjectClass;
    }

    /// <summary>
    /// Creates the Room class for locations
    /// </summary>
    private static ObjectClass CreateRoomClass()
    {
        // Check if Room class already exists
        var existingRoomClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Room");
        if (existingRoomClass != null)
        {
            Logger.Debug("Room class already exists, skipping creation");
            return existingRoomClass;
        }

        var baseObjectClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
        
        var roomClass = new ObjectClass
        {
            Id = "Room",
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
        
        DbProvider.Instance.Insert("objectclasses", roomClass);
        Logger.Debug("Created Room class");
        return roomClass;
    }

    /// <summary>
    /// Creates the Exit class for connections between rooms
    /// </summary>
    private static ObjectClass CreateExitClass()
    {
        // Check if Exit class already exists
        var existingExitClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Exit");
        if (existingExitClass != null)
        {
            Logger.Debug("Exit class already exists, skipping creation");
            return existingExitClass;
        }

        var baseObjectClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
        
        var exitClass = new ObjectClass
        {
            Id = "Exit",
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
        
        DbProvider.Instance.Insert("objectclasses", exitClass);
        Logger.Debug("Created Exit class");
        return exitClass;
    }

    /// <summary>
    /// Creates the Container class for objects that can hold other objects
    /// </summary>
    private static ObjectClass CreateContainerClass()
    {
        // Check if Container class already exists
        var existingContainerClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Container");
        if (existingContainerClass != null)
        {
            Logger.Debug("Container class already exists, skipping creation");
            return existingContainerClass;
        }

        var baseObjectClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
        
        var containerClass = new ObjectClass
        {
            Id = "Container",
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
        
        DbProvider.Instance.Insert("objectclasses", containerClass);
        Logger.Debug("Created Container class");
        return containerClass;
    }

    /// <summary>
    /// Creates the Item class for portable objects
    /// </summary>
    private static ObjectClass CreateItemClass()
    {
        // Check if Item class already exists
        var existingItemClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Item");
        if (existingItemClass != null)
        {
            Logger.Debug("Item class already exists, skipping creation");
            return existingItemClass;
        }

        var baseObjectClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
        
        var itemClass = new ObjectClass
        {
            Id = "Item",
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
        
        DbProvider.Instance.Insert("objectclasses", itemClass);
        Logger.Debug("Created Item class");
        return itemClass;
    }

    /// <summary>
    /// Creates the Player class for player characters
    /// </summary>
    private static ObjectClass CreatePlayerClass()
    {
        // Check if PlayerManager already created this
        var existingPlayerClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Player");
        if (existingPlayerClass != null)
        {
            Logger.Debug("Player class already exists, skipping creation");
            return existingPlayerClass;
        }

        var baseObjectClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
        
        var playerClass = new ObjectClass
        {
            Id = "Player",
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
        
        DbProvider.Instance.Insert("objectclasses", playerClass);
        Logger.Debug("Created Player class");
        return playerClass;
    }

    /// <summary>
    /// Gets the base Object class
    /// </summary>
    public static ObjectClass? GetBaseObjectClass()
    {
        return DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
    }

    /// <summary>
    /// Gets a core class by name
    /// </summary>
    public static ObjectClass? GetCoreClass(string className)
    {
        return DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == className);
    }
}



