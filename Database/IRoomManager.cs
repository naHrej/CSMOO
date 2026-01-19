using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Interface for room management operations
/// </summary>
public interface IRoomManager
{
    /// <summary>
    /// Creates the starting room and some basic areas
    /// </summary>
    void CreateStartingRoom();
    
    /// <summary>
    /// Creates bidirectional exits between two rooms
    /// </summary>
    void CreateExit(GameObject fromRoom, GameObject toRoom, string direction, string returnDirection);
    
    /// <summary>
    /// Creates a simple item in the world
    /// </summary>
    GameObject CreateSimpleItem(string name, string shortDesc, string longDesc, string? locationId = null);
    
    /// <summary>
    /// Gets the default starting room for new players
    /// </summary>
    GameObject? GetStartingRoom();
    
    /// <summary>
    /// Gets all rooms in the world
    /// </summary>
    List<GameObject> GetAllRooms();
    
    /// <summary>
    /// Gets all exits from a room
    /// </summary>
    List<GameObject> GetExits(string roomId);
    
    /// <summary>
    /// Finds an exit in a specific direction from a room
    /// </summary>
    GameObject? FindExitInDirection(string roomId, string direction);
    
    /// <summary>
    /// Gets the destination room from an exit
    /// </summary>
    GameObject? GetExitDestination(GameObject exit);
    
    /// <summary>
    /// Checks if a room exists and is valid
    /// </summary>
    bool IsValidRoom(string? roomId);
    
    /// <summary>
    /// Gets all exits from a room
    /// </summary>
    List<dynamic> GetExits(GameObject room);
    
    /// <summary>
    /// Gets all items in a room (excludes exits and players)
    /// </summary>
    List<dynamic> GetItems(GameObject room);
    
    /// <summary>
    /// Gets all players in a room
    /// </summary>
    List<dynamic> GetPlayers(GameObject room);
    
    /// <summary>
    /// Gets basic statistics about rooms in the world
    /// </summary>
    Dictionary<string, object> GetRoomStatistics();
}
