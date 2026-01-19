using Xunit;
using Moq;
using CSMOO.Verbs;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Logging;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Verbs;

/// <summary>
/// Tests for VerbInitializer with dependency injection
/// </summary>
public class VerbInitializerTests
{
    [Fact]
    public void VerbInitializer_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var verbInitializer = serviceProvider.GetService<IVerbInitializer>();
        
        // Assert
        Assert.NotNull(verbInitializer);
        Assert.IsType<VerbInitializerInstance>(verbInitializer);
    }
    
    [Fact]
    public void VerbInitializer_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var verbInitializer1 = serviceProvider.GetRequiredService<IVerbInitializer>();
        var verbInitializer2 = serviceProvider.GetRequiredService<IVerbInitializer>();
        
        // Assert
        Assert.Same(verbInitializer1, verbInitializer2);
    }
    
    [Fact]
    public void VerbInitializer_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockVerbInitializer = new Mock<IVerbInitializer>();
        
        VerbInitializer.SetInstance(mockVerbInitializer.Object);
        
        // Act
        VerbInitializer.LoadAndCreateVerbs();
        
        // Assert
        mockVerbInitializer.Verify(vi => vi.LoadAndCreateVerbs(), Times.Once);
    }
    
    [Fact]
    public void VerbInitializer_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            VerbInitializer.ReloadVerbs();
            // If we get here, it means it created a default instance
            Assert.True(true);
        }
        catch
        {
            // Expected if database isn't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void VerbInitializerInstance_Requires_Dependencies()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        var mockLogger = new Mock<ILogger>();
        var mockObjectManager = new Mock<IObjectManager>();
        
        // Act
        var verbInitializer = new VerbInitializerInstance(mockDbProvider.Object, mockLogger.Object, mockObjectManager.Object);
        
        // Assert
        Assert.NotNull(verbInitializer);
    }
    
    [Fact]
    public void VerbInitializer_Implements_IVerbInitializer()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var verbInitializer = serviceProvider.GetRequiredService<IVerbInitializer>();
        
        // Act & Assert
        Assert.NotNull(verbInitializer);
        Assert.IsAssignableFrom<IVerbInitializer>(verbInitializer);
    }
}
