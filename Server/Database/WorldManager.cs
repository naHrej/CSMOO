using System.Collections.Generic;
using System.Linq;
using CSMOO.Server.Database.World;

namespace CSMOO.Server.Database;

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
    public static void CreateExit(string fromRoomId, string toRoomId, string direction, string returnDirection)
    {
        RoomManager.CreateExit(fromRoomId, toRoomId, direction, returnDirection);
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
    public static List<GameObject> GetExitsFromRoom(string roomId)
    {
        return RoomManager.GetExitsFromRoom(roomId);
    }
}
