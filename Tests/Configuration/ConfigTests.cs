using Xunit;
using CSMOO.Configuration;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Configuration;

/// <summary>
/// Tests for Config class and IConfig interface
/// </summary>
public class ConfigTests
{
    [Fact]
    public void Config_Implements_IConfig()
    {
        // Arrange & Act
        var config = Config.Load();
        
        // Assert
        Assert.IsAssignableFrom<IConfig>(config);
        Assert.NotNull(config.Server);
        Assert.NotNull(config.Database);
        Assert.NotNull(config.Logging);
        Assert.NotNull(config.Scripting);
    }
    
    [Fact]
    public void Config_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var config = serviceProvider.GetRequiredService<IConfig>();
        
        // Assert
        Assert.NotNull(config);
        Assert.IsAssignableFrom<Config>(config);
    }
    
    [Fact]
    public void Config_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var config1 = serviceProvider.GetRequiredService<IConfig>();
        var config2 = serviceProvider.GetRequiredService<IConfig>();
        
        // Assert
        Assert.Same(config1, config2);
    }
}
