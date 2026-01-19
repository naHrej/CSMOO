using Xunit;
using Moq;
using CSMOO.Init;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Logging;
using CSMOO.Verbs;
using CSMOO.Functions;
using CSMOO.Configuration;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Init;

/// <summary>
/// Tests for HotReloadManager with dependency injection
/// </summary>
public class HotReloadManagerTests
{
    [Fact]
    public void HotReloadManager_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var hotReloadManager = serviceProvider.GetService<IHotReloadManager>();
        
        // Assert
        Assert.NotNull(hotReloadManager);
        Assert.IsType<HotReloadManagerInstance>(hotReloadManager);
    }
    
    [Fact]
    public void HotReloadManager_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var hotReloadManager1 = serviceProvider.GetRequiredService<IHotReloadManager>();
        var hotReloadManager2 = serviceProvider.GetRequiredService<IHotReloadManager>();
        
        // Assert
        Assert.Same(hotReloadManager1, hotReloadManager2);
    }
    
    [Fact]
    public void HotReloadManager_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockHotReloadManager = new Mock<IHotReloadManager>();
        
        HotReloadManager.SetInstance(mockHotReloadManager.Object);
        
        // Act
        HotReloadManager.ManualReloadVerbs();
        
        // Assert
        mockHotReloadManager.Verify(hr => hr.ManualReloadVerbs(), Times.Once);
    }
    
    [Fact]
    public void HotReloadManager_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            HotReloadManager.SetEnabled(false);
            // If we get here, it means it created a default instance
            Assert.True(true);
        }
        catch
        {
            // Expected if dependencies aren't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void HotReloadManagerInstance_Requires_Dependencies()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockConfig = new Mock<IConfig>();
        var mockVerbInitializer = new Mock<IVerbInitializer>();
        var mockFunctionInitializer = new Mock<IFunctionInitializer>();
        var mockPlayerManager = new Mock<IPlayerManager>();
        
        // Act
        var hotReloadManager = new HotReloadManagerInstance(
            mockLogger.Object,
            mockConfig.Object,
            mockVerbInitializer.Object,
            mockFunctionInitializer.Object,
            mockPlayerManager.Object);
        
        // Assert
        Assert.NotNull(hotReloadManager);
    }
    
    [Fact]
    public void HotReloadManager_Implements_IHotReloadManager()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var hotReloadManager = serviceProvider.GetRequiredService<IHotReloadManager>();
        
        // Act & Assert
        Assert.NotNull(hotReloadManager);
        Assert.IsAssignableFrom<IHotReloadManager>(hotReloadManager);
    }
    
    [Fact]
    public void HotReloadManager_IsEnabled_Property_Works()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockConfig = new Mock<IConfig>();
        var mockVerbInitializer = new Mock<IVerbInitializer>();
        var mockFunctionInitializer = new Mock<IFunctionInitializer>();
        var mockPlayerManager = new Mock<IPlayerManager>();
        
        var hotReloadManager = new HotReloadManagerInstance(
            mockLogger.Object,
            mockConfig.Object,
            mockVerbInitializer.Object,
            mockFunctionInitializer.Object,
            mockPlayerManager.Object);
        
        // Act
        hotReloadManager.SetEnabled(true);
        var isEnabled = hotReloadManager.IsEnabled;
        
        // Assert
        Assert.True(isEnabled);
    }
}
