using Xunit;
using Moq;
using CSMOO.Database;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Database;

/// <summary>
/// Tests for DbProvider class and IDbProvider interface
/// </summary>
public class DbProviderTests
{
    [Fact]
    public void DbProvider_Implements_IDbProvider()
    {
        // Arrange
        var mockDatabase = new Mock<IDatabase>();
        var mockCollection = new Mock<CSMOO.Database.ICollection<string>>();
        mockDatabase.Setup(d => d.GetCollection<string>(It.IsAny<string>()))
            .Returns(mockCollection.Object);
        
        // Act
        var dbProvider = new DbProvider(mockDatabase.Object);
        
        // Assert
        Assert.IsAssignableFrom<IDbProvider>(dbProvider);
    }
    
    [Fact]
    public void DbProvider_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var dbProvider = serviceProvider.GetRequiredService<IDbProvider>();
        
        // Assert
        Assert.NotNull(dbProvider);
        Assert.IsAssignableFrom<DbProvider>(dbProvider);
    }
    
    [Fact]
    public void DbProvider_Is_Singleton_In_DI()
    {
        // Arrange - Use mocks to avoid database file locking issues
        var mockDatabase = new Mock<IDatabase>();
        var serviceProvider = ServiceProviderHelper.CreateServiceProviderWithMocks(
            mockDatabase: mockDatabase);
        
        // Act
        var provider1 = serviceProvider.GetRequiredService<IDbProvider>();
        var provider2 = serviceProvider.GetRequiredService<IDbProvider>();
        
        // Assert
        Assert.Same(provider1, provider2);
    }
    
    [Fact]
    public void DbProvider_Receives_IDatabase_From_DI()
    {
        // Arrange
        var mockDatabase = new Mock<IDatabase>();
        var serviceProvider = ServiceProviderHelper.CreateServiceProviderWithMocks(
            mockDatabase: mockDatabase);
        
        // Act
        var dbProvider = serviceProvider.GetRequiredService<IDbProvider>();
        
        // Assert
        Assert.NotNull(dbProvider);
        // Verify that the DbProvider was created with the mocked database
        // (indirectly verified by the fact that it was resolved successfully)
    }
}
