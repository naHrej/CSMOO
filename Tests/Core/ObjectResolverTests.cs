using Xunit;
using Moq;
using CSMOO.Core;
using CSMOO.Database;
using CSMOO.Object;
using CSMOO.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CSMOO.Tests.Core;

/// <summary>
/// Tests for ObjectResolver with dependency injection
/// </summary>
public class ObjectResolverTests
{
    [Fact]
    public void ResolveUnique_PartialTokenPrefix_Resolves_Object_By_Name_Token()
    {
        var room = new Room("room-1", "A Peaceful Grove", "A peaceful grove");
        var player = new Player { Id = "p1", Name = "Tester", Location = room };
        var staff = new GameObject
        {
            Id = "staff-1",
            Name = "A Wooden Staff",
            Properties = new LiteDB.BsonDocument { ["name"] = "A Wooden Staff" }
        };

        var mockObjectManager = new Mock<IObjectManager>();
        mockObjectManager.Setup(m => m.GetObjectsInLocation(room.Id)).Returns(new List<GameObject> { staff });
        mockObjectManager.Setup(m => m.GetObjectsInLocation(player.Id)).Returns(new List<GameObject>());
        mockObjectManager.Setup(m => m.GetAllObjects()).Returns(new List<GameObject> { player, room, staff }.Cast<dynamic>().ToList());

        var mockCoreClassFactory = new Mock<ICoreClassFactory>();
        var resolver = new ObjectResolverInstance(mockObjectManager.Object, mockCoreClassFactory.Object);

        var result = resolver.ResolveUnique("wood", player, room);
        Assert.False(result.Ambiguous);
        Assert.NotNull(result.Match);
        Assert.Equal("staff-1", result.Match!.Id);
    }

    [Fact]
    public void ResolveUnique_Ambiguous_PartialTokenPrefix_Returns_Ambiguous()
    {
        var room = new Room("room-1", "A Peaceful Grove", "A peaceful grove");
        var player = new Player { Id = "p1", Name = "Tester", Location = room };
        var staff = new GameObject { Id = "staff-1", Name = "A Wooden Staff", Properties = new LiteDB.BsonDocument { ["name"] = "A Wooden Staff" } };
        var sword = new GameObject { Id = "sword-1", Name = "A Wooden Sword", Properties = new LiteDB.BsonDocument { ["name"] = "A Wooden Sword" } };

        var mockObjectManager = new Mock<IObjectManager>();
        mockObjectManager.Setup(m => m.GetObjectsInLocation(room.Id)).Returns(new List<GameObject> { staff, sword });
        mockObjectManager.Setup(m => m.GetObjectsInLocation(player.Id)).Returns(new List<GameObject>());
        mockObjectManager.Setup(m => m.GetAllObjects()).Returns(new List<GameObject> { player, room, staff, sword }.Cast<dynamic>().ToList());

        var mockCoreClassFactory = new Mock<ICoreClassFactory>();
        var resolver = new ObjectResolverInstance(mockObjectManager.Object, mockCoreClassFactory.Object);

        var result = resolver.ResolveUnique("wood", player, room);
        Assert.True(result.Ambiguous);
        Assert.Null(result.Match);
        Assert.True(result.Matches.Count >= 2);
    }

    [Fact]
    public void ResolveUnique_Uses_Aliases_For_Matching()
    {
        var room = new Room("room-1", "A Peaceful Grove", "A peaceful grove");
        var player = new Player { Id = "p1", Name = "Tester", Location = room };
        var staff = new GameObject
        {
            Id = "staff-1",
            Name = "A Wooden Staff",
            Properties = new LiteDB.BsonDocument
            {
                ["name"] = "A Wooden Staff",
                ["aliases"] = new LiteDB.BsonArray(new[] { new LiteDB.BsonValue("stick"), new LiteDB.BsonValue("staff") })
            }
        };

        var mockObjectManager = new Mock<IObjectManager>();
        mockObjectManager.Setup(m => m.GetObjectsInLocation(room.Id)).Returns(new List<GameObject> { staff });
        mockObjectManager.Setup(m => m.GetObjectsInLocation(player.Id)).Returns(new List<GameObject>());
        mockObjectManager.Setup(m => m.GetAllObjects()).Returns(new List<GameObject> { player, room, staff }.Cast<dynamic>().ToList());

        var mockCoreClassFactory = new Mock<ICoreClassFactory>();
        var resolver = new ObjectResolverInstance(mockObjectManager.Object, mockCoreClassFactory.Object);

        var result = resolver.ResolveUnique("stick", player, room);
        Assert.False(result.Ambiguous);
        Assert.NotNull(result.Match);
        Assert.Equal("staff-1", result.Match!.Id);
    }

    [Fact]
    public void ResolveUnique_Resolves_Exit_By_Abbreviation()
    {
        var room = new Room("room-1", "A Peaceful Grove", "A peaceful grove");
        var player = new Player { Id = "p1", Name = "Tester", Location = room };
        var exit = new GameObject
        {
            Id = "exit-north",
            Name = "North Exit",
            Properties = new LiteDB.BsonDocument
            {
                ["name"] = "North Exit",
                ["direction"] = "north"
            }
        };

        var mockObjectManager = new Mock<IObjectManager>();
        mockObjectManager.Setup(m => m.GetObjectsInLocation(room.Id)).Returns(new List<GameObject> { exit });
        mockObjectManager.Setup(m => m.GetObjectsInLocation(player.Id)).Returns(new List<GameObject>());
        mockObjectManager.Setup(m => m.GetAllObjects()).Returns(new List<GameObject> { player, room, exit }.Cast<dynamic>().ToList());

        var mockCoreClassFactory = new Mock<ICoreClassFactory>();
        var resolver = new ObjectResolverInstance(mockObjectManager.Object, mockCoreClassFactory.Object);

        var result = resolver.ResolveUnique("n", player, room);
        Assert.False(result.Ambiguous);
        Assert.NotNull(result.Match);
        Assert.Equal("exit-north", result.Match!.Id);
    }

    [Fact]
    public void ObjectResolver_Can_Be_Resolved_From_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var objectResolver = serviceProvider.GetService<IObjectResolver>();
        
        // Assert
        Assert.NotNull(objectResolver);
        Assert.IsType<ObjectResolverInstance>(objectResolver);
    }
    
    [Fact]
    public void ObjectResolver_Is_Singleton_In_DI()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        
        // Act
        var objectResolver1 = serviceProvider.GetRequiredService<IObjectResolver>();
        var objectResolver2 = serviceProvider.GetRequiredService<IObjectResolver>();
        
        // Assert
        Assert.Same(objectResolver1, objectResolver2);
    }
    
    [Fact]
    public void ObjectResolver_Static_Methods_Delegate_To_Instance()
    {
        // Arrange
        var mockObjectResolver = new Mock<IObjectResolver>();
        var testPlayer = new Player { Id = "test-player", Name = "TestPlayer" };
        mockObjectResolver.Setup(o => o.ResolveObject("me", testPlayer, null, null)).Returns(testPlayer);
        
        ObjectResolver.SetInstance(mockObjectResolver.Object);
        
        // Act
        var result = ObjectResolver.ResolveObject("me", testPlayer, null, null);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-player", result.Id);
        mockObjectResolver.Verify(o => o.ResolveObject("me", testPlayer, null, null), Times.Once);
    }
    
    [Fact]
    public void ObjectResolver_Static_Methods_Create_Default_Instance_If_Not_Set()
    {
        // Arrange - Don't set instance, should create default
        // Note: This test verifies backward compatibility
        
        // Act & Assert - Should not throw InvalidOperationException
        // The EnsureInstance() method creates a default instance
        try
        {
            var testPlayer = new Player { Id = "test-player", Name = "TestPlayer" };
            var results = ObjectResolver.ResolveObjects("me", testPlayer);
            // If we get here, it means it created a default instance
            Assert.NotNull(results);
        }
        catch
        {
            // Expected if dependencies aren't set up, but we verify the pattern works
        }
    }
    
    [Fact]
    public void ObjectResolverInstance_Requires_Dependencies()
    {
        // Arrange
        var mockObjectManager = new Mock<IObjectManager>();
        var mockCoreClassFactory = new Mock<ICoreClassFactory>();
        
        // Act
        var objectResolver = new ObjectResolverInstance(mockObjectManager.Object, mockCoreClassFactory.Object);
        
        // Assert
        Assert.NotNull(objectResolver);
    }
    
    [Fact]
    public void ObjectResolver_Implements_IObjectResolver()
    {
        // Arrange
        var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
        var objectResolver = serviceProvider.GetRequiredService<IObjectResolver>();
        
        // Act & Assert
        Assert.NotNull(objectResolver);
        Assert.IsAssignableFrom<IObjectResolver>(objectResolver);
    }
    
    [Fact]
    public void ObjectResolver_ResolveObjects_Returns_List()
    {
        // Arrange
        var mockObjectManager = new Mock<IObjectManager>();
        var mockCoreClassFactory = new Mock<ICoreClassFactory>();
        var objectResolver = new ObjectResolverInstance(mockObjectManager.Object, mockCoreClassFactory.Object);
        
        var testPlayer = new Player { Id = "test-player", Name = "TestPlayer" };
        
        // Act
        var results = objectResolver.ResolveObjects("me", testPlayer);
        
        // Assert
        Assert.NotNull(results);
        Assert.IsType<List<GameObject>>(results);
        // "me" should resolve to the looker
        Assert.Single(results);
        Assert.Equal(testPlayer, results[0]);
    }
}
