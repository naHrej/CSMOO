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
        IRoomManager roomManager)
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
    }

    // Backward compatibility constructor
    public ScriptEngineFactory()
        : this(ScriptEngineFactoryStatic.CreateDefaultObjectManager(), ScriptEngineFactoryStatic.CreateDefaultLogger(), ScriptEngineFactoryStatic.CreateDefaultConfig(), ScriptEngineFactoryStatic.CreateDefaultObjectResolver(),
               ScriptEngineFactoryStatic.CreateDefaultVerbResolver(), ScriptEngineFactoryStatic.CreateDefaultFunctionResolver(), ScriptEngineFactoryStatic.CreateDefaultDbProvider(),
               ScriptEngineFactoryStatic.CreateDefaultPlayerManager(), ScriptEngineFactoryStatic.CreateDefaultVerbManager(), ScriptEngineFactoryStatic.CreateDefaultRoomManager())
    {
    }

    public ScriptEngine Create()
    {
        return new ScriptEngine(_objectManager, _logger, _config, _objectResolver, _verbResolver, _functionResolver, _dbProvider, _playerManager, _verbManager, _roomManager);
    }
}

/// <summary>
/// Static wrapper for ScriptEngineFactory (backward compatibility)
/// Delegates to ScriptEngineFactoryInstance for dependency injection support
/// </summary>
public static class ScriptEngineFactoryStatic
{
    private static IScriptEngineFactory? _instance;
    
    /// <summary>
    /// Sets the script engine factory instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IScriptEngineFactory instance)
    {
        _instance = instance;
    }
    
    private static IScriptEngineFactory Instance
    {
        get
        {
            if (_instance != null) return _instance;
            // Create default instance for backward compatibility
            return new ScriptEngineFactory();
        }
    }
    
    /// <summary>
    /// Creates a new ScriptEngine instance
    /// </summary>
    public static ScriptEngine Create()
    {
        return Instance.Create();
    }
    
    // Helper methods for backward compatibility
    internal static IObjectManager CreateDefaultObjectManager()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        return new ObjectManagerInstance(dbProvider, classManager);
    }

    internal static ILogger CreateDefaultLogger()
    {
        return new LoggerInstance(Config.Instance);
    }

    internal static IConfig CreateDefaultConfig()
    {
        return Config.Instance;
    }

    internal static IObjectResolver CreateDefaultObjectResolver()
    {
        var dbProvider = DbProvider.Instance;
        var config = Config.Instance;
        var logger = new LoggerInstance(config);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        var coreClassFactory = new CoreClassFactoryInstance(dbProvider, logger);
        return new ObjectResolverInstance(objectManager, coreClassFactory);
    }

    internal static IVerbResolver CreateDefaultVerbResolver()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        return new VerbResolverInstance(dbProvider, objectManager, logger);
    }

    internal static IFunctionResolver CreateDefaultFunctionResolver()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        return new FunctionResolverInstance(dbProvider, objectManager);
    }

    internal static IDbProvider CreateDefaultDbProvider()
    {
        return DbProvider.Instance;
    }

    internal static IPlayerManager CreateDefaultPlayerManager()
    {
        return new PlayerManagerInstance(DbProvider.Instance);
    }

    internal static IVerbManager CreateDefaultVerbManager()
    {
        return new VerbManagerInstance(DbProvider.Instance);
    }

    internal static IRoomManager CreateDefaultRoomManager()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        return new RoomManagerInstance(dbProvider, logger, objectManager);
    }
}
