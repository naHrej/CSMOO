# Database Abstraction Layer

## Overview

Create a database abstraction layer to allow swapping database implementations without changing application code. This enables migration from LiteDB to PostgreSQL (or other databases) with minimal code changes.

## Design Goals

**Database Agnostic:**
- Application code doesn't know which database is used
- Swap database implementations without changing business logic
- Support multiple databases (LiteDB, PostgreSQL, etc.)
- Easy to test with in-memory implementations

**Benefits:**
- ✅ Easy migration path (LiteDB → PostgreSQL)
- ✅ Can test with different databases
- ✅ Future-proof (can add MongoDB, etc.)
- ✅ Better testability (in-memory implementations)
- ✅ Cleaner separation of concerns

## Architecture

### Abstraction Layers

```
Application Layer
    ↓
Repository Layer (IDbProvider, IObjectRepository, etc.)
    ↓
Database Abstraction (IDatabase, ICollection, etc.)
    ↓
Database Implementation (LiteDbDatabase, PostgreSqlDatabase, etc.)
```

### Core Interfaces

**1. IDatabase Interface**
```csharp
public interface IDatabase : IDisposable
{
    ICollection<T> GetCollection<T>(string name);
    void BeginTransaction();
    void CommitTransaction();
    void RollbackTransaction();
    bool IsInTransaction { get; }
}
```

**2. ICollection Interface**
```csharp
public interface ICollection<T>
{
    void Insert(T item);
    bool Update(T item);
    bool Delete(string id);
    T? FindById(string id);
    IEnumerable<T> FindAll();
    IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
    T? FindOne(Expression<Func<T, bool>> predicate);
    void EnsureIndex(Expression<Func<T, object>> field);
    long Count();
    long Count(Expression<Func<T, bool>> predicate);
}
```

**3. IDbProvider Interface (Enhanced)**
```csharp
public interface IDbProvider
{
    // Generic operations
    void Insert<T>(string collectionName, T item);
    bool Update<T>(string collectionName, T item);
    bool Delete<T>(string collectionName, string id);
    T? FindById<T>(string collectionName, string id);
    IEnumerable<T> FindAll<T>(string collectionName);
    IEnumerable<T> Find<T>(string collectionName, Expression<Func<T, bool>> predicate);
    T? FindOne<T>(string collectionName, Expression<Func<T, bool>> predicate);
    
    // Transaction support
    void BeginTransaction();
    void CommitTransaction();
    void RollbackTransaction();
    
    // Index management
    void EnsureIndex<T>(string collectionName, Expression<Func<T, object>> field);
}
```

## Implementation Strategy

### Phase 1: Create Abstraction Layer

**Step 1: Define Interfaces**
1. Create `IDatabase` interface
2. Create `ICollection<T>` interface
3. Enhance `IDbProvider` interface
4. Create transaction interfaces

**Step 2: Create LiteDB Implementation**
1. Implement `LiteDbDatabase : IDatabase`
2. Implement `LiteDbCollection<T> : ICollection<T>`
3. Update `DbProvider` to use abstraction
4. Keep existing LiteDB code working

**Step 3: Update Application Code**
1. All code uses `IDbProvider` (already done)
2. No direct LiteDB references in business logic
3. Database-specific code only in implementation layer

### Phase 2: Add PostgreSQL Implementation

**Step 1: Create PostgreSQL Implementation**
1. Implement `PostgreSqlDatabase : IDatabase`
2. Implement `PostgreSqlCollection<T> : ICollection<T>`
3. Map LiteDB operations to PostgreSQL
4. Handle JSON properties (BsonDocument → JSONB)

**Step 2: Configuration**
1. Add database type configuration
2. Connection string configuration
3. Factory pattern for database creation

**Step 3: Testing**
1. Test with LiteDB
2. Test with PostgreSQL
3. Verify same behavior

### Phase 3: Migration Support

**Step 1: Migration Tools**
1. Data migration scripts
2. Schema migration
3. Validation tools

**Step 2: Dual Write (Optional)**
1. Write to both databases during migration
2. Verify consistency
3. Switch reads gradually

## Interface Design

### IDatabase

```csharp
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
    
    /// <summary>
    /// Execute a raw query (database-specific, use sparingly)
    /// </summary>
    TResult ExecuteRaw<TResult>(Func<object, TResult> query);
}
```

### ICollection<T>

```csharp
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
    /// Find items matching predicate
    /// </summary>
    IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Find first item matching predicate
    /// </summary>
    T? FindOne(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Ensure an index exists on a field
    /// </summary>
    void EnsureIndex(Expression<Func<T, object>> field);
    
    /// <summary>
    /// Ensure an index exists on a field (with options)
    /// </summary>
    void EnsureIndex(Expression<Func<T, object>> field, IndexOptions options);
    
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
```

### Index Options

```csharp
public class IndexOptions
{
    public bool Unique { get; set; } = false;
    public bool Sparse { get; set; } = false;
    public string? Name { get; set; }
}
```

## Implementation Examples

### LiteDB Implementation

```csharp
public class LiteDbDatabase : IDatabase
{
    private readonly LiteDatabase _database;
    private LiteDB.Transaction? _transaction;
    
    public LiteDbDatabase(string connectionString)
    {
        _database = new LiteDatabase(connectionString);
    }
    
    public ICollection<T> GetCollection<T>(string name)
    {
        var collection = _database.GetCollection<T>(name);
        return new LiteDbCollection<T>(collection);
    }
    
    public void BeginTransaction()
    {
        _transaction = _database.BeginTrans();
    }
    
    public void CommitTransaction()
    {
        _transaction?.Commit();
        _transaction = null;
    }
    
    public void RollbackTransaction()
    {
        _transaction?.Rollback();
        _transaction = null;
    }
    
    public bool IsInTransaction => _transaction != null;
    
    public void Dispose()
    {
        _transaction?.Dispose();
        _database?.Dispose();
    }
}

public class LiteDbCollection<T> : ICollection<T>
{
    private readonly ILiteCollection<T> _collection;
    
    public LiteDbCollection(ILiteCollection<T> collection)
    {
        _collection = collection;
    }
    
    public void Insert(T item) => _collection.Insert(item);
    
    public void InsertBulk(IEnumerable<T> items) => _collection.InsertBulk(items);
    
    public bool Update(T item) => _collection.Update(item);
    
    public int UpdateBulk(IEnumerable<T> items) => _collection.Update(items);
    
    public bool Delete(string id) => _collection.Delete(id);
    
    public int DeleteBulk(IEnumerable<string> ids) => _collection.DeleteMany(x => ids.Contains(GetId(x)));
    
    public T? FindById(string id) => _collection.FindById(id);
    
    public IEnumerable<T> FindAll() => _collection.FindAll();
    
    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate) => _collection.Find(predicate);
    
    public T? FindOne(Expression<Func<T, bool>> predicate) => _collection.FindOne(predicate);
    
    public void EnsureIndex(Expression<Func<T, object>> field) => _collection.EnsureIndex(field);
    
    public void EnsureIndex(Expression<Func<T, object>> field, IndexOptions options)
    {
        _collection.EnsureIndex(field, options.Unique);
    }
    
    public long Count() => _collection.Count();
    
    public long Count(Expression<Func<T, bool>> predicate) => _collection.Count(predicate);
    
    public bool Exists(Expression<Func<T, bool>> predicate) => _collection.Exists(predicate);
    
    private string GetId(T item)
    {
        // Extract ID from item (reflection or interface)
        // Implementation depends on how IDs are stored
        return ((dynamic)item).Id;
    }
}
```

### PostgreSQL Implementation

```csharp
public class PostgreSqlDatabase : IDatabase
{
    private readonly NpgsqlConnection _connection;
    private NpgsqlTransaction? _transaction;
    
    public PostgreSqlDatabase(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
    }
    
    public ICollection<T> GetCollection<T>(string name)
    {
        return new PostgreSqlCollection<T>(_connection, name);
    }
    
    public void BeginTransaction()
    {
        _transaction = _connection.BeginTransaction();
    }
    
    public void CommitTransaction()
    {
        _transaction?.Commit();
        _transaction = null;
    }
    
    public void RollbackTransaction()
    {
        _transaction?.Rollback();
        _transaction = null;
    }
    
    public bool IsInTransaction => _transaction != null;
    
    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }
}

public class PostgreSqlCollection<T> : ICollection<T>
{
    private readonly NpgsqlConnection _connection;
    private readonly string _tableName;
    
    public PostgreSqlCollection(NpgsqlConnection connection, string tableName)
    {
        _connection = connection;
        _tableName = tableName;
    }
    
    public void Insert(T item)
    {
        // Serialize to JSON, insert into PostgreSQL
        var json = JsonSerializer.Serialize(item);
        var sql = $"INSERT INTO {_tableName} (id, data) VALUES (@id, @data::jsonb)";
        using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("id", GetId(item));
        cmd.Parameters.AddWithValue("data", json);
        cmd.ExecuteNonQuery();
    }
    
    public T? FindById(string id)
    {
        var sql = $"SELECT data FROM {_tableName} WHERE id = @id";
        using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("id", id);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var json = reader.GetString(0);
            return JsonSerializer.Deserialize<T>(json);
        }
        return default;
    }
    
    // ... implement other methods
}
```

## Database Factory

```csharp
public interface IDatabaseFactory
{
    IDatabase CreateDatabase();
}

public class DatabaseFactory : IDatabaseFactory
{
    private readonly IConfig _config;
    
    public DatabaseFactory(IConfig config)
    {
        _config = config;
    }
    
    public IDatabase CreateDatabase()
    {
        var dbType = _config.Database.Type; // "LiteDB" or "PostgreSQL"
        var connectionString = _config.Database.ConnectionString;
        
        return dbType switch
        {
            "LiteDB" => new LiteDbDatabase(connectionString),
            "PostgreSQL" => new PostgreSqlDatabase(connectionString),
            _ => throw new NotSupportedException($"Database type '{dbType}' not supported")
        };
    }
}
```

## Configuration

```csharp
public class DatabaseConfig
{
    public string Type { get; set; } = "LiteDB"; // "LiteDB" or "PostgreSQL"
    public string ConnectionString { get; set; } = "gamedata.db";
    public int? MaxPoolSize { get; set; } // For PostgreSQL
    public int? CommandTimeout { get; set; } // For PostgreSQL
}
```

## Migration Path

### Step 1: Create Abstraction (Week 1)

1. Define `IDatabase` and `ICollection<T>` interfaces
2. Create `LiteDbDatabase` and `LiteDbCollection<T>` implementations
3. Update `DbProvider` to use `IDatabase` instead of `LiteDatabase`
4. Test that everything still works

### Step 2: Refactor Application Code (Week 1)

1. Ensure all code uses `IDbProvider` (already mostly done)
2. Remove direct `LiteDatabase` references
3. Remove direct `ILiteCollection` references
4. All database access through abstraction

### Step 3: Add PostgreSQL Support (Week 2-3)

1. Create `PostgreSqlDatabase` and `PostgreSqlCollection<T>` implementations
2. Handle JSON properties (BsonDocument → JSONB)
3. Add database factory
4. Add configuration support
5. Test with PostgreSQL

### Step 4: Migration Tools (Week 3-4)

1. Create data migration scripts
2. Create schema migration
3. Create validation tools
4. Test migration process

## Benefits

**Immediate Benefits:**
- Cleaner code (no direct database dependencies)
- Better testability (can use in-memory implementations)
- Easier to mock for testing

**Future Benefits:**
- Easy to swap databases
- Can support multiple databases
- Can migrate gradually
- Can test with different databases

## Testing Strategy

### Unit Tests

**In-Memory Implementation:**
```csharp
public class InMemoryDatabase : IDatabase
{
    private Dictionary<string, Dictionary<string, object>> _collections = new();
    
    public ICollection<T> GetCollection<T>(string name)
    {
        if (!_collections.ContainsKey(name))
            _collections[name] = new Dictionary<string, object>();
        
        return new InMemoryCollection<T>(_collections[name]);
    }
    
    // ... implement other methods
}
```

**Test Example:**
```csharp
[Test]
public void TestObjectManager()
{
    // Use in-memory database for testing
    var db = new InMemoryDatabase();
    var dbProvider = new DbProvider(db);
    var objectManager = new ObjectManagerInstance(dbProvider);
    
    // Test object operations
    var obj = objectManager.CreateObject("TestObject", "test-class");
    Assert.NotNull(obj);
    
    var retrieved = objectManager.GetObject(obj.Id);
    Assert.AreEqual(obj.Id, retrieved.Id);
}
```

### Integration Tests

**Test with Real Databases:**
```csharp
[Test]
public void TestWithLiteDB()
{
    var db = new LiteDbDatabase("test.db");
    TestDatabaseOperations(db);
}

[Test]
public void TestWithPostgreSQL()
{
    var db = new PostgreSqlDatabase("postgresql://...");
    TestDatabaseOperations(db);
}

private void TestDatabaseOperations(IDatabase db)
{
    // Same test code works with both databases
    var collection = db.GetCollection<GameObject>("objects");
    var obj = new GameObject { Id = "test-1", Name = "Test" };
    collection.Insert(obj);
    
    var retrieved = collection.FindById("test-1");
    Assert.NotNull(retrieved);
    Assert.AreEqual("Test", retrieved.Name);
}
```

## Challenges

### Challenge 1: JSON Properties

**Problem:**
- LiteDB uses `BsonDocument` for Properties
- PostgreSQL uses JSONB
- Need to handle conversion

**Solution:**
- Use `JsonElement` or custom JSON type in abstraction
- Convert in implementation layer
- Or use `Dictionary<string, object>` in interface

### Challenge 2: Query Differences

**Problem:**
- LiteDB uses LINQ expressions
- PostgreSQL uses SQL
- Some queries may not translate directly

**Solution:**
- Keep queries simple
- Use expression tree translation
- Or provide database-specific query methods

### Challenge 3: Transactions

**Problem:**
- Different transaction models
- LiteDB: Simple transactions
- PostgreSQL: Full ACID transactions

**Solution:**
- Abstract transaction interface
- Each implementation handles its own transaction model
- Document differences

## Implementation Checklist

- [ ] Create `IDatabase` interface
- [ ] Create `ICollection<T>` interface
- [ ] Create `LiteDbDatabase` implementation
- [ ] Create `LiteDbCollection<T>` implementation
- [ ] Update `DbProvider` to use `IDatabase`
- [ ] Remove direct LiteDB references from application code
- [ ] Add database factory
- [ ] Add configuration support
- [ ] Create in-memory implementation for testing
- [ ] Write unit tests
- [ ] Create `PostgreSqlDatabase` implementation (future)
- [ ] Create `PostgreSqlCollection<T>` implementation (future)
- [ ] Create migration tools (future)

## Related Documentation

- [DATABASE_PERFORMANCE.md](./DATABASE_PERFORMANCE.md) - Performance analysis
- [MIGRATION_PLAN.md](./MIGRATION_PLAN.md) - Overall migration plan
- [REFACTORING_STRATEGY.md](./REFACTORING_STRATEGY.md) - Code refactoring
