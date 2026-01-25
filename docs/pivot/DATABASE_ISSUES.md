# Current Database Implementation Issues

## Overview

Analysis of the current database implementation to identify hacks, anti-patterns, and areas needing refactoring.

## Issues Identified

### 1. Static Singleton Pattern (Backward Compatibility Hack)

**Location:** `GameDatabase.cs`, `DbProvider.cs`

**Problem:**
```csharp
// GameDatabase.cs
public static GameDatabase Instance
{
    get
    {
        lock (_lock)
        {
            return _instance ??= new GameDatabase(Config.Instance.Database.GameDataFile);
        }
    }
}

// DbProvider.cs
public static DbProvider Instance => _instance ?? throw new InvalidOperationException(...);
```

**Issues:**
- Static singletons everywhere (backward compatibility hack)
- Creates new instance if not set (hidden dependency)
- Not using DI properly
- Hard to test
- Thread safety concerns

**Impact:** High - Makes testing difficult, hides dependencies

### 2. Circular Dependency Hack

**Location:** `DbProvider.cs`

**Problem:**
```csharp
private IObjectManager? _objectManager;

public void SetObjectManager(IObjectManager objectManager)
{
    _objectManager = objectManager;
}
```

**Issues:**
- Manual dependency injection after construction
- Circular dependency: DbProvider needs ObjectManager, ObjectManager needs DbProvider
- Setter injection is a code smell
- Nullable field that might not be set

**Impact:** High - Indicates architectural problem

### 3. Type Checking Hacks in Generic Methods

**Location:** `DbProvider.cs`

**Problem:**
```csharp
public IEnumerable<T> FindAll<T>(string collectionName)
{
    var results = GetCollection<T>(collectionName).FindAll();
    // If T is GameObject, update the singleton cache
    if (typeof(T) == typeof(GameObject))
    {
        var list = new List<T>();
        foreach (var obj in results)
        {
            var go = obj as GameObject;
            if (go != null)
            {
                if (_objectManager != null)
                {
                    _objectManager.CacheGameObject(go);
                }
                list.Add(obj);
            }
        }
        return list;
    }
    return results;
}
```

**Issues:**
- Type checking in generic methods (violates generic principles)
- Special-casing GameObject everywhere
- Mixing concerns (database + caching)
- Inefficient (type checking on every call)
- Hard to extend (what if we need special handling for other types?)

**Impact:** High - Violates SOLID principles, makes code brittle

### 4. Inefficient Query Implementation

**Location:** `LiteCollectionAdapter.cs`

**Problem:**
```csharp
public IEnumerable<T> Find(Func<T, bool> predicate) => 
    _collection.FindAll().Where(predicate);

public T? FindOne(Func<T, bool> predicate) => 
    _collection.FindAll().FirstOrDefault(predicate);
```

**Issues:**
- Loads ALL records into memory, then filters
- No index usage
- Terrible performance with large datasets
- Should use `_collection.Find(predicate)` for proper query

**Impact:** Critical - Performance killer with 10K+ objects

### 5. Direct LiteDB Type Exposure

**Location:** `IGameDatabase.cs`

**Problem:**
```csharp
public interface IGameDatabase : IDisposable
{
    ILiteCollection<T> GetCollection<T>(string name);
}
```

**Issues:**
- Interface exposes LiteDB-specific type (`ILiteCollection<T>`)
- Not abstracted - can't swap databases
- Violates dependency inversion principle
- Application code depends on LiteDB

**Impact:** Critical - Blocks database migration

### 6. Cache Logic in Database Layer

**Location:** `DbProvider.cs`

**Problem:**
```csharp
// DbProvider handles caching, not just database operations
if (typeof(T) == typeof(GameObject) && result is GameObject go)
{
    if (_objectManager != null)
    {
        var cached = _objectManager.CacheGameObject(go);
        return cached is T t ? t : default;
    }
}
```

**Issues:**
- Database layer shouldn't know about caching
- Violates single responsibility principle
- Tight coupling between DbProvider and ObjectManager
- Makes DbProvider do too much

**Impact:** Medium - Architectural issue, but works

### 7. Adapter Pattern Misuse

**Location:** `LiteCollectionAdapter.cs`

**Problem:**
```csharp
public class LiteCollectionAdapter<T> : IDbCollection<T>
{
    private readonly LiteDB.ILiteCollection<T> _collection;
    // ...
}
```

**Issues:**
- Adapter exists but doesn't properly abstract
- Still exposes LiteDB-specific behavior
- Inefficient implementations (FindAll().Where())
- IDbCollection interface may not be complete

**Impact:** Medium - Good idea, poor execution

### 8. Index Setup in Constructor

**Location:** `GameDatabase.cs`

**Problem:**
```csharp
public GameDatabase(string connectionString)
{
    _database = new LiteDatabase(connectionString);
    
    // Set up indexes for better performance
    var objectClasses = _database.GetCollection<ObjectClass>("objectclasses");
    objectClasses.EnsureIndex(x => x.Id);
    // ... many more indexes
}
```

**Issues:**
- Index setup in constructor (should be migration/initialization)
- Hard-coded index definitions
- No way to add indexes later without code changes
- Mixing schema definition with instance creation

**Impact:** Low - Works but not ideal

### 9. Static Config Access

**Location:** `GameDatabase.cs`

**Problem:**
```csharp
return _instance ??= new GameDatabase(Config.Instance.Database.GameDataFile);
```

**Issues:**
- Static access to Config
- Hidden dependency
- Hard to test
- Not using DI

**Impact:** Medium - Testing difficulty

### 10. No Transaction Support

**Location:** `IDbProvider`, `DbProvider`

**Problem:**
- No transaction methods in interface
- Can't batch operations
- No rollback capability
- Each operation is independent

**Impact:** Medium - Limits optimization opportunities

## Summary of Issues

### Critical Issues (Must Fix)

1. **Direct LiteDB Type Exposure** - Blocks database migration
2. **Inefficient Query Implementation** - Performance killer
3. **Type Checking Hacks** - Violates SOLID, makes code brittle

### High Priority Issues (Should Fix)

4. **Static Singleton Pattern** - Testing difficulty, hidden dependencies
5. **Circular Dependency Hack** - Architectural problem
6. **Cache Logic in Database Layer** - Violates SRP

### Medium Priority Issues (Nice to Fix)

7. **Adapter Pattern Misuse** - Good idea, needs better execution
8. **No Transaction Support** - Limits optimization
9. **Static Config Access** - Testing difficulty

### Low Priority Issues (Can Fix Later)

10. **Index Setup in Constructor** - Works but not ideal

## Refactoring Priorities

### Phase 1: Critical Fixes

1. **Create Proper Abstraction**
   - New `IDatabase` interface (no LiteDB types)
   - New `ICollection<T>` interface
   - Remove `ILiteCollection<T>` from interfaces

2. **Fix Query Implementation**
   - Use proper LiteDB queries (not FindAll().Where())
   - Or use expression tree translation

3. **Remove Type Checking Hacks**
   - Move caching to ObjectManager
   - DbProvider only does database operations
   - Use proper generic design

### Phase 2: High Priority Fixes

4. **Remove Static Singletons**
   - Use DI properly
   - Remove `Instance` properties
   - Remove `SetInstance()` methods

5. **Fix Circular Dependency**
   - Refactor to remove circular dependency
   - Use events or observer pattern for cache updates
   - Or use repository pattern

6. **Separate Concerns**
   - Move caching out of DbProvider
   - DbProvider only does database operations
   - ObjectManager handles caching

### Phase 3: Medium Priority

7. **Improve Adapter**
   - Proper abstraction
   - Efficient implementations
   - Complete interface

8. **Add Transaction Support**
   - Add to interfaces
   - Implement in both layers
   - Support batch operations

## Recommended Refactoring Approach

### Step 1: Create Clean Interfaces

```csharp
// No LiteDB types!
public interface IDatabase : IDisposable
{
    ICollection<T> GetCollection<T>(string name);
    void BeginTransaction();
    void CommitTransaction();
    void RollbackTransaction();
}

public interface ICollection<T>
{
    void Insert(T item);
    bool Update(T item);
    bool Delete(string id);
    T? FindById(string id);
    IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
    // ... proper methods
}
```

### Step 2: Implement Properly

```csharp
// LiteDB implementation
public class LiteDbDatabase : IDatabase { ... }
public class LiteDbCollection<T> : ICollection<T>
{
    // Use proper queries!
    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
    {
        return _collection.Find(predicate); // Not FindAll().Where()!
    }
}
```

### Step 3: Remove Hacks

- Remove static singletons
- Remove type checking
- Remove cache logic from DbProvider
- Fix circular dependency

### Step 4: Use DI Properly

```csharp
// In Program.cs
services.AddSingleton<IDatabase>(sp => 
    new LiteDbDatabase(config.Database.ConnectionString));
services.AddSingleton<IDbProvider, DbProvider>();
services.AddSingleton<IObjectManager, ObjectManagerInstance>();
```

## Testing Impact

**Current State:**
- Hard to test (static singletons)
- Can't mock database easily
- Type checking makes tests brittle

**After Refactoring:**
- Easy to test (DI, interfaces)
- Can use in-memory database for tests
- Clean separation of concerns

## Migration Path

1. **Create new interfaces** (don't break existing code yet)
2. **Implement new classes** (alongside old ones)
3. **Update DbProvider** to use new interfaces
4. **Remove old code** (static singletons, hacks)
5. **Add tests** (now that it's testable)

## Conclusion

The current implementation has several hacks that work but are problematic:
- **Performance issues** (inefficient queries)
- **Architectural issues** (circular dependencies, mixed concerns)
- **Testing issues** (static singletons, hard to mock)
- **Migration blockers** (LiteDB types in interfaces)

**Recommendation:** Refactor as part of Phase 0 cleanup. The abstraction layer work will naturally fix most of these issues.
