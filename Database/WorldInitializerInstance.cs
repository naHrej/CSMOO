using CSMOO.Functions;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Verbs;

namespace CSMOO.Database;

/// <summary>
/// Instance-based world initializer implementation for dependency injection
/// </summary>
public class WorldInitializerInstance : IWorldInitializer
{
    private readonly ILogger _logger;
    private readonly IDbProvider _dbProvider;
    private readonly IObjectManager _objectManager;
    private readonly IPlayerManager _playerManager;
    private readonly IRoomManager _roomManager;
    private readonly ICoreClassFactory _coreClassFactory;
    private readonly IVerbInitializer _verbInitializer;
    private readonly IFunctionInitializer _functionInitializer;
    private readonly IPropertyInitializer _propertyInitializer;
    
    public WorldInitializerInstance(
        ILogger logger,
        IDbProvider dbProvider,
        IObjectManager objectManager,
        IPlayerManager playerManager,
        IRoomManager roomManager,
        ICoreClassFactory coreClassFactory,
        IVerbInitializer verbInitializer,
        IFunctionInitializer functionInitializer,
        IPropertyInitializer propertyInitializer)
    {
        _logger = logger;
        _dbProvider = dbProvider;
        _objectManager = objectManager;
        _playerManager = playerManager;
        _roomManager = roomManager;
        _coreClassFactory = coreClassFactory;
        _verbInitializer = verbInitializer;
        _functionInitializer = functionInitializer;
        _propertyInitializer = propertyInitializer;
    }
    
    /// <summary>
    /// Initializes the basic world structure with core classes
    /// </summary>
    public void InitializeWorld()
    {
        _logger.DisplaySectionHeader("WORLD INITIALIZATION");
        _logger.Info("Initializing game world...");

        try
        {
            // Create fundamental object classes
            _coreClassFactory.CreateCoreClasses();
            
            // Load and create verbs from C# class definitions
            _verbInitializer.LoadAndCreateVerbs();
            
            // Load and create functions from C# class definitions
            _functionInitializer.LoadAndCreateFunctions();
            
            // Load and set properties from C# class definitions
            _propertyInitializer.LoadAndSetProperties();
            
            // Create the starting room and basic world areas
            _roomManager.CreateStartingRoom();

            _logger.Info("World initialization completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.Error("World initialization failed", ex);
            throw;
        }
    }

    /// <summary>
    /// Gets basic world statistics for display
    /// </summary>
    public void PrintWorldStatistics()
    {
        var classCount = _dbProvider.FindAll<ObjectClass>("objectclasses").Count();
        var objectCount = _objectManager.GetAllObjects().Count();
        var playerCount = _playerManager.GetAllPlayers().Count();
        var roomStats = _roomManager.GetRoomStatistics();

        _logger.Game("\n=== World Statistics ===");
        _logger.Game($"Object Classes: {classCount}");
        _logger.Game($"Game Objects: {objectCount}");
        _logger.Game($"Players: {playerCount}");
        _logger.Game($"Rooms: {roomStats["TotalRooms"]}");
        _logger.Game($"Rooms with Items: {roomStats["RoomsWithItems"]}");
        _logger.Game($"Rooms with Players: {roomStats["RoomsWithPlayers"]}");
        _logger.Game($"Total Exits: {roomStats["TotalExits"]}");
        _logger.Game("========================\n");
    }
}
