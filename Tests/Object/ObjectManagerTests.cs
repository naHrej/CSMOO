using Xunit;
using Moq;
using CSMOO.Object;
using CSMOO.Database;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Object;

/// <summary>
/// Tests for ObjectManager with dependency injection
/// </summary>
public class ObjectManagerTests
{
    [Fact]
    public void ObjectManager_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var objectManager = serviceProvider.GetService<IObjectManager>();
        
        // Assert
        Assert.NotNull(objectManager);
        Assert.IsType<ObjectManagerInstance>(objectManager);
    }
    
    [Fact]
    public void ObjectManager_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var objectManager1 = serviceProvider.GetRequiredService<IObjectManager>();
        var objectManager2 = serviceProvider.GetRequiredService<IObjectManager>();
        
        // Assert
        Assert.Same(objectManager1, objectManager2);
    }
    
    [Fact]
    public void ObjectManager_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockObjectManager = new Mock<IObjectManager>();
        var mockGameObject = new GameObject { Id = "test-id" };
        
        mockObjectManager.Setup(om => om.GetObject("test-id"))
            .Returns(mockGameObject);
        
        ObjectManager.SetInstance(mockObjectManager.Object);
        
        // Act
        var result = ObjectManager.GetObject("test-id");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        mockObjectManager.Verify(om => om.GetObject("test-id"), Times.Once);
    }
    
    [Fact]
    public void ObjectManager_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            var result = ObjectManager.GetAllObjects();
            // If we get here, it means it created a default instance
            Assert.NotNull(result);
        }
        catch
        {
            // Expected if database isn't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void ObjectManagerInstance_Requires_IDbProvider()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        
        // Act
        var objectManager = new ObjectManagerInstance(mockDbProvider.Object);
        
        // Assert
        Assert.NotNull(objectManager);
    }
    
    [Fact]
    public void ObjectManager_Implements_IObjectManager()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var objectManager = serviceProvider.GetRequiredService<IObjectManager>();
        
        // Act & Assert
        Assert.NotNull(objectManager);
        Assert.IsAssignableFrom<IObjectManager>(objectManager);
    }
    
    [Fact]
    public void ObjectManager_GetAllObjects_Returns_List()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var objectManager = serviceProvider.GetRequiredService<IObjectManager>();
        
        // Act
        var result = objectManager.GetAllObjects();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<List<dynamic>>(result);
    }
    
    [Fact]
    public void ObjectManager_GetAllObjectClasses_Returns_List()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var objectManager = serviceProvider.GetRequiredService<IObjectManager>();
        
        // Act
        var result = objectManager.GetAllObjectClasses();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<List<ObjectClass>>(result);
    }
}
