using System.Collections.Generic;
using System.Linq;
using CSMOO.Server.Database;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Script-safe wrapper for WorldManager
/// </summary>
public class ScriptWorldManager
{
    public string? GetStartingRoom()
    {
        return Database.WorldManager.GetStartingRoom()?.Id;
    }

    public List<string> GetAllRooms()
    {
        return [.. Database.WorldManager.GetAllRooms().Select(room => room.Id)];
    }

    public void CreateExit(GameObject fromRoom, GameObject toRoom, string direction, string returnDirection)
    {
        Database.WorldManager.CreateExit(fromRoom, toRoom, direction, returnDirection);
    }

    public string CreateSimpleItem(string name, string shortDesc, string longDesc, string? locationId = null)
    {
        return Database.WorldManager.CreateSimpleItem(name, shortDesc, longDesc, locationId).Id;
    }
}
