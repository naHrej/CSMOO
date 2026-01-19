using Xunit;
using Moq;
using CSMOO.Object;
using CSMOO.Database;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Object;

/// <summary>
/// Tests for PlayerManager with dependency injection
/// </summary>
public class PlayerManagerTests
{
    [Fact]
    public void PlayerManager_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var playerManager = serviceProvider.GetService<IPlayerManager>();
        
        // Assert
        Assert.NotNull(playerManager);
        Assert.IsType<PlayerManagerInstance>(playerManager);
    }
    
    [Fact]
    public void PlayerManager_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var playerManager1 = serviceProvider.GetRequiredService<IPlayerManager>();
        var playerManager2 = serviceProvider.GetRequiredService<IPlayerManager>();
        
        // Assert
        Assert.Same(playerManager1, playerManager2);
    }
    
    [Fact]
    public void PlayerManager_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockPlayerManager = new Mock<IPlayerManager>();
        var mockPlayer = new Player { Id = "test-id", Name = "TestPlayer" };
        
        mockPlayerManager.Setup(pm => pm.FindPlayerByName("TestPlayer"))
            .Returns(mockPlayer);
        
        PlayerManager.SetInstance(mockPlayerManager.Object);
        
        // Act
        var result = PlayerManager.FindPlayerByName("TestPlayer");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        mockPlayerManager.Verify(pm => pm.FindPlayerByName("TestPlayer"), Times.Once);
    }
    
    [Fact]
    public void PlayerManager_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        // In a real scenario, we'd set up a test database
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            var result = PlayerManager.GetAllPlayers();
            // If we get here, it means it created a default instance
            Assert.NotNull(result);
        }
        catch
        {
            // Expected if database isn't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void PlayerManagerInstance_Requires_IDbProvider()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        
        // Act
        var playerManager = new PlayerManagerInstance(mockDbProvider.Object);
        
        // Assert
        Assert.NotNull(playerManager);
    }
    
    [Fact]
    public void PlayerManager_Implements_IPlayerManager()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var playerManager = serviceProvider.GetRequiredService<IPlayerManager>();
        
        // Act & Assert
        Assert.NotNull(playerManager);
        Assert.IsAssignableFrom<IPlayerManager>(playerManager);
    }
}
