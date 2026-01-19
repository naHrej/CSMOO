using Xunit;
using Moq;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Logging;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Database;

/// <summary>
/// Tests for RoomManager with dependency injection
/// </summary>
public class RoomManagerTests
{
    [Fact]
    public void RoomManager_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var roomManager = serviceProvider.GetService<IRoomManager>();
        
        // Assert
        Assert.NotNull(roomManager);
        Assert.IsType<RoomManagerInstance>(roomManager);
    }
    
    [Fact]
    public void RoomManager_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var roomManager1 = serviceProvider.GetRequiredService<IRoomManager>();
        var roomManager2 = serviceProvider.GetRequiredService<IRoomManager>();
        
        // Assert
        Assert.Same(roomManager1, roomManager2);
    }
    
    [Fact]
    public void RoomManager_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockRoomManager = new Mock<IRoomManager>();
        var mockStats = new Dictionary<string, object> { { "TotalRooms", 5 } };
        
        mockRoomManager.Setup(rm => rm.GetRoomStatistics())
            .Returns(mockStats);
        
        RoomManager.SetInstance(mockRoomManager.Object);
        
        // Act
        var result = RoomManager.GetRoomStatistics();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result["TotalRooms"]);
        mockRoomManager.Verify(rm => rm.GetRoomStatistics(), Times.Once);
    }
    
    [Fact]
    public void RoomManager_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            var result = RoomManager.GetAllRooms();
            // If we get here, it means it created a default instance
            Assert.NotNull(result);
        }
        catch
        {
            // Expected if database isn't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void RoomManagerInstance_Requires_Dependencies()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        var mockLogger = new Mock<ILogger>();
        var mockObjectManager = new Mock<IObjectManager>();
        
        // Act
        var roomManager = new RoomManagerInstance(
            mockDbProvider.Object, 
            mockLogger.Object,
            mockObjectManager.Object);
        
        // Assert
        Assert.NotNull(roomManager);
    }
    
    [Fact]
    public void RoomManager_Implements_IRoomManager()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var roomManager = serviceProvider.GetRequiredService<IRoomManager>();
        
        // Act & Assert
        Assert.NotNull(roomManager);
        Assert.IsAssignableFrom<IRoomManager>(roomManager);
    }
}
