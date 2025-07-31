using System.Collections.Generic;
using System.Linq;
using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Legacy wrapper for WorldInitializer to maintain compatibility
/// </summary>
public static class WorldManager
{
    /// <summary>
    /// Initializes the basic world structure with core classes
    /// </summary>
    public static void InitializeWorld()
    {
        WorldInitializer.InitializeWorld();
    }

    /// <summary>
    /// Creates bidirectional exits between two rooms
    /// </summary>
    public static void CreateExit(GameObject fromRoom, GameObject toRoom, string direction, string returnDirection)
    {
        RoomManager.CreateExit(fromRoom, toRoom, direction, returnDirection);
    }

    /// <summary>
    /// Creates a simple item in the world
    /// </summary>
    public static GameObject CreateSimpleItem(string name, string shortDesc, string longDesc, string? locationId = null)
    {
        return RoomManager.CreateSimpleItem(name, shortDesc, longDesc, locationId);
    }

    /// <summary>
    /// Gets the default starting room for new players
    /// </summary>
    public static GameObject? GetStartingRoom()
    {
        return RoomManager.GetStartingRoom();
    }

    /// <summary>
    /// Gets all rooms in the world
    /// </summary>
    public static List<GameObject> GetAllRooms()
    {
        return RoomManager.GetAllRooms();
    }

    /// <summary>
    /// Gets all exits from a room
    /// </summary>
    public static List<dynamic> GetExits(string roomId)
    {
        return RoomManager.GetExits(roomId).Cast<dynamic>().ToList();
    }
        /// <summary>
    /// Gets all exits from a room
    /// </summary>
    public static List<dynamic> GetExits(GameObject room)
    {
        return RoomManager.GetExits(room).Cast<dynamic>().ToList();
    }
}



