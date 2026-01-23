using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Verbs;
using CSMOO.Functions;
using LiteDB;
using CSMOO.Object;
using CSMOO.Scripting;
using CSMOO.Configuration;
using CSMOO.Init;

namespace CSMOO.Core;

/// <summary>
/// Built-in functions for verb scripts - provides clean, consistent API without casting or long namespaces
/// </summary>
public static class Builtins
{
    private static IBuiltinsInstance? _instance;
    
    /// <summary>
    /// Sets the builtins instance for dependency injection
    /// </summary>
    public static void SetInstance(IBuiltinsInstance instance)
    {
        _instance = instance;
    }
    
    /// <summary>
    /// Ensures an instance exists (creates default if not set)
    /// </summary>
    private static void EnsureInstance()
    {
        if (_instance == null)
        {
            // Create default instances for backward compatibility
            var dbProvider = DbProvider.Instance;
            var config = Config.Instance;
            var logger = new LoggerInstance(config);
            var classManager = new ClassManagerInstance(dbProvider, logger);
            var objectManager = new ObjectManagerInstance(dbProvider, classManager);
            
            // Create PropertyManager and InstanceManager and set them on ObjectManagerInstance
            var propertyManager = new PropertyManagerInstance(dbProvider, classManager, objectManager);
            if (objectManager is ObjectManagerInstance omi)
            {
                omi.SetPropertyManager(propertyManager);
            }
            
            var instanceManager = new InstanceManagerInstance(dbProvider, classManager, objectManager, propertyManager);
            if (objectManager is ObjectManagerInstance omi2)
            {
                omi2.SetInstanceManager(instanceManager);
            }
            
            var playerManager = new PlayerManagerInstance(dbProvider);
            // PlayerManagerInstance requires ObjectManager to be set via SetObjectManager
            if (playerManager is PlayerManagerInstance pmi)
            {
                pmi.SetObjectManager(objectManager);
            }
            var permissionManager = new PermissionManagerInstance(dbProvider, logger);
            var functionResolver = new FunctionResolverInstance(dbProvider, objectManager);
            var verbResolver = new VerbResolverInstance(dbProvider, objectManager, logger);
            var verbManager = new VerbManagerInstance(dbProvider);
            var roomManager = new RoomManagerInstance(dbProvider, logger, objectManager);
            var objectResolver = CreateDefaultObjectResolver();
            var compilationCache = new CompilationCache();
            var scriptEngineFactory = new ScriptEngineFactory(objectManager, logger, config, objectResolver, verbResolver, functionResolver, dbProvider, playerManager, verbManager, roomManager, compilationCache);
            _instance = new BuiltinsInstance(
                objectManager,
                playerManager,
                permissionManager,
                functionResolver,
                verbManager,
                roomManager,
                logger,
                scriptEngineFactory);
        }
    }
    
    private static IObjectResolver CreateDefaultObjectResolver()
    {
        var dbProvider = DbProvider.Instance;
        var config = Config.Instance;
        var logger = new LoggerInstance(config);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        var coreClassFactory = new CoreClassFactoryInstance(dbProvider, logger);
        return new ObjectResolverInstance(objectManager, coreClassFactory);
    }
    
    // Helper properties to get managers (from instance if available, otherwise create default instances)
    private static IObjectManager ObjectManagerInstance
    {
        get
        {
            if (_instance != null) return _instance.ObjectManager;
            EnsureInstance();
            return _instance!.ObjectManager;
        }
    }
    
    private static IPlayerManager PlayerManagerInstance
    {
        get
        {
            if (_instance != null) return _instance.PlayerManager;
            EnsureInstance();
            return _instance!.PlayerManager;
        }
    }
    
    private static IPermissionManager PermissionManagerInstance
    {
        get
        {
            if (_instance != null) return _instance.PermissionManager;
            EnsureInstance();
            return _instance!.PermissionManager;
        }
    }
    
    private static IFunctionResolver FunctionResolverInstance
    {
        get
        {
            if (_instance != null) return _instance.FunctionResolver;
            EnsureInstance();
            return _instance!.FunctionResolver;
        }
    }
    
    private static IVerbManager VerbManagerInstance
    {
        get
        {
            if (_instance != null) return _instance.VerbManager;
            EnsureInstance();
            return _instance!.VerbManager;
        }
    }
    
    private static IRoomManager RoomManagerInstance
    {
        get
        {
            if (_instance != null) return _instance.RoomManager;
            EnsureInstance();
            return _instance!.RoomManager;
        }
    }
    
    private static ILogger LoggerInstance
    {
        get
        {
            if (_instance != null) return _instance.Logger;
            EnsureInstance();
            return _instance!.Logger;
        }
    }
    
    private static IScriptEngineFactory ScriptEngineFactoryInstance
    {
        get
        {
            if (_instance != null) return _instance.ScriptEngineFactory;
            EnsureInstance();
            return _instance!.ScriptEngineFactory;
        }
    }
    
    /// <summary>
    /// Current script context - set by the script engine before execution (legacy, now UnifiedScriptGlobals)
    /// </summary>
    public static ScriptGlobals? CurrentContext { get; set; }
    
    private static readonly System.Threading.AsyncLocal<ScriptGlobals?> _unified = new();
    public static ScriptGlobals? UnifiedContext
    {
        get => _unified.Value;
        set => _unified.Value = value;
    }

    #region Object Management

    /// <summary>
    /// Find a game object by its ID
    /// </summary>
    public static dynamic? FindObject(string objectId)
    {
        if (string.IsNullOrEmpty(objectId)) return null;
        return ObjectManagerInstance.GetObject(objectId);
    }
    
    
    /// <summary>
    /// Get the string value of an object property (GameObject overload)
    /// </summary>
    public static BsonValue? GetProperty(GameObject obj, string propertyName)
    {
        return ObjectManagerInstance.GetProperty(obj, propertyName);
    }

    public static string[] GetAllPropertyNames(GameObject obj)
    {
        return ObjectManagerInstance.GetPropertyNames(obj);
    }

  
    
    /// <summary>
    /// Get the string value of an object property with default (GameObject overload)
    /// </summary>
    public static string GetProperty(GameObject obj, string propertyName, string defaultValue = "")
    {
        var property = ObjectManagerInstance.GetProperty(obj, propertyName) as BsonValue;
        return property?.AsString ?? defaultValue;
    }
    
    /// <summary>
    /// Get the string value of an object property with default (dynamic overload for script compatibility)
    /// </summary>
    public static string GetProperty(dynamic obj, string propertyName, string defaultValue = "")
    {
        // Handle null
        if (obj == null)
            return defaultValue;
        
        // Convert dynamic to GameObject if possible (most common case)
        // Use pattern matching to handle both GameObject and its subtypes
        if (obj is GameObject gameObject)
        {
            return GetProperty(gameObject, propertyName, defaultValue);
        }
        
        // If it's a BsonValue, return default (shouldn't happen, but handle gracefully)
        if (obj is LiteDB.BsonValue)
        {
            return defaultValue;
        }
        
        // Try to get the object ID if it's a GameObject-like object
        string? objectId = null;
        try
        {
            // Try accessing Id property directly
            var idValue = obj.Id;
            if (idValue != null)
            {
                objectId = idValue.ToString();
            }
        }
        catch
        {
            // If Id property doesn't exist or throws, try to get it as a string
            try
            {
                var str = obj.ToString();
                // Only use as objectId if it looks like a GUID
                if (!string.IsNullOrEmpty(str) && Guid.TryParse(str, out Guid guid))
                {
                    objectId = str;
                }
            }
            catch
            {
                // If all else fails, return default
                return defaultValue;
            }
        }
        
        if (!string.IsNullOrEmpty(objectId))
        {
            var gameObj = ObjectManagerInstance.GetObject(objectId);
            if (gameObj != null)
            {
                return GetProperty(gameObj, propertyName, defaultValue);
            }
        }
        
        return defaultValue;
    }
    
    /// <summary>
    /// Get the boolean value of an object property (GameObject overload)
    /// </summary>
    public static bool GetBoolProperty(GameObject obj, string propertyName, bool defaultValue = false)
    {
        var property = ObjectManagerInstance.GetProperty(obj, propertyName) as BsonValue;
        return property?.AsBoolean ?? defaultValue;
    }
    
    /// <summary>
    /// Get the boolean value of an object property (dynamic overload for script compatibility)
    /// </summary>
    public static bool GetBoolProperty(dynamic obj, string propertyName, bool defaultValue = false)
    {
        // Handle null
        if (obj == null)
            return defaultValue;
        
        // Convert dynamic to GameObject if possible (most common case)
        if (obj is GameObject gameObject)
        {
            return GetBoolProperty(gameObject, propertyName, defaultValue);
        }
        
        // Try to get the object ID if it's a GameObject-like object
        string? objectId = null;
        try
        {
            // Try accessing Id property directly
            var idValue = obj.Id;
            if (idValue != null)
            {
                objectId = idValue.ToString();
            }
        }
        catch
        {
            // If Id property doesn't exist or throws, try to get it as a string
            try
            {
                var str = obj.ToString();
                // Only use as objectId if it looks like a GUID
                if (!string.IsNullOrEmpty(str) && Guid.TryParse(str, out Guid guid))
                {
                    objectId = str;
                }
            }
            catch
            {
                // If all else fails, return default
                return defaultValue;
            }
        }
        
        if (!string.IsNullOrEmpty(objectId))
        {
            var gameObj = ObjectManagerInstance.GetObject(objectId);
            if (gameObj != null)
            {
                return GetBoolProperty(gameObj, propertyName, defaultValue);
            }
        }
        
        return defaultValue;
    }
    

    
    /// <summary>
    /// Set a property on an object (GameObject overload)
    /// </summary>
    public static void SetProperty(GameObject obj, string propertyName, string value)
    {
        if (obj != null)
        {
            ObjectManagerInstance.SetProperty(obj, propertyName, value);
        }
    }
    

    
    /// <summary>
    /// Set a boolean property on an object (GameObject overload)
    /// </summary>
    public static void SetBoolProperty(GameObject obj, string propertyName, bool value)
    {
        if (obj != null)
        {
            ObjectManagerInstance.SetProperty(obj, propertyName, value);
        }       
    }
    
    /// <summary>
    /// Get all objects in a location - returns GameObject dynamic objects (backward compatibility)
    /// </summary>
    public static List<dynamic> GetObjectsInLocation(string locationId)
    {
        var gameObjects = ObjectManagerInstance.GetObjectsInLocation(locationId);
        return gameObjects.Cast<dynamic>().ToList();
    }
    
    /// <summary>
    /// Get all objects in a location - typed version returns List<GameObject>
    /// </summary>
    public static List<GameObject> GetObjectsInLocationTyped(string locationId)
    {
        return ObjectManagerInstance.GetObjectsInLocation(locationId);
    }
    

    
    /// <summary>
    /// Move an object to a new location (GameObject overload)
    /// </summary>
    public static bool MoveObject(GameObject obj, string newLocationId)
    {
        if (obj != null)
        {
            ObjectManagerInstance.SetProperty(obj, "location", newLocationId);
            return true;
        }
        return false;
    }
    
    
    /// <summary>
    /// Get the name of an object (GameObject overload)
    /// </summary>
    public static string GetObjectName(GameObject obj)
    {
        return GetObjectName(obj);
    }
    
  
    
    /// <summary>
    /// Get the short description of an object (GameObject overload)
    /// </summary>
    public static string GetObjectShortDesc(GameObject obj)
    {
        return GetObjectShortDesc(obj);
    }
    

    
    /// <summary>
    /// Get the long description of an object (GameObject overload)
    /// </summary>
    public static string GetObjectLongDesc(GameObject obj)
    {
        return GetProperty(obj, "longDescription");
    }


    #endregion

    #region Player Management

    /// <summary>
    /// Find a player by name
    /// </summary>
    public static dynamic? FindPlayer(string playerName)
    {
            return ObjectManagerInstance.GetAllObjects()
            .OfType<Player>()
            .FirstOrDefault(p => p.Name.Contains(playerName, StringComparison.OrdinalIgnoreCase));
       
    }
    
    
    /// <summary>
    /// Find a player by ID
    /// </summary>
    public static dynamic? FindPlayerById(string playerId)
    {
        return ObjectManagerInstance.GetObject<Player>(playerId);
    }
    
    /// <summary>
    /// Get all online players - dynamic version for backward compatibility
    /// </summary>
    public static List<dynamic> GetOnlinePlayers()
    {
        return PlayerManagerInstance.GetOnlinePlayers().AsEnumerable().Cast<dynamic>().ToList();
    }
    
    /// <summary>
    /// Get all online players - typed version returns List<Player>
    /// </summary>
    public static List<Player> GetOnlinePlayersTyped()
    {
        return PlayerManagerInstance.GetOnlinePlayers().ToList();
    }
    
    /// <summary>
    /// Get all players (online and offline) - useful for lambda filtering - dynamic version for backward compatibility
    /// </summary>
    public static List<dynamic> GetAllPlayers()
    {
        return ObjectManagerInstance.GetAllObjects()
            .OfType<Player>()
            .Cast<dynamic>()
            .ToList();
    }
    
    /// <summary>
    /// Get all players (online and offline) - typed version returns List<Player>
    /// </summary>
    public static List<Player> GetAllPlayersTyped()
    {
        return ObjectManagerInstance.GetAllObjects()
            .OfType<Player>()
            .ToList();
    }
    
    /// <summary>
    /// Get all game objects - useful for lambda filtering and searching - dynamic version for backward compatibility
    /// </summary>
    public static List<dynamic> GetAllObjects()
    {
        return ObjectManagerInstance.GetAllObjects()
            .OfType<GameObject>()
            .Cast<dynamic>()
            .ToList();
    }
    
    /// <summary>
    /// Get all game objects - typed version returns List<GameObject>
    /// </summary>
    public static List<GameObject> GetAllObjectsTyped()
    {
        return ObjectManagerInstance.GetAllObjects()
            .OfType<GameObject>()
            .ToList();
    }
    
    /// <summary>
    /// Get all object classes - useful for lambda filtering
    /// </summary>
    public static List<dynamic> GetAllObjectClasses()
    {
        return ObjectManagerInstance.GetAllObjectClasses().Cast<dynamic>().ToList();
    }

    public static List<dynamic> GetObjectsByClass(string className)
    {
        if (string.IsNullOrEmpty(className)) return new List<dynamic>();
        
        var objectClass = ObjectManagerInstance.GetClassByName(className);
        
        if (objectClass == null) return new List<dynamic>();
        
        return ObjectManagerInstance.GetAllObjects()
            .Where(obj => obj.ClassId == objectClass.Id)
            .Cast<dynamic>()
            .ToList();
    }
    
    /// <summary>
    /// Get objects by class name - typed version returns List<GameObject>
    /// </summary>
    public static List<GameObject> GetObjectsByClassTyped(string className)
    {
        if (string.IsNullOrEmpty(className)) return new List<GameObject>();
        
        var objectClass = ObjectManagerInstance.GetClassByName(className);
        
        if (objectClass == null) return new List<GameObject>();
        
        return ObjectManagerInstance.GetAllObjects()
            .OfType<GameObject>()
            .Where(obj => obj.ClassId == objectClass.Id)
            .ToList();
    }
    
    /// <summary>
    /// Get the inheritance chain for a class
    /// </summary>
    public static List<ObjectClass> GetInheritanceChain(string classId)
    {
        return ObjectManagerInstance.GetInheritanceChain(classId);
    }

    /// <summary>
    /// Get an object class by its name
    /// </summary>
    public static ObjectClass? GetClassByName(string className)
    {
        if (string.IsNullOrEmpty(className)) return null;
        return ObjectManagerInstance.GetClassByName(className);
    }
    
    /// <summary>
    /// Get an object class by its ID
    /// </summary>
    public static ObjectClass? GetClass(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return null;
        return ObjectManagerInstance.GetClass(classId);
    }

    public static List<Verb> GetVerbsOnClass(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return new List<Verb>();
        return DbProvider.Instance.FindVerbsByObjectId(classId).ToList();
    }

    public static List<Function> GetFunctionsOnClass(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return new List<Function>();
        return FunctionResolverInstance.GetFunctionsForObject(classId, true);
    }
    
    /// <summary>
    /// Check if a player has a specific permission flag
    /// </summary>
    public static bool HasFlag(Player player, string flagName)
    {
        if (Enum.TryParse<PermissionManager.Flag>(flagName, true, out var flag))
        {
            return PermissionManagerInstance.HasFlag(player, flag);
        }
        return false;
    }
    
    /// <summary>
    /// Check if a player has Admin flag
    /// </summary>
    public static bool IsAdmin(Player player)
    {
        return PermissionManagerInstance.HasFlag(player, PermissionManager.Flag.Admin);
    }
    
    /// <summary>
    /// Check if a player has Moderator flag
    /// </summary>
    public static bool IsModerator(Player player)
    {
        return PermissionManagerInstance.HasFlag(player, PermissionManager.Flag.Moderator);
    }
    
    /// <summary>
    /// Check if a player has Programmer flag
    /// </summary>
    public static bool IsProgrammer(Player player)
    {
        return PermissionManagerInstance.HasFlag(player, PermissionManager.Flag.Programmer);
    }
    

    
    /// <summary>
    /// Get formatted flags string for a player
    /// </summary>
    public static string GetPlayerFlagsString(Player player)
    {
        return PermissionManagerInstance.GetFlagsString(player);
    }
    
    /// <summary>
    /// Check if a player has Admin flag (dynamic overload for UnifiedScriptEngine)
    /// </summary>
    public static bool IsAdmin(dynamic? player)
    {
        if (player == null) return false;
        
        // Handle GameObject wrapper
        if (player is GameObject gameObject)
        {
            var dbPlayer = ObjectManagerInstance.GetObject<Player>( gameObject.Id);
            return dbPlayer != null && PermissionManagerInstance.HasFlag(dbPlayer, PermissionManager.Flag.Admin);
        }
        
        // Handle direct Database.Player
        if (player is Player dbPlayerDirect)
        {
            return PermissionManagerInstance.HasFlag(dbPlayerDirect, PermissionManager.Flag.Admin);
        }
        
        // Handle dynamic object with Id property
        if (player.Id != null)
        {
            var dbPlayer = ObjectManagerInstance.GetObject<Player>( (string)player.Id);
            return dbPlayer != null && PermissionManagerInstance.HasFlag(dbPlayer, PermissionManager.Flag.Admin);
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a player has Moderator flag (dynamic overload for UnifiedScriptEngine)
    /// </summary>
    public static bool IsModerator(dynamic? player)
    {
        if (player == null) return false;
        
        // Handle GameObject wrapper
        if (player is GameObject gameObject)
        {
            var dbPlayer = ObjectManagerInstance.GetObject<Player>(gameObject.Id);
            return dbPlayer != null && PermissionManagerInstance.HasFlag(dbPlayer, PermissionManager.Flag.Moderator);
        }
        
        // Handle direct Database.Player
        if (player is Player dbPlayerDirect)
        {
            return PermissionManagerInstance.HasFlag(dbPlayerDirect, PermissionManager.Flag.Moderator);
        }
        
        // Handle dynamic object with Id property
        if (player.Id != null)
        {
            var dbPlayer = ObjectManagerInstance.GetObject<Player>( (string)player.Id);
            return dbPlayer != null && PermissionManagerInstance.HasFlag(dbPlayer, PermissionManager.Flag.Moderator);
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a player has Programmer flag (dynamic overload for UnifiedScriptEngine)
    /// </summary>
    public static bool IsProgrammer(dynamic? player)
    {
        if (player == null) return false;
        
        // Handle GameObject wrapper
        if (player is GameObject gameObject)
        {
            var dbPlayer = ObjectManagerInstance.GetObject<Player>( gameObject.Id);
            return dbPlayer != null && PermissionManagerInstance.HasFlag(dbPlayer, PermissionManager.Flag.Programmer);
        }
        
        // Handle direct Database.Player
        if (player is Player dbPlayerDirect)
        {
            return PermissionManagerInstance.HasFlag(dbPlayerDirect, PermissionManager.Flag.Programmer);
        }
        
        // Handle dynamic object with Id property
        if (player.Id != null)
        {
            var dbPlayer = ObjectManagerInstance.GetObject<Player>( (string)player.Id);
            return dbPlayer != null && PermissionManagerInstance.HasFlag(dbPlayer, PermissionManager.Flag.Programmer);
        }
        
        return false;
    }
    
    /// <summary>
    /// Get all flags for a player as a list of strings (dynamic overload for UnifiedScriptEngine)
    /// </summary>
    public static List<string> GetPlayerFlags(GameObject? player)
    {
        if (player == null) return new List<string>();
        
        // Handle GameObject wrapper
        if (player is GameObject gameObject)
        {
            var dbPlayer = ObjectManagerInstance.GetObject<Player>(gameObject.Id);
            return dbPlayer != null ? PermissionManagerInstance.GetPlayerFlags(dbPlayer).Select(f => f.ToString()).ToList() : new List<string>();
        }
        
        // Handle direct Database.Player
        if (player is Player dbPlayerDirect)
        {
            return PermissionManagerInstance.GetPlayerFlags(dbPlayerDirect).Select(f => f.ToString()).ToList();
        }
        
        // Handle dynamic object with Id property
        if (player.Id != null)
        {
            var dbPlayer = ObjectManagerInstance.GetObject<Player>((string)player.Id);
            return dbPlayer != null ? PermissionManagerInstance.GetPlayerFlags(dbPlayer).Select(f => f.ToString()).ToList() : new List<string>();
        }
        
        return new List<string>();
    }
    
    #endregion
    
    #region Object Finding and Resolution
    
    /// <summary>
    /// Smart object resolution - finds players first, then objects by name
    /// </summary>
    public static string? ResolveObject(string objectName, Player currentPlayer)
    {
        if (string.IsNullOrEmpty(objectName)) return null;
        
        // Handle special keywords
        switch (objectName.ToLower())
        {
            case "me":
                return currentPlayer.Id;
            case "here":
                return currentPlayer.Location?.Id;
            case "system":
                // Find the system object
                var allObjects = ObjectManagerInstance.GetAllObjects();
                var systemObj = allObjects.FirstOrDefault(obj =>
                    (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
                    (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
                return systemObj?.Id;
        }
        
        // Check if it's a DBREF (starts with # followed by digits)
        if (objectName.StartsWith("#") && int.TryParse(objectName.Substring(1), out int dbref))
        {
            var obj = ObjectManagerInstance.GetObjectByDbRef(dbref);
            return obj?.Id;
        }

        // Check if it's a class reference (starts with "class:" or ends with ".class")
        if (objectName.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectName.Substring(6); // Remove "class:" prefix
            var objectClass = ObjectManagerInstance.GetClassByName(className);
            return objectClass?.Id;
        }
        
        if (objectName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
        {
            var className = objectName.Substring(0, objectName.Length - 6); // Remove ".class" suffix
            var objectClass = ObjectManagerInstance.GetClassByName(className);
            return objectClass?.Id;
        }

        // Check if it's a direct class ID (like "Room", "Exit", etc.)
        var classById = ObjectManagerInstance.GetClass(objectName);
        if (classById != null)
        {
            return classById.Id;
        }
        
        // Try to find a player first
        var player = FindPlayer(objectName);
        if (player != null)
        {
            return player.Id;
        }
        
        // Try to find object by name in current location
        if (currentPlayer.Location != null)
        {
            var objectsInRoom = GetObjectsInLocation(currentPlayer.Location.Id);
            var foundObject = objectsInRoom.FirstOrDefault(obj =>
            {
                var gameObject = obj.GameObject as GameObject;
                if (gameObject == null) return false;
                var objName = GetObjectName(gameObject);
                return objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
            });
            
            if (foundObject != null)
            {
                var gameObject = foundObject.GameObject as GameObject;
                if (gameObject != null) return gameObject.Id;
            }
        }
        
        // Try player inventory
        var inventory = GetObjectsInLocation(currentPlayer.Id);
        var inventoryObject = inventory.FirstOrDefault(obj =>
        {
            var gameObject = obj.GameObject as GameObject;
            if (gameObject == null) return false;
            var objName = GetObjectName(gameObject);
            return objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
        });
        
        if (inventoryObject != null)
        {
            var gameObject = inventoryObject.GameObject as GameObject;
            if (gameObject != null) return gameObject.Id;
        }
        
        // If not found as an object, try as a class name
        var directClass = ObjectManagerInstance.GetClassByName(objectName);

        if (directClass != null)
        {
            return directClass.Id;
        }

        // Finally, search globally for any object with a matching name
        var globalObjects = ObjectManagerInstance.GetAllObjects();
        var globalObject = globalObjects.FirstOrDefault(obj =>
        {
            var objName = GetObjectName(obj);
            return objName.Equals(objectName, StringComparison.OrdinalIgnoreCase) ||
                   objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
        });
        
        return globalObject?.Id;
    }
    
    /// <summary>
    /// Find an object by name in the current room
    /// </summary>
    public static dynamic? FindObjectInRoom(string objectName, Player currentPlayer)
    {
        if (currentPlayer.Location == null) return null;
        
        var objectsInRoom = GetObjectsInLocation(currentPlayer.Location.Id);
        var foundObject = objectsInRoom.FirstOrDefault(obj =>
        {
            var gameObject = obj.GameObject as GameObject;
            if (gameObject == null) return false;
            var objName = GetObjectName(gameObject);
            return objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
        });
        
        return foundObject?.GameObject as GameObject;
    }
    
    /// <summary>
    /// Find an object by name in player's inventory
    /// </summary>
    public static dynamic? FindObjectInInventory(string objectName, Player currentPlayer)
    {
        var inventory = GetObjectsInLocation(currentPlayer.Id);
        var foundObject = inventory.FirstOrDefault(obj =>
        {
            var gameObject = obj.GameObject as GameObject;
            if (gameObject == null) return false;
            var objName = GetObjectName(gameObject);
            return objName.StartsWith(objectName, StringComparison.OrdinalIgnoreCase);
        });
        
        return foundObject?.GameObject as GameObject;
    }
    
    public static dynamic? FindObjectById(string objectId)
    {
        if (string.IsNullOrEmpty(objectId)) return null;
        return ObjectManagerInstance.GetObject(objectId);
    }
    
    #endregion

    #region Verb Management

    /// <summary>
    /// Get all verbs on an object
    /// </summary>
    public static List<(Verb verb, string source)> GetVerbsOnObject(string objectId)
    {
        return CreateDefaultVerbResolver().GetAllVerbsOnObject(objectId);
    }

    /// <summary>
    /// Get all verbs on an object (GameObject overload)
    /// </summary>
    public static List<(Verb verb, string source)> GetVerbsOnObject(GameObject obj)
    {
        return GetVerbsOnObject(obj.Id);
    }

    /// <summary>
    /// Get all functions on an object
    /// </summary>
    public static List<(Function function, string source)> GetFunctionsOnObject(string objectId)
    {
        
        return FunctionResolverInstance.GetAllFunctionsOnObject(objectId);
    }

    /// <summary>
    /// Get all functions on an object (GameObject overload)
    /// </summary>
    public static List<(Function function, string source)> GetFunctionsOnObject(GameObject obj)
    {
        return GetFunctionsOnObject(obj.Id);
    }

    /// <summary>
    /// Get all verbs from the database (for help system)
    /// </summary>
    public static List<Verb> GetAllVerbs()
    {
        return VerbManagerInstance.GetAllVerbs();
    }

    /// <summary>
    /// Get all functions from the database (for help system)
    /// </summary>
    public static List<Function> GetAllFunctions()
    {
        return FunctionManager.GetAllFunctions();
    }

    /// <summary>
    /// Get help metadata (description and summary) for a category or topic
    /// </summary>
    public static (string? Description, string? Summary) GetHelpMetadata(string name)
    {
        return CodeDefinitionParser.GetHelpMetadata(name);
    }
    
    /// <summary>
    /// Get the general help preamble text
    /// </summary>
    public static string? GetHelpPreamble()
    {
        return CodeDefinitionParser.GetHelpPreamble();
    }
    
    /// <summary>
    /// Get the class name for a function's ObjectId
    /// For class functions, ObjectId points to an ObjectClass, not a GameObject
    /// </summary>
    public static string? GetFunctionClassName(string objectId)
    {
        try
        {
            // First try to get it as an ObjectClass (for class functions)
            var objectClass = ObjectManagerInstance.GetClass(objectId);
            if (objectClass != null)
            {
                return objectClass.Name;
            }
            
            // If not found as a class, try as a GameObject (for instance functions)
            var obj = ObjectManagerInstance.GetObject(objectId);
            if (obj != null)
            {
                // For instance functions, get the class name from the object's ClassId
                if (!string.IsNullOrEmpty(obj.ClassId))
                {
                    var objClass = ObjectManagerInstance.GetClass(obj.ClassId);
                    if (objClass != null)
                    {
                        return objClass.Name;
                    }
                }
                // Fallback to object name if no class found
                return obj.Name;
            }
        }
        catch
        {
            // If lookup fails, return null
        }
        return null;
    }

    /// <summary>
    /// Find a specific function on an object (with inheritance)
    /// </summary>
    public static Function? FindFunction(string objectId, string functionName)
    {
        return FunctionResolverInstance.FindFunction(objectId, functionName);
    }
    
    /// <summary>
    /// Find a specific function on an object (GameObject overload)
    /// </summary>
    public static Function? FindFunction(GameObject obj, string functionName)
    {
        return FindFunction(obj.Id, functionName);
    }
    
    private static IVerbResolver CreateDefaultVerbResolver()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        return new VerbResolverInstance(dbProvider, objectManager, logger);
    }
    
    #endregion
    
    #region Player Identification
    

    
    /// <summary>
    /// Check if an object represents a player and return the player (GameObject overload)
    /// </summary>
    public static dynamic? GetPlayerFromObject(GameObject obj)
    {
        var playerIdProperty = GetProperty(obj, "playerId");
        if (!string.IsNullOrEmpty(playerIdProperty))
        {
            return FindPlayerById(playerIdProperty);
        }
        return null;
    }

    
    /// <summary>
    /// Check if an object directly represents a player (GameObject overload)
    /// </summary>    
    public static bool IsPlayerObject(GameObject obj)
    {
        // Check if this objectId is actually a player ID
        var player = FindPlayerById(obj.Id);
        return player != null;
    }
    
    #endregion
    
    #region Messaging
    
    /// <summary>
    /// Send a message to a player
    /// </summary>
    public static void Notify(Player player, string message)
    {
        if (player?.SessionGuid != null && CurrentContext?.CommandProcessor != null)
        {
            CurrentContext.CommandProcessor.SendToPlayer(message, player.SessionGuid);
        }
    }
    
    /// <summary>
    /// Send a message to all players in a room
    /// </summary>
    public static void NotifyRoom(string roomId, string message, Player? excludePlayer = null)
    {
        var playersInRoom = GetObjectsInLocation(roomId);
        foreach (var obj in playersInRoom)
        {
            // Extract the GameObject from the dynamic wrapper
            var gameObject = obj.GameObject as GameObject;
            if (gameObject != null)
            {
                var player = GetPlayerFromObject(gameObject);
                if (player != null && (excludePlayer == null || player?.Id != excludePlayer.Id))
                {
                    // The script engine will handle the actual notification
                    // This is a placeholder for the interface
                }
            }
        }
    }
    
    #endregion
    
    #region Utility Functions
    

    
    /// <summary>
    /// Get a friendly display name for an object (GameObject overload)
    /// </summary>
    public static string GetDisplayName(GameObject obj)
    {
        var name = GetObjectName(obj);
        var shortDesc = GetObjectShortDesc(obj);
        
        if (!string.IsNullOrEmpty(shortDesc))
        {
            return $"{name} ({shortDesc})";
        }
        return name;
        //return GetDisplayName(obj.Id);
    }
    

    /// <summary>
    /// Check if an object is gettable (GameObject overload)
    /// </summary>
    public static bool IsGettable(GameObject obj)
    {
         return GetBoolProperty(obj, "gettable", false);
    }
    
    /// <summary>
    /// Join arguments into a single string starting from a specific index
    /// </summary>
    public static string JoinArgs(List<string> args, int startIndex = 0)
    {
        if (args == null || startIndex >= args.Count) return "";
        return string.Join(" ", args.Skip(startIndex));
    }
    

    
    /// <summary>
    /// Get the class of an object (GameObject overload)
    /// </summary>
    public static dynamic? GetObjectClass(GameObject obj)
    {
        if (obj != null && !string.IsNullOrEmpty(obj.ClassId))
        {
            return ObjectManagerInstance.GetClass(obj.ClassId);
        }
        return null;
    }

    /// <summary>
    /// Get current player from script context
    /// </summary>
    public static dynamic? GetCurrentPlayer()
    {
        // Check unified context first, then fall back to old context
        if (((Player?)UnifiedContext?.Player) != null)
        {
            // Convert GameObject to Database.Player
            var unifiedPlayer = (Player?)UnifiedContext.Player;
            if (unifiedPlayer != null)
                return unifiedPlayer;
            var id = unifiedPlayer?.Id;
            if (!string.IsNullOrEmpty(id))
                return ObjectManagerInstance.GetObject<Player>(id);
            return null;
        }
        return (Player?)CurrentContext?.Player;
    }

    /// <summary>
    /// Get players in a room
    /// </summary>
    public static List<dynamic> GetPlayersInRoom(string roomId)
    {
        if (roomId == null) return new List<dynamic>();
        
        return PlayerManagerInstance.GetOnlinePlayers()
            .Where(p => p.Location?.Id == roomId)
            .ToList<dynamic>();
    }
    
    /// <summary>
    /// Get players in a room
    /// </summary>
    public static List<dynamic> GetPlayersInRoom(GameObject room)
    {
        if (room is null) return new List<dynamic>();
        return PlayerManagerInstance.GetOnlinePlayers()
            .Where(p => p.Location?.Id == room.Id)
            .Cast<dynamic>()
            .ToList();
    }
    
    /// <summary>
    /// Get players in a room (dynamic overload for script compatibility)
    /// </summary>
    public static List<dynamic> GetPlayersInRoom(dynamic room)
    {
        // Handle null or BsonValue directly
        if (room == null || room is LiteDB.BsonValue)
            return new List<dynamic>();
        
        // Convert dynamic to GameObject if possible
        if (room is GameObject gameObject)
        {
            return GetPlayersInRoom(gameObject);
        }
        
        // Try to extract ID and get the object
        try
        {
            var idProperty = room?.Id;
            if (idProperty != null)
            {
                var id = idProperty.ToString();
                if (!string.IsNullOrEmpty(id))
                {
                    var obj = ObjectManagerInstance.GetObject(id);
                    if (obj != null)
                    {
                        return GetPlayersInRoom(obj);
                    }
                }
            }
        }
        catch
        {
            // If extraction fails, return empty list
        }
        
        return new List<dynamic>();
    }
    #endregion

    #region Room and Movement Helpers

    /// <summary>
    /// Simple room display - just return the room description
    /// </summary>
    public static string GetRoomDescription()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer?.Location == null) return "You are nowhere.";

        var room = ObjectManagerInstance.GetObject(currentPlayer.Location.Id);
        if (room == null) return "You are in a void.";

        var name = GetProperty(room, "name")?.AsString ?? "Unknown Room";
        var longDesc = GetProperty(room, "longDescription")?.AsString ?? "You see nothing special.";

        return $"=== {name} ===\n{longDesc}";
    }

    /// <summary>
    /// Display room information to current player
    /// </summary>
    public static void ShowRoom()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null) return;
        
        Notify(currentPlayer, GetRoomDescription());
        
        // Show exits
        if (currentPlayer.Location != null)
        {
            var exits = RoomManagerInstance.GetExits(currentPlayer.Location);
            if (exits.Any())
            {
                var exitNames = ((IEnumerable<GameObject>)exits).Select(e => GetProperty(e, "direction")?.AsString).Where(d => d != null);
                Notify(currentPlayer, $"Exits: {string.Join(", ", exitNames)}");
            }

            // Show objects
            var exitClassId = ObjectManagerInstance.GetClassByName("Exit")?.Id;
            var playerClassId = ObjectManagerInstance.GetClassByName("Player")?.Id;
            var objects = ((IEnumerable<dynamic>)GetObjectsInLocation(currentPlayer.Location.Id))
                .Where(obj => {
                    var gameObject = obj.GameObject as GameObject;
                    return gameObject != null && 
                           gameObject.ClassId != exitClassId &&
                           gameObject.ClassId != playerClassId;
                });

            foreach (var obj in objects)
            {
                var gameObject = obj.GameObject as GameObject;
                if (gameObject != null)
                {
                    var visible = GetProperty(gameObject, "visible")?.AsBoolean ?? true;
                    if (visible)
                    {
                        var shortDesc = GetProperty(gameObject, "shortDescription")?.AsString ?? "something";
                        Notify(currentPlayer, $"You see {shortDesc} here.");
                    }
                }
            }

            // Show other players
            var otherPlayers = ((IEnumerable<dynamic>)GetPlayersInRoom(currentPlayer.Location)).Where(p => p.Id != currentPlayer.Id);
            foreach (var otherPlayer in otherPlayers)
            {
                Notify(currentPlayer, $"{otherPlayer.Name} is here.");
            }
        }
    }

    /// <summary>
    /// Look at a specific object
    /// </summary>
    public static void LookAtObject(string target)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer?.Location == null) return;

        target = target.ToLower();
        var objects = GetObjectsInLocation(currentPlayer.Location.Id);
        
        var targetObject = ((IEnumerable<dynamic>)objects).FirstOrDefault(obj =>
        {
            var gameObject = obj.GameObject as GameObject;
            if (gameObject == null) return false;
            var name = GetProperty(gameObject, "name")?.AsString?.ToLower();
            var shortDesc = GetProperty(gameObject, "shortDescription")?.AsString?.ToLower();
            return name?.Contains(target) == true || shortDesc?.Contains(target) == true;
        });

        if (targetObject == null)
        {
            Notify(currentPlayer, "You don't see that here.");
            return;
        }

        var gameObj = targetObject.GameObject as GameObject;
        if (gameObj != null)
        {
            var longDesc = GetProperty(gameObj, "longDescription")?.AsString ?? "You see nothing special.";
            Notify(currentPlayer, longDesc);
        }
    }

    /// <summary>
    /// Find an item in the current room by name - returns dynamic wrapper
    /// </summary>
    public static dynamic? FindItemInRoom(string itemName)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer?.Location == null) return null;

        itemName = itemName.ToLower();
        var roomObjects = GetObjectsInLocation(currentPlayer.Location.Id);
        
        var foundObject = ((IEnumerable<GameObject>)roomObjects).FirstOrDefault(gameObject =>
        {
            if (gameObject == null) return false;
            var name = GetProperty(gameObject, "name")?.AsString?.ToLower();
            var shortDesc = GetProperty(gameObject, "shortDescription")?.AsString?.ToLower();
            return name?.Contains(itemName) == true || shortDesc?.Contains(itemName) == true;
        });
        
        return foundObject;
    }

    /// <summary>
    /// Find an item in the player's inventory by name - returns dynamic wrapper
    /// </summary>
    public static dynamic? FindItemInInventory(string itemName)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null) return null;

        itemName = itemName.ToLower();
        var playerGameObject = ObjectManagerInstance.GetObject(currentPlayer.Id);
        if (playerGameObject?.Contents == null) return null;

        var foundObject = ((IEnumerable<string>)playerGameObject.Contents)
            .Select(id => ObjectManagerInstance.GetObject(id))
            .FirstOrDefault(obj =>
            {
                if (obj == null) return false;
                var name = GetProperty(obj, "name")?.AsString?.ToLower();
                var shortDesc = GetProperty(obj, "shortDescription")?.AsString?.ToLower();
                return name?.Contains(itemName) == true || shortDesc?.Contains(itemName) == true;
            });
        return foundObject;
    }

    /// <summary>
    /// Move an object to a new location
    /// </summary>
    public static bool MoveObject(GameObject gameObj, GameObject destination)
    {
        try
        {
            ObjectManagerInstance.MoveObject(gameObj, destination);
            return true;
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Failed to move object {gameObj.Id} to {destination.Id}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Send a message to all players in the current room
    /// </summary>
    public static void SayToRoom(string message, bool excludeSelf = true)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer?.Location == null) return;

        var playersInRoom = GetPlayersInRoom(currentPlayer.Location);
        foreach (var player in playersInRoom)
        {
            if (excludeSelf && player.Id == currentPlayer.Id) continue;
            var targetPlayer = ObjectManagerInstance.GetObject<Player>(player.Id);
            if (targetPlayer != null)
            {
                Notify(targetPlayer, message);
            }
        }
    }

    /// <summary>
    /// Get all exits from a room
    /// </summary>
    public static List<dynamic> GetExits(GameObject room)
    {
        return RoomManagerInstance.GetExits(room);
    }
    
    /// <summary>
    /// Get all exits from a room (dynamic overload for script compatibility)
    /// </summary>
    public static List<dynamic> GetExits(dynamic room)
    {
        // Handle null or BsonValue directly
        if (room == null || room is LiteDB.BsonValue)
            return new List<dynamic>();
        
        // Convert dynamic to GameObject if possible
        if (room is GameObject gameObject)
        {
            return GetExits(gameObject);
        }
        
        // Try to extract ID and get the object
        try
        {
            var idProperty = room?.Id;
            if (idProperty != null)
            {
                var id = idProperty.ToString();
                if (!string.IsNullOrEmpty(id))
                {
                    var obj = ObjectManagerInstance.GetObject(id);
                    if (obj != null)
                    {
                        return GetExits(obj);
                    }
                }
            }
        }
        catch
        {
            // If extraction fails, return empty list
        }
        
        return new List<dynamic>();
    }

    public static List<dynamic> GetContents(GameObject room)
    {
        return RoomManagerInstance.GetItems(room);
    }
    
    /// <summary>
    /// Get all objects in a room (excluding exits and players) - for script compatibility
    /// </summary>
    public static List<dynamic> GetObjectsInRoom(dynamic room)
    {
        // Handle null or BsonValue directly
        if (room == null || room is LiteDB.BsonValue)
            return new List<dynamic>();
        
        // Convert dynamic to GameObject if possible
        if (room is GameObject gameObject)
        {
            return GetContents(gameObject);
        }
        
        // Try to extract ID and get the object
        try
        {
            var idProperty = room?.Id;
            if (idProperty != null)
            {
                var id = idProperty.ToString();
                if (!string.IsNullOrEmpty(id))
                {
                    var obj = ObjectManagerInstance.GetObject(id);
                    if (obj != null)
                    {
                        return GetContents(obj);
                    }
                }
            }
        }
        catch
        {
            // If extraction fails, return empty list
        }
        
        return new List<dynamic>();
    }


    /// <summary>
    /// Show the player's inventory
    /// </summary>
    public static void ShowInventory()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null) return;

        var playerGameObject = ObjectManagerInstance.GetObject(currentPlayer.Id);
        if (playerGameObject?.Contents == null || !playerGameObject!.Contents.Any())
        {
            Notify(currentPlayer, "You are carrying nothing.");
            return;
        }

        Notify(currentPlayer, "You are carrying:");
        foreach (var itemId in playerGameObject!.Contents)
        {
            var item = ObjectManagerInstance.GetObject(itemId);
            if (item != null)
            {
                var name = GetProperty(item, "shortDescription") ?? "something";
                Notify(currentPlayer, $"  {name}");
            }
        }
    }

    /// <summary>
    /// Get information about a specific verb on an object
    /// </summary>
    public static VerbInfo? GetVerbInfo(string objectSpec, string verbName)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null) return null;
        
        var objectId = ResolveObject(objectSpec, currentPlayer);
        if (objectId == null)
        {
            return null;
        }

        var verb = ((IEnumerable<Verb>)VerbManagerInstance.GetVerbsOnObject(objectId))
            .FirstOrDefault(v => v.Name.ToLower() == verbName.ToLower());

        if (verb == null)
        {
            return null;
        }

        var obj = ObjectManagerInstance.GetObject(objectId);
        return new VerbInfo
        {
            ObjectId = objectId,
            ObjectName = obj != null ? GetObjectName(obj) : objectId,
            VerbName = verb.Name,
            Aliases = verb.Aliases,
            Pattern = verb.Pattern,
            Description = verb.Description,
            CreatedBy = verb.CreatedBy,
            CreatedAt = verb.CreatedAt,
            Code = verb.Code ?? "",
            CodeLines = string.IsNullOrEmpty(verb.Code) ? new string[0] : verb.Code.Split('\n')
        };
    }

    #endregion

    #region Lambda-Friendly Helper Methods

    /// <summary>
    /// Execute an action for each player matching a condition
    /// Usage: ForEachPlayer(p => p.IsOnline, p => notify(p, "Hello!"));
    /// </summary>
    public static void ForEachPlayer(Func<dynamic, bool> predicate, Action<dynamic> action)
    {
        var players = GetAllPlayers().Where(predicate);
        foreach (var player in players)
        {
            try
            {
                action(player);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error executing action on player {player.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Execute an action for each object matching a condition
    /// Usage: ForEachObject(obj => GetProperty(obj.Id, "type") == "weapon", obj => SetProperty(obj.Id, "sharpened", "true"));
    /// </summary>
    public static void ForEachObject(Func<dynamic, bool> predicate, Action<dynamic> action)
    {
        var objects = GetAllObjects().Where(predicate);
        foreach (var obj in objects)
        {
            try
            {
                action(obj);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error executing action on object {obj.Id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Find players matching a condition - returns a list for further processing
    /// Usage: var admins = FindPlayers(p => IsAdmin(p));
    /// </summary>
    public static List<dynamic> FindPlayers(Func<dynamic, bool> predicate)
    {
        return GetAllPlayers().Where(predicate).ToList();
    }

    /// <summary>
    /// Find objects matching a condition - returns a list for further processing
    /// Usage: var weapons = FindObjects(obj => GetProperty(obj.Id, "type") == "weapon");
    /// </summary>
    public static List<dynamic> FindObjects(Func<dynamic, bool> predicate)
    {
        return GetAllObjects().Where(predicate).ToList();
    }

    /// <summary>
    /// Find objects in a location matching a condition
    /// Usage: var redItems = FindObjectsInLocation(roomId, obj => GetProperty(obj.Id, "color") == "red");
    /// </summary>
    public static List<dynamic> FindObjectsInLocation(string locationId, Func<dynamic, bool> predicate)
    {
        return GetObjectsInLocation(locationId).Where(predicate).ToList();
    }

    /// <summary>
    /// Find objects in a location matching a condition (strongly-typed for GameObject)
    /// Usage: var redItems = FindObjectsInLocationTyped(roomId, obj => obj.color == "red");
    /// </summary>
    public static List<dynamic> FindObjectsInLocationTyped(string locationId, Func<GameObject, bool> predicate)
    {
        var objects = GetObjectsInLocation(locationId);
        return objects.Cast<GameObject>().Where(predicate).Cast<dynamic>().ToList();
    }

    /// <summary>
    /// Filter dynamic objects with strongly-typed predicate
    /// Usage: var filtered = FilterObjects(GetObjectsInLocation(roomId), obj => obj.visible == true);
    /// </summary>
    public static List<dynamic> FilterObjects(IEnumerable<dynamic> objects, Func<GameObject, bool> predicate)
    {
        return objects.Cast<GameObject>().Where(predicate).Cast<dynamic>().ToList();
    }

    /// <summary>
    /// Transform dynamic objects to another type
    /// Usage: var names = SelectFromObjects(GetObjectsInLocation(roomId), obj => obj.name);
    /// </summary>
    public static List<dynamic> SelectFromObjects<T>(IEnumerable<dynamic> objects, Func<GameObject, T> selector)
    {
        return objects.Cast<GameObject>().Select(selector).Cast<dynamic>().ToList();
    }

    /// <summary>
    /// Count players matching a condition
    /// Usage: var onlineCount = CountPlayers(p => p.IsOnline);
    /// </summary>
    public static int CountPlayers(Func<dynamic, bool> predicate)
    {
        return GetAllPlayers().Count(predicate);
    }

    /// <summary>
    /// Count objects matching a condition
    /// Usage: var weaponCount = CountObjects(obj => GetProperty(obj.Id, "type") == "weapon");
    /// </summary>
    public static int CountObjects(Func<dynamic, bool> predicate)
    {
        return GetAllObjects().Count(predicate);
    }

    /// <summary>
    /// Check if any player matches a condition
    /// Usage: var hasAdmin = AnyPlayer(p => IsAdmin(p));
    /// </summary>
    public static bool AnyPlayer(Func<dynamic, bool> predicate)
    {
        return GetAllPlayers().Any(predicate);
    }

    /// <summary>
    /// Check if any object matches a condition
    /// Usage: var hasWeapon = AnyObject(obj => GetProperty(obj.Id, "type") == "weapon");
    /// </summary>
    public static bool AnyObject(Func<dynamic, bool> predicate)
    {
        return GetAllObjects().Any(predicate);
    }

    /// <summary>
    /// Transform a list of objects using a lambda
    /// Usage: var names = Transform(GetObjectsInLocation(roomId), obj => obj.name);
    /// </summary>
    public static List<T> Transform<TSource, T>(IEnumerable<TSource> source, Func<TSource, T> selector)
    {
        return source.Select(selector).ToList();
    }

    #endregion

    #region Script Execution

    /// <summary>
    /// Log a message from script context
    /// </summary>
    public static void Log(string message)
    {
        LoggerInstance.Info(message);
    }

    /// <summary>
    /// Execute C# script code with the same environment as verb/function execution
    /// </summary>
    public static string ExecuteScript(string scriptCode, Player player, Commands.CommandProcessor commandProcessor, string? thisObjectId = null, string? input = null)
    {
        try
        {
            var engine = ScriptEngineFactoryInstance.Create();
            
            // Create a temporary verb structure for execution
            var tempVerb = new Verb
            {
                Name = "script",
                Code = scriptCode,
                ObjectId = player?.Id ?? "system"
            };

            if(player == null)
            {
                throw new ArgumentNullException(nameof(player), "Player cannot be null");
            }
            
            return engine.ExecuteVerb(tempVerb, input ?? "", player, commandProcessor, thisObjectId);
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Script execution error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Execute C# script code with the same environment as verb/function execution (overload with variables)
    /// </summary>
    public static string ExecuteScript(string scriptCode, Player player, Commands.CommandProcessor commandProcessor, string? thisObjectId = null, string? input = null, Dictionary<string, string>? variables = null)
    {
        try
        {
            var engine = ScriptEngineFactoryInstance.Create();
            
            // Create a temporary verb structure for execution
            var tempVerb = new Verb
            {
                Name = "script",
                Code = scriptCode,
                ObjectId = thisObjectId ?? "system"
            };
            
            return engine.ExecuteVerb(tempVerb, input ?? "", player, commandProcessor, thisObjectId, variables);
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Script execution error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Execute C# script code with the same environment as verb/function execution (GameObject overload)
    /// </summary>
    public static string ExecuteScript(string scriptCode, GameObject player, Commands.CommandProcessor commandProcessor, GameObject thisObject, string? input = null)
    {
        try
        {
            // Look up the Database.Player from the GameObject player
            var dbPlayer = ObjectManagerInstance.GetObject<Player>(player.Id);
            if (dbPlayer == null)
            {
                throw new ArgumentException($"Player with ID '{player.Id}' not found in database");
            }
            // Use the object ID directly from GameObject
            var objectId = thisObject?.Id ?? "system";
            return ExecuteScript(scriptCode, dbPlayer, commandProcessor, objectId, input);
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Script execution error: {ex.Message}");
            throw;
        }
    }

    #endregion

}



