namespace CSMOO.Database;

/// <summary>
/// Database abstraction interface - no database-specific types
/// </summary>
public interface IDatabase : IDisposable
{
    /// <summary>
    /// Get a collection by name
    /// </summary>
    ICollection<T> GetCollection<T>(string name);
    
    /// <summary>
    /// Begin a transaction
    /// </summary>
    void BeginTransaction();
    
    /// <summary>
    /// Commit the current transaction
    /// </summary>
    void CommitTransaction();
    
    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    void RollbackTransaction();
    
    /// <summary>
    /// Check if currently in a transaction
    /// </summary>
    bool IsInTransaction { get; }
}
