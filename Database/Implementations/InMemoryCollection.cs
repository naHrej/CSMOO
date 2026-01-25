using System.Linq.Expressions;
using System.Collections.Concurrent;
using CSMOO.Database;

namespace CSMOO.Database.Implementations;

/// <summary>
/// In-memory implementation of ICollection<T> for testing
/// </summary>
public class InMemoryCollection<T> : ICollection<T>
{
    private readonly ConcurrentDictionary<string, T> _data = new();
    private readonly Func<T, string> _getId;
    
    public InMemoryCollection(Func<T, string> getId)
    {
        _getId = getId;
    }
    
    public void Insert(T item)
    {
        var id = _getId(item);
        _data[id] = item;
    }
    
    public void InsertBulk(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            Insert(item);
        }
    }
    
    public bool Update(T item)
    {
        var id = _getId(item);
        if (_data.ContainsKey(id))
        {
            _data[id] = item;
            return true;
        }
        return false;
    }
    
    public int UpdateBulk(IEnumerable<T> items)
    {
        int count = 0;
        foreach (var item in items)
        {
            if (Update(item))
                count++;
        }
        return count;
    }
    
    public bool Delete(string id)
    {
        return _data.TryRemove(id, out _);
    }
    
    public int DeleteBulk(IEnumerable<string> ids)
    {
        int count = 0;
        foreach (var id in ids)
        {
            if (Delete(id))
                count++;
        }
        return count;
    }
    
    public T? FindById(string id)
    {
        _data.TryGetValue(id, out var value);
        return value;
    }
    
    public IEnumerable<T> FindAll()
    {
        return _data.Values;
    }
    
    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        return _data.Values.Where(compiled);
    }
    
    public T? FindOne(Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        return _data.Values.FirstOrDefault(compiled);
    }
    
    public void EnsureIndex(Expression<Func<T, object>> field, IndexOptions? options = null)
    {
        // No-op for in-memory - indexes not needed
    }
    
    public long Count()
    {
        return _data.Count;
    }
    
    public long Count(Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        return _data.Values.Count(compiled);
    }
    
    public bool Exists(Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        return _data.Values.Any(compiled);
    }
}
