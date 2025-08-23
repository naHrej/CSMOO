using CSMOO.Database;
using CSMOO.Object;

namespace CSMOO.Scripting;

/// <summary>
/// Script-safe wrapper for PlayerManager
/// </summary>
public class ScriptPlayerManager
{
    public List<string> GetOnlinePlayerNames()
    {
        return [.. PlayerManager.GetOnlinePlayers().Select(p => p.Name)];
    }

    public string? GetPlayerLocation(string playerName)
    {
        var player = ObjectManager.GetAllObjects().FirstOrDefault(p => p.Name == playerName && p is Player);
        return player?.Location?.Id;
    }
}



