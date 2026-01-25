using System.Linq.Expressions;

namespace CSMOO.Database;

/// <summary>
/// Collection abstraction interface - no database-specific types
/// </summary>
public interface ICollection<T>
{
    /// <summary>
    /// Insert a new item
    /// </summary>
    void Insert(T item);
    
    /// <summary>
    /// Insert multiple items
    /// </summary>
    void InsertBulk(IEnumerable<T> items);
    
    /// <summary>
    /// Update an existing item
    /// </summary>
    bool Update(T item);
    
    /// <summary>
    /// Update multiple items
    /// </summary>
    int UpdateBulk(IEnumerable<T> items);
    
    /// <summary>
    /// Delete an item by ID
    /// </summary>
    bool Delete(string id);
    
    /// <summary>
    /// Delete multiple items
    /// </summary>
    int DeleteBulk(IEnumerable<string> ids);
    
    /// <summary>
    /// Find an item by ID
    /// </summary>
    T? FindById(string id);
    
    /// <summary>
    /// Find all items
    /// </summary>
    IEnumerable<T> FindAll();
    
    /// <summary>
    /// Find items matching predicate (uses Expression for proper query translation)
    /// </summary>
    IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Find first item matching predicate
    /// </summary>
    T? FindOne(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Ensure an index exists on a field
    /// </summary>
    void EnsureIndex(Expression<Func<T, object>> field, IndexOptions? options = null);
    
    /// <summary>
    /// Get count of items
    /// </summary>
    long Count();
    
    /// <summary>
    /// Get count of items matching predicate
    /// </summary>
    long Count(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Check if an item exists
    /// </summary>
    bool Exists(Expression<Func<T, bool>> predicate);
}
