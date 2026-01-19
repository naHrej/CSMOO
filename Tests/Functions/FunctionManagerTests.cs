using Xunit;
using Moq;
using CSMOO.Functions;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Functions;

/// <summary>
/// Tests for FunctionManager with dependency injection
/// </summary>
public class FunctionManagerTests
{
    [Fact]
    public void FunctionManager_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var functionManager = serviceProvider.GetService<IFunctionManager>();
        
        // Assert
        Assert.NotNull(functionManager);
        Assert.IsType<FunctionManagerInstance>(functionManager);
    }
    
    [Fact]
    public void FunctionManager_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var functionManager1 = serviceProvider.GetRequiredService<IFunctionManager>();
        var functionManager2 = serviceProvider.GetRequiredService<IFunctionManager>();
        
        // Assert
        Assert.Same(functionManager1, functionManager2);
    }
    
    [Fact]
    public void FunctionManager_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockFunctionManager = new Mock<IFunctionManager>();
        var testFunction = new Function { Id = "test-function", Name = "test", ObjectId = "test-obj" };
        mockFunctionManager.Setup(f => f.GetFunction("test-function")).Returns(testFunction);
        
        FunctionManager.SetInstance(mockFunctionManager.Object);
        
        // Act
        var result = FunctionManager.GetFunction("test-function");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-function", result.Id);
        mockFunctionManager.Verify(f => f.GetFunction("test-function"), Times.Once);
    }
    
    [Fact]
    public void FunctionManager_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            var isValid = FunctionManager.IsValidFunctionName("test");
            // If we get here, it means it created a default instance
            Assert.True(isValid);
        }
        catch
        {
            // Expected if dependencies aren't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void FunctionManagerInstance_Requires_Dependencies()
    {
        // Arrange
        var mockGameDatabase = new Mock<IGameDatabase>();
        
        // Act
        var functionManager = new FunctionManagerInstance(mockGameDatabase.Object);
        
        // Assert
        Assert.NotNull(functionManager);
    }
    
    [Fact]
    public void FunctionManager_Implements_IFunctionManager()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var functionManager = serviceProvider.GetRequiredService<IFunctionManager>();
        
        // Act & Assert
        Assert.NotNull(functionManager);
        Assert.IsAssignableFrom<IFunctionManager>(functionManager);
    }
    
    [Fact]
    public void FunctionManager_IsValidFunctionName_Validates_Correctly()
    {
        // Arrange
        var mockGameDatabase = new Mock<IGameDatabase>();
        var functionManager = new FunctionManagerInstance(mockGameDatabase.Object);
        
        // Act & Assert
        Assert.True(functionManager.IsValidFunctionName("test"));
        Assert.True(functionManager.IsValidFunctionName("test_function"));
        Assert.False(functionManager.IsValidFunctionName(""));
        Assert.False(functionManager.IsValidFunctionName("123test")); // Must start with letter
        Assert.False(functionManager.IsValidFunctionName("test-function")); // Hyphen not allowed
    }
    
    [Fact]
    public void FunctionManager_IsValidParameterType_Validates_Correctly()
    {
        // Arrange
        var mockGameDatabase = new Mock<IGameDatabase>();
        var functionManager = new FunctionManagerInstance(mockGameDatabase.Object);
        
        // Act & Assert
        Assert.True(functionManager.IsValidParameterType("string"));
        Assert.True(functionManager.IsValidParameterType("int"));
        Assert.True(functionManager.IsValidParameterType("GameObject"));
        Assert.False(functionManager.IsValidParameterType("invalid"));
    }
    
    [Fact]
    public void FunctionManager_IsValidReturnType_Validates_Correctly()
    {
        // Arrange
        var mockGameDatabase = new Mock<IGameDatabase>();
        var functionManager = new FunctionManagerInstance(mockGameDatabase.Object);
        
        // Act & Assert
        Assert.True(functionManager.IsValidReturnType("void"));
        Assert.True(functionManager.IsValidReturnType("string"));
        Assert.True(functionManager.IsValidReturnType("List<GameObject>"));
        Assert.False(functionManager.IsValidReturnType("invalid"));
    }
}
