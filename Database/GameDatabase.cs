using LiteDB;
using CSMOO.Configuration;
using CSMOO.Object;
using CSMOO.Functions;

namespace CSMOO.Database;

/// <summary>
/// Legacy GameDatabase class - kept for backward compatibility
/// New code should use IDatabase and LiteDbDatabase
/// </summary>
[Obsolete("Use IDatabase and LiteDbDatabase instead. This class is kept for backward compatibility only.")]
public class GameDatabase : IGameDatabase
{
    private readonly LiteDatabase _database;

    public GameDatabase(string connectionString)
    {
        _database = new LiteDatabase(connectionString);
        
        // Set up indexes for better performance
        var objectClasses = _database.GetCollection<ObjectClass>("objectclasses");
        objectClasses.EnsureIndex(x => x.Id);
        objectClasses.EnsureIndex(x => x.Name);
        
        var gameObjects = _database.GetCollection<GameObject>("gameobjects");
        gameObjects.EnsureIndex(x => x.Id);
        gameObjects.EnsureIndex(x => x.ClassId);
        gameObjects.EnsureIndex("Properties.location");
        gameObjects.EnsureIndex("Properties.owner");
        
        var players = _database.GetCollection<Player>("players");
        players.EnsureIndex(x => x.Id);
        players.EnsureIndex(x => x.Name);
        players.EnsureIndex(x => x.SessionGuid);
        
        var verbs = _database.GetCollection<Verb>("verbs");
        verbs.EnsureIndex(x => x.Id);
        verbs.EnsureIndex(x => x.ObjectId);
        verbs.EnsureIndex(x => x.Name);
        
        var functions = _database.GetCollection<GameFunction>("functions");
        functions.EnsureIndex(x => x.Id);
        functions.EnsureIndex(x => x.Name);
    }

    // All direct collection access is now private; use DbProvider for all DB access.

    /// <summary>
    /// Generic method to get any collection
    /// </summary>
    public ILiteCollection<T> GetCollection<T>(string name) => _database.GetCollection<T>(name);

    public void Dispose()
    {
        _database?.Dispose();
    }
}



