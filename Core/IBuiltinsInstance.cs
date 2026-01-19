using CSMOO.Database;
using CSMOO.Functions;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Scripting;
using CSMOO.Verbs;

namespace CSMOO.Core;

/// <summary>
/// Interface for Builtins instance implementation
/// </summary>
public interface IBuiltinsInstance
{
    IObjectManager ObjectManager { get; }
    IPlayerManager PlayerManager { get; }
    IPermissionManager PermissionManager { get; }
    IFunctionResolver FunctionResolver { get; }
    IVerbManager VerbManager { get; }
    IRoomManager RoomManager { get; }
    ILogger Logger { get; }
    IScriptEngineFactory ScriptEngineFactory { get; }
}
