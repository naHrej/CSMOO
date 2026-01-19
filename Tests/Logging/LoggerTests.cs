using Xunit;
using Moq;
using CSMOO.Logging;
using CSMOO.Configuration;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Logging;

/// <summary>
/// Tests for Logger class and ILogger interface
/// </summary>
public class LoggerTests
{
    [Fact]
    public void LoggerInstance_Implements_ILogger()
    {
        // Arrange
        var mockConfig = new Mock<IConfig>();
        var loggingConfig = new LoggingConfig
        {
            EnableConsoleLogging = false,
            EnableFileLogging = false
        };
        mockConfig.Setup(c => c.Logging).Returns(loggingConfig);
        
        // Act
        var logger = new LoggerInstance(mockConfig.Object);
        
        // Assert
        Assert.IsAssignableFrom<ILogger>(logger);
    }
    
    [Fact]
    public void Logger_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var logger = serviceProvider.GetRequiredService<ILogger>();
        
        // Assert
        Assert.NotNull(logger);
        Assert.IsAssignableFrom<LoggerInstance>(logger);
    }
    
    [Fact]
    public void Logger_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var logger1 = serviceProvider.GetRequiredService<ILogger>();
        var logger2 = serviceProvider.GetRequiredService<ILogger>();
        
        // Assert
        Assert.Same(logger1, logger2);
    }
    
    [Fact]
    public void Logger_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        Logger.SetInstance(mockLogger.Object);
        
        // Act
        Logger.Info("Test message");
        
        // Assert
        mockLogger.Verify(l => l.Info("Test message"), Times.Once);
    }
    
    [Fact]
    public void Logger_Can_Be_Mocked_In_Tests()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var serviceProvider = ServiceProviderHelper.CreateServiceProviderWithMocks(
            mockLogger: mockLogger);
        
        // Act
        var logger = serviceProvider.GetRequiredService<ILogger>();
        logger.Info("Test");
        
        // Assert
        mockLogger.Verify(l => l.Info("Test"), Times.Once);
    }
}
