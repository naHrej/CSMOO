using CSMOO.Database;
using CSMOO.Object;

namespace CSMOO.Scripting;

/// <summary>
/// Script-safe wrapper for RoomManager and WorldInitializer
/// Returns string IDs instead of GameObject references (script-safe)
/// </summary>
public class ScriptWorldManager
{
    public string? GetStartingRoom()
    {
        return RoomManager.GetStartingRoom()?.Id;
    }

    public List<string> GetAllRooms()
    {
        return [.. RoomManager.GetAllRooms().Select(room => room.Id)];
    }

    public void CreateExit(GameObject fromRoom, GameObject toRoom, string direction, string returnDirection)
    {
        RoomManager.CreateExit(fromRoom, toRoom, direction, returnDirection);
    }

    public string CreateSimpleItem(string name, string shortDesc, string longDesc, string? locationId = null)
    {
        return RoomManager.CreateSimpleItem(name, shortDesc, longDesc, locationId).Id;
    }
}



