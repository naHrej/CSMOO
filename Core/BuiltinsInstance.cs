using CSMOO.Database;
using CSMOO.Functions;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Scripting;
using CSMOO.Verbs;

namespace CSMOO.Core;

/// <summary>
/// Instance-based Builtins implementation for dependency injection
/// </summary>
public class BuiltinsInstance : IBuiltinsInstance
{
    public IObjectManager ObjectManager { get; }
    public IPlayerManager PlayerManager { get; }
    public IPermissionManager PermissionManager { get; }
    public IFunctionResolver FunctionResolver { get; }
    public IVerbResolver VerbResolver { get; }
    public IVerbManager VerbManager { get; }
    public IRoomManager RoomManager { get; }
    public ILogger Logger { get; }
    public IScriptEngineFactory ScriptEngineFactory { get; }

    public BuiltinsInstance(
        IObjectManager objectManager,
        IPlayerManager playerManager,
        IPermissionManager permissionManager,
        IFunctionResolver functionResolver,
        IVerbResolver verbResolver,
        IVerbManager verbManager,
        IRoomManager roomManager,
        ILogger logger,
        IScriptEngineFactory scriptEngineFactory)
    {
        ObjectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
        PlayerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
        PermissionManager = permissionManager ?? throw new ArgumentNullException(nameof(permissionManager));
        FunctionResolver = functionResolver ?? throw new ArgumentNullException(nameof(functionResolver));
        VerbResolver = verbResolver ?? throw new ArgumentNullException(nameof(verbResolver));
        VerbManager = verbManager ?? throw new ArgumentNullException(nameof(verbManager));
        RoomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ScriptEngineFactory = scriptEngineFactory ?? throw new ArgumentNullException(nameof(scriptEngineFactory));
    }
}
