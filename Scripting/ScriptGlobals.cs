using CSMOO.Commands;
using CSMOO.Database;
using CSMOO.Functions;
using CSMOO.Object;
using CSMOO.Verbs;
using CSMOO.Configuration;
using CSMOO.Logging;
using LiteDB;

namespace CSMOO.Scripting;

/// <summary>
/// Unified script globals for both verb and function execution
/// </summary>
public class ScriptGlobals
{
    private readonly IObjectManager _objectManager;
    private readonly IVerbResolver _verbResolver;
    private readonly IFunctionResolver _functionResolver;
    private readonly IDbProvider _dbProvider;

    // Primary constructor with DI dependencies
    public ScriptGlobals(
        IObjectManager objectManager,
        IVerbResolver verbResolver,
        IFunctionResolver functionResolver,
        IDbProvider dbProvider)
    {
        _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
        _verbResolver = verbResolver ?? throw new ArgumentNullException(nameof(verbResolver));
        _functionResolver = functionResolver ?? throw new ArgumentNullException(nameof(functionResolver));
        _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        
        // Initialize script managers with DI dependencies
        ObjectManager = new ScriptObjectManager(_objectManager);
        var playerManager = CreateDefaultPlayerManager();
        // PlayerManagerInstance requires ObjectManager to be set via SetObjectManager
        if (playerManager is PlayerManagerInstance pmi)
        {
            pmi.SetObjectManager(_objectManager);
        }
        PlayerManager = new ScriptPlayerManager(playerManager, _objectManager);
    }

    // Backward compatibility constructor
    public ScriptGlobals()
        : this(CreateDefaultObjectManager(), CreateDefaultVerbResolver(), CreateDefaultFunctionResolver(), CreateDefaultDbProvider())
    {
    }

    private static IObjectManager CreateDefaultObjectManager()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        return new ObjectManagerInstance(dbProvider, classManager);
    }

    private static IVerbResolver CreateDefaultVerbResolver()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        return new VerbResolverInstance(dbProvider, objectManager, logger);
    }

    private static IFunctionResolver CreateDefaultFunctionResolver()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        return new FunctionResolverInstance(dbProvider, objectManager);
    }

    private static IPlayerManager CreateDefaultPlayerManager()
    {
        return new PlayerManagerInstance(DbProvider.Instance);
    }

    // EnhancedScriptGlobals logic
    private ScriptObjectFactory? _objectFactory;
    public void InitializeObjectFactory()
    {
        ScriptHelpers? helpers = Helpers;
        if (helpers != null)
        {
            _objectFactory = new ScriptObjectFactory(
                Player, 
                CommandProcessor, 
                helpers,
                _objectManager,
                _functionResolver,
                _dbProvider);
        }
    }

    private static IDbProvider CreateDefaultDbProvider()
    {
        return DbProvider.Instance;
    }
    /// <summary>
    /// Get an object by reference - dynamic version for backward compatibility
    /// </summary>
    public dynamic? obj(string objectReference)
    {
        return _objectFactory?.GetObject(objectReference);
    }

    /// <summary>
    /// Get an object by reference - typed version returns GameObject?
    /// </summary>
    public GameObject? GetObject(string objectReference)
    {
        var scriptObj = _objectFactory?.GetObject(objectReference);
        // ScriptObject wraps a GameObject, so we need to extract it
        // For now, resolve directly using ObjectResolver
        return CSMOO.Core.ObjectResolver.ResolveObject(objectReference, Player);
    }

    /// <summary>
    /// Get an object by reference with type casting - typed version with generic type parameter
    /// </summary>
    public T? obj<T>(string objectReference) where T : GameObject
    {
        return GetObject(objectReference) as T;
    }

    /// <summary>
    /// Get an object by ID - dynamic version for backward compatibility
    /// </summary>
    public dynamic? objById(string objectId)
    {
        return _objectFactory?.GetObjectById(objectId);
    }

    /// <summary>
    /// Get an object by ID - typed version returns GameObject?
    /// </summary>
    public GameObject? GetGameObjectByIdFromObjById(string objectId)
    {
        return _objectManager.GetObject(objectId);
    }

    /// <summary>
    /// Get an object by ID with type casting - typed version with generic type parameter
    /// </summary>
    public T? objById<T>(string objectId) where T : GameObject
    {
        return _objectManager.GetObject(objectId) as T;
    }

    // ScriptGlobals Say/notify/SayToRoom
    public void Say(string message)
    {
        if (Helpers != null) Helpers.Say(message);
        else CommandProcessor.SendToPlayer(message);
    }
    public void notify(Player targetPlayer, string message)
    {
        if (Helpers != null) Helpers.notify(targetPlayer, message);
        else CommandProcessor.SendToPlayer(message, targetPlayer.SessionGuid);
    }
    public void SayToRoom(string message, bool excludeSelf = true)
    {
        Helpers?.SayToRoom(message, excludeSelf);
    }
    
    
    
    
    

    // (No unique methods from VerbScriptGlobals needed; retain only original UnifiedScriptGlobals logic)
    
    /// <summary>
    /// The current player as GameObject (now with dynamic support)
    /// Always set during verb/function execution
    /// </summary>
    public Player Player { get; set; } = null!; // Initialized before use
    public CommandProcessor CommandProcessor { get; set; } = null!; // Initialized before use
    public ScriptObjectManager ObjectManager { get; set; }
    public ScriptWorldManager WorldManager { get; set; } = new ScriptWorldManager();
    public ScriptPlayerManager PlayerManager { get; set; }
    public ScriptHelpers? Helpers { get; set; }
    private GameObject? _this;
    
    /// <summary>
    /// The current object (This) - returns as dynamic to allow access to subtype-specific methods and properties
    /// Internally stored as GameObject? with correct subtype after conversion
    /// </summary>
    public dynamic? This 
    { 
        get => _this; 
        set => _this = value as GameObject; 
    }
    
    /// <summary>
    /// Typed version of This - returns GameObject? for strict typing
    /// </summary>
    public GameObject? ThisGameObject 
    { 
        get => _this; 
        set => _this = value; 
    }
    
    /// <summary>
    /// Typed access to This as Player (null if not a Player)
    /// </summary>
    public Player? ThisPlayer => _this as Player;
    
    /// <summary>
    /// Typed access to This as Room (null if not a Room)
    /// </summary>
    public Room? ThisRoom => _this as Room;
    
    /// <summary>
    /// Typed access to This as Exit (null if not an Exit)
    /// </summary>
    public Exit? ThisExit => _this as Exit;
    
    public dynamic? ThisObject { get => This; set => This = value; }
    
    private GameObject? _caller;
    
    /// <summary>
    /// The object that called this verb/function - dynamic version for backward compatibility
    /// </summary>
    public dynamic? Caller 
    { 
        get => _caller; 
        set => _caller = value as GameObject; 
    }
    
    /// <summary>
    /// Typed version of Caller - returns GameObject? for strict typing
    /// </summary>
    public GameObject? CallerGameObject 
    { 
        get => _caller; 
        set => _caller = value; 
    }
    
    /// <summary>
    /// Typed access to Caller as Player (null if not a Player)
    /// </summary>
    public Player? CallerPlayer => _caller as Player;
    public int CallDepth { get; set; } = 0;
    public string ThisObjectId { get; set; } = string.Empty;



    /// <summary>
    /// Get the underlying GameObject for This (for internal use)
    /// </summary>
    public GameObject? GetThisGameObject()
    {
        return This;
    }
    
    /// <summary>
    /// Get the underlying GameObject for Player (for internal use)
    /// </summary>
    public GameObject GetPlayerGameObject()
    {
        return Player;
    }

    /// <summary>
    /// Get the player's location as a GameObject (for scripts that need the actual GameObject)
    /// </summary>
    public GameObject? GetPlayerLocation()
    {
        return Player.Location;
    }

    /// <summary>
    /// Get an object by its DBref number (for #4.property syntax support) - dynamic version for backward compatibility
    /// </summary>
    public dynamic? GetObjectByDbRef(int dbRef)
    {
        var obj = _objectManager.GetObjectByDbRef(dbRef);
        return obj;
    }

    /// <summary>
    /// Get an object by its DBref number - typed version returns GameObject?
    /// </summary>
    public GameObject? GetGameObjectByDbRef(int dbRef)
    {
        return _objectManager.GetObjectByDbRef(dbRef);
    }

    /// <summary>
    /// Get an object by its DBref number with type casting - typed version with generic type parameter
    /// </summary>
    public T? GetObjectByDbRef<T>(int dbRef) where T : GameObject
    {
        return _objectManager.GetObjectByDbRef(dbRef) as T;
    }

    /// <summary>
    /// Get an object by its ID (for $objectId.property syntax support) - dynamic version for backward compatibility
    /// </summary>
    public dynamic? GetObjectById(string objectId)
    {
        var obj = _objectManager.GetObject(objectId);
        return obj;
    }

    /// <summary>
    /// Get an object by its ID - typed version returns GameObject?
    /// </summary>
    public GameObject? GetGameObjectById(string objectId)
    {
        return _objectManager.GetObject(objectId);
    }

    /// <summary>
    /// Get an object by its ID with type casting - typed version with generic type parameter
    /// </summary>
    public T? GetObjectById<T>(string objectId) where T : GameObject
    {
        return _objectManager.GetObject(objectId) as T;
    }

    /// <summary>
    /// The player's current location as GameObject (alternative to 'here' ScriptObject)
    /// Usage: var location = Location; // Returns GameObject instead of ScriptObject
    /// </summary>
    public GameObject? Location => GetPlayerLocation();

    /// <summary>
    /// The complete input string that triggered this verb (null for functions)
    /// </summary>
    public string? Input { get; set; }
    
    /// <summary>
    /// Parsed arguments from the input (empty for functions)
    /// </summary>
    public List<string> Args { get; set; } = new List<string>();
    
    /// <summary>
    /// The name of the verb being executed (null for functions)
    /// </summary>
    public string? Verb { get; set; }

    /// <summary>
    /// Named variables extracted from the verb pattern (e.g., {item}, {person}) (empty for functions)
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Parameters passed to the function (empty for verbs)
    /// </summary>
    public object?[] Parameters { get; set; } = Array.Empty<object?>();

    /// <summary>
    /// The object ID that called this function (same as This.Id)
    /// </summary>
    public string CallingObjectId { get; set; } = "";

    private readonly Dictionary<string, object?> _namedParameters = new();

    /// <summary>
    /// Sets a named parameter that can be accessed in the script
    /// </summary>
    public void SetParameter(string name, object? value)
    {
        _namedParameters[name] = value;
    }

    /// <summary>
    /// Gets a named parameter
    /// </summary>
    public object? GetParameter(string name)
    {
        return _namedParameters.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// Get a property from the current object (this)
    /// </summary>
    public object? GetThisProperty(string propertyName)
    {
        var thisGameObject = GetThisGameObject();
        if (thisGameObject == null) return null;
        return _objectManager.GetProperty(thisGameObject, propertyName)?.RawValue;
    }

    /// <summary>
    /// Set a property on the current object (this)
    /// </summary>
    public void SetThisProperty(string propertyName, object? value)
    {
        var thisGameObject = GetThisGameObject();
        if (thisGameObject == null) return;
        
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

        _objectManager.SetProperty(thisGameObject, propertyName, bsonValue);
    }

    /// <summary>
    /// Send a message to a specific GameObject player
    /// </summary>
    public void notify(GameObject targetPlayer, string message)
    {
        var dbPlayer = targetPlayer as Player ?? 
                      _objectManager.GetObject<Player>(targetPlayer.Id);

        if (dbPlayer != null)
        {
            CommandProcessor.SendToPlayer(message, dbPlayer.SessionGuid);
        }
    }

    /// <summary>
    /// Get a player by name or ID for use with notify()
    /// </summary>
    public GameObject? GetPlayer(string nameOrId)
    {
        // Try by name first
        var player = _objectManager.GetAllObjects()
            .FirstOrDefault(obj => obj is Player && 
                                   (obj.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) || 
                                    obj.Id.Equals(nameOrId, StringComparison.OrdinalIgnoreCase)));
        
        if (player != null) return _objectManager.GetObject(player.Id);
        
        // Try by ID
        var playerById = _objectManager.GetObject<Player>(nameOrId);
        return playerById != null ? _objectManager.GetObject(playerById.Id) : null;
    }

    /// <summary>
    /// Get the current player for use with notify() - dynamic version for backward compatibility
    /// </summary>
    public dynamic? me => Player;

    /// <summary>
    /// Get the current player - typed version returns Player?
    /// </summary>
    public Player? MePlayer => Player;

    /// <summary>
    /// Get the current player for use with notify() - dynamic version for backward compatibility
    /// </summary>
    public dynamic? player => Player;

    /// <summary>
    /// Get the current player - typed version returns Player?
    /// </summary>
    public Player? PlayerGameObject => Player;

    /// <summary>
    /// The current location as GameObject - returns the actual GameObject, not ScriptObject
    /// If script is running on a player: returns player's location
    /// If script is running on a room: returns the room itself
    /// Dynamic version for backward compatibility
    /// </summary>
    public dynamic? here
    {
        get
        {
            return Player!.Location as dynamic;
        }
    }

    /// <summary>
    /// The current location - typed version returns GameObject?
    /// </summary>
    public GameObject? HereGameObject => Player.Location;

    /// <summary>
    /// The current location - typed version as Room?
    /// </summary>
    public Room? HereRoom => Player.Location as Room;

    /// <summary>
    /// Check if an object is a room using class inheritance and properties
    /// </summary>
    private bool IsRoom(GameObject obj)
    {
        // Check if it inherits from Room class
        var roomClass = _objectManager.GetAllObjectClasses().FirstOrDefault(c => c.Name == "Room");
        if (roomClass != null && (obj.ClassId == roomClass.Id || _objectManager.InheritsFrom(obj.ClassId, roomClass.Id)))
        {
            return true;
        }

        // Fallback: check for explicit room properties
        var isRoomProperty = _objectManager.GetProperty(obj, "isRoom")?.AsBoolean == true;
        if (isRoomProperty) return true;

        // Additional fallback: check if it has room-like characteristics
        return HasRoomCharacteristics(obj);
    }

    /// <summary>
    /// Check if an object has room-like characteristics
    /// </summary>
    private bool HasRoomCharacteristics(GameObject obj)
    {
        // First check if it inherits from Room class
        var roomClass = _objectManager.GetAllObjectClasses().FirstOrDefault(c => c.Name == "Room");
        if (roomClass != null && (obj.ClassId == roomClass.Id || _objectManager.InheritsFrom(obj.ClassId, roomClass.Id)))
        {
            return true;
        }

        // Fallback: check if object has room-like properties
        var hasExits = _objectManager.GetObjectsInLocation(obj.Id).Any(o =>
            _objectManager.GetProperty(o, "isExit")?.AsBoolean == true);
        var hasLongDesc = !string.IsNullOrEmpty(_objectManager.GetProperty(obj, "longDescription")?.AsString);
        var isRoomProperty = _objectManager.GetProperty(obj, "isRoom")?.AsBoolean == true;
        
        return hasExits || hasLongDesc || isRoomProperty;
    }


    /// <summary>
    /// Find an object in the current room by name
    /// </summary>
    public string? FindObjectInRoom(string name)
    {
        var playerGameObject = GetPlayerGameObject();
        if (playerGameObject?.Location == null) return null;

        var objects = _objectManager.GetObjectsInLocation(playerGameObject.Location);
        var targetObject = objects.FirstOrDefault(obj =>
        {
            var objName = _objectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = _objectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            name = name.ToLower();
            return objName?.Contains(name) == true || shortDesc?.Contains(name) == true;
        });

        return targetObject?.Id;
    }

    /// <summary>
    /// Call a verb on an object from within another script
    /// </summary>
    public object? CallVerb(string objectRef, string verbName, params object[] args)
    {
        try
        {
            // Prevent calling system programming commands from scripts
            if (objectRef.Equals("system", StringComparison.OrdinalIgnoreCase) && verbName.StartsWith("@"))
            {
                throw new InvalidOperationException($"Cannot call system programming command '{verbName}' from within a script. Programming commands must be executed directly from the command line.");
            }

            // Resolve the object reference
            var objectId = ResolveObjectFromScript(objectRef);
            if (objectId == null)
            {
                throw new ArgumentException($"Object '{objectRef}' not found");
            }

            // Find the verb on the object (with inheritance)
            var allVerbsOnObject = _verbResolver.GetAllVerbsOnObject(objectId);
            var verbMatch = allVerbsOnObject.FirstOrDefault(v => 
                v.verb.Name.Equals(verbName, StringComparison.OrdinalIgnoreCase));

            if (verbMatch.verb == null)
            {
                throw new ArgumentException($"Verb '{verbName}' not found on object '{objectRef}'");
            }

            // Execute the verb with the provided arguments
            var scriptEngine = ScriptEngineFactoryStatic.Create();
            
            // Build input string from arguments
            var inputArgs = args.Select(a => a?.ToString() ?? "").ToArray();
            var input = verbName + (inputArgs.Length > 0 ? " " + string.Join(" ", inputArgs) : "");
            
            // Get Database.Player from the original Player context
            // This ensures the original calling player is preserved throughout the call chain
            var dbPlayer = Player as Player;
            
            if (dbPlayer == null || CommandProcessor == null)
            {
                throw new InvalidOperationException("Cannot call verb without valid player and command processor context");
            }
            
            var result = scriptEngine.ExecuteVerb(verbMatch.verb, input, dbPlayer, CommandProcessor, objectId);
            
            // Try to parse result as different types
            if (string.IsNullOrEmpty(result)) return null;
            if (bool.TryParse(result, out bool boolVal)) return boolVal;
            if (int.TryParse(result, out int intVal)) return intVal;
            if (double.TryParse(result, out double doubleVal)) return doubleVal;
            return result; // Return as string if no other type matches
        }
        catch (Exception ex)
        {
            var dbPlayer = Player as Player;
            if (dbPlayer != null) notify(dbPlayer, $"Error calling {objectRef}:{verbName}() - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Call a function on an object from within another script
    /// </summary>
    public object? CallFunction(string objectRef, string functionName, params object?[] parameters)
    {
        // Use the original Database.Player that was set in the ScriptGlobals
        // This ensures the original calling player is preserved throughout the call chain
        var dbPlayer = Player as Player;
        
        if (dbPlayer == null)
            throw new InvalidOperationException("No player context available.");

        var objectId = _functionResolver.ResolveObjectReference(objectRef, dbPlayer.Id, dbPlayer.Location?.Id ?? "");
        if (objectId == null)
        {
            throw new ArgumentException($"Object '{objectRef}' not found.");
        }

        var function = _functionResolver.FindFunction(objectId, functionName);
        if (function == null)
        {
            throw new ArgumentException($"Function '{functionName}' not found on object '{objectRef}'.");
        }

        var engine = ScriptEngineFactoryStatic.Create();
        return engine.ExecuteFunction(function, parameters, dbPlayer, CommandProcessor, objectId);
    }

    /// <summary>
    /// Call a function on a GameObject directly (typed, no dynamic casting needed)
    /// This is used by the precompiler to rewrite method calls like obj.Description() to CallFunctionOnObject(obj, "Description")
    /// </summary>
    public object? CallFunctionOnObject(GameObject? obj, string functionName, params object?[]? args)
    {
        if (obj == null)
            return null;

        var function = _functionResolver.FindFunction(obj.Id, functionName);
        if (function == null)
        {
            throw new Exceptions.FunctionExecutionException($"Function '{functionName}' not found on object {obj.Name}(#{obj.DbRef}). Check function name and ensure it's defined on this object or its class.");
        }

        var engine = ScriptEngineFactoryStatic.Create();
        return engine.ExecuteFunction(function, args ?? new object[0], Player, CommandProcessor, obj.Id);
    }

    /// <summary>
    /// Resolve object reference from script context
    /// </summary>
    private string? ResolveObjectFromScript(string objectRef)
    {
        // Handle special keywords
        switch (objectRef.ToLower())
        {
            case "this":
                return GetThisGameObject()?.Id;
            case "me":
                return GetPlayerGameObject()?.Id;
            case "here":
                return GetPlayerGameObject()?.Location?.Id;
            case "system":
                return GetSystemObjectId();
        }
        
        // Check if it's a DBREF (starts with # followed by digits)
        if (objectRef.StartsWith("#") && int.TryParse(objectRef.Substring(1), out int dbref))
        {
            var obj = _objectManager.GetObjectByDbRef(dbref);
            return obj?.Id;
        }
        
        // Check if it's a class reference
        if (objectRef.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectRef.Substring(6);
            var objectClass = _objectManager.GetAllObjectClasses()
                .FirstOrDefault(c => c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return objectClass?.Id;
        }
        
        if (objectRef.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectRef.Substring(0, objectRef.Length - 6);
            var objectClass = _objectManager.GetAllObjectClasses()
                .FirstOrDefault(c => c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return objectClass?.Id;
        }

        // Check if it's a direct class ID
        if (_objectManager.GetAllObjectClasses().FirstOrDefault(c => c.Id == objectRef) is { } classByIdObj)
        {
            return classByIdObj.Id;
        }
        
        // Try to find by name
        var match = _objectManager.GetAllObjects()
            .FirstOrDefault(obj =>
            {
                var objName = (_objectManager.GetProperty(obj, "name") as BsonValue)?.AsString;
                return objName?.Equals(objectRef, StringComparison.OrdinalIgnoreCase) == true;
            });
        if (match != null) return match.Id;
        
        // Try as class name
        var objectClass2 = _objectManager.GetAllObjectClasses()
            .FirstOrDefault(c => 
                c.Name.Equals(objectRef, StringComparison.OrdinalIgnoreCase));
        return objectClass2?.Id;
    }

    /// <summary>
    /// Get system object ID (helper method)
    /// </summary>
    private string GetSystemObjectId()
    {
        var systemObj = _objectManager.GetAllObjects()
            .FirstOrDefault(obj => 
                (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
                (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
        return systemObj?.Id ?? "";
    }

    /// <summary>
    /// Call a verb on the current object (this)
    /// </summary>
    public object? ThisVerb(string verbName, params object[] args)
    {
        return CallVerb("this", verbName, args);
    }

    /// <summary>
    /// Call a verb on the player object (me)
    /// </summary>
    public object? Me(string verbName, params object[] args)
    {
        return CallVerb("me", verbName, args);
    }

    /// <summary>
    /// Call a verb on the current room (here)
    /// </summary>
    public object? Here(string verbName, params object[] args)
    {
        // Use the here property to get the correct location
        var hereObj = here;
        if (hereObj == null) return null;
        return CallVerb($"#{hereObj.DbRef}", verbName, args);
    }

    /// <summary>
    /// Call a verb on the system object
    /// </summary>
    public object? System(string verbName, params object[] args)
    {
        return CallVerb("system", verbName, args);
    }

    /// <summary>
    /// Call a verb on an object by DBREF
    /// </summary>
    public object? Object(int dbref, string verbName, params object[] args)
    {
        return CallVerb($"#{dbref}", verbName, args);
    }

    /// <summary>
    /// Call a verb on a class
    /// </summary>
    public object? Class(string className, string verbName, params object[] args)
    {
        return CallVerb($"class:{className}", verbName, args);
    }
}



