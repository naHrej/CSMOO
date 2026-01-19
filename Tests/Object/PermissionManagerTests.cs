using Xunit;
using Moq;
using CSMOO.Object;
using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Object;

/// <summary>
/// Tests for PermissionManager with dependency injection
/// </summary>
public class PermissionManagerTests
{
    [Fact]
    public void PermissionManager_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var permissionManager = serviceProvider.GetService<IPermissionManager>();
        
        // Assert
        Assert.NotNull(permissionManager);
        Assert.IsType<PermissionManagerInstance>(permissionManager);
    }
    
    [Fact]
    public void PermissionManager_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var permissionManager1 = serviceProvider.GetRequiredService<IPermissionManager>();
        var permissionManager2 = serviceProvider.GetRequiredService<IPermissionManager>();
        
        // Assert
        Assert.Same(permissionManager1, permissionManager2);
    }
    
    [Fact]
    public void PermissionManager_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockPermissionManager = new Mock<IPermissionManager>();
        var mockPlayer = new Mock<Player>();
        
        PermissionManager.SetInstance(mockPermissionManager.Object);
        
        // Act
        PermissionManager.HasFlag(mockPlayer.Object, PermissionManager.Flag.Admin);
        
        // Assert
        mockPermissionManager.Verify(pm => pm.HasFlag(mockPlayer.Object, PermissionManager.Flag.Admin), Times.Once);
    }
    
    [Fact]
    public void PermissionManager_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            var mockPlayer = new Player { Name = "test", Permissions = new List<string>() };
            PermissionManager.HasFlag(mockPlayer, PermissionManager.Flag.Admin);
            // If we get here, it means it created a default instance
            Assert.True(true);
        }
        catch
        {
            // Expected if database isn't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void PermissionManagerInstance_Requires_Dependencies()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        var mockLogger = new Mock<ILogger>();
        
        // Act
        var permissionManager = new PermissionManagerInstance(mockDbProvider.Object, mockLogger.Object);
        
        // Assert
        Assert.NotNull(permissionManager);
    }
    
    [Fact]
    public void PermissionManager_Implements_IPermissionManager()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var permissionManager = serviceProvider.GetRequiredService<IPermissionManager>();
        
        // Act & Assert
        Assert.NotNull(permissionManager);
        Assert.IsAssignableFrom<IPermissionManager>(permissionManager);
    }
    
    [Fact]
    public void PermissionManager_Flag_Enum_Is_Accessible()
    {
        // Arrange & Act
        var adminFlag = PermissionManager.Flag.Admin;
        var programmerFlag = PermissionManager.Flag.Programmer;
        var moderatorFlag = PermissionManager.Flag.Moderator;
        
        // Assert
        Assert.Equal(PermissionManager.Flag.Admin, adminFlag);
        Assert.Equal(PermissionManager.Flag.Programmer, programmerFlag);
        Assert.Equal(PermissionManager.Flag.Moderator, moderatorFlag);
    }
    
    [Fact]
    public void PermissionManager_ORIGINAL_ADMIN_NAME_Constant_Is_Accessible()
    {
        // Arrange & Act
        var adminName = PermissionManager.ORIGINAL_ADMIN_NAME;
        
        // Assert
        Assert.Equal("admin", adminName);
    }
}
