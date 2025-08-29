using System;
using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Object;
using System.Linq;

namespace CSMOO.Core;

/// <summary>
/// Handles server initialization and startup procedures
/// </summary>
public static class ServerInitializer
{
    /// <summary>
    /// Initializes the game server with database and world setup
    /// </summary>
    public static void Initialize()
    {
        Logger.DisplaySectionHeader("SERVER INITIALIZATION");
        Logger.Info("Initializing CSMOO Server...");
        
        try
        {
            // Initialize the database connection
            Logger.Info("Setting up database...");
            var db = GameDatabase.Instance; // This creates the singleton instance
            
            // Initialize the world structure
            Logger.Info("Initializing world...");
            WorldInitializer.InitializeWorld();
            

            // Migrate existing objects to have DBREFs
            ObjectManager.MigrateDbRefs();

            // Load all GameObjects into the singleton cache
            ObjectManager.LoadAllObjectsToCache();
            
            // Create a test admin player if none exists
            CreateDefaultAdminIfNeeded();

            // Set owners for all created objects
            SetObjectOwners();

            // Initialize hot reload functionality
            HotReloadManager.Initialize();
            CoreHotReloadManager.Initialize();
            
            Logger.Info("Server initialization complete!");
            WorldInitializer.PrintWorldStatistics();
        }
        catch (Exception ex)
        {
            Logger.Error($"Server initialization failed", ex);
            throw;
        }
    }

        private static void SetObjectOwners()
    {
        Logger.Info("Setting object owners...");
        var allObjects = ObjectManager.GetAllObjects();
        var adminPlayer = PlayerManager.GetAllPlayers().FirstOrDefault(p => p.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));
        if (adminPlayer == null)
        {
            Logger.Error("No admin player found to assign as owner. Skipping owner assignment.");
            return;
        }
        int updatedCount = 0;

        foreach (dynamic obj in allObjects)
        {
            if (obj.Owner != null)
                continue; // Owner already set
            if (obj is Player)
            {
                obj.Owner = obj; // Players own themselves
                continue;
            }

            // If we reach here, it means we need to assign an owner
                Logger.Debug($"Setting owner for object {obj.Name} ({obj.Id}) to {adminPlayer.Name} player.");
            obj.Owner = adminPlayer;
            //ObjectManager.UpdateObject(obj);
            updatedCount++;
        }
        
        Logger.Info($"Set owners for {updatedCount} objects.");
    }

    /// <summary>
    /// Creates a default admin player for testing if no players exist
    /// </summary>
    private static void CreateDefaultAdminIfNeeded()
    {
        var existingPlayers = PlayerManager.GetAllPlayers();
        if (existingPlayers.Any())
        {
            Logger.Info($"Found {existingPlayers.Count()} existing players in database.");
            return;
        }

        Logger.Info("No players found. Creating default admin player...");

        var startingRoom = RoomManager.GetStartingRoom();
        var admin = PlayerManager.CreatePlayer("Admin", "password", startingRoom?.Id);

        // Initialize proper admin permissions using the permission system
        PermissionManager.InitializeAdminPermissions(admin);

        // Set some admin-specific properties
        ObjectManager.SetProperty(admin, "level", 100);
        ObjectManager.SetProperty(admin, "health", 1000);
        ObjectManager.SetProperty(admin, "maxHealth", 1000);
        ObjectManager.SetProperty(admin, "name", "Admin");
        ObjectManager.SetProperty(admin, "shortDescription", "the Administrator");
        ObjectManager.SetProperty(admin, "longDescription",
            "The all-powerful administrator of this realm. They have the ability to create and modify the world itself.");

        Logger.Info("Default admin player created (username: admin, password: password)");
    }

    /// <summary>
    /// Shuts down the server gracefully
    /// </summary>
    public static void Shutdown()
    {
        Logger.Info("Shutting down CSMOO Server...");
        
        // Shutdown hot reload manager
        HotReloadManager.Shutdown();
        CoreHotReloadManager.Shutdown();
        
        // Disconnect all players
        var onlinePlayers = PlayerManager.GetOnlinePlayers();
        foreach (var player in onlinePlayers)
        {
            PlayerManager.DisconnectPlayer(player.Id);
        }
        
        // Close database connection
        GameDatabase.Instance.Dispose();
        
        Logger.Info("Server shutdown complete.");
    }
}
