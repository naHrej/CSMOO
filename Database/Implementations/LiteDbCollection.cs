using System.Linq.Expressions;
using LiteDB;

namespace CSMOO.Database.Implementations;

/// <summary>
/// LiteDB implementation of ICollection<T>
/// </summary>
public class LiteDbCollection<T> : ICollection<T>
{
    private readonly ILiteCollection<T> _collection;
    
    public LiteDbCollection(ILiteCollection<T> collection)
    {
        _collection = collection;
    }
    
    public void Insert(T item)
    {
        _collection.Insert(item);
    }
    
    public void InsertBulk(IEnumerable<T> items)
    {
        _collection.InsertBulk(items);
    }
    
    public bool Update(T item)
    {
        return _collection.Update(item);
    }
    
    public int UpdateBulk(IEnumerable<T> items)
    {
        return _collection.Update(items);
    }
    
    public bool Delete(string id)
    {
        return _collection.Delete(id);
    }
    
    public int DeleteBulk(IEnumerable<string> ids)
    {
        return _collection.DeleteMany(x => ids.Contains(GetId(x)));
    }
    
    public T? FindById(string id)
    {
        return _collection.FindById(id);
    }
    
    public IEnumerable<T> FindAll()
    {
        return _collection.FindAll();
    }
    
    /// <summary>
    /// Find items matching predicate - CRITICAL FIX: Use proper query, not FindAll().Where()
    /// </summary>
    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
    {
        // Use proper LiteDB query - this uses indexes!
        return _collection.Find(predicate);
    }
    
    public T? FindOne(Expression<Func<T, bool>> predicate)
    {
        return _collection.FindOne(predicate);
    }
    
    public void EnsureIndex(Expression<Func<T, object>> field, IndexOptions? options = null)
    {
        if (options?.Unique == true)
        {
            _collection.EnsureIndex(field, true);
        }
        else
        {
            _collection.EnsureIndex(field);
        }
    }
    
    public long Count()
    {
        return _collection.Count();
    }
    
    public long Count(Expression<Func<T, bool>> predicate)
    {
        return _collection.Count(predicate);
    }
    
    public bool Exists(Expression<Func<T, bool>> predicate)
    {
        return _collection.Exists(predicate);
    }
    
    /// <summary>
    /// Extract ID from item (assumes item has Id property or uses BsonId)
    /// </summary>
    private string GetId(T item)
    {
        // Try to get Id property via reflection
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null)
        {
            var value = idProperty.GetValue(item);
            return value?.ToString() ?? string.Empty;
        }
        
        // Fallback: try to get via BsonId attribute
        var bsonIdProperty = typeof(T).GetProperties()
            .FirstOrDefault(p => p.GetCustomAttributes(typeof(BsonIdAttribute), false).Any());
        if (bsonIdProperty != null)
        {
            var value = bsonIdProperty.GetValue(item);
            return value?.ToString() ?? string.Empty;
        }
        
        throw new InvalidOperationException($"Cannot extract ID from type {typeof(T).Name}. Type must have an 'Id' property or property marked with [BsonId].");
    }
}
