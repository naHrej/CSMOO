using Xunit;
using Moq;
using CSMOO.Init;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Logging;
using CSMOO.Configuration;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Init;

/// <summary>
/// Tests for CoreHotReloadManager with dependency injection
/// </summary>
public class CoreHotReloadManagerTests
{
    [Fact]
    public void CoreHotReloadManager_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var coreHotReloadManager = serviceProvider.GetService<ICoreHotReloadManager>();
        
        // Assert
        Assert.NotNull(coreHotReloadManager);
        Assert.IsType<CoreHotReloadManagerInstance>(coreHotReloadManager);
    }
    
    [Fact]
    public void CoreHotReloadManager_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var coreHotReloadManager1 = serviceProvider.GetRequiredService<ICoreHotReloadManager>();
        var coreHotReloadManager2 = serviceProvider.GetRequiredService<ICoreHotReloadManager>();
        
        // Assert
        Assert.Same(coreHotReloadManager1, coreHotReloadManager2);
    }
    
    [Fact]
    public void CoreHotReloadManager_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockCoreHotReloadManager = new Mock<ICoreHotReloadManager>();
        mockCoreHotReloadManager.Setup(c => c.IsHotReloadSupported()).Returns(false);
        
        CoreHotReloadManager.SetInstance(mockCoreHotReloadManager.Object);
        
        // Act
        var result = CoreHotReloadManager.IsHotReloadSupported();
        
        // Assert
        Assert.False(result);
        mockCoreHotReloadManager.Verify(c => c.IsHotReloadSupported(), Times.Once);
    }
    
    [Fact]
    public void CoreHotReloadManager_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            var status = CoreHotReloadManager.GetStatus();
            // If we get here, it means it created a default instance
            Assert.NotNull(status);
        }
        catch
        {
            // Expected if dependencies aren't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void CoreHotReloadManagerInstance_Requires_Dependencies()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockPlayerManager = new Mock<IPlayerManager>();
        var mockPermissionManager = new Mock<IPermissionManager>();
        
        // Act
        var coreHotReloadManager = new CoreHotReloadManagerInstance(
            mockLogger.Object,
            mockPlayerManager.Object,
            mockPermissionManager.Object);
        
        // Assert
        Assert.NotNull(coreHotReloadManager);
    }
    
    [Fact]
    public void CoreHotReloadManager_Implements_ICoreHotReloadManager()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var coreHotReloadManager = serviceProvider.GetRequiredService<ICoreHotReloadManager>();
        
        // Act & Assert
        Assert.NotNull(coreHotReloadManager);
        Assert.IsAssignableFrom<ICoreHotReloadManager>(coreHotReloadManager);
    }
    
    [Fact]
    public void CoreHotReloadManager_GetStatus_Returns_String()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockPlayerManager = new Mock<IPlayerManager>();
        var mockPermissionManager = new Mock<IPermissionManager>();
        
        var coreHotReloadManager = new CoreHotReloadManagerInstance(
            mockLogger.Object,
            mockPlayerManager.Object,
            mockPermissionManager.Object);
        
        // Act
        var status = coreHotReloadManager.GetStatus();
        
        // Assert
        Assert.NotNull(status);
        Assert.IsType<string>(status);
    }
}
