using LiteDB;

namespace CSMOO.Database;

/// <summary>
/// Interface for game database operations
/// </summary>
public interface IGameDatabase : IDisposable
{
    /// <summary>
    /// Generic method to get any collection
    /// </summary>
    ILiteCollection<T> GetCollection<T>(string name);
}
