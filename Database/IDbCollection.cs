using System;
using System.Collections.Generic;

namespace CSMOO.Database;

/// <summary>
/// Define a minimal IDbCollection<T> interface for compatibility
/// </summary>
public interface IDbCollection<T>
{
    void Insert(T item);
    bool Update(T item);
    bool Delete(string id);
    IEnumerable<T> FindAll();
    IEnumerable<T> Find(Func<T, bool> predicate);
    T? FindOne(Func<T, bool> predicate);
    T? FindById(string id);
}



