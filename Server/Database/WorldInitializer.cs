using CSMOO.Server.Database.World;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Database;

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
            
            // Load and create verbs from JSON files
            VerbInitializer.LoadAndCreateVerbs();
            
            // Load and create functions from JSON files
            World.FunctionInitializer.LoadAndCreateFunctions();
            
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
        var objectCount = DbProvider.Instance.FindAll<GameObject>("gameobjects").Count();
        var playerCount = DbProvider.Instance.FindAll<Player>("players").Count();
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
