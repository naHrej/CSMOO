using System.Collections.Generic;
using System.Linq;
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
        var player = DbProvider.Instance.FindOne<Player>("players", p => p.Name == playerName);
        return player?.Location?.Id;
    }
}



