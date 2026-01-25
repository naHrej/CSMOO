using CSMOO.Functions;
using CSMOO.Object;
using System.Linq.Expressions;

namespace CSMOO.Database;

/// <summary>
/// Centralized provider for all database operations (CRUD) on collections.
/// All DB access should go through this class.
/// </summary>
public class DbProvider : IDbProvider
{
    private static DbProvider? _instance;
    public static DbProvider Instance => _instance ?? throw new InvalidOperationException("DbProvider instance not set. Call DbProvider.SetInstance() first.");
    private readonly IDatabase _db;
    
    public DbProvider(IDatabase db)
    {
        _db = db;
    }
    
    /// <summary>
    /// Sets the static instance for backward compatibility (used by GameObject data class)
    /// </summary>
    public static void SetInstance(DbProvider instance)
    {
        _instance = instance;
    }
    
    // Expose: find all verbs for an object
    public IEnumerable<Verb> FindVerbsByObjectId(string objectId)
    {
        return Find<Verb>("verbs", v => v.ObjectId == objectId);
    }

    // Generic Insert
    public void Insert<T>(string collectionName, T item)
    {
        GetCollection<T>(collectionName).Insert(item);
    }

    // Generic Update
    public bool Update<T>(string collectionName, T item)
    {
        return GetCollection<T>(collectionName).Update(item);
    }

    // Generic Delete
    public bool Delete<T>(string collectionName, string id)
    {
        return GetCollection<T>(collectionName).Delete(id);
    }

    // Generic FindAll
    public IEnumerable<T> FindAll<T>(string collectionName)
    {
        return GetCollection<T>(collectionName).FindAll();
    }

    // Generic Find (with predicate)
    public IEnumerable<T> Find<T>(string collectionName, Expression<Func<T, bool>> predicate)
    {
        return GetCollection<T>(collectionName).Find(predicate);
    }

    // Generic FindOne
    public T? FindOne<T>(string collectionName, Expression<Func<T, bool>> predicate)
    {
        return GetCollection<T>(collectionName).FindOne(predicate);
    }

    // Generic FindById
    public T? FindById<T>(string collectionName, string id)
    {
        return GetCollection<T>(collectionName).FindById(id);
    }

    // Helper to get the collection from IDatabase
    private ICollection<T> GetCollection<T>(string collectionName)
    {
        return _db.GetCollection<T>(collectionName);
    }

    // Expose only what FunctionResolver needs: find all functions for an object
    public IEnumerable<Function> FindFunctionsByObjectId(string objectId)
    {
        return Find<Function>("functions", f => f.ObjectId == objectId);
    }
}



