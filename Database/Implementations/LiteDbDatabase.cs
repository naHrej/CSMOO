using LiteDB;
using CSMOO.Configuration;
using CSMOO.Database;
using CSMOO.Object;

namespace CSMOO.Database.Implementations;

/// <summary>
/// LiteDB implementation of IDatabase
/// </summary>
public class LiteDbDatabase : IDatabase
{
    private readonly LiteDatabase _database;
    private bool _inTransaction = false;
    
    public LiteDbDatabase(string connectionString)
    {
        _database = new LiteDatabase(connectionString);
        
        // Set up indexes for better performance
        // TODO: Move to DatabaseInitializer or migration system (technical debt)
        EnsureIndexes();
    }
    
    private void EnsureIndexes()
    {
        var objectClasses = _database.GetCollection<Object.ObjectClass>("objectclasses");
        objectClasses.EnsureIndex(x => x.Id);
        objectClasses.EnsureIndex(x => x.Name);
        
        var gameObjects = _database.GetCollection<Object.GameObject>("gameobjects");
        gameObjects.EnsureIndex(x => x.Id);
        gameObjects.EnsureIndex(x => x.ClassId);
        gameObjects.EnsureIndex("Properties.location");
        gameObjects.EnsureIndex("Properties.owner");
        
        var players = _database.GetCollection<Object.Player>("players");
        players.EnsureIndex(x => x.Id);
        players.EnsureIndex(x => x.Name);
        players.EnsureIndex(x => x.SessionGuid);
        
        var verbs = _database.GetCollection<Verb>("verbs");
        verbs.EnsureIndex(x => x.Id);
        verbs.EnsureIndex(x => x.ObjectId);
        verbs.EnsureIndex(x => x.Name);
        
        var functions = _database.GetCollection<Functions.GameFunction>("functions");
        functions.EnsureIndex(x => x.Id);
        functions.EnsureIndex(x => x.Name);
    }
    
    public ICollection<T> GetCollection<T>(string name)
    {
        var collection = _database.GetCollection<T>(name);
        return new LiteDbCollection<T>(collection);
    }
    
    public void BeginTransaction()
    {
        if (_inTransaction)
            throw new InvalidOperationException("Transaction already in progress");
        
        _database.BeginTrans();
        _inTransaction = true;
    }
    
    public void CommitTransaction()
    {
        if (!_inTransaction)
            throw new InvalidOperationException("No transaction in progress");
        
        _database.Commit();
        _inTransaction = false;
    }
    
    public void RollbackTransaction()
    {
        if (!_inTransaction)
            throw new InvalidOperationException("No transaction in progress");
        
        _database.Rollback();
        _inTransaction = false;
    }
    
    public bool IsInTransaction => _inTransaction;
    
    public void Dispose()
    {
        _database?.Dispose();
    }
}
