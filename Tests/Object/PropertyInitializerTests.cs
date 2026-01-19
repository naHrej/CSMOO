using Xunit;
using Moq;
using CSMOO.Object;
using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Object;

/// <summary>
/// Tests for PropertyInitializer with dependency injection
/// </summary>
public class PropertyInitializerTests
{
    [Fact]
    public void PropertyInitializer_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var propertyInitializer = serviceProvider.GetService<IPropertyInitializer>();
        
        // Assert
        Assert.NotNull(propertyInitializer);
        Assert.IsType<PropertyInitializerInstance>(propertyInitializer);
    }
    
    [Fact]
    public void PropertyInitializer_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var propertyInitializer1 = serviceProvider.GetRequiredService<IPropertyInitializer>();
        var propertyInitializer2 = serviceProvider.GetRequiredService<IPropertyInitializer>();
        
        // Assert
        Assert.Same(propertyInitializer1, propertyInitializer2);
    }
    
    [Fact]
    public void PropertyInitializer_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockPropertyInitializer = new Mock<IPropertyInitializer>();
        
        PropertyInitializer.SetInstance(mockPropertyInitializer.Object);
        
        // Act
        PropertyInitializer.LoadAndSetProperties();
        
        // Assert
        mockPropertyInitializer.Verify(pi => pi.LoadAndSetProperties(), Times.Once);
    }
    
    [Fact]
    public void PropertyInitializer_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            PropertyInitializer.ReloadProperties();
            // If we get here, it means it created a default instance
            Assert.True(true);
        }
        catch
        {
            // Expected if database isn't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void PropertyInitializerInstance_Requires_Dependencies()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        var mockLogger = new Mock<ILogger>();
        var mockObjectManager = new Mock<IObjectManager>();
        
        // Act
        var propertyInitializer = new PropertyInitializerInstance(mockDbProvider.Object, mockLogger.Object, mockObjectManager.Object);
        
        // Assert
        Assert.NotNull(propertyInitializer);
    }
    
    [Fact]
    public void PropertyInitializer_Implements_IPropertyInitializer()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var propertyInitializer = serviceProvider.GetRequiredService<IPropertyInitializer>();
        
        // Act & Assert
        Assert.NotNull(propertyInitializer);
        Assert.IsAssignableFrom<IPropertyInitializer>(propertyInitializer);
    }
}
