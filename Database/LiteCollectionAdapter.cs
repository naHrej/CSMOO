namespace CSMOO.Database;

/// <summary>
/// Adapter to wrap ILiteCollection<T> as IDbCollection<T>
/// </summary>
public class LiteCollectionAdapter<T> : IDbCollection<T>
{
    private readonly LiteDB.ILiteCollection<T> _collection;
    
    public LiteCollectionAdapter(LiteDB.ILiteCollection<T> collection) 
    { 
        _collection = collection; 
    }
    
    public void Insert(T item) => _collection.Insert(item);
    public bool Update(T item) => _collection.Update(item);
    public bool Delete(string id) => _collection.Delete(id);
    public IEnumerable<T> FindAll() => _collection.FindAll();
    // For Find/FindOne, require a predicate string or BsonExpression for LiteDB compatibility
    public IEnumerable<T> Find(Func<T, bool> predicate) => _collection.FindAll().Where(predicate);
    public T? FindOne(Func<T, bool> predicate) => _collection.FindAll().FirstOrDefault(predicate);
    public T? FindById(string id) => _collection.FindById(id);
}


