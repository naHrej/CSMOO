# Database Performance Analysis: LiteDB

## Overview

This document analyzes LiteDB performance characteristics and potential bottlenecks for scaling to 50-100 concurrent players with thousands to tens of thousands of game objects.

## Current Usage

### LiteDB Configuration

**Current Setup:**
- Single-file database (`gamedata.db`)
- Embedded database (no separate server)
- File-based storage
- No connection pooling (embedded)
- Synchronous operations

**Database Structure:**
- `objects` collection - All game objects
- `classes` collection - Object class definitions
- `verbs` collection - Verb definitions
- `functions` collection - Function definitions
- `properties` collection - Property definitions

## Performance Characteristics

### LiteDB Strengths

**Good For:**
- ✅ Small to medium datasets (< 100K records)
- ✅ Single-user or low-concurrency scenarios
- ✅ Embedded applications
- ✅ Simple queries
- ✅ Fast reads (when indexed)
- ✅ Zero configuration
- ✅ Lightweight

### LiteDB Limitations

**Potential Issues:**
- ❌ **File-based locking**: Single file = single writer
- ❌ **No connection pooling**: Embedded, not client-server
- ❌ **Limited concurrency**: Write operations block
- ❌ **No query optimization**: Simple query engine
- ❌ **Memory usage**: Loads entire index into memory
- ❌ **No replication**: Single point of failure
- ❌ **Limited scalability**: Not designed for high concurrency

## Performance Bottlenecks

### 1. File Locking

**Problem:**
- LiteDB uses file-level locking
- Only one write operation at a time
- Concurrent writes queue and block
- With 50-100 players, writes will queue significantly

**Impact:**
```
Player 1: Updates object → Locks file
Player 2: Updates object → Waits for lock
Player 3: Updates object → Waits for lock
...
Player 100: Updates object → Long wait time
```

**Example:**
```csharp
// All these operations will queue
objectManager.UpdateObject(obj1);  // Locks file
objectManager.UpdateObject(obj2);  // Waits...
objectManager.UpdateObject(obj3);  // Waits...
```

**Estimated Impact:**
- 50 players: Moderate delays (100-500ms per write)
- 100 players: Significant delays (500ms-2s per write)
- High activity: Severe delays (2s+ per write)

### 2. Index Size and Memory

**Problem:**
- LiteDB loads all indexes into memory
- With 10,000+ objects, indexes can be large
- Each object has multiple indexed fields (Id, Name, ClassId, Location, etc.)
- Memory usage grows with object count

**Estimated Memory Usage:**
```
10,000 objects × 5 indexes × ~100 bytes = ~5MB indexes
50,000 objects × 5 indexes × ~100 bytes = ~25MB indexes
100,000 objects × 5 indexes × ~100 bytes = ~50MB indexes
```

**Impact:**
- Memory usage acceptable for 10K-50K objects
- May become issue at 100K+ objects
- Index rebuilds can be slow

### 3. Query Performance

**Problem:**
- LiteDB queries are simple (no complex optimization)
- Unindexed queries scan entire collection
- Complex queries can be slow
- No query plan optimization

**Example Slow Queries:**
```csharp
// Slow: No index on Location
var objectsInRoom = db.GetCollection<GameObject>("objects")
    .Find(x => x.Location == roomId);  // Full table scan

// Slow: Complex query
var players = db.GetCollection<GameObject>("objects")
    .Find(x => x.ClassId == "Player" && x.Location == roomId && x.Properties["online"] == true);
```

**Impact:**
- Simple indexed queries: Fast (< 10ms)
- Unindexed queries: Slow (100ms-1s+)
- Complex queries: Very slow (1s+)

### 4. Write Performance

**Problem:**
- Each write operation locks the file
- Writes are synchronous (blocking)
- No batch write optimization
- Index updates on every write

**Write Operations:**
- Object updates
- Property changes
- Verb/function updates
- Player location changes
- Inventory changes

**Estimated Write Frequency:**
```
50 players × 10 writes/second = 500 writes/second
100 players × 10 writes/second = 1000 writes/second
```

**Impact:**
- With file locking, writes will queue
- Average write delay: 100-500ms (50 players)
- Average write delay: 500ms-2s (100 players)
- Peak times: 2s+ delays

### 5. No Caching Layer

**Problem:**
- LiteDB doesn't have built-in caching
- Every read hits the database file
- No in-memory cache for frequently accessed objects
- Repeated reads of same object = repeated file I/O

**Impact:**
- Frequent object reads: Slow
- Room contents queries: Slow
- Player lookups: Slow

## Performance Estimates

### Scenario: 50 Players, 10,000 Objects

**Read Operations:**
- Indexed queries: < 10ms (acceptable)
- Unindexed queries: 100-500ms (slow)
- Object lookups: < 5ms (acceptable)

**Write Operations:**
- Single write: 50-200ms (acceptable)
- Concurrent writes: 200-500ms (moderate delay)
- Peak writes: 500ms-1s (noticeable delay)

**Overall Assessment:** ⚠️ **Marginal** - May work but with noticeable delays

### Scenario: 100 Players, 50,000 Objects

**Read Operations:**
- Indexed queries: 10-50ms (acceptable)
- Unindexed queries: 500ms-2s (very slow)
- Object lookups: 5-20ms (acceptable)

**Write Operations:**
- Single write: 200-500ms (slow)
- Concurrent writes: 500ms-2s (significant delay)
- Peak writes: 2s-5s (severe delay)

**Overall Assessment:** ❌ **Problematic** - Significant performance issues expected

## Mitigation Strategies

### 1. Implement Caching Layer

**Solution:**
- Add in-memory cache for frequently accessed objects
- Cache room contents, player data, etc.
- Reduce database reads

**Implementation:**
```csharp
public class CachedObjectManager
{
    private IObjectManager _dbManager;
    private MemoryCache _cache = new();
    
    public GameObject GetObject(string id)
    {
        // Check cache first
        if (_cache.TryGetValue(id, out var cached))
            return cached;
        
        // Load from database
        var obj = _dbManager.GetObject(id);
        
        // Cache it
        _cache.Set(id, obj, TimeSpan.FromMinutes(5));
        
        return obj;
    }
}
```

**Benefits:**
- Reduces database reads by 80-90%
- Faster object lookups
- Less file I/O

**Limitations:**
- Doesn't solve write bottleneck
- Memory usage increases
- Cache invalidation complexity

### 2. Batch Writes

**Solution:**
- Group multiple writes into batches
- Reduce file lock contention
- Write multiple objects in single transaction

**Implementation:**
```csharp
public void BatchUpdateObjects(List<GameObject> objects)
{
    using (var transaction = _db.BeginTrans())
    {
        var collection = _db.GetCollection<GameObject>("objects");
        foreach (var obj in objects)
        {
            collection.Update(obj);
        }
        transaction.Commit();
    }
}
```

**Benefits:**
- Reduces file lock time
- Faster bulk updates
- Better write throughput

**Limitations:**
- Still single writer
- Doesn't solve concurrency issue
- Complex transaction management

### 3. Read Replicas (Not Available)

**Problem:**
- LiteDB doesn't support read replicas
- Can't separate reads from writes
- All operations hit same file

**Alternative:**
- Use in-memory cache as "read replica"
- Writes go to database
- Reads come from cache when possible

### 4. Optimize Indexes

**Solution:**
- Ensure all frequently queried fields are indexed
- Remove unused indexes
- Optimize index structure

**Critical Indexes:**
- `Id` (primary key) - Already indexed
- `Location` - For room contents queries
- `ClassId` - For type queries
- `Name` - For name lookups

**Implementation:**
```csharp
var collection = db.GetCollection<GameObject>("objects");
collection.EnsureIndex(x => x.Location);
collection.EnsureIndex(x => x.ClassId);
collection.EnsureIndex(x => x.Name);
```

**Benefits:**
- Faster queries
- Reduced scan operations

**Limitations:**
- Doesn't solve write bottleneck
- Index maintenance overhead

### 5. Write Queue/Async Writes

**Solution:**
- Queue writes instead of blocking
- Process writes asynchronously
- Return immediately to player

**Implementation:**
```csharp
public class AsyncObjectManager
{
    private Queue<GameObject> _writeQueue = new();
    private Task _writeTask;
    
    public void UpdateObject(GameObject obj)
    {
        // Queue write, don't block
        _writeQueue.Enqueue(obj);
        
        // Return immediately
    }
    
    private async Task ProcessWrites()
    {
        while (true)
        {
            if (_writeQueue.TryDequeue(out var obj))
            {
                await Task.Run(() => _dbManager.UpdateObject(obj));
            }
            await Task.Delay(10); // Small delay to batch
        }
    }
}
```

**Benefits:**
- Non-blocking writes
- Better player experience
- Can batch writes

**Limitations:**
- Data consistency issues (write may not be immediate)
- Risk of data loss if server crashes
- Complex error handling

## Alternative Database Options

### Option 1: PostgreSQL

**Pros:**
- ✅ Excellent concurrency (connection pooling)
- ✅ Handles 100+ concurrent users easily
- ✅ ACID transactions
- ✅ Complex queries
- ✅ Proven scalability
- ✅ JSON support (for Properties)

**Cons:**
- ❌ Requires separate server
- ❌ More complex setup
- ❌ Larger footprint
- ❌ More configuration

**Migration Effort:** Medium (2-3 weeks)

### Option 2: SQLite with WAL Mode

**Pros:**
- ✅ Better concurrency than LiteDB
- ✅ WAL mode allows concurrent readers
- ✅ Still embedded (no server)
- ✅ Similar API to LiteDB
- ✅ Better performance

**Cons:**
- ❌ Still single writer
- ❌ Limited scalability
- ❌ Not as good as PostgreSQL

**Migration Effort:** Low (1 week)

### Option 3: MongoDB

**Pros:**
- ✅ Excellent for document storage
- ✅ Good concurrency
- ✅ Scales well
- ✅ JSON-native
- ✅ Good for game objects

**Cons:**
- ❌ Requires separate server
- ❌ More complex
- ❌ Different query language
- ❌ Larger footprint

**Migration Effort:** Medium-High (3-4 weeks)

### Option 4: Redis + LiteDB Hybrid

**Pros:**
- ✅ Redis for hot data (cache)
- ✅ LiteDB for persistence
- ✅ Fast reads from Redis
- ✅ Writes to LiteDB async

**Cons:**
- ❌ Two systems to manage
- ❌ Data sync complexity
- ❌ Still have LiteDB write bottleneck

**Migration Effort:** Medium (2-3 weeks)

## Recommendations

### Short Term (Current Scale: < 50 Players)

**Strategy:**
1. Implement caching layer
2. Optimize indexes
3. Batch writes where possible
4. Monitor performance

**Expected Result:**
- Acceptable performance for < 50 players
- Some delays during peak times
- Manageable with optimizations

### Medium Term (50-100 Players)

**Strategy:**
1. Implement async write queue
2. Aggressive caching
3. Consider SQLite with WAL mode
4. Monitor for bottlenecks

**Expected Result:**
- May work with optimizations
- Will hit limits at 100 players
- Consider migration if issues arise

### Long Term (100+ Players)

**Strategy:**
1. **Migrate to PostgreSQL** (recommended)
2. Or MongoDB if document-focused
3. Implement connection pooling
4. Optimize queries

**Expected Result:**
- Handles 100+ players easily
- Scales to 500+ players
- Professional-grade performance

## Migration Path: LiteDB → PostgreSQL

### Phase 1: Preparation (1 week)

1. Create PostgreSQL schema
2. Map LiteDB collections to PostgreSQL tables
3. Create migration scripts
4. Test migration on copy of data

### Phase 2: Dual Write (1 week)

1. Write to both LiteDB and PostgreSQL
2. Verify data consistency
3. Monitor performance
4. Fix any issues

### Phase 3: Read Migration (1 week)

1. Switch reads to PostgreSQL
2. Keep LiteDB as backup
3. Monitor performance
4. Verify correctness

### Phase 4: Complete Migration (1 week)

1. Remove LiteDB writes
2. Remove LiteDB code
3. Clean up
4. Performance testing

**Total Effort:** 4 weeks

## Performance Testing Plan

### Test Scenarios

1. **50 Players, 10,000 Objects**
   - Measure read latency
   - Measure write latency
   - Measure concurrent write delays
   - Monitor memory usage

2. **100 Players, 50,000 Objects**
   - Same measurements
   - Identify bottlenecks
   - Test peak load

3. **Stress Test**
   - 200 players (over capacity)
   - Measure degradation
   - Identify breaking point

### Metrics to Monitor

- **Read Latency:** P50, P95, P99
- **Write Latency:** P50, P95, P99
- **Concurrent Write Queue:** Average wait time
- **Memory Usage:** Peak, average
- **File I/O:** Reads/second, writes/second
- **Player Experience:** Perceived lag

## Conclusion

**LiteDB Assessment:**
- ⚠️ **Marginal for 50 players** - May work with optimizations
- ❌ **Problematic for 100 players** - Significant issues expected
- ❌ **Not suitable for 100+ players** - Will hit hard limits

**Recommendation:**
1. **Short term:** Optimize LiteDB (caching, indexes, batching)
2. **Medium term:** Monitor performance, plan migration
3. **Long term:** Migrate to PostgreSQL for scalability

**Migration Timeline:**
- Start planning at 30-40 players
- Begin migration at 50 players
- Complete migration before 100 players

## Related Documentation

- [MIGRATION_PLAN.md](./MIGRATION_PLAN.md) - Overall migration plan
- [ARCHITECTURE_PIVOT.md](./ARCHITECTURE_PIVOT.md) - Architecture decisions
