using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Instance-based core class factory implementation for dependency injection
/// </summary>
public class CoreClassFactoryInstance : ICoreClassFactory
{
    private readonly IDbProvider _dbProvider;
    private readonly ILogger _logger;
    
    public CoreClassFactoryInstance(IDbProvider dbProvider, ILogger logger)
    {
        _dbProvider = dbProvider;
        _logger = logger;
    }
    
    /// <summary>
    /// Creates the fundamental object classes that everything inherits from
    /// </summary>
    public void CreateCoreClasses()
    {
        // Check if core classes already exist
        if (_dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject") != null)
        {
            return;
        }

        _logger.Info("Creating core object classes...");

        CreateBaseObjectClass();
        CreateRoomClass();
        CreateExitClass();
        CreateContainerClass();
        CreateItemClass();
        CreatePlayerClass();

        _logger.Info("Core object classes created successfully");
    }

    /// <summary>
    /// Creates the base Object class that everything inherits from
    /// </summary>
    private ObjectClass CreateBaseObjectClass()
    {
        // Check if GameObject class already exists
        var existingGameObjectClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
        if (existingGameObjectClass != null)
        {
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
        
        _dbProvider.Insert("objectclasses", baseObjectClass);
        return baseObjectClass;
    }

    /// <summary>
    /// Creates the Room class for locations
    /// </summary>
    private ObjectClass CreateRoomClass()
    {
        // Check if Room class already exists
        var existingRoomClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Room");
        if (existingRoomClass != null)
        {
            return existingRoomClass;
        }

        var baseObjectClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
        
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
        
        _dbProvider.Insert("objectclasses", roomClass);
        return roomClass;
    }

    /// <summary>
    /// Creates the Exit class for connections between rooms
    /// </summary>
    private ObjectClass CreateExitClass()
    {
        // Check if Exit class already exists
        var existingExitClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Exit");
        if (existingExitClass != null)
        {
            return existingExitClass;
        }

        var baseObjectClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
        
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
        
        _dbProvider.Insert("objectclasses", exitClass);
        return exitClass;
    }

    /// <summary>
    /// Creates the Container class for objects that can hold other objects
    /// </summary>
    private ObjectClass CreateContainerClass()
    {
        // Check if Container class already exists
        var existingContainerClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Container");
        if (existingContainerClass != null)
        {
            return existingContainerClass;
        }

        var baseObjectClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
        
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
        
        _dbProvider.Insert("objectclasses", containerClass);
        return containerClass;
    }

    /// <summary>
    /// Creates the Item class for portable objects
    /// </summary>
    private ObjectClass CreateItemClass()
    {
        // Check if Item class already exists
        var existingItemClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Item");
        if (existingItemClass != null)
        {
            return existingItemClass;
        }

        var baseObjectClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
        
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
        
        _dbProvider.Insert("objectclasses", itemClass);
        return itemClass;
    }

    /// <summary>
    /// Creates the Player class for player characters
    /// </summary>
    private ObjectClass CreatePlayerClass()
    {
        // Check if PlayerManager already created this
        var existingPlayerClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Player");
        if (existingPlayerClass != null)
        {
            return existingPlayerClass;
        }

        var baseObjectClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
        
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
        
        _dbProvider.Insert("objectclasses", playerClass);
        return playerClass;
    }

    /// <summary>
    /// Gets the base Object class
    /// </summary>
    public ObjectClass? GetBaseObjectClass()
    {
        return _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "GameObject");
    }

    /// <summary>
    /// Gets a core class by name
    /// </summary>
    public ObjectClass? GetCoreClass(string className)
    {
        return _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == className);
    }
}
