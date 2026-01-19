using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Configuration;

namespace CSMOO.Database;

/// <summary>
/// Static wrapper for RoomManager (backward compatibility)
/// Delegates to RoomManagerInstance for dependency injection support
/// </summary>
public static class RoomManager
{
    private static IRoomManager? _instance;
    
    /// <summary>
    /// Sets the room manager instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IRoomManager instance)
    {
        _instance = instance;
    }
    
    private static IRoomManager Instance => _instance ?? throw new InvalidOperationException("RoomManager instance not set. Call RoomManager.SetInstance() first. Static access is no longer supported - use dependency injection.");
    /// <summary>
    /// Creates the starting room and some basic areas
    /// </summary>
    public static void CreateStartingRoom()
    {
        Instance.CreateStartingRoom();
    }

    /// <summary>
    /// Creates bidirectional exits between two rooms
    /// </summary>
    public static void CreateExit(GameObject fromRoom, GameObject toRoom, string direction, string returnDirection)
    {
        Instance.CreateExit(fromRoom, toRoom, direction, returnDirection);
    }

    /// <summary>
    /// Creates a simple item in the world
    /// </summary>
    public static GameObject CreateSimpleItem(string name, string shortDesc, string longDesc, string? locationId = null)
    {
        return Instance.CreateSimpleItem(name, shortDesc, longDesc, locationId);
    }

    /// <summary>
    /// Gets the default starting room for new players
    /// </summary>
    public static GameObject? GetStartingRoom()
    {
        return Instance.GetStartingRoom();
    }

    /// <summary>
    /// Gets all rooms in the world
    /// </summary>
    public static List<GameObject> GetAllRooms()
    {
        return Instance.GetAllRooms();
    }

    /// <summary>
    /// Gets all exits from a room
    /// </summary>
    public static List<GameObject> GetExits(string roomId)
    {
        return Instance.GetExits(roomId);
    }

    /// <summary>
    /// Finds an exit in a specific direction from a room
    /// </summary>
    public static GameObject? FindExitInDirection(string roomId, string direction)
    {
        return Instance.FindExitInDirection(roomId, direction);
    }

    /// <summary>
    /// Gets the destination room from an exit
    /// </summary>
    public static GameObject? GetExitDestination(GameObject exit)
    {
        return Instance.GetExitDestination(exit);
    }

    /// <summary>
    /// Checks if a room exists and is valid
    /// </summary>
    public static bool IsValidRoom(string? roomId)
    {
        return Instance.IsValidRoom(roomId);
    }

    /// <summary>
    /// Gets all exits from a room
    /// </summary>
    public static List<dynamic> GetExits(GameObject room)
    {
        return Instance.GetExits(room);
    }
    
    /// <summary>
    /// Gets all items in a room (excludes exits and players)
    /// </summary>
    public static List<dynamic> GetItems(GameObject room)
    {
        return Instance.GetItems(room);
    }
    
    /// <summary>
    /// Gets all players in a room
    /// </summary>
    public static List<dynamic> GetPlayers(GameObject room)
    {
        return Instance.GetPlayers(room);
    }

    /// <summary>
    /// Gets basic statistics about rooms in the world
    /// </summary>
    public static Dictionary<string, object> GetRoomStatistics()
    {
        return Instance.GetRoomStatistics();
    }
}



