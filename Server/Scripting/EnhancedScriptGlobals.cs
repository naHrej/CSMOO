using CSMOO.Server.Database;
using CSMOO.Server.Commands;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Enhanced script globals with natural object syntax support
/// </summary>
public class EnhancedScriptGlobals : ScriptGlobals
{
    private ScriptObjectFactory? _objectFactory;

    /// <summary>
    /// Initialize the object factory
    /// </summary>
    public void InitializeObjectFactory()
    {
        // Check for Helpers in base, derived class, or UnifiedScriptGlobals
        ScriptHelpers? helpers = null;
        Database.Player? dbPlayer = null;
        
        if (this is UnifiedScriptGlobals unifiedGlobals)
        {
            helpers = unifiedGlobals.Helpers;
            // Convert GameObject back to Database.Player if needed
            dbPlayer = (Database.Player?)unifiedGlobals.Player ?? 
                      DbProvider.Instance.FindById<Database.Player>("players", ((Database.Player?)unifiedGlobals.Player)?.Id ?? "");
        }
        // VerbScriptGlobals merged: handle legacy ThisObjectId
        else if (this is UnifiedScriptGlobals legacyVerbGlobals && !string.IsNullOrEmpty(legacyVerbGlobals.ThisObjectId))
        {
            helpers = legacyVerbGlobals.Helpers;
            dbPlayer = (Database.Player?)legacyVerbGlobals.Player;
        }
        else
        {
            helpers = Helpers;
            dbPlayer = Player as Database.Player;
        }
        
        if (dbPlayer != null && CommandProcessor != null && helpers != null)
        {
            _objectFactory = new ScriptObjectFactory(dbPlayer, CommandProcessor, helpers);
        }
    }

    /// <summary>
    /// Get a natural syntax object wrapper
    /// Usage: var player = obj("me"); player.Name = "value"; player:verb(args);
    /// </summary>
    public dynamic? obj(string objectReference)
    {
        return _objectFactory?.GetObject(objectReference);
    }

    /// <summary>
    /// Get a natural syntax object wrapper by ID
    /// Usage: var obj = objById("some-guid"); obj.Name = "value"; obj:verb(args);
    /// </summary>
    public dynamic? objById(string objectId)
    {
        return _objectFactory?.GetObjectById(objectId);
    }

    /// <summary>
    /// Get the "me" object (current player)
    /// Usage: var player = me; player.Name = "NewName"; player:say("Hello!");
    /// </summary>
    public dynamic? me => _objectFactory?.GetObject("me");

    /// <summary>
    /// Get the "here" object (current room)
    /// Usage: var room = here; room.Description = "A new description"; room:announce("Hello room!");
    /// </summary>
    public dynamic? here => _objectFactory?.GetObject("here");

    /// <summary>
    /// Get the "system" object
    /// Usage: var sys = system; sys:logMessage("Something happened");
    /// </summary>
    public dynamic? system => _objectFactory?.GetObject("system");
}
