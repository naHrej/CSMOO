using Microsoft.Extensions.DependencyInjection;
using CSMOO.Configuration;
using CSMOO.Logging;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Verbs;
using CSMOO.Functions;
using CSMOO.Init;
using CSMOO.Core;
using Moq;

namespace CSMOO.Tests.TestHelpers;

/// <summary>
/// Helper class for setting up dependency injection in tests
/// </summary>
public static class ServiceProviderHelper
{
    /// <summary>
    /// Creates a service provider with all real implementations
    /// </summary>
    public static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }
    
    /// <summary>
    /// Creates a service provider with mocked dependencies
    /// </summary>
    public static ServiceProvider CreateServiceProviderWithMocks(
        Mock<IConfig>? mockConfig = null,
        Mock<ILogger>? mockLogger = null,
        Mock<IDatabase>? mockDatabase = null,
        Mock<IDbProvider>? mockDbProvider = null)
    {
        var services = new ServiceCollection();
        
        // Use mocks if provided, otherwise use real implementations
        if (mockConfig != null)
        {
            services.AddSingleton<IConfig>(mockConfig.Object);
        }
        else
        {
            services.AddSingleton<IConfig>(sp => Config.Load());
        }
        
        if (mockLogger != null)
        {
            services.AddSingleton<ILogger>(mockLogger.Object);
        }
        else
        {
            services.AddSingleton<ILogger>(sp => 
            {
                var config = sp.GetRequiredService<IConfig>();
                return new LoggerInstance(config);
            });
        }
        
        if (mockDatabase != null)
        {
            services.AddSingleton<IDatabase>(mockDatabase.Object);
        }
        else
        {
            services.AddSingleton<IDatabase>(sp =>
            {
                var config = sp.GetRequiredService<IConfig>();
                return new Database.Implementations.LiteDbDatabase(config.Database.GameDataFile);
            });
        }
        
        if (mockDbProvider != null)
        {
            services.AddSingleton<IDbProvider>(mockDbProvider.Object);
        }
        else
        {
            services.AddSingleton<IDbProvider>(sp =>
            {
                var db = sp.GetRequiredService<IDatabase>();
                return new DbProvider(db);
            });
        }
        
        return services.BuildServiceProvider();
    }
    
    /// <summary>
    /// Configures services (same as Program.cs)
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Configuration - singleton
        services.AddSingleton<IConfig>(sp => Config.Load());
        
        // Logging - singleton
        services.AddSingleton<ILogger>(sp => 
        {
            var config = sp.GetRequiredService<IConfig>();
            return new LoggerInstance(config);
        });
        
        // Database - singleton
        services.AddSingleton<IDatabase>(sp =>
        {
            var config = sp.GetRequiredService<IConfig>();
            return new Database.Implementations.LiteDbDatabase(config.Database.GameDataFile);
        });
        
        // DbProvider - singleton
        services.AddSingleton<IDbProvider>(sp =>
        {
            var db = sp.GetRequiredService<IDatabase>();
            return new DbProvider(db);
        });
        
        // PlayerManager - singleton
        services.AddSingleton<IPlayerManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            return new PlayerManagerInstance(dbProvider);
        });
        
        // ClassManager - singleton
        services.AddSingleton<IClassManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            return new ClassManagerInstance(dbProvider, logger);
        });
        
        // ObjectManager - singleton
        services.AddSingleton<IObjectManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var classManager = sp.GetRequiredService<IClassManager>();
            return new ObjectManagerInstance(dbProvider, classManager);
        });
        
        // PropertyManager - singleton
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
        
        // InstanceManager - singleton
        services.AddSingleton<IInstanceManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var classManager = sp.GetRequiredService<IClassManager>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var propertyManager = sp.GetRequiredService<IPropertyManager>();
            var instanceManager = new InstanceManagerInstance(dbProvider, classManager, objectManager, propertyManager);
            
            // Set the instance manager in ObjectManagerInstance to complete the circular reference
            if (objectManager is ObjectManagerInstance omi)
            {
                omi.SetInstanceManager(instanceManager);
            }
            
            return instanceManager;
        });
        
        // RoomManager - singleton
        services.AddSingleton<IRoomManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            return new RoomManagerInstance(dbProvider, logger, objectManager);
        });
        
        // CoreClassFactory - singleton
        services.AddSingleton<ICoreClassFactory>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            return new CoreClassFactoryInstance(dbProvider, logger);
        });
        
        // VerbInitializer - singleton
        services.AddSingleton<IVerbInitializer>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            return new VerbInitializerInstance(dbProvider, logger, objectManager);
        });
        
        // FunctionInitializer - singleton
        services.AddSingleton<IFunctionInitializer>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var functionManager = sp.GetRequiredService<IFunctionManager>();
            return new FunctionInitializerInstance(dbProvider, logger, objectManager, functionManager);
        });
        
        // PropertyInitializer - singleton
        services.AddSingleton<IPropertyInitializer>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            return new PropertyInitializerInstance(dbProvider, logger, objectManager);
        });
        
        // HotReloadManager - singleton
        services.AddSingleton<IHotReloadManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger>();
            var config = sp.GetRequiredService<IConfig>();
            var verbInitializer = sp.GetRequiredService<IVerbInitializer>();
            var functionInitializer = sp.GetRequiredService<IFunctionInitializer>();
            var playerManager = sp.GetRequiredService<IPlayerManager>();
            return new HotReloadManagerInstance(logger, config, verbInitializer, functionInitializer, playerManager);
        });
        
        // CoreHotReloadManager - singleton
        services.AddSingleton<ICoreHotReloadManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger>();
            var playerManager = sp.GetRequiredService<IPlayerManager>();
            var permissionManager = sp.GetRequiredService<IPermissionManager>();
            return new CoreHotReloadManagerInstance(logger, playerManager, permissionManager);
        });
        
        // VerbManager - singleton
        services.AddSingleton<IVerbManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            return new VerbManagerInstance(dbProvider);
        });
        
        // FunctionManager - singleton
        services.AddSingleton<IFunctionManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            return new FunctionManagerInstance(dbProvider);
        });
        
        // VerbResolver - singleton
        services.AddSingleton<IVerbResolver>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var logger = sp.GetRequiredService<ILogger>();
            return new VerbResolverInstance(dbProvider, objectManager, logger);
        });
        
        // ObjectResolver - singleton
        services.AddSingleton<IObjectResolver>(sp =>
        {
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var coreClassFactory = sp.GetRequiredService<ICoreClassFactory>();
            return new ObjectResolverInstance(objectManager, coreClassFactory);
        });
        
        // FunctionResolver - singleton
        services.AddSingleton<IFunctionResolver>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            return new FunctionResolverInstance(dbProvider, objectManager);
        });
        
        // WorldInitializer - singleton
        services.AddSingleton<IWorldInitializer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger>();
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var objectManager = sp.GetRequiredService<IObjectManager>();
            var playerManager = sp.GetRequiredService<IPlayerManager>();
            var roomManager = sp.GetRequiredService<IRoomManager>();
            var coreClassFactory = sp.GetRequiredService<ICoreClassFactory>();
            var verbInitializer = sp.GetRequiredService<IVerbInitializer>();
            var functionInitializer = sp.GetRequiredService<IFunctionInitializer>();
            var propertyInitializer = sp.GetRequiredService<IPropertyInitializer>();
            return new WorldInitializerInstance(logger, dbProvider, objectManager, playerManager, roomManager, coreClassFactory, verbInitializer, functionInitializer, propertyInitializer);
        });
        
        // PermissionManager - singleton
        services.AddSingleton<IPermissionManager>(sp =>
        {
            var dbProvider = sp.GetRequiredService<IDbProvider>();
            var logger = sp.GetRequiredService<ILogger>();
            return new PermissionManagerInstance(dbProvider, logger);
        });
    }
}
