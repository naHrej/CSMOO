using System;
using CSMOO.Server.Database;

namespace CSMOO.Server;

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
        Console.WriteLine("Initializing CSMOO Server...");
        
        try
        {
            // Initialize the database connection
            Console.WriteLine("Setting up database...");
            var db = GameDatabase.Instance; // This creates the singleton instance
            
            // Initialize the world structure
            Console.WriteLine("Initializing world...");
            WorldManager.InitializeWorld();
            
            // Migrate existing objects to have DBREFs
            ObjectManager.MigrateDbRefs();
            
            // Create a test admin player if none exists
            CreateDefaultAdminIfNeeded();
                  Console.WriteLine("Server initialization complete!");
        PrintWorldStats();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server initialization failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates a default admin player for testing if no players exist
    /// </summary>
    private static void CreateDefaultAdminIfNeeded()
    {
        var existingPlayers = GameDatabase.Instance.Players.FindAll();
        if (existingPlayers.Any())
        {
            Console.WriteLine($"Found {existingPlayers.Count()} existing players in database.");
            return;
        }

        Console.WriteLine("No players found. Creating default admin player...");
        
        var startingRoom = WorldManager.GetStartingRoom();
        var admin = PlayerManager.CreatePlayer("admin", "password", startingRoom?.Id);
        admin.Permissions.Add("admin");
        admin.Permissions.Add("builder");
        
        // Set some admin-specific properties
        ObjectManager.SetProperty(admin, "level", 100);
        ObjectManager.SetProperty(admin, "health", 1000);
        ObjectManager.SetProperty(admin, "maxHealth", 1000);
        ObjectManager.SetProperty(admin, "name", "Admin");
        ObjectManager.SetProperty(admin, "shortDescription", "the Administrator");
        ObjectManager.SetProperty(admin, "longDescription", 
            "The all-powerful administrator of this realm. They have the ability to create and modify the world itself.");

        Console.WriteLine("Default admin player created (username: admin, password: password)");
    }

    /// <summary>
    /// Prints statistics about the current world state
    /// </summary>
    private static void PrintWorldStats()
    {
        var classCount = GameDatabase.Instance.ObjectClasses.Count();
        var objectCount = GameDatabase.Instance.GameObjects.Count();
        var playerCount = GameDatabase.Instance.Players.Count();
        var roomCount = WorldManager.GetAllRooms().Count;

        Console.WriteLine("\n=== World Statistics ===");
        Console.WriteLine($"Object Classes: {classCount}");
        Console.WriteLine($"Game Objects: {objectCount}");
        Console.WriteLine($"Players: {playerCount}");
        Console.WriteLine($"Rooms: {roomCount}");
        Console.WriteLine("========================\n");
    }

    /// <summary>
    /// Shuts down the server gracefully
    /// </summary>
    public static void Shutdown()
    {
        Console.WriteLine("Shutting down CSMOO Server...");
        
        // Disconnect all players
        var onlinePlayers = PlayerManager.GetOnlinePlayers();
        foreach (var player in onlinePlayers)
        {
            PlayerManager.DisconnectPlayer(player.Id);
        }
        
        // Close database connection
        GameDatabase.Instance.Dispose();
        
        Console.WriteLine("Server shutdown complete.");
    }
}
