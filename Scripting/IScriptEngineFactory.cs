using CSMOO.Core;
using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Configuration;
using CSMOO.Object;
using CSMOO.Verbs;
using CSMOO.Functions;

namespace CSMOO.Scripting;

/// <summary>
/// Factory interface for creating ScriptEngine instances
/// </summary>
public interface IScriptEngineFactory
{
    /// <summary>
    /// Creates a new ScriptEngine instance
    /// </summary>
    ScriptEngine Create();
}

/// <summary>
/// Default implementation of IScriptEngineFactory
/// </summary>
public class ScriptEngineFactory : IScriptEngineFactory
{
    private readonly IObjectManager _objectManager;
    private readonly ILogger _logger;
    private readonly IConfig _config;
    private readonly IObjectResolver _objectResolver;
    private readonly IVerbResolver _verbResolver;
    private readonly IFunctionResolver _functionResolver;
    private readonly IDbProvider _dbProvider;
    private readonly IPlayerManager _playerManager;
    private readonly IVerbManager _verbManager;
    private readonly IRoomManager _roomManager;
    private readonly ICompilationCache _compilationCache;

    // Primary constructor with DI dependencies
    public ScriptEngineFactory(
        IObjectManager objectManager,
        ILogger logger,
        IConfig config,
        IObjectResolver objectResolver,
        IVerbResolver verbResolver,
        IFunctionResolver functionResolver,
        IDbProvider dbProvider,
        IPlayerManager playerManager,
        IVerbManager verbManager,
        IRoomManager roomManager,
        ICompilationCache compilationCache)
    {
        _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _objectResolver = objectResolver ?? throw new ArgumentNullException(nameof(objectResolver));
        _verbResolver = verbResolver ?? throw new ArgumentNullException(nameof(verbResolver));
        _functionResolver = functionResolver ?? throw new ArgumentNullException(nameof(functionResolver));
        _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
        _verbManager = verbManager ?? throw new ArgumentNullException(nameof(verbManager));
        _roomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
        _compilationCache = compilationCache ?? throw new ArgumentNullException(nameof(compilationCache));
    }


    public ScriptEngine Create()
    {
        return new ScriptEngine(_objectManager, _logger, _config, _objectResolver, _verbResolver, _functionResolver, _dbProvider, _playerManager, _verbManager, _roomManager, _compilationCache);
    }
}

