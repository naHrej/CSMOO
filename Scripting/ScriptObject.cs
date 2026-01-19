using System.Dynamic;
using CSMOO.Database;
using CSMOO.Commands;
using CSMOO.Functions;
using CSMOO.Logging;
using CSMOO.Configuration;
using CSMOO.Exceptions;
using LiteDB;
using CSMOO.Object;

namespace CSMOO.Scripting;

/// <summary>
/// Dynamic object wrapper that provides natural syntax for object property access and verb calls
/// Supports syntax like: player.Name, player.Name = "value", player:verbname(args)
/// </summary>
public class ScriptObject : DynamicObject
{
    private readonly string _objectId;
    private readonly Player _currentPlayer;
    private readonly CommandProcessor _commandProcessor;
    private readonly ScriptHelpers _helpers;
    private readonly IObjectManager _objectManager;
    private readonly IFunctionResolver _functionResolver;
    private readonly IDbProvider _dbProvider;

    // Primary constructor with DI dependencies
    public ScriptObject(
        string objectId, 
        Player currentPlayer, 
        CommandProcessor commandProcessor, 
        ScriptHelpers helpers,
        IObjectManager objectManager,
        IFunctionResolver functionResolver,
        IDbProvider dbProvider)
    {
        _objectId = objectId ?? throw new ArgumentNullException(nameof(objectId));
        _currentPlayer = currentPlayer ?? throw new ArgumentNullException(nameof(currentPlayer));
        _commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
        _helpers = helpers ?? throw new ArgumentNullException(nameof(helpers));
        _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
        _functionResolver = functionResolver ?? throw new ArgumentNullException(nameof(functionResolver));
        _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
    }

    // Backward compatibility constructor
    public ScriptObject(string objectId, Player currentPlayer, CommandProcessor commandProcessor, ScriptHelpers helpers)
        : this(objectId, currentPlayer, commandProcessor, helpers, 
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
    /// The object ID this wrapper represents
    /// </summary>
    public string ObjectId => _objectId;

    /// <summary>
    /// Get the actual GameObject
    /// </summary>
    public GameObject? GetGameObject()
    {
        // Use injected ObjectManager
        return _objectManager.GetObject(_objectId);
    }

    /// <summary>
    /// Handles property getting: player.Name
    /// </summary>
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        var propertyName = binder.Name;
        
        try
        {
            var obj = GetGameObject();
            if (obj == null)
            {
                throw new ArgumentException($"Object {_objectId} not found");
            }

            // Direct database lookup for the property
            var propertyValue = _objectManager.GetProperty(obj, propertyName);
            
            if (propertyValue == null)
            {
                throw new ArgumentException($"Property '{propertyName}' not found on object {_objectId}");
            }
            
            // Convert BsonValue to appropriate C# type
            result = propertyValue.RawValue;
            return true;
        }
        catch (Exception ex)
        {
            // Let the error bubble up to the script engine
            throw new InvalidOperationException($"Error accessing property '{propertyName}' on object {_objectId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Handles property setting: player.Name = "value"
    /// </summary>
    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        var propertyName = binder.Name;
        
        try
        {
            var obj = GetGameObject();
            if (obj == null)
            {
                throw new ArgumentException($"Object {_objectId} not found");
            }

            // Convert value to BsonValue
            BsonValue bsonValue = value switch
            {
                null => BsonValue.Null,
                string s => new BsonValue(s),
                int i => new BsonValue(i),
                long l => new BsonValue(l),
                double d => new BsonValue(d),
                float f => new BsonValue((double)f),
                bool b => new BsonValue(b),
                DateTime dt => new BsonValue(dt),
                BsonValue bv => bv,
                _ => new BsonValue(value.ToString() ?? "")
            };

            _objectManager.SetProperty(obj, propertyName, bsonValue);
            return true;
        }
        catch (Exception ex)
        {
            // Let the error bubble up to the script engine
            throw new InvalidOperationException($"Error setting property '{propertyName}' on object {_objectId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Handles method calls (verbs and functions): player.getName() or dynamic property access
    /// </summary>
    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        var methodName = binder.Name;
        
        try
        {
            // First try to find a verb
            var verb = FindVerb(methodName);
            if (verb != null)
            {
                // Try to call the verb on this object
                var verbResult = CallVerb(methodName, args);
                result = verbResult;
                return true;
            }

            // If no verb found, try to find a function
            var function = FindFunction(methodName);
            if (function != null)
            {
                // Try to call the function on this object
                var functionResult = CallFunction(methodName, args);
                result = functionResult;
                return true;
            }

            throw new ArgumentException($"Verb or function '{methodName}' not found on object {_objectId}");
        }
        catch (Exception ex)
        {
            // Let the error bubble up to the script engine
            throw new InvalidOperationException($"Error calling '{methodName}' on object {_objectId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Call a verb on this object
    /// </summary>
    public string CallVerb(string verbName, params object?[]? args)
    {
        // Find the verb on this object or its class hierarchy
        var verb = FindVerb(verbName);
        if (verb == null)
        {
            throw new ArgumentException($"Verb '{verbName}' not found on object {_objectId}");
        }

        // Convert arguments to strings
        var stringArgs = args?.Select(arg => arg?.ToString() ?? "").ToList() ?? new List<string>();
        
        // Create input string for the verb
        var input = verbName;
        if (stringArgs.Any())
        {
            input += " " + string.Join(" ", stringArgs);
        }

        // Execute the verb using the unified script engine
        var scriptEngine = ScriptEngineFactoryStatic.Create();
        return scriptEngine.ExecuteVerb(verb, input, _currentPlayer, _commandProcessor, _objectId);
    }

    /// <summary>
    /// Call a function on this object
    /// </summary>
    public object? CallFunction(string functionName, params object?[]? args)
    {
        try
        {
            // Find the function on this object or its class hierarchy
            var function = FindFunction(functionName);
            if (function == null)
            {
                throw new ArgumentException($"Function '{functionName}' not found on object {_objectId}");
            }

            // Execute the function using the unified script engine
            var functionEngine = ScriptEngineFactoryStatic.Create();
            var result = functionEngine.ExecuteFunction(function, args ?? new object[0], _currentPlayer, _commandProcessor, _objectId);
            return result;
        }
        catch (Exception ex)
        {
            // Just log and re-throw - don't wrap in additional ScriptExecutionException
            // This prevents nested error message chains
            Logger.Error($"Exception in ScriptObject.CallFunction for {_objectId}.{functionName}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Find a verb on this object or its class hierarchy
    /// </summary>
    private Verb? FindVerb(string verbName)
    {
        var verbs = _dbProvider.Find<Verb>("verbs", v => v.ObjectId == _objectId).ToList();
        var verb = verbs.FirstOrDefault(v =>
            v.Name.Equals(verbName, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(v.Aliases) &&
             v.Aliases.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(alias => alias.Equals(verbName, StringComparison.OrdinalIgnoreCase))));

        if (verb != null) return verb;

        // If not found, try the object's class hierarchy
        var obj = GetGameObject();
        if (obj != null)
        {
            var objectClass = _dbProvider.FindById<ObjectClass>("objectclasses", obj.ClassId);
            while (objectClass != null)
            {
                var classVerbs = _dbProvider.Find<Verb>("verbs", v => v.ObjectId == objectClass.Id).ToList();
                verb = classVerbs.FirstOrDefault(v =>
                    v.Name.Equals(verbName, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(v.Aliases) &&
                     v.Aliases.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Any(alias => alias.Equals(verbName, StringComparison.OrdinalIgnoreCase))));

                if (verb != null) return verb;

                // Move up the class hierarchy
                if (!string.IsNullOrEmpty(objectClass.ParentClassId))
                {
                    objectClass = _dbProvider.FindById<ObjectClass>("objectclasses", objectClass.ParentClassId);
                }
                else
                {
                    break;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Find a function on this object or its class hierarchy
    /// </summary>
    private Function? FindFunction(string functionName)
    {
        // Use the injected FunctionResolver to find the function
        return _functionResolver.FindFunction(_objectId, functionName);
    }

    /// <summary>
    /// String representation
    /// </summary>
    public override string ToString()
    {
        var obj = GetGameObject();
        if (obj == null) return $"ScriptObject({_objectId}) [INVALID]";
        
        var nameProperty = _objectManager.GetProperty(obj, "name");
        var shortDescProperty = _objectManager.GetProperty(obj, "shortDescription");
        
        var name = nameProperty?.AsString;
        var shortDesc = shortDescProperty?.AsString;
        
        return name ?? shortDesc ?? $"Object #{obj.DbRef}";
    }
}



