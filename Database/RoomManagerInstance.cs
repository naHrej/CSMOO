using CSMOO.Logging;
using CSMOO.Object;
using LiteDB;

namespace CSMOO.Database;

/// <summary>
/// Instance-based room manager implementation for dependency injection
/// </summary>
public class RoomManagerInstance : IRoomManager
{
    private readonly IDbProvider _dbProvider;
    private readonly ILogger _logger;
    private readonly IObjectManager _objectManager;
    
    public RoomManagerInstance(IDbProvider dbProvider, ILogger logger, IObjectManager objectManager)
    {
        _dbProvider = dbProvider;
        _logger = logger;
        _objectManager = objectManager;
    }
    
    /// <summary>
    /// Creates the starting room and some basic areas
    /// </summary>
    public void CreateStartingRoom()
    {
        // Check if starting room already exists
        var existingStartingRoom = FindStartingRoom();
        if (existingStartingRoom != null)
        {
            return;
        }

        _logger.Info("Creating starting room and basic world areas...");

        var roomClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Room");
        if (roomClass == null)
        {
            _logger.Error("Room class not found. Core classes must be created first.");
            return;
        }

        // Create the starting room
        var startingRoom = _objectManager.CreateInstance(roomClass.Id);
        _objectManager.SetProperty(startingRoom, "name", new BsonValue("The Nexus"));
        _objectManager.SetProperty(startingRoom, "shortDescription", new BsonValue("the Nexus"));
        _objectManager.SetProperty(startingRoom, "longDescription", new BsonValue(
            "This is the central hub of the CSMOO world. A shimmering portal of energy " +
            "connects this place to all other realms. New adventurers often find themselves " +
            "here when they first enter the world."));
        _objectManager.SetProperty(startingRoom, "isStartingRoom", new BsonValue(true));

        // Create a simple connected room
        var secondRoom = _objectManager.CreateInstance(roomClass.Id);
        _objectManager.SetProperty(secondRoom, "name", new BsonValue("A Peaceful Grove"));
        _objectManager.SetProperty(secondRoom, "shortDescription", new BsonValue("a peaceful grove"));
        _objectManager.SetProperty(secondRoom, "longDescription", new BsonValue(
            "A tranquil grove surrounded by ancient oak trees. Sunlight filters through " +
            "the canopy above, creating dancing patterns on the soft grass below. " +
            "A gentle breeze carries the scent of wildflowers."));
        // Set the location of the second room
        _objectManager.SetProperty(secondRoom, "location", BsonValue.Null);

        // Create exits between the rooms
        CreateExit(startingRoom, secondRoom, "North", "South");

        // Create a simple item in the grove
        CreateSimpleItem("A Wooden Staff", "a wooden staff", 
            "A simple wooden staff, worn smooth by countless hands. It radiates a faint magical aura.",
            secondRoom.Id);

        _logger.Info("Starting room and basic areas created successfully");
    }

    /// <summary>
    /// Creates bidirectional exits between two rooms
    /// </summary>
    public void CreateExit(GameObject fromRoom, GameObject toRoom, string direction, string returnDirection)
    {
        var exitClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Exit");
        if (exitClass == null)
        {
            _logger.Error("Exit class not found. Core classes must be created first.");
            return;
        }

        // Create the forward exit
        var forwardExit = _objectManager.CreateInstance(exitClass.Id, fromRoom.Id);
        _objectManager.SetProperty(forwardExit, "name", new BsonValue(direction));
        _objectManager.SetProperty(forwardExit, "shortDescription", new BsonValue(direction));
        _objectManager.SetProperty(forwardExit, "longDescription", new BsonValue($"An exit leading {direction}."));
        _objectManager.SetProperty(forwardExit, "direction", new BsonValue(direction));
        _objectManager.SetProperty(forwardExit, "destination", new BsonValue(toRoom.Id));

        // Create the return exit
        var returnExit = _objectManager.CreateInstance(exitClass.Id, toRoom.Id);
        _objectManager.SetProperty(returnExit, "name", new BsonValue(returnDirection));
        _objectManager.SetProperty(returnExit, "shortDescription", new BsonValue(returnDirection));
        _objectManager.SetProperty(returnExit, "longDescription", new BsonValue($"An exit leading {returnDirection}."));
        _objectManager.SetProperty(returnExit, "direction", new BsonValue(returnDirection));
        _objectManager.SetProperty(returnExit, "destination", new BsonValue(fromRoom.Id));
        // Set location for both exits
        _objectManager.SetProperty(forwardExit, "location", new BsonValue(fromRoom.Id));
        _objectManager.SetProperty(returnExit, "location", new BsonValue(toRoom.Id));
    }

    /// <summary>
    /// Creates a simple item in the world
    /// </summary>
    public GameObject CreateSimpleItem(string name, string shortDesc, string longDesc, string? locationId = null)
    {
        var itemClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Item");
        if (itemClass == null)
            throw new InvalidOperationException("Item class not found. Core classes must be created first.");

        var item = _objectManager.CreateInstance(itemClass.Id, locationId);
        _objectManager.SetProperty(item, "name", new BsonValue(name));
        _objectManager.SetProperty(item, "shortDescription", new BsonValue(shortDesc));
        _objectManager.SetProperty(item, "longDescription", new BsonValue(longDesc));
        _objectManager.SetProperty(item, "location", locationId != null ? new BsonValue(locationId) : BsonValue.Null);

        return item;
    }

    /// <summary>
    /// Gets the default starting room for new players
    /// </summary>
    public GameObject? GetStartingRoom()
    {
        return FindStartingRoom();
    }

    /// <summary>
    /// Finds the starting room
    /// </summary>
    private GameObject? FindStartingRoom()
    {
        var allGameObjects = _dbProvider.FindAll<GameObject>("gameobjects");
        return allGameObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isStartingRoom") && obj.Properties["isStartingRoom"].AsBoolean == true);
    }

    /// <summary>
    /// Gets all rooms in the world
    /// </summary>
    public List<GameObject> GetAllRooms()
    {
        var roomClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Room");
        if (roomClass == null) return new List<GameObject>();

        return _objectManager.FindObjectsByClass(roomClass.Id);
    }

    /// <summary>
    /// Gets all exits from a room
    /// </summary>
    public List<GameObject> GetExits(string roomId)
    {
        var exitClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Exit");
        if (exitClass == null) return new List<GameObject>();

        return _objectManager.GetObjectsInLocation(roomId)
            .Where(obj => obj.ClassId == exitClass.Id)
            .ToList();
    }

    /// <summary>
    /// Finds an exit in a specific direction from a room
    /// </summary>
    public GameObject? FindExitInDirection(string roomId, string direction)
    {
        var exits = GetExits(roomId);
        return exits.FirstOrDefault(exit =>
        {
            var exitDirection = _objectManager.GetProperty(exit, "direction")?.AsString?.ToLower();
            return exitDirection == direction.ToLower();
        });
    }

    /// <summary>
    /// Gets the destination room from an exit
    /// </summary>
    public GameObject? GetExitDestination(GameObject exit)
    {
        var destinationId = _objectManager.GetProperty(exit, "destination")?.AsString;
        return destinationId == null ? null : _objectManager.GetObject(destinationId);
    }

    /// <summary>
    /// Checks if a room exists and is valid
    /// </summary>
    public bool IsValidRoom(string? roomId)
    {
        if (roomId == null) return false;
        
        var room = _objectManager.GetObject(roomId);
        if (room == null) return false;

        var roomClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Room");
        return roomClass != null && _objectManager.InheritsFrom(room.ClassId, roomClass.Id);
    }

    /// <summary>
    /// Gets all exits from a room
    /// </summary>
    public List<dynamic> GetExits(GameObject room)
    {
        var roomContents = _objectManager.GetObjectsInLocation(room).Cast<dynamic>().ToList();
        var exitClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Exit");
        if (exitClass == null) return [];
        return roomContents
            .Where(obj => obj.ClassId == exitClass.Id)
            .ToList();
    }
    
    /// <summary>
    /// Gets all items in a room (excludes exits and players)
    /// </summary>
    public List<dynamic> GetItems(GameObject room)
    {
        var roomContents = _objectManager.GetObjectsInLocation(room).Cast<dynamic>().ToList();
        var itemClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Item");      
        if (itemClass == null) return [];
        return roomContents
            .Where(obj => obj.ClassId == itemClass.Id)
            .ToList();
    }
    
    /// <summary>
    /// Gets all players in a room
    /// </summary>
    public List<dynamic> GetPlayers(GameObject room)
    {
        var roomContents = _objectManager.GetObjectsInLocation(room).Cast<dynamic>().ToList();
        var playerClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Player");
        if (playerClass == null) return [];
        return roomContents.Where(obj =>
            _objectManager.InheritsFrom(obj.ClassId, playerClass.Id)
        ).ToList();
    }

    /// <summary>
    /// Gets basic statistics about rooms in the world
    /// </summary>
    public Dictionary<string, object> GetRoomStatistics()
    {
        var allRooms = GetAllRooms();
        var stats = new Dictionary<string, object>
        {
            ["TotalRooms"] = allRooms.Count,
            ["RoomsWithItems"] = allRooms.Count(room => GetItems(room).Any()),
            ["RoomsWithPlayers"] = allRooms.Count(room => GetPlayers(room).Any()),
            ["TotalExits"] = allRooms.Sum(room => GetExits(room.Id).Count)
        };

        return stats;
    }
}
