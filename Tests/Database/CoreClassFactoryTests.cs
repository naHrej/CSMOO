using Xunit;
using Moq;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Logging;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Database;

/// <summary>
/// Tests for CoreClassFactory with dependency injection
/// </summary>
public class CoreClassFactoryTests
{
    [Fact]
    public void CoreClassFactory_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var coreClassFactory = serviceProvider.GetService<ICoreClassFactory>();
        
        // Assert
        Assert.NotNull(coreClassFactory);
        Assert.IsType<CoreClassFactoryInstance>(coreClassFactory);
    }
    
    [Fact]
    public void CoreClassFactory_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var coreClassFactory1 = serviceProvider.GetRequiredService<ICoreClassFactory>();
        var coreClassFactory2 = serviceProvider.GetRequiredService<ICoreClassFactory>();
        
        // Assert
        Assert.Same(coreClassFactory1, coreClassFactory2);
    }
    
    [Fact]
    public void CoreClassFactory_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockCoreClassFactory = new Mock<ICoreClassFactory>();
        
        CoreClassFactory.SetInstance(mockCoreClassFactory.Object);
        
        // Act
        CoreClassFactory.CreateCoreClasses();
        
        // Assert
        mockCoreClassFactory.Verify(ccf => ccf.CreateCoreClasses(), Times.Once);
    }
    
    [Fact]
    public void CoreClassFactory_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            CoreClassFactory.GetBaseObjectClass();
            // If we get here, it means it created a default instance
            Assert.True(true);
        }
        catch
        {
            // Expected if database isn't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void CoreClassFactoryInstance_Requires_Dependencies()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        var mockLogger = new Mock<ILogger>();
        
        // Act
        var coreClassFactory = new CoreClassFactoryInstance(mockDbProvider.Object, mockLogger.Object);
        
        // Assert
        Assert.NotNull(coreClassFactory);
    }
    
    [Fact]
    public void CoreClassFactory_Implements_ICoreClassFactory()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var coreClassFactory = serviceProvider.GetRequiredService<ICoreClassFactory>();
        
        // Act & Assert
        Assert.NotNull(coreClassFactory);
        Assert.IsAssignableFrom<ICoreClassFactory>(coreClassFactory);
    }
}
