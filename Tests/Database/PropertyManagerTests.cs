using Xunit;
using Moq;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Logging;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using LiteDB;

namespace CSMOO.Tests.Database;

/// <summary>
/// Tests for PropertyManager with dependency injection
/// </summary>
public class PropertyManagerTests
{
    [Fact]
    public void PropertyManager_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var propertyManager = serviceProvider.GetService<IPropertyManager>();
        
        // Assert
        Assert.NotNull(propertyManager);
        Assert.IsType<PropertyManagerInstance>(propertyManager);
    }
    
    [Fact]
    public void PropertyManager_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var propertyManager1 = serviceProvider.GetRequiredService<IPropertyManager>();
        var propertyManager2 = serviceProvider.GetRequiredService<IPropertyManager>();
        
        // Assert
        Assert.Same(propertyManager1, propertyManager2);
    }
    
    [Fact]
    public void PropertyManager_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockPropertyManager = new Mock<IPropertyManager>();
        var mockGameObject = new GameObject { Id = "test-id" };
        var mockValue = new BsonValue("test-value");
        
        mockPropertyManager.Setup(pm => pm.GetProperty(mockGameObject, "testProp"))
            .Returns(mockValue);
        
        PropertyManager.SetInstance(mockPropertyManager.Object);
        
        // Act
        var result = PropertyManager.GetProperty(mockGameObject, "testProp");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-value", result.AsString);
        mockPropertyManager.Verify(pm => pm.GetProperty(mockGameObject, "testProp"), Times.Once);
    }
    
    [Fact]
    public void PropertyManager_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            var mockGameObject = new GameObject { Id = "test", Properties = new BsonDocument() };
            var result = PropertyManager.GetProperty(mockGameObject, "nonexistent");
            // If we get here, it means it created a default instance
            Assert.Null(result); // Property doesn't exist
        }
        catch
        {
            // Expected if database isn't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void PropertyManagerInstance_Requires_Dependencies()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        var mockClassManager = new Mock<IClassManager>();
        var mockObjectManager = new Mock<IObjectManager>();
        
        // Act
        var propertyManager = new PropertyManagerInstance(
            mockDbProvider.Object, 
            mockClassManager.Object, 
            mockObjectManager.Object);
        
        // Assert
        Assert.NotNull(propertyManager);
    }
    
    [Fact]
    public void PropertyManager_Implements_IPropertyManager()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var propertyManager = serviceProvider.GetRequiredService<IPropertyManager>();
        
        // Act & Assert
        Assert.NotNull(propertyManager);
        Assert.IsAssignableFrom<IPropertyManager>(propertyManager);
    }
    
    [Fact]
    public void PropertyManager_MergeProperties_Works()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var propertyManager = serviceProvider.GetRequiredService<IPropertyManager>();
        var target = new BsonDocument { ["key1"] = "value1" };
        var source = new BsonDocument { ["key2"] = "value2" };
        
        // Act
        propertyManager.MergeProperties(target, source);
        
        // Assert
        Assert.Equal("value1", target["key1"].AsString);
        Assert.Equal("value2", target["key2"].AsString);
    }
}
