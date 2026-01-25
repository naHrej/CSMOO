using System.Reflection;
using CSMOO.Database;

namespace CSMOO.Database.Implementations;

/// <summary>
/// In-memory implementation of IDatabase for testing
/// </summary>
public class InMemoryDatabase : IDatabase
{
    private readonly Dictionary<string, object> _collections = new();
    private bool _inTransaction = false;
    
    public ICollection<T> GetCollection<T>(string name)
    {
        if (!_collections.TryGetValue(name, out var collection))
        {
            // Create new in-memory collection
            // Try to get Id property via reflection
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
            {
                // Try BsonId attribute
                idProperty = typeof(T).GetProperties()
                    .FirstOrDefault(p => p.GetCustomAttributes(typeof(LiteDB.BsonIdAttribute), false).Any());
            }
            
            if (idProperty == null)
            {
                throw new InvalidOperationException($"Type {typeof(T).Name} must have an 'Id' property or property marked with [BsonId].");
            }
            
            Func<T, string> getId = item =>
            {
                var value = idProperty.GetValue(item);
                return value?.ToString() ?? string.Empty;
            };
            
            collection = new InMemoryCollection<T>(getId);
            _collections[name] = collection;
        }
        
        return (ICollection<T>)collection;
    }
    
    public void BeginTransaction()
    {
        _inTransaction = true;
    }
    
    public void CommitTransaction()
    {
        _inTransaction = false;
    }
    
    public void RollbackTransaction()
    {
        _inTransaction = false;
        // For in-memory, rollback means clearing all collections
        _collections.Clear();
    }
    
    public bool IsInTransaction => _inTransaction;
    
    public void Dispose()
    {
        _collections.Clear();
    }
}
