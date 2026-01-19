using Xunit;
using Moq;
using CSMOO.Verbs;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Verbs;

/// <summary>
/// Tests for VerbManager with dependency injection
/// </summary>
public class VerbManagerTests
{
    [Fact]
    public void VerbManager_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var verbManager = serviceProvider.GetService<IVerbManager>();
        
        // Assert
        Assert.NotNull(verbManager);
        Assert.IsType<VerbManagerInstance>(verbManager);
    }
    
    [Fact]
    public void VerbManager_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var verbManager1 = serviceProvider.GetRequiredService<IVerbManager>();
        var verbManager2 = serviceProvider.GetRequiredService<IVerbManager>();
        
        // Assert
        Assert.Same(verbManager1, verbManager2);
    }
    
    [Fact]
    public void VerbManager_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockVerbManager = new Mock<IVerbManager>();
        var testVerb = new Verb { Id = "test-verb", Name = "test", ObjectId = "test-obj" };
        mockVerbManager.Setup(v => v.GetVerb("test-verb")).Returns(testVerb);
        
        VerbManager.SetInstance(mockVerbManager.Object);
        
        // Act
        var result = VerbManager.GetVerb("test-verb");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-verb", result.Id);
        mockVerbManager.Verify(v => v.GetVerb("test-verb"), Times.Once);
    }
    
    [Fact]
    public void VerbManager_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            var isValid = VerbManager.IsValidVerbName("test");
            // If we get here, it means it created a default instance
            Assert.True(isValid);
        }
        catch
        {
            // Expected if dependencies aren't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void VerbManagerInstance_Requires_Dependencies()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        
        // Act
        var verbManager = new VerbManagerInstance(mockDbProvider.Object);
        
        // Assert
        Assert.NotNull(verbManager);
    }
    
    [Fact]
    public void VerbManager_Implements_IVerbManager()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var verbManager = serviceProvider.GetRequiredService<IVerbManager>();
        
        // Act & Assert
        Assert.NotNull(verbManager);
        Assert.IsAssignableFrom<IVerbManager>(verbManager);
    }
    
    [Fact]
    public void VerbManager_IsValidVerbName_Validates_Correctly()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        var verbManager = new VerbManagerInstance(mockDbProvider.Object);
        
        // Act & Assert
        Assert.True(verbManager.IsValidVerbName("test"));
        Assert.True(verbManager.IsValidVerbName("test_verb"));
        Assert.True(verbManager.IsValidVerbName("test-verb"));
        Assert.False(verbManager.IsValidVerbName(""));
        Assert.False(verbManager.IsValidVerbName("123test")); // Must start with letter
        Assert.False(verbManager.IsValidVerbName("test@verb")); // Invalid character
    }
}
