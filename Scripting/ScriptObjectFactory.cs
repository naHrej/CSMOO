using CSMOO.Commands;
using CSMOO.Database;
using CSMOO.Functions;
using CSMOO.Object;
using CSMOO.Configuration;
using CSMOO.Logging;

namespace CSMOO.Scripting;

/// <summary>
/// Factory for creating ScriptObject instances with natural syntax support
/// </summary>
public class ScriptObjectFactory
{
    private readonly Player _currentPlayer;
    private readonly CommandProcessor _commandProcessor;
    private readonly ScriptHelpers _helpers;
    private readonly IObjectManager _objectManager;
    private readonly IFunctionResolver _functionResolver;
    private readonly IDbProvider _dbProvider;

    // Primary constructor with DI dependencies
    public ScriptObjectFactory(
        Player currentPlayer, 
        CommandProcessor commandProcessor, 
        ScriptHelpers helpers,
        IObjectManager objectManager,
        IFunctionResolver functionResolver,
        IDbProvider dbProvider)
    {
        _currentPlayer = currentPlayer ?? throw new ArgumentNullException(nameof(currentPlayer));
        _commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
        _helpers = helpers ?? throw new ArgumentNullException(nameof(helpers));
        _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
        _functionResolver = functionResolver ?? throw new ArgumentNullException(nameof(functionResolver));
        _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
    }

    // Backward compatibility constructor
    public ScriptObjectFactory(Player currentPlayer, CommandProcessor commandProcessor, ScriptHelpers helpers)
        : this(currentPlayer, commandProcessor, helpers, 
               CreateDefaultObjectManager(), CreateDefaultFunctionResolver(), CreateDefaultDbProvider())
    {
    }

    private static IObjectManager CreateDefaultObjectManager()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        return new ObjectManagerInstance(dbProvider, classManager);
    }

    private static IFunctionResolver CreateDefaultFunctionResolver()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        return new FunctionResolverInstance(dbProvider, objectManager);
    }

    private static IDbProvider CreateDefaultDbProvider()
    {
        return DbProvider.Instance;
    }

    /// <summary>
    /// Create a ScriptObject for the given object reference
    /// Supports: "me", "here", "system", "#123", object names, etc.
    /// </summary>
    public dynamic? GetObject(string objectReference)
    {
        var objectId = _helpers.ResolveObject(objectReference);
        if (objectId == null) return null;
        
        return new ScriptObject(objectId, _currentPlayer, _commandProcessor, _helpers, _objectManager, _functionResolver, _dbProvider);
    }

    /// <summary>
    /// Create a ScriptObject for a direct object ID
    /// </summary>
    public dynamic? GetObjectById(string objectId)
    {
        var obj = _objectManager.GetObject(objectId);
        if (obj == null) return null;
        return new ScriptObject(objectId, _currentPlayer, _commandProcessor, _helpers, _objectManager, _functionResolver, _dbProvider);
    }
}



