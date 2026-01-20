# Dependency Injection Architecture

This document describes the current dependency injection (DI) architecture, migration status, and patterns used in CSMOO.

## Overview

CSMOO is in the process of migrating from static singleton patterns to dependency injection. The migration is nearly complete, with all critical components converted to DI while maintaining backward compatibility through static wrapper classes.

**Status**: Phase 1 & 2 Complete ✅ | Phase 3 (Cleanup) Pending

## Current Architecture

### DI Container

The DI container is configured in `Program.ConfigureServices()` using Microsoft.Extensions.DependencyInjection.

**All services are registered as singletons** because:
- Game state is shared across all connections
- Managers maintain internal caches
- Database connections are singleton
- Logging is centralized

### Service Registration Pattern

Services are registered with factory functions that resolve dependencies:

```csharp
services.AddSingleton<IConfig>(sp => Config.Load());
services.AddSingleton<ILogger>(sp => 
{
    var config = sp.GetRequiredService<IConfig>();
    return new LoggerInstance(config);
});
services.AddSingleton<IObjectManager>(sp =>
{
    var dbProvider = sp.GetRequiredService<IDbProvider>();
    var classManager = sp.GetRequiredService<IClassManager>();
    return new ObjectManagerInstance(dbProvider, classManager);
});
```

## Static Wrapper Pattern

During migration, each service has both:
1. **Interface + Instance Implementation**: DI-compatible (e.g., `IObjectManager` / `ObjectManagerInstance`)
2. **Static Wrapper**: Backward compatibility (e.g., `ObjectManager`)

### Static Wrapper Structure

```csharp
public static class ObjectManager
{
    private static IObjectManager? _instance;
    
    public static void SetInstance(IObjectManager instance)
    {
        _instance = instance;
    }
    
    private static IObjectManager Instance => 
        _instance ?? throw new InvalidOperationException("Instance not set");
    
    // All methods delegate to instance
    public static GameObject? GetObject(string id)
    {
        return Instance.GetObject(id);
    }
}
```

### Static Instance Setup

In `Program.Main()`, after building the service provider, static instances are set:

```csharp
var objectManager = serviceProvider.GetRequiredService<IObjectManager>();
ObjectManager.SetInstance(objectManager);

var logger = serviceProvider.GetRequiredService<ILogger>();
Logger.SetInstance(logger);
// ... more SetInstance() calls
```

This allows legacy code and data classes (`GameObject`, `ObjectClass`) to continue using static access while new code uses DI.

## Converted Components

### ✅ All Major Components (39+)

All core components have been converted to DI:

1. **Configuration**: `IConfig` / `Config`
2. **Logging**: `ILogger` / `LoggerInstance`
3. **Database**: `IGameDatabase` / `GameDatabase`, `IDbProvider` / `DbProvider`
4. **Object System**: `IObjectManager`, `IClassManager`, `IInstanceManager`, `IPropertyManager`
5. **Player Management**: `IPlayerManager` / `PlayerManagerInstance`
6. **Rooms**: `IRoomManager` / `RoomManagerInstance`
7. **Permissions**: `IPermissionManager` / `PermissionManagerInstance`
8. **Verbs**: `IVerbManager`, `IVerbResolver`, `IVerbInitializer`
9. **Functions**: `IFunctionManager`, `IFunctionResolver`, `IFunctionInitializer`
10. **Scripting**: `IScriptEngineFactory` / `ScriptEngineFactory`, `ScriptEngine`
11. **Sessions**: `ISessionHandler` / `SessionHandlerInstance`
12. **Initialization**: `IWorldInitializer` / `WorldInitializerInstance`
13. **Hot Reload**: `IHotReloadManager`, `ICoreHotReloadManager`
14. **Commands**: `CommandProcessor`, `ProgrammingCommands`
15. **Network**: `HttpServer`
16. **Core**: `IObjectResolver` / `ObjectResolverInstance`, `IBuiltinsInstance` / `BuiltinsInstance`

See `DI_MIGRATION_STATUS.md` for complete list.

## Circular Dependencies

CSMOO has several circular dependencies that are resolved through setter injection:

### ObjectManager ↔ PropertyManager ↔ InstanceManager

**The Problem**:
- `ObjectManager` needs `PropertyManager` for property operations
- `PropertyManager` needs `ObjectManager` for object lookups
- `InstanceManager` needs both `ObjectManager` and `PropertyManager`

**The Solution**: Setter injection after DI container creation

```csharp
// In Program.ConfigureServices()
services.AddSingleton<IObjectManager>(sp => {
    var objectManager = new ObjectManagerInstance(...);
    // PropertyManager and InstanceManager will set themselves later
    return objectManager;
});

services.AddSingleton<IPropertyManager>(sp => {
    var propertyManager = new PropertyManagerInstance(...);
    // Set on ObjectManager to complete circular reference
    var objectManager = sp.GetRequiredService<IObjectManager>();
    if (objectManager is ObjectManagerInstance omi)
    {
        omi.SetPropertyManager(propertyManager);
    }
    return propertyManager;
});

services.AddSingleton<IInstanceManager>(sp => {
    var instanceManager = new InstanceManagerInstance(...);
    // Set on ObjectManager to complete circular reference
    var objectManager = sp.GetRequiredService<IObjectManager>();
    if (objectManager is ObjectManagerInstance omi)
    {
        omi.SetInstanceManager(instanceManager);
    }
    return instanceManager;
});
```

### DbProvider ↔ ObjectManager

**The Problem**:
- `DbProvider` needs `ObjectManager` for object caching
- `ObjectManager` needs `DbProvider` for database access

**The Solution**: Setter injection

```csharp
services.AddSingleton<IObjectManager>(sp => {
    var objectManager = new ObjectManagerInstance(...);
    var dbProvider = sp.GetRequiredService<IDbProvider>();
    // Set ObjectManager on DbProvider for caching
    if (dbProvider is DbProvider dbp)
    {
        dbp.SetObjectManager(objectManager);
    }
    return objectManager;
});
```

### PlayerManager ↔ ObjectManager

Similar pattern: `ObjectManager` is set on `PlayerManagerInstance` after creation.

## Remaining Static Access

### Acceptable Static Access

These are acceptable because they're data classes that can't use DI:

#### `GameObject` and `ObjectClass`

**Why**: These are serialized data classes stored in the database. They can't have constructor-injected dependencies.

**Current Pattern**:
```csharp
public class GameObject
{
    public GameObject? Location
    {
        get
        {
            var loc = Properties["location"].AsString;
            return loc != null ? ObjectManager.GetObject(loc) : null; // Static access
        }
    }
}
```

**Solution**: Use static wrappers that delegate to DI instances (already implemented).

### Remaining Legacy Static Access

There are still **11 occurrences** of `.Instance` static access in:

1. **`Init/ServerInitializer.cs`** (2) - Legacy initialization methods
2. **`Object/GameObject.cs`** (1) - Data class limitation (acceptable)
3. **`Examples/ObjectSystemExample.cs`** (3) - Example code (acceptable)
4. **`Commands/ProgrammingCommands.cs`** (2) - Needs investigation
5. **`Core/Builtins.cs`** (1) - Needs investigation
6. **`Database/GameDatabase.cs`** (1) - Singleton pattern (acceptable)
7. **`Commands/CommandProcessor.cs`** (1) - Needs investigation

**Action Required**: Investigate and convert items 4, 5, and 7.

## Data Classes and DI

### The Challenge

`GameObject` and `ObjectClass` are:
- Serialized to/from database using LiteDB
- Created by deserialization (no constructor control)
- Used extensively in scripts and throughout codebase

### Current Solution

Static wrapper classes that delegate to DI instances:

```csharp
// GameObject uses static wrapper
ObjectManager.GetObject(id) // Static call
    ↓
ObjectManager.Instance.GetObject(id) // Delegates to DI instance
    ↓
ObjectManagerInstance.GetObject(id) // Actual implementation
```

**Pros**:
- Allows data classes to access services
- Maintains backward compatibility
- No breaking changes during migration

**Cons**:
- Still uses static access pattern
- Can't be easily mocked in tests
- Violates dependency inversion principle

### Future Options

1. **Service Locator Pattern**: Inject `IServiceProvider` into data classes
2. **Pass Dependencies Through Methods**: Pass managers as parameters
3. **Keep Static Wrappers**: Acceptable trade-off for data classes

**Recommendation**: Keep static wrappers for data classes. They delegate to DI instances and provide a clean API.

## Migration Phases

### Phase 1: Convert Core Components ✅

- [x] Convert all interfaces and implementations
- [x] Set up DI container configuration
- [x] Create static wrapper classes
- [x] Convert `CommandProcessor` and related components

### Phase 2: Convert Supporting Components ✅

- [x] Convert scripting components
- [x] Convert network servers
- [x] Convert all managers
- [x] Handle data class limitations

### Phase 3: Cleanup (Pending)

- [ ] Remove legacy initialization methods
- [ ] Remove `EnsureInstance()` methods from static wrappers
- [ ] Investigate and convert remaining static access
- [ ] Remove static wrapper classes (or keep for data classes)
- [ ] Update tests to use DI

## DI Registration in Program.cs

The DI container is configured with careful attention to:

1. **Dependency Order**: Services registered in dependency order
2. **Circular Resolution**: Setter injection for circular dependencies
3. **Force Creation**: Some services force creation of dependencies to ensure initialization order

Example:

```csharp
services.AddSingleton<IRoomManager>(sp =>
{
    // Force InstanceManager creation first
    var _ = sp.GetRequiredService<IInstanceManager>();
    
    var dbProvider = sp.GetRequiredService<IDbProvider>();
    var logger = sp.GetRequiredService<ILogger>();
    var objectManager = sp.GetRequiredService<IObjectManager>();
    return new RoomManagerInstance(dbProvider, logger, objectManager);
});
```

## Testing with DI

### Current State

Tests still use static access patterns in many places, which makes testing difficult.

### Future State

With complete DI migration:
- Tests can inject mocks/stubs
- Components can be tested in isolation
- No static state to manage in tests

### Example Test Pattern (Future)

```csharp
[Test]
public void TestObjectManager()
{
    var mockDbProvider = new Mock<IDbProvider>();
    var mockClassManager = new Mock<IClassManager>();
    
    var objectManager = new ObjectManagerInstance(
        mockDbProvider.Object, 
        mockClassManager.Object
    );
    
    // Test objectManager in isolation
}
```

## Benefits of DI Migration

1. **Testability**: Components can be tested in isolation
2. **Flexibility**: Easy to swap implementations
3. **Clear Dependencies**: Constructor signatures show dependencies
4. **Lifecycle Control**: DI container manages object lifecycle
5. **Reduced Coupling**: No static dependencies

## Challenges and Trade-offs

### Challenges

1. **Circular Dependencies**: Resolved via setter injection (acceptable)
2. **Data Classes**: Static wrappers provide acceptable solution
3. **Migration Complexity**: Dual implementation during migration

### Trade-offs

1. **Static Wrappers**: Temporary code during migration, but needed for data classes
2. **Setter Injection**: Less ideal than constructor injection, but necessary for circular deps
3. **Service Provider Access**: Some components need `IServiceProvider` to resolve dependencies

## Next Steps

1. **Complete Phase 3**: Remove legacy code paths
2. **Convert Remaining Static Access**: Investigate and convert items in `ProgrammingCommands`, `Builtins`, `CommandProcessor`
3. **Update Tests**: Migrate tests to use DI
4. **Document Patterns**: Establish coding standards for DI usage

## Related Documentation

- `DI_MIGRATION_STATUS.md` - Detailed migration status
- `COMPONENTS.md` - Component documentation
- `DEAD_CODE.md` - Legacy code patterns
- `ARCHITECTURE.md` - Overall system architecture
