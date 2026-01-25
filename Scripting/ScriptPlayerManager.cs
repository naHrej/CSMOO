using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Configuration;
using CSMOO.Logging;

namespace CSMOO.Scripting;

/// <summary>
/// Script-safe wrapper for PlayerManager
/// </summary>
public class ScriptPlayerManager
{
    private readonly IPlayerManager _playerManager;
    private readonly IObjectManager _objectManager;

    // Primary constructor with DI dependencies
    public ScriptPlayerManager(IPlayerManager playerManager, IObjectManager objectManager)
    {
        _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
        _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
        
        // PlayerManagerInstance requires ObjectManager to be set via SetObjectManager
        if (playerManager is PlayerManagerInstance pmi)
        {
            pmi.SetObjectManager(objectManager);
        }
    }


    public List<string> GetOnlinePlayerNames()
    {
        return [.. _playerManager.GetOnlinePlayers().Select(p => p.Name)];
    }

    public string? GetPlayerLocation(string playerName)
    {
        var player = _objectManager.GetAllObjects().FirstOrDefault(p => p.Name == playerName && p is Player);
        return player?.Location?.Id;
    }
}



