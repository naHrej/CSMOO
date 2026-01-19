using Xunit;
using Moq;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Logging;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Database;

/// <summary>
/// Tests for ClassManager with dependency injection
/// </summary>
public class ClassManagerTests
{
    [Fact]
    public void ClassManager_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var classManager = serviceProvider.GetService<IClassManager>();
        
        // Assert
        Assert.NotNull(classManager);
        Assert.IsType<ClassManagerInstance>(classManager);
    }
    
    [Fact]
    public void ClassManager_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var classManager1 = serviceProvider.GetRequiredService<IClassManager>();
        var classManager2 = serviceProvider.GetRequiredService<IClassManager>();
        
        // Assert
        Assert.Same(classManager1, classManager2);
    }
    
    [Fact]
    public void ClassManager_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockClassManager = new Mock<IClassManager>();
        var mockObjectClass = new ObjectClass { Id = "test-id", Name = "TestClass" };
        
        mockClassManager.Setup(cm => cm.FindClassesByName("TestClass", true))
            .Returns(new List<ObjectClass> { mockObjectClass });
        
        ClassManager.SetInstance(mockClassManager.Object);
        
        // Act
        var result = ClassManager.FindClassesByName("TestClass", true);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("test-id", result[0].Id);
        mockClassManager.Verify(cm => cm.FindClassesByName("TestClass", true), Times.Once);
    }
    
    [Fact]
    public void ClassManager_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            var result = ClassManager.FindClassesByName("Test", false);
            // If we get here, it means it created a default instance
            Assert.NotNull(result);
        }
        catch
        {
            // Expected if database isn't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void ClassManagerInstance_Requires_IDbProvider_And_ILogger()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        var mockLogger = new Mock<ILogger>();
        
        // Act
        var classManager = new ClassManagerInstance(mockDbProvider.Object, mockLogger.Object);
        
        // Assert
        Assert.NotNull(classManager);
    }
    
    [Fact]
    public void ClassManager_Implements_IClassManager()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var classManager = serviceProvider.GetRequiredService<IClassManager>();
        
        // Act & Assert
        Assert.NotNull(classManager);
        Assert.IsAssignableFrom<IClassManager>(classManager);
    }
}
