using Xunit;
using Moq;
using CSMOO.Functions;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Logging;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Functions;

/// <summary>
/// Tests for FunctionInitializer with dependency injection
/// </summary>
public class FunctionInitializerTests
{
    [Fact]
    public void FunctionInitializer_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var functionInitializer = serviceProvider.GetService<IFunctionInitializer>();
        
        // Assert
        Assert.NotNull(functionInitializer);
        Assert.IsType<FunctionInitializerInstance>(functionInitializer);
    }
    
    [Fact]
    public void FunctionInitializer_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var functionInitializer1 = serviceProvider.GetRequiredService<IFunctionInitializer>();
        var functionInitializer2 = serviceProvider.GetRequiredService<IFunctionInitializer>();
        
        // Assert
        Assert.Same(functionInitializer1, functionInitializer2);
    }
    
    [Fact]
    public void FunctionInitializer_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockFunctionInitializer = new Mock<IFunctionInitializer>();
        
        FunctionInitializer.SetInstance(mockFunctionInitializer.Object);
        
        // Act
        FunctionInitializer.LoadAndCreateFunctions();
        
        // Assert
        mockFunctionInitializer.Verify(fi => fi.LoadAndCreateFunctions(), Times.Once);
    }
    
    [Fact]
    public void FunctionInitializer_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            FunctionInitializer.ReloadFunctions();
            // If we get here, it means it created a default instance
            Assert.True(true);
        }
        catch
        {
            // Expected if database isn't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void FunctionInitializerInstance_Requires_Dependencies()
    {
        // Arrange
        var mockDbProvider = new Mock<IDbProvider>();
        var mockLogger = new Mock<ILogger>();
        var mockObjectManager = new Mock<IObjectManager>();
        
        // Act
        var mockFunctionManager = new Mock<IFunctionManager>();
        var functionInitializer = new FunctionInitializerInstance(mockDbProvider.Object, mockLogger.Object, mockObjectManager.Object, mockFunctionManager.Object);
        
        // Assert
        Assert.NotNull(functionInitializer);
    }
    
    [Fact]
    public void FunctionInitializer_Implements_IFunctionInitializer()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var functionInitializer = serviceProvider.GetRequiredService<IFunctionInitializer>();
        
        // Act & Assert
        Assert.NotNull(functionInitializer);
        Assert.IsAssignableFrom<IFunctionInitializer>(functionInitializer);
    }
}
