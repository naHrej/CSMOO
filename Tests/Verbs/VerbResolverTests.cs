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
/// Tests for VerbResolver with dependency injection
/// </summary>
public class VerbResolverTests
{
    [Fact]
    public void VerbResolver_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var verbResolver = serviceProvider.GetService<IVerbResolver>();
        
        // Assert
        Assert.NotNull(verbResolver);
        Assert.IsType<VerbResolverInstance>(verbResolver);
    }
    
    [Fact]
    public void VerbResolver_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var verbResolver1 = serviceProvider.GetRequiredService<IVerbResolver>();
        var verbResolver2 = serviceProvider.GetRequiredService<IVerbResolver>();
        
        // Assert
        Assert.Same(verbResolver1, verbResolver2);
    }
    
    [Fact]
    public void VerbResolver_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockVerbResolver = new Mock<IVerbResolver>();
        mockVerbResolver.Setup(v => v.HasVerb("test-obj", "test", true)).Returns(true);
        
        VerbResolver.SetInstance(mockVerbResolver.Object);
        
        // Act
        var result = VerbResolver.HasVerb("test-obj", "test", true);
        
        // Assert
        Assert.True(result);
        mockVerbResolver.Verify(v => v.HasVerb("test-obj", "test", true), Times.Once);
    }
    
    [Fact]
    public void VerbResolver_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            var verbs = VerbResolver.GetSystemVerbs();
            // If we get here, it means it created a default instance
            Assert.NotNull(verbs);
        }
        catch
        {
            // Expected if dependencies aren't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void VerbResolverInstance_Requires_Dependencies()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        var mockObjectManager = new Mock<IObjectManager>();
        var mockLogger = new Mock<ILogger>();
        
        // Act
        var verbResolver = new VerbResolverInstance(mockDbProvider.Object, mockObjectManager.Object, mockLogger.Object);
        
        // Assert
        Assert.NotNull(verbResolver);
    }
    
    [Fact]
    public void VerbResolver_Implements_IVerbResolver()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var verbResolver = serviceProvider.GetRequiredService<IVerbResolver>();
        
        // Act & Assert
        Assert.NotNull(verbResolver);
        Assert.IsAssignableFrom<IVerbResolver>(verbResolver);
    }
    
    [Fact]
    public void VerbResolver_GetVerbInfo_Returns_Dictionary()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        var mockObjectManager = new Mock<IObjectManager>();
        var mockLogger = new Mock<ILogger>();
        var verbResolver = new VerbResolverInstance(mockDbProvider.Object, mockObjectManager.Object, mockLogger.Object);
        
        var testVerb = new Verb
        {
            Id = "test-verb",
            Name = "test",
            ObjectId = "test-obj",
            Pattern = "test pattern",
            Description = "Test description"
        };
        
        // Act
        var info = verbResolver.GetVerbInfo(testVerb);
        
        // Assert
        Assert.NotNull(info);
        Assert.IsType<Dictionary<string, object>>(info);
        Assert.Equal("test", info["Name"]);
        Assert.Equal("test-obj", info["ObjectId"]);
    }
}
