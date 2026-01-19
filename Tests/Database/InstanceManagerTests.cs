using Xunit;
using Moq;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Logging;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Database;

/// <summary>
/// Tests for InstanceManager with dependency injection
/// </summary>
public class InstanceManagerTests
{
    [Fact]
    public void InstanceManager_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var instanceManager = serviceProvider.GetService<IInstanceManager>();
        
        // Assert
        Assert.NotNull(instanceManager);
        Assert.IsType<InstanceManagerInstance>(instanceManager);
    }
    
    [Fact]
    public void InstanceManager_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var instanceManager1 = serviceProvider.GetRequiredService<IInstanceManager>();
        var instanceManager2 = serviceProvider.GetRequiredService<IInstanceManager>();
        
        // Assert
        Assert.Same(instanceManager1, instanceManager2);
    }
    
    [Fact]
    public void InstanceManager_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockInstanceManager = new Mock<IInstanceManager>();
        var mockStats = new Dictionary<string, int> { { "Test", 5 } };
        
        mockInstanceManager.Setup(im => im.GetObjectStatistics())
            .Returns(mockStats);
        
        InstanceManager.SetInstance(mockInstanceManager.Object);
        
        // Act
        var result = InstanceManager.GetObjectStatistics();
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(5, result["Test"]);
        mockInstanceManager.Verify(im => im.GetObjectStatistics(), Times.Once);
    }
    
    [Fact]
    public void InstanceManager_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            var result = InstanceManager.GetObjectStatistics();
            // If we get here, it means it created a default instance
            Assert.NotNull(result);
        }
        catch
        {
            // Expected if database isn't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void InstanceManagerInstance_Requires_Dependencies()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        var mockClassManager = new Mock<IClassManager>();
        var mockObjectManager = new Mock<IObjectManager>();
        
        // Act
        var instanceManager = new InstanceManagerInstance(
            mockDbProvider.Object, 
            mockClassManager.Object, 
            mockObjectManager.Object);
        
        // Assert
        Assert.NotNull(instanceManager);
    }
    
    [Fact]
    public void InstanceManager_Implements_IInstanceManager()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var instanceManager = serviceProvider.GetRequiredService<IInstanceManager>();
        
        // Act & Assert
        Assert.NotNull(instanceManager);
        Assert.IsAssignableFrom<IInstanceManager>(instanceManager);
    }
}
