using CSMOO.Functions;
using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Centralized provider for all database operations (CRUD) on collections.
/// All DB access should go through this class.
/// </summary>
public class DbProvider : IDbProvider
{
    private static DbProvider? _instance;
    public static DbProvider Instance => _instance ?? throw new InvalidOperationException("DbProvider instance not set. Call DbProvider.SetInstance() first.");
    private readonly IGameDatabase _db;
    private IObjectManager? _objectManager;
    
    public DbProvider(IGameDatabase db)
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
    
    /// <summary>
    /// Sets the object manager (used to resolve circular dependency)
    /// </summary>
    public void SetObjectManager(IObjectManager objectManager)
    {
        _objectManager = objectManager;
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
        var results = GetCollection<T>(collectionName).FindAll();
        // If T is GameObject, update the singleton cache
        if (typeof(T) == typeof(GameObject))
        {
            var list = new List<T>();
            foreach (var obj in results)
            {
                var go = obj as GameObject;
                if (go != null)
                {
                    if (_objectManager != null)
                    {
                        _objectManager.CacheGameObject(go);
                    }
                    list.Add(obj);
                }
            }
            return list;
        }
        return results;
    }

    // Generic Find (with predicate)
    public IEnumerable<T> Find<T>(string collectionName, Func<T, bool> predicate)
    {
        var results = GetCollection<T>(collectionName).Find(predicate);
        if (typeof(T) == typeof(GameObject))
        {
            var list = new List<T>();
            foreach (var obj in results)
            {
                var go = obj as GameObject;
                if (go != null)
                {
                    if (_objectManager != null)
                    {
                        _objectManager.CacheGameObject(go);
                    }
                    list.Add(obj);
                }
            }
            return list;
        }
        return results;
    }

    // Generic FindOne
    public T? FindOne<T>(string collectionName, Func<T, bool> predicate)
    {
        var result = GetCollection<T>(collectionName).FindOne(predicate);
        if (typeof(T) == typeof(GameObject) && result is GameObject go)
        {
            if (_objectManager != null)
            {
                var cached = _objectManager.CacheGameObject(go);
                return cached is T t ? t : default;
            }
        }
        return result;
    }

    // Generic FindById
    public T? FindById<T>(string collectionName, string id)
    {
        var result = GetCollection<T>(collectionName).FindById(id);
        if (typeof(T) == typeof(GameObject) && result is GameObject go)
        {
            if (_objectManager != null)
            {
                var cached = _objectManager.CacheGameObject(go);
                return cached is T t ? t : default;
            }
        }
        return result;
    }

    // Helper to get the collection from GameDatabase (now private)
    private IDbCollection<T> GetCollection<T>(string collectionName)
    {
        return new LiteCollectionAdapter<T>(_db.GetCollection<T>(collectionName));
    }

    // Expose only what FunctionResolver needs: find all functions for an object
    public IEnumerable<Function> FindFunctionsByObjectId(string objectId)
    {
        return Find<Function>("functions", f => f.ObjectId == objectId);
    }
}



