using System.Linq.Expressions;
using CSMOO.Functions;
using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Interface for database provider operations (CRUD) on collections.
/// All DB access should go through this interface.
/// </summary>
public interface IDbProvider
{
    /// <summary>
    /// Generic Insert
    /// </summary>
    void Insert<T>(string collectionName, T item);
    
    /// <summary>
    /// Generic Update
    /// </summary>
    bool Update<T>(string collectionName, T item);
    
    /// <summary>
    /// Generic Delete
    /// </summary>
    bool Delete<T>(string collectionName, string id);
    
    /// <summary>
    /// Generic FindAll
    /// </summary>
    IEnumerable<T> FindAll<T>(string collectionName);
    
    /// <summary>
    /// Generic FindById
    /// </summary>
    T? FindById<T>(string collectionName, string id);
    
    /// <summary>
    /// Generic FindOne
    /// </summary>
    T? FindOne<T>(string collectionName, Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Generic Find (with predicate)
    /// </summary>
    IEnumerable<T> Find<T>(string collectionName, Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Find all verbs for an object
    /// </summary>
    IEnumerable<Verb> FindVerbsByObjectId(string objectId);
    
    /// <summary>
    /// Find all functions for an object
    /// </summary>
    IEnumerable<Function> FindFunctionsByObjectId(string objectId);
}
