using CSMOO.Logging;
using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Manages room creation, navigation, and related utilities
/// </summary>
public static class RoomManager
{
    /// <summary>
    /// Creates the starting room and some basic areas
    /// </summary>
    public static void CreateStartingRoom()
    {
        // Check if starting room already exists
        var existingStartingRoom = FindStartingRoom();
        if (existingStartingRoom != null)
        {
            Logger.Debug("Starting room already exists, skipping creation");
            return;
        }

        Logger.Info("Creating starting room and basic world areas...");

        var roomClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Room");
        if (roomClass == null)
        {
            Logger.Error("Room class not found. Core classes must be created first.");
            return;
        }

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
        // Set the location of the second room
        ObjectManager.SetProperty(secondRoom, "location", null!);

        // Create exits between the rooms
        CreateExit(startingRoom, secondRoom, "north", "south");

        // Create a simple item in the grove
        CreateSimpleItem("A Wooden Staff", "a wooden staff", 
            "A simple wooden staff, worn smooth by countless hands. It radiates a faint magical aura.",
            secondRoom.Id);

        Logger.Info("Starting room and basic areas created successfully");
    }

    /// <summary>
    /// Creates bidirectional exits between two rooms
    /// </summary>
    public static void CreateExit(GameObject fromRoom, GameObject toRoom, string direction, string returnDirection)
    {
        var exitClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Exit");
        if (exitClass == null)
        {
            Logger.Error("Exit class not found. Core classes must be created first.");
            return;
        }

        // Create the forward exit
        var forwardExit = ObjectManager.CreateInstance(exitClass.Id, fromRoom.Id);
        ObjectManager.SetProperty(forwardExit, "name", direction);
        ObjectManager.SetProperty(forwardExit, "shortDescription", direction);
        ObjectManager.SetProperty(forwardExit, "longDescription", $"An exit leading {direction}.");
        ObjectManager.SetProperty(forwardExit, "direction", direction);
        ObjectManager.SetProperty(forwardExit, "destination", toRoom.Id);

        // Create the return exit
        var returnExit = ObjectManager.CreateInstance(exitClass.Id, toRoom.Id);
        ObjectManager.SetProperty(returnExit, "name", returnDirection);
        ObjectManager.SetProperty(returnExit, "shortDescription", returnDirection);
        ObjectManager.SetProperty(returnExit, "longDescription", $"An exit leading {returnDirection}.");
        ObjectManager.SetProperty(returnExit, "direction", returnDirection);
        ObjectManager.SetProperty(returnExit, "destination", fromRoom.Id);
        // Set location for both exits
        ObjectManager.SetProperty(forwardExit, "location", fromRoom.Id);
        ObjectManager.SetProperty(returnExit, "location", toRoom.Id);

        Logger.Debug($"Created bidirectional exit: {direction} from #{fromRoom.Id} to #{toRoom.Id}");
    }

    /// <summary>
    /// Creates a simple item in the world
    /// </summary>
    public static GameObject CreateSimpleItem(string name, string shortDesc, string longDesc, string? locationId = null)
    {
        var itemClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Item");
        if (itemClass == null)
            throw new InvalidOperationException("Item class not found. Core classes must be created first.");

        var item = ObjectManager.CreateInstance(itemClass.Id, locationId);
        ObjectManager.SetProperty(item, "name", name);
        ObjectManager.SetProperty(item, "shortDescription", shortDesc);
        ObjectManager.SetProperty(item, "longDescription", longDesc);
        ObjectManager.SetProperty(item, "location", locationId);

        Logger.Debug($"Created item '{name}' in location #{locationId}");
        return item;
    }

    /// <summary>
    /// Gets the default starting room for new players
    /// </summary>
    public static GameObject? GetStartingRoom()
    {
        return FindStartingRoom();
    }

    /// <summary>
    /// Finds the starting room
    /// </summary>
    private static GameObject? FindStartingRoom()
    {
        var allGameObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects");
        return allGameObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isStartingRoom") && obj.Properties["isStartingRoom"].AsBoolean == true);
    }

    /// <summary>
    /// Gets all rooms in the world
    /// </summary>
    public static List<GameObject> GetAllRooms()
    {
        var roomClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Room");
        if (roomClass == null) return new List<GameObject>();

        return ObjectManager.FindObjectsByClass(roomClass.Id);
    }

    /// <summary>
    /// Gets all exits from a room
    /// </summary>
    public static List<GameObject> GetExits(string roomId)
    {
        var exitClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Exit");
        if (exitClass == null) return new List<GameObject>();

        return ObjectManager.GetObjectsInLocation(roomId)
            .Where(obj => obj.ClassId == exitClass.Id)
            .ToList();
    }
    /// <summary>
    /// Gets all exits from a room
    /// </summary>
    public static List<GameObject> GetExits(GameObject room)
    {
        var exitClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Exit");
        if (exitClass == null) return new List<GameObject>();

        return ObjectManager.GetObjectsInLocation(room)
            .Where(obj => obj.ClassId == exitClass.Id)
            .ToList();
    }
    
    /// <summary>
    /// Finds an exit in a specific direction from a room
    /// </summary>
    public static GameObject? FindExitInDirection(string roomId, string direction)
    {
        var exits = GetExits(roomId);
        return exits.FirstOrDefault(exit =>
        {
            var exitDirection = ObjectManager.GetProperty(exit, "direction")?.AsString?.ToLower();
            return exitDirection == direction.ToLower();
        });
    }

    /// <summary>
    /// Gets the destination room from an exit
    /// </summary>
    public static GameObject? GetExitDestination(GameObject exit)
    {
        var destinationId = ObjectManager.GetProperty(exit, "destination")?.AsString;
        return destinationId == null ? null : ObjectManager.GetObject(destinationId);
    }

    /// <summary>
    /// Checks if a room exists and is valid
    /// </summary>
    public static bool IsValidRoom(string? roomId)
    {
        if (roomId == null) return false;
        
        var room = ObjectManager.GetObject(roomId);
        if (room == null) return false;

        var roomClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Room");
        return roomClass != null && ObjectManager.InheritsFrom(room.ClassId, roomClass.Id);
    }

    /// <summary>
    /// Gets all items in a room (excludes exits and players)
    /// </summary>
    public static List<GameObject> GetItemsInRoom(string roomId)
    {
        var roomContents = ObjectManager.GetObjectsInLocation(roomId);
        var exitClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Exit");
        var playerClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Player");

        return roomContents.Where(obj => 
            (exitClass == null || obj.ClassId != exitClass.Id) &&
            (playerClass == null || !ObjectManager.InheritsFrom(obj.ClassId, playerClass.Id))
        ).ToList();
    }

    /// <summary>
    /// Gets all players in a room
    /// </summary>
    public static List<GameObject> GetPlayersInRoom(string roomId)
    {
        var roomContents = ObjectManager.GetObjectsInLocation(roomId);
        var playerClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Player");
        
        if (playerClass == null) return new List<GameObject>();

        return roomContents.Where(obj => 
            ObjectManager.InheritsFrom(obj.ClassId, playerClass.Id)
        ).ToList();
    }

    /// <summary>
    /// Gets basic statistics about rooms in the world
    /// </summary>
    public static Dictionary<string, object> GetRoomStatistics()
    {
        var allRooms = GetAllRooms();
        var stats = new Dictionary<string, object>
        {
            ["TotalRooms"] = allRooms.Count,
            ["RoomsWithItems"] = allRooms.Count(room => GetItemsInRoom(room.Id).Any()),
            ["RoomsWithPlayers"] = allRooms.Count(room => GetPlayersInRoom(room.Id).Any()),
            ["TotalExits"] = allRooms.Sum(room => GetExits(room.Id).Count)
        };

        return stats;
    }
}



