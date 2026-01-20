using CSMOO.Configuration;
using CSMOO.Logging;
using CSMOO.Network;
using CSMOO.Database;
using CSMOO.Init;
using CSMOO.Object;
using CSMOO.Verbs;
using CSMOO.Functions;
using CSMOO.Core;
using CSMOO.Scripting;
using CSMOO.Sessions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace CSMOO;

internal class Program
{
    static void Main(string[] args)
    {
        ServiceProvider? serviceProvider = null;
        
        try
        {
            // Enable Unicode support for console output
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            
            // Build the dependency injection container
            var services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
            
            // Get services from DI
            var config = serviceProvider.GetRequiredService<IConfig>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            
            // Request InstanceManager and PropertyManager FIRST to ensure they're set on ObjectManagerInstance
            // This uses the factory pattern - when these are created, they set themselves on ObjectManagerInstance
            var instanceManager = serviceProvider.GetRequiredService<IInstanceManager>();
            var propertyManager = serviceProvider.GetRequiredService<IPropertyManager>();
            
            // NOW request ObjectManager - it will already have InstanceManager and PropertyManager set
            var objectManager = serviceProvider.GetRequiredService<IObjectManager>();
            var dbProvider = serviceProvider.GetRequiredService<IDbProvider>();
            var playerManager = serviceProvider.GetRequiredService<IPlayerManager>();
            
            // Set ObjectManager on PlayerManagerInstance (needed for circular dependency resolution)
            // This must be done before SessionHandler is used, as SessionHandler uses PlayerManager
            if (playerManager is PlayerManagerInstance pmi)
            {
                pmi.SetObjectManager(objectManager);
            }
            
            // Set static instances for data classes (GameObject, ObjectClass) that can't use DI
            // These are serialized data classes that need static access
            ObjectManager.SetInstance(objectManager);
            if (dbProvider is DbProvider dbp)
            {
                DbProvider.SetInstance(dbp);
            }
            
            // Set InstanceManager static instance (used by ObjectManagerInstance)
            InstanceManager.SetInstance(instanceManager);
            
            // Set PropertyManager static instance (used by ObjectManagerInstance)
            PropertyManager.SetInstance(propertyManager);
            
            // Set SessionHandler static instance (used by Network servers)
            // Get SessionHandler AFTER setting ObjectManager on PlayerManager to ensure it has the correct dependencies
            var sessionHandler = serviceProvider.GetRequiredService<ISessionHandler>();
            SessionHandler.SetInstance(sessionHandler);
            
            // Set Logger static instance (used by ServerInitializer and other components)
            Logger.SetInstance(logger);
            
            // Set RoomManager static instance (used by ServerInitializer)
            var roomManager = serviceProvider.GetRequiredService<IRoomManager>();
            RoomManager.SetInstance(roomManager);
            
            // Set PermissionManager static instance (used by ServerInitializer)
            var permissionManager = serviceProvider.GetRequiredService<IPermissionManager>();
            PermissionManager.SetInstance(permissionManager);
            
            // Set ObjectResolver static instance (used by System.cs verbs: Look, Examine, Get, Drop)
            var objectResolver = serviceProvider.GetRequiredService<IObjectResolver>();
            ObjectResolver.SetInstance(objectResolver);
            
            // Set VerbResolver static instance (used by @verbs command)
            var verbResolver = serviceProvider.GetRequiredService<IVerbResolver>();
            VerbResolver.SetInstance(verbResolver);
            
            // Set GameDatabase static instance (used by @cleanup command)
            var gameDatabase = serviceProvider.GetRequiredService<IGameDatabase>();
            if (gameDatabase is GameDatabase gdb)
            {
                GameDatabase.SetInstance(gdb);
            }
            
            // Initialize logging system (sets up log rotation and directories)
            logger.Initialize();
            
            // Display startup banner
            logger.DisplayBanner();
            
            logger.DisplaySectionHeader("SYSTEM INITIALIZATION");
            logger.Info("Starting CSMOO Server...");
            logger.Info($"Server configuration: Port={config.Server.Port}, ShowDebugInConsole={config.Server.ShowDebugInConsole}");
            logger.Info($"Database files: Game={config.Database.GameDataFile}, Log={config.Database.LogDataFile}");
            
            // Initialize the server and world (pass ServiceProvider for DI)
            ServerInitializer.Initialize(serviceProvider);
            
            // Start both servers (pass serviceProvider for DI)
            var telnetServer = new TelnetServer(config.Server.Port, serviceProvider);
            var webSocketServer = new WebSocketServer(config.Server.WsPort, serviceProvider); // Use next port for WebSocket
            var httpServer = new HttpServer(config, logger, objectManager); // Initialize HTTP server with DI
            
            try
            {
                // Start WebSocket server asynchronously
                _ = Task.Run(async () => await webSocketServer.StartAsync());
                _ = Task.Run(async () => await httpServer.StartAsync());


                // Start Telnet server (blocking call)
                telnetServer.Start();
            }
            catch (Exception ex)
            {
                logger.Error($"An error occurred during server operation", ex);
            }
            finally
            {
                telnetServer.Stop();
                webSocketServer.Stop();
                ServerInitializer.Shutdown(serviceProvider);
                logger.Info("Server has stopped.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        finally
        {
            // Dispose service provider
            serviceProvider?.Dispose();
        }
    }
    
    /// <summary>
    /// Configures dependency injection services
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        // Configuration - singleton (load once, use everywhere)
        services.AddSingleton<IConfig>(sp => Config.Load());
        
        // Logging - singleton (one logger instance for the entire application)
        services.AddSingleton<ILogger>(sp => 
        {
            var config = sp.GetRequiredService<IConfig>();
            return new LoggerInstance(config);
        });
        
        // Database - singleton (one database connection for the entire application)
        services.AddSingleton<IGameDatabase>(sp =>
        {
            var config = sp.GetRequiredService<IConfig>();
            return new GameDatabase(config.Database.GameDataFile);
        });
        
        // DbProvider - singleton (one provider instance for the entire application)
        // Note: ObjectManager will be set later to resolve circular dependency
        services.AddSingleton<IDbProvider>(sp =>
        {
            var db = sp.GetRequiredService<IGameDatabase>();
            return new DbProvider(db);
        });
        
        // PlayerManager - singleton (one player manager instance for the entire application)
        // Note: ObjectManager will be set later in Main() after service provider is built to resolve circular dependency
        services.AddSingleton<IPlayerManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            return new PlayerManagerInstance(dbProvider);
        });
        
        // ClassManager - singleton (one class manager instance for the entire application)
        services.AddSingleton<IClassManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            return new ClassManagerInstance(dbProvider, logger);
        });
        
        // ObjectManager - singleton (one object manager instance for the entire application)
        // Note: Registered before IPropertyManager and IInstanceManager to break circular dependency
        // IMPORTANT: InstanceManager and PropertyManager will be set later via their factory functions
        services.AddSingleton<IObjectManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var classManager = sp.GetRequiredService<IClassManager>();
            var objectManager = new ObjectManagerInstance(dbProvider, classManager);
            
            // Set the object manager in DbProvider to resolve circular dependency
            if (dbProvider is DbProvider dbp)
            {
                dbp.SetObjectManager(objectManager);
            }
            
            // Set the object manager in PlayerManager to resolve circular dependency
            var playerManager = sp.GetRequiredService<IPlayerManager>();
            if (playerManager is PlayerManagerInstance pmi)
            {
                pmi.SetObjectManager(objectManager);
            }
            
            // NOTE: InstanceManager and PropertyManager will be set on objectManager
            // by their respective factory functions when they are created
            
            return objectManager;
        });
        
        // PropertyManager - singleton (one property manager instance for the entire application)
        // Note: Registered after IObjectManager to resolve circular dependency
        services.AddSingleton<IPropertyManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var classManager = sp.GetRequiredService<IClassManager>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var propertyManager = new PropertyManagerInstance(dbProvider, classManager, objectManager);
            
            // Set the property manager in ObjectManagerInstance to complete the circular reference
            if (objectManager is ObjectManagerInstance omi)
            {
                omi.SetPropertyManager(propertyManager);
            }
            
            return propertyManager;
        });
        
        // InstanceManager - singleton (one instance manager instance for the entire application)
        // Note: Registered after IObjectManager and IPropertyManager to resolve circular dependency
        services.AddSingleton<IInstanceManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var classManager = sp.GetRequiredService<IClassManager>();
            // Get ObjectManager BEFORE creating InstanceManager to ensure it exists
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var propertyManager = sp.GetRequiredService<IPropertyManager>();
            
            // Create InstanceManager
            var instanceManager = new InstanceManagerInstance(dbProvider, classManager, objectManager, propertyManager);
            
            // IMMEDIATELY set the instance manager in ObjectManagerInstance to complete the circular reference
            // This MUST happen before any other code uses ObjectManagerInstance
            if (objectManager is ObjectManagerInstance omi)
            {
                omi.SetInstanceManager(instanceManager);
            }
            else
            {
                throw new InvalidOperationException($"Expected ObjectManagerInstance but got {objectManager?.GetType().Name ?? "null"}");
            }
            
            return instanceManager;
        });
        
        // RoomManager - singleton (one room manager instance for the entire application)
        services.AddSingleton<IRoomManager>(sp =>
        {
            // Force InstanceManager to be created first to ensure it's set on ObjectManager
            var _ = sp.GetRequiredService<IInstanceManager>();
            
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            return new RoomManagerInstance(dbProvider, logger, objectManager);
        });
        
        // WorldInitializer - singleton (one world initializer instance for the entire application)
        // Note: Access IInstanceManager to ensure it's created and set on ObjectManager before RoomManager uses it
        services.AddSingleton<IWorldInitializer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger>();
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var playerManager = sp.GetRequiredService<IPlayerManager>();
            // Access InstanceManager to ensure it's created and set on ObjectManager before RoomManager uses it
            var instanceManager = sp.GetRequiredService<IInstanceManager>();
            var roomManager = sp.GetRequiredService<IRoomManager>();
            var coreClassFactory = sp.GetRequiredService<ICoreClassFactory>();
            var verbInitializer = sp.GetRequiredService<IVerbInitializer>();
            var functionInitializer = sp.GetRequiredService<IFunctionInitializer>();
            var propertyInitializer = sp.GetRequiredService<IPropertyInitializer>();
            return new WorldInitializerInstance(logger, dbProvider, objectManager, playerManager, roomManager, coreClassFactory, verbInitializer, functionInitializer, propertyInitializer);
        });
        
        // PermissionManager - singleton (one permission manager instance for the entire application)
        services.AddSingleton<IPermissionManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            return new PermissionManagerInstance(dbProvider, logger);
        });
        
        // CoreClassFactory - singleton (one core class factory instance for the entire application)
        services.AddSingleton<ICoreClassFactory>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            return new CoreClassFactoryInstance(dbProvider, logger);
        });
        
        // VerbInitializer - singleton (one verb initializer instance for the entire application)
        services.AddSingleton<IVerbInitializer>(sp =>
        {
            // Force InstanceManager to be created first to ensure it's set on ObjectManager
            var _ = sp.GetRequiredService<IInstanceManager>();
            
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            return new VerbInitializerInstance(dbProvider, logger, objectManager);
        });
        
        // FunctionInitializer - singleton (one function initializer instance for the entire application)
        services.AddSingleton<IFunctionInitializer>(sp =>
        {
            // Force InstanceManager to be created first to ensure it's set on ObjectManager
            var _ = sp.GetRequiredService<IInstanceManager>();
            
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var functionManager = sp.GetRequiredService<IFunctionManager>();
            return new FunctionInitializerInstance(dbProvider, logger, objectManager, functionManager);
        });
        
        // PropertyInitializer - singleton (one property initializer instance for the entire application)
        services.AddSingleton<IPropertyInitializer>(sp =>
        {
            // Force InstanceManager to be created first to ensure it's set on ObjectManager
            var _ = sp.GetRequiredService<IInstanceManager>();
            
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            return new PropertyInitializerInstance(dbProvider, logger, objectManager);
        });
        
        // HotReloadManager - singleton (one hot reload manager instance for the entire application)
        services.AddSingleton<IHotReloadManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger>();
            var config = sp.GetRequiredService<IConfig>();
            var verbInitializer = sp.GetRequiredService<IVerbInitializer>();
            var functionInitializer = sp.GetRequiredService<IFunctionInitializer>();
            var playerManager = sp.GetRequiredService<IPlayerManager>();
            return new HotReloadManagerInstance(logger, config, verbInitializer, functionInitializer, playerManager);
        });
        
        // CoreHotReloadManager - singleton (one core hot reload manager instance for the entire application)
        services.AddSingleton<ICoreHotReloadManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger>();
            var playerManager = sp.GetRequiredService<IPlayerManager>();
            var permissionManager = sp.GetRequiredService<IPermissionManager>();
            return new CoreHotReloadManagerInstance(logger, playerManager, permissionManager);
        });
        
        // VerbManager - singleton (one verb manager instance for the entire application)
        services.AddSingleton<IVerbManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            return new VerbManagerInstance(dbProvider);
        });
        
        // FunctionManager - singleton (one function manager instance for the entire application)
        services.AddSingleton<IFunctionManager>(sp =>
        {
            var gameDatabase = sp.GetRequiredService<IGameDatabase>();
            return new FunctionManagerInstance(gameDatabase);
        });
        
        // VerbResolver - singleton (one verb resolver instance for the entire application)
        services.AddSingleton<IVerbResolver>(sp =>
        {
            // Force InstanceManager to be created first to ensure it's set on ObjectManager
            var _ = sp.GetRequiredService<IInstanceManager>();
            
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var logger = sp.GetRequiredService<ILogger>();
            return new VerbResolverInstance(dbProvider, objectManager, logger);
        });
        
        // ObjectResolver - singleton (one object resolver instance for the entire application)
        services.AddSingleton<IObjectResolver>(sp =>
        {
            // Force InstanceManager to be created first to ensure it's set on ObjectManager
            var _ = sp.GetRequiredService<IInstanceManager>();
            
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var coreClassFactory = sp.GetRequiredService<ICoreClassFactory>();
            return new ObjectResolverInstance(objectManager, coreClassFactory);
        });
        
        // FunctionResolver - singleton (one function resolver instance for the entire application)
        services.AddSingleton<IFunctionResolver>(sp =>
        {
            // Force InstanceManager to be created first to ensure it's set on ObjectManager
            var _ = sp.GetRequiredService<IInstanceManager>();
            
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            return new FunctionResolverInstance(dbProvider, objectManager);
        });
        
        // CompilationCache - singleton (one cache instance for the entire application)
        services.AddSingleton<CSMOO.Scripting.ICompilationCache>(sp => new CSMOO.Scripting.CompilationCache());
        
        // ScriptPrecompiler - singleton (one precompiler instance for the entire application)
        services.AddSingleton<CSMOO.Scripting.IScriptPrecompiler>(sp =>
        {
            // Force InstanceManager to be created first to ensure it's set on ObjectManager
            var _ = sp.GetRequiredService<IInstanceManager>();
            
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var logger = sp.GetRequiredService<ILogger>();
            var config = sp.GetRequiredService<IConfig>();
            var objectResolver = sp.GetRequiredService<IObjectResolver>();
            var verbResolver = sp.GetRequiredService<IVerbResolver>();
            var functionResolver = sp.GetRequiredService<IFunctionResolver>();
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var playerManager = sp.GetRequiredService<IPlayerManager>();
            var verbManager = sp.GetRequiredService<IVerbManager>();
            var roomManager = sp.GetRequiredService<IRoomManager>();
            return new CSMOO.Scripting.ScriptPrecompiler(objectManager, logger, config, objectResolver, verbResolver, functionResolver, dbProvider, playerManager, verbManager, roomManager);
        });
        
        // CompilationInitializer - singleton (one initializer instance for the entire application)
        services.AddSingleton<CSMOO.Scripting.ICompilationInitializer>(sp =>
        {
            var precompiler = sp.GetRequiredService<CSMOO.Scripting.IScriptPrecompiler>();
            var cache = sp.GetRequiredService<CSMOO.Scripting.ICompilationCache>();
            var verbManager = sp.GetRequiredService<IVerbManager>();
            var functionManager = sp.GetRequiredService<IFunctionManager>();
            var logger = sp.GetRequiredService<ILogger>();
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            return new CSMOO.Scripting.CompilationInitializer(precompiler, cache, verbManager, functionManager, logger, dbProvider);
        });
        
        // ScriptEngineFactory - singleton (one factory instance for the entire application)
        services.AddSingleton<IScriptEngineFactory>(sp =>
        {
            // Force InstanceManager to be created first to ensure it's set on ObjectManager
            var _ = sp.GetRequiredService<IInstanceManager>();
            
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var logger = sp.GetRequiredService<ILogger>();
            var config = sp.GetRequiredService<IConfig>();
            var objectResolver = sp.GetRequiredService<IObjectResolver>();
            var verbResolver = sp.GetRequiredService<IVerbResolver>();
            var functionResolver = sp.GetRequiredService<IFunctionResolver>();
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var playerManager = sp.GetRequiredService<IPlayerManager>();
            var verbManager = sp.GetRequiredService<IVerbManager>();
            var roomManager = sp.GetRequiredService<IRoomManager>();
            var compilationCache = sp.GetRequiredService<CSMOO.Scripting.ICompilationCache>();
            return new ScriptEngineFactory(objectManager, logger, config, objectResolver, verbResolver, functionResolver, dbProvider, playerManager, verbManager, roomManager, compilationCache);
        });
        
        // SessionHandler - singleton (one session handler instance for the entire application)
        // Note: Request IObjectManager first to ensure it's created and sets ObjectManager on PlayerManager
        services.AddSingleton<ISessionHandler>(sp =>
        {
            // Request ObjectManager first to ensure it's created and sets ObjectManager on PlayerManager
            var _ = sp.GetRequiredService<IObjectManager>();
            var playerManager = sp.GetRequiredService<IPlayerManager>();
            return new SessionHandlerInstance(playerManager);
        });
    }
}

