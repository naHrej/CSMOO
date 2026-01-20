using System;
using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Object;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using CSMOO.Verbs;
using CSMOO.Functions;

namespace CSMOO.Init;

/// <summary>
/// Handles server initialization and startup procedures
/// </summary>
public static class ServerInitializer
{
    /// <summary>
    /// Initializes the game server with database and world setup
    /// Uses static instances for backward compatibility
    /// </summary>
    public static void Initialize()
    {
        // For backward compatibility, use static instances
        // In production, use Initialize(ServiceProvider) instead
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

            // Load all GameObjects into the singleton cache
            ObjectManager.LoadAllObjectsToCache();
            
            // Create a test admin player if none exists
            CreateDefaultAdminIfNeeded();

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
    
    /// <summary>
    /// Initializes the game server with database and world setup using dependency injection
    /// </summary>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger>();
        var database = serviceProvider.GetRequiredService<IGameDatabase>();
        var dbProvider = serviceProvider.GetRequiredService<IDbProvider>();
        var playerManager = serviceProvider.GetRequiredService<IPlayerManager>();
        var objectManager = serviceProvider.GetRequiredService<IObjectManager>();
        var worldInitializer = serviceProvider.GetRequiredService<IWorldInitializer>();
        var hotReloadManager = serviceProvider.GetRequiredService<IHotReloadManager>();
        var permissionManager = serviceProvider.GetRequiredService<IPermissionManager>();
        
        // Set static PermissionManager instance for backward compatibility
        PermissionManager.SetInstance(permissionManager);
        
        logger.DisplaySectionHeader("SERVER INITIALIZATION");
        logger.Info("Initializing CSMOO Server...");
        
        try
        {
            // Initialize the database connection
            logger.Info("Setting up database...");
            // Database is already initialized via DI, just verify it's accessible
            _ = database;
            
            // Initialize the world structure
            logger.Info("Initializing world...");
            worldInitializer.InitializeWorld();

            // Load all GameObjects into the singleton cache
            objectManager.LoadAllObjectsToCache();
            
            // Create a test admin player if none exists
            CreateDefaultAdminIfNeeded(playerManager, objectManager, permissionManager);

            // Load functions and verbs before hot reload initialization
            var functionInitializer = serviceProvider.GetRequiredService<IFunctionInitializer>();
            var verbInitializer = serviceProvider.GetRequiredService<IVerbInitializer>();
            
            logger.Info("Loading function definitions...");
            functionInitializer.LoadAndCreateFunctions();
            
            logger.Info("Loading verb definitions...");
            verbInitializer.LoadAndCreateVerbs();

            // Initialize hot reload functionality
            hotReloadManager.Initialize();
            var coreHotReloadManager = serviceProvider.GetRequiredService<ICoreHotReloadManager>();
            coreHotReloadManager.Initialize();
            
            // Recompile all scripts on startup
            logger.Info("Recompiling all scripts...");
            var compilationInitializer = serviceProvider.GetRequiredService<CSMOO.Scripting.ICompilationInitializer>();
            try
            {
                compilationInitializer.RecompileAllAsync().Wait();
                var stats = compilationInitializer.GetStatistics();
                logger.Info($"Script recompilation complete: {stats.VerbsCompiled} verbs, {stats.FunctionsCompiled} functions compiled. {stats.VerbsFailed} verbs, {stats.FunctionsFailed} functions failed.");
            }
            catch (Exception ex)
            {
                logger.Warning($"Script recompilation had errors: {ex.Message}");
                // Don't fail startup if recompilation fails
            }
            
            logger.Info("Server initialization complete!");
            worldInitializer.PrintWorldStatistics();
        }
        catch (Exception ex)
        {
            logger.Error($"Server initialization failed", ex);
            throw;
        }
    }

    private static void SetObjectOwners(Player adminPlayer)
    {
        SetObjectOwners(adminPlayer, null);
    }
    
    private static void SetObjectOwners(Player adminPlayer, IObjectManager? objectManager)
    {
        Logger.Info("Setting object owners...");
        var allObjects = objectManager != null ? objectManager.GetAllObjects() : ObjectManager.GetAllObjects();
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
                if (objectManager != null)
                {
                    objectManager.UpdateObject(obj);
                }
                else
                {
                    DbProvider.Instance.Update("gameobjects", obj);
                }
                continue;
            }

            // If we reach here, it means we need to assign an owner
            obj.Owner = adminPlayer;
            if (objectManager != null)
            {
                objectManager.UpdateObject(obj);
            }
            else
            {
                ObjectManager.UpdateObject(obj);
            }
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

        SetObjectOwners(admin);
    }
    
    /// <summary>
    /// Creates a default admin player for testing if no players exist (DI version)
    /// </summary>
    private static void CreateDefaultAdminIfNeeded(IPlayerManager playerManager, IObjectManager objectManager, IPermissionManager permissionManager)
    {
        var existingPlayers = playerManager.GetAllPlayers();
        if (existingPlayers.Any())
        {
            Logger.Info($"Found {existingPlayers.Count()} existing players in database.");
            
            // Check if admin player exists and has permissions, if not initialize them
            var adminPlayer = existingPlayers.FirstOrDefault(p => 
                p.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));
            
            if (adminPlayer != null)
            {
                if (!permissionManager.HasFlag(adminPlayer, PermissionManager.Flag.Admin))
                {
                    Logger.Info("Admin player exists but lacks admin permissions. Initializing permissions...");
                    permissionManager.InitializeAdminPermissions(adminPlayer);
                    Logger.Info($"Admin permissions initialized. Flags: {permissionManager.GetFlagsString(adminPlayer)}");
                }
                else
                {
                    Logger.Info($"Admin player found with permissions. Flags: {permissionManager.GetFlagsString(adminPlayer)}");
                }
            }
            
            return;
        }

        Logger.Info("No players found. Creating default admin player...");

        var startingRoom = RoomManager.GetStartingRoom();
        var admin = playerManager.CreatePlayer("Admin", "password", startingRoom?.Id);

        // Initialize proper admin permissions using the permission system
        permissionManager.InitializeAdminPermissions(admin);
        Logger.Info($"Admin permissions initialized. Flags: {permissionManager.GetFlagsString(admin)}");

        // Set some admin-specific properties
        objectManager.SetProperty(admin, "level", 100);
        objectManager.SetProperty(admin, "health", 1000);
        objectManager.SetProperty(admin, "maxHealth", 1000);
        objectManager.SetProperty(admin, "name", "Admin");
        objectManager.SetProperty(admin, "shortDescription", "the Administrator");
        objectManager.SetProperty(admin, "longDescription",
            "The all-powerful administrator of this realm. They have the ability to create and modify the world itself.");

        // Verify permissions are still set after property updates
        var reloadedAdmin = playerManager.FindPlayerByName("Admin");
        if (reloadedAdmin != null)
        {
            var finalFlags = permissionManager.GetFlagsString(reloadedAdmin);
            var hasAdminFlag = permissionManager.HasFlag(reloadedAdmin, PermissionManager.Flag.Admin);
            Logger.Info($"Admin player verification after property updates - Flags: {finalFlags}, Has Admin: {hasAdminFlag}");
            
            // If admin flag is missing, re-initialize
            if (!hasAdminFlag)
            {
                Logger.Warning("Admin flag missing after property updates! Re-initializing permissions...");
                permissionManager.InitializeAdminPermissions(reloadedAdmin);
                Logger.Info($"Re-initialized admin permissions. Flags: {permissionManager.GetFlagsString(reloadedAdmin)}");
            }
        }

        Logger.Info("Default admin player created (username: admin, password: password)");

        SetObjectOwners(admin, objectManager);
    }

    /// <summary>
    /// Shuts down the server gracefully
    /// </summary>
    public static void Shutdown()
    {
        Shutdown(null);
    }
    
    /// <summary>
    /// Shuts down the server gracefully using dependency injection
    /// </summary>
    public static void Shutdown(IServiceProvider? serviceProvider)
    {
        var logger = serviceProvider?.GetService<ILogger>();
        var database = serviceProvider?.GetService<IGameDatabase>();
        
        // Use injected logger if available, otherwise fall back to static
        if (logger != null)
        {
            logger.Info("Shutting down CSMOO Server...");
        }
        else
        {
            Logger.Info("Shutting down CSMOO Server...");
        }
        
        // Shutdown hot reload manager
        var hotReloadManager = serviceProvider?.GetService<IHotReloadManager>();
        if (hotReloadManager != null)
        {
            hotReloadManager.Shutdown();
        }
        else
        {
            HotReloadManager.Shutdown();
        }
        
        var coreHotReloadManager = serviceProvider?.GetService<ICoreHotReloadManager>();
        if (coreHotReloadManager != null)
        {
            coreHotReloadManager.Shutdown();
        }
        else
        {
            CoreHotReloadManager.Shutdown();
        }
        
        // Disconnect all players
        var playerManager = serviceProvider?.GetService<IPlayerManager>();
        if (playerManager != null)
        {
            var onlinePlayers = playerManager.GetOnlinePlayers();
            foreach (var player in onlinePlayers)
            {
                playerManager.DisconnectPlayer(player.Id);
            }
        }
        else
        {
            // Fallback to static for backward compatibility
            var onlinePlayers = PlayerManager.GetOnlinePlayers();
            foreach (var player in onlinePlayers)
            {
                PlayerManager.DisconnectPlayer(player.Id);
            }
        }
        
        // Close database connection - use injected database if available
        if (database != null)
        {
            database.Dispose();
        }
        else
        {
            GameDatabase.Instance.Dispose();
        }
        
        if (logger != null)
        {
            logger.Info("Server shutdown complete.");
        }
        else
        {
            Logger.Info("Server shutdown complete.");
        }
    }
}
