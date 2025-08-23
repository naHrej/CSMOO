using CSMOO.Functions;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Verbs;

namespace CSMOO.Database;

/// <summary>
/// Main coordinator for world initialization and management
/// </summary>
public static class WorldInitializer
{
    /// <summary>
    /// Initializes the basic world structure with core classes
    /// </summary>
    public static void InitializeWorld()
    {
        Logger.DisplaySectionHeader("WORLD INITIALIZATION");
        Logger.Info("Initializing game world...");

        try
        {
            // Create fundamental object classes
            CoreClassFactory.CreateCoreClasses();
            
            // Load and create verbs from C# class definitions
            VerbInitializer.LoadAndCreateVerbs();
            
            // Load and create functions from C# class definitions
            FunctionInitializer.LoadAndCreateFunctions();
            
            // Load and set properties from C# class definitions
            PropertyInitializer.LoadAndSetProperties();
            
            // Create the starting room and basic world areas
            RoomManager.CreateStartingRoom();

            Logger.Info("World initialization completed successfully!");
        }
        catch (Exception ex)
        {
            Logger.Error("World initialization failed", ex);
            throw;
        }
    }

    /// <summary>
    /// Gets basic world statistics for display
    /// </summary>
    public static void PrintWorldStatistics()
    {
        var classCount = DbProvider.Instance.FindAll<ObjectClass>("objectclasses").Count();
        var objectCount = ObjectManager.GetAllObjects().Count();
        var playerCount = PlayerManager.GetAllPlayers().Count();
        var roomStats = RoomManager.GetRoomStatistics();

        Logger.Game("\n=== World Statistics ===");
        Logger.Game($"Object Classes: {classCount}");
        Logger.Game($"Game Objects: {objectCount}");
        Logger.Game($"Players: {playerCount}");
        Logger.Game($"Rooms: {roomStats["TotalRooms"]}");
        Logger.Game($"Rooms with Items: {roomStats["RoomsWithItems"]}");
        Logger.Game($"Rooms with Players: {roomStats["RoomsWithPlayers"]}");
        Logger.Game($"Total Exits: {roomStats["TotalExits"]}");
        Logger.Game("========================\n");
    }
}



