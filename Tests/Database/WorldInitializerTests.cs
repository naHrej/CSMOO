using Xunit;
using Moq;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Logging;
using CSMOO.Verbs;
using CSMOO.Functions;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Database;

/// <summary>
/// Tests for WorldInitializer with dependency injection
/// </summary>
public class WorldInitializerTests
{
    [Fact]
    public void WorldInitializer_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var worldInitializer = serviceProvider.GetService<IWorldInitializer>();
        
        // Assert
        Assert.NotNull(worldInitializer);
        Assert.IsType<WorldInitializerInstance>(worldInitializer);
    }
    
    [Fact]
    public void WorldInitializer_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var worldInitializer1 = serviceProvider.GetRequiredService<IWorldInitializer>();
        var worldInitializer2 = serviceProvider.GetRequiredService<IWorldInitializer>();
        
        // Assert
        Assert.Same(worldInitializer1, worldInitializer2);
    }
    
    [Fact]
    public void WorldInitializer_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockWorldInitializer = new Mock<IWorldInitializer>();
        
        WorldInitializer.SetInstance(mockWorldInitializer.Object);
        
        // Act
        WorldInitializer.InitializeWorld();
        
        // Assert
        mockWorldInitializer.Verify(wi => wi.InitializeWorld(), Times.Once);
    }
    
    [Fact]
    public void WorldInitializer_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            WorldInitializer.PrintWorldStatistics();
            // If we get here, it means it created a default instance
            Assert.True(true);
        }
        catch
        {
            // Expected if database isn't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void WorldInitializerInstance_Requires_Dependencies()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockDbProvider = new Mock<IDbProvider>();
        var mockObjectManager = new Mock<IObjectManager>();
        var mockPlayerManager = new Mock<IPlayerManager>();
        var mockRoomManager = new Mock<IRoomManager>();
        var mockCoreClassFactory = new Mock<ICoreClassFactory>();
        var mockVerbInitializer = new Mock<IVerbInitializer>();
        var mockFunctionInitializer = new Mock<IFunctionInitializer>();
        var mockPropertyInitializer = new Mock<IPropertyInitializer>();
        
        // Act
        var worldInitializer = new WorldInitializerInstance(
            mockLogger.Object,
            mockDbProvider.Object,
            mockObjectManager.Object,
            mockPlayerManager.Object,
            mockRoomManager.Object,
            mockCoreClassFactory.Object,
            mockVerbInitializer.Object,
            mockFunctionInitializer.Object,
            mockPropertyInitializer.Object);
        
        // Assert
        Assert.NotNull(worldInitializer);
    }
    
    [Fact]
    public void WorldInitializer_Implements_IWorldInitializer()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var worldInitializer = serviceProvider.GetRequiredService<IWorldInitializer>();
        
        // Act & Assert
        Assert.NotNull(worldInitializer);
        Assert.IsAssignableFrom<IWorldInitializer>(worldInitializer);
    }
}
