# Interface Contracts

This document analyzes all interfaces (contracts) in CSMOO, their responsibilities, dependencies, and usage patterns.

## Interface Count

**Total Interfaces**: 28

## Interface Categories

### Configuration & Infrastructure (3)

#### `IConfig`
- **Purpose**: Application configuration access
- **Implementation**: `Config`
- **Key Members**: `Server`, `Database`, `Logging`, `Scripting` config objects
- **Dependencies**: None (foundation)
- **Consumers**: All layers

#### `ILogger`
- **Purpose**: Logging operations
- **Implementation**: `LoggerInstance`
- **Key Members**: `Info()`, `Warning()`, `Error()`, `Game()`, `DisplayBanner()`
- **Dependencies**: `IConfig`
- **Consumers**: All layers that need logging

#### `IClientConnection`
- **Purpose**: Abstract connection for protocol independence
- **Implementations**: `TelnetConnection`, `WebSocketConnection`
- **Key Members**: `Send()`, `Disconnect()`
- **Dependencies**: None
- **Consumers**: `CommandProcessor`, `SessionHandler`

---

### Database Layer (4)

#### `IGameDatabase`
- **Purpose**: LiteDB database wrapper
- **Implementation**: `GameDatabase`
- **Key Members**: `GetCollection<T>(string name)`, `Dispose()`
- **Dependencies**: `IConfig`
- **Consumers**: `IDbProvider`

#### `IDbProvider`
- **Purpose**: High-level database operations
- **Implementation**: `DbProvider`
- **Key Members**: `Insert()`, `Update()`, `Delete()`, `Find()`, `FindAll()`, `FindById()`, `FindOne()`
- **Dependencies**: `IGameDatabase`, `IObjectManager` (for caching)
- **Consumers**: All managers, repositories

#### `IDbCollection<T>`
- **Purpose**: Collection abstraction for LiteDB
- **Implementation**: `LiteCollectionAdapter<T>`
- **Key Members**: Standard collection operations
- **Dependencies**: `IGameDatabase`
- **Consumers**: `IDbProvider`

---

### Object System (5)

#### `IObjectManager`
- **Purpose**: Central object lifecycle and operations
- **Implementation**: `ObjectManagerInstance`
- **Key Members**: `GetObject()`, `CreateInstance()`, `UpdateObject()`, `GetProperty()`, `SetProperty()`
- **Dependencies**: `IDbProvider`, `IClassManager`, `IPropertyManager`, `IInstanceManager`
- **Consumers**: Almost all components

#### `IClassManager`
- **Purpose**: Class definition management
- **Implementation**: `ClassManagerInstance`
- **Key Members**: `CreateClass()`, `GetInheritanceChain()`, `InheritsFrom()`, `GetSubclasses()`
- **Dependencies**: `IDbProvider`, `ILogger`
- **Consumers**: `IObjectManager`, `ICoreClassFactory`

#### `IPropertyManager`
- **Purpose**: Property inheritance and access
- **Implementation**: `PropertyManagerInstance`
- **Key Members**: `GetProperty()`, `SetProperty()`, `HasProperty()`, `GetAllPropertyNames()`
- **Dependencies**: `IDbProvider`, `IClassManager`, `IObjectManager` (circular)
- **Consumers**: `IObjectManager`

#### `IInstanceManager`
- **Purpose**: Instance creation and spatial relationships
- **Implementation**: `InstanceManagerInstance`
- **Key Members**: `CreateInstance()`, `DestroyInstance()`, `MoveObject()`, `GetObjectsInLocation()`
- **Dependencies**: `IDbProvider`, `IClassManager`, `IObjectManager`, `IPropertyManager` (circular)
- **Consumers**: `IObjectManager`, `IRoomManager`

#### `IObjectResolver`
- **Purpose**: Resolve object references from strings
- **Implementation**: `ObjectResolverInstance`
- **Key Members**: `ResolveObject()`, `ResolveClass()`, `ResolveKeyword()`
- **Dependencies**: `IObjectManager`, `ICoreClassFactory`
- **Consumers**: `ScriptEngine`, `CommandProcessor`

---

### Player & Session Management (2)

#### `IPlayerManager`
- **Purpose**: Player account and session management
- **Implementation**: `PlayerManagerInstance`
- **Key Members**: `CreatePlayer()`, `AuthenticatePlayer()`, `GetPlayerByName()`, `ConnectPlayerToSession()`
- **Dependencies**: `IDbProvider`, `IObjectManager` (set after creation)
- **Consumers**: `ISessionHandler`, `CommandProcessor`

#### `ISessionHandler`
- **Purpose**: Session tracking and management
- **Implementation**: `SessionHandlerInstance`
- **Key Members**: `AddSession()`, `RemoveSession()`, `LoginPlayer()`, `GetPlayerForSession()`
- **Dependencies**: `IPlayerManager`
- **Consumers**: Network servers

---

### Verb System (3)

#### `IVerbManager`
- **Purpose**: Verb CRUD operations
- **Implementation**: `VerbManagerInstance`
- **Key Members**: `GetVerb()`, `GetVerbsForObject()`, `CreateVerb()`, `UpdateVerb()`, `DeleteVerb()`
- **Dependencies**: `IDbProvider`
- **Consumers**: `IVerbResolver`, `IVerbInitializer`, `CommandProcessor`

#### `IVerbResolver`
- **Purpose**: Resolve commands to verb definitions
- **Implementation**: `VerbResolverInstance`
- **Key Members**: `FindMatchingVerb()`, `FindMatchingVerbWithVariables()`, `GetVerbsForObject()`
- **Dependencies**: `IDbProvider`, `IObjectManager`, `ILogger`
- **Consumers**: `CommandProcessor`, `ScriptEngine`

#### `IVerbInitializer`
- **Purpose**: Load verb definitions from files/database
- **Implementation**: `VerbInitializerInstance`
- **Key Members**: `LoadAndCreateVerbs()`, `ReloadVerbs()`
- **Dependencies**: `IDbProvider`, `ILogger`, `IObjectManager`
- **Consumers**: `ServerInitializer`, `IHotReloadManager`

---

### Function System (3)

#### `IFunctionManager`
- **Purpose**: Function CRUD operations
- **Implementation**: `FunctionManagerInstance`
- **Key Members**: `GetFunction()`, `CreateFunction()`, `UpdateFunction()`, `DeleteFunction()`
- **Dependencies**: `IGameDatabase`
- **Consumers**: `IFunctionResolver`, `IFunctionInitializer`

#### `IFunctionResolver`
- **Purpose**: Resolve function calls
- **Implementation**: `FunctionResolverInstance`
- **Key Members**: `ResolveFunction()`, `GetFunctionsForObject()`
- **Dependencies**: `IDbProvider`, `IObjectManager`
- **Consumers**: `ScriptEngine`

#### `IFunctionInitializer`
- **Purpose**: Load function definitions from files/database
- **Implementation**: `FunctionInitializerInstance`
- **Key Members**: `LoadAndCreateFunctions()`, `ReloadFunctions()`
- **Dependencies**: `IDbProvider`, `ILogger`, `IObjectManager`, `IFunctionManager`
- **Consumers**: `ServerInitializer`, `IHotReloadManager`

---

### Scripting (1)

#### `IScriptEngineFactory`
- **Purpose**: Factory for creating ScriptEngine instances
- **Implementation**: `ScriptEngineFactory`
- **Key Members**: `Create()`
- **Dependencies**: All ScriptEngine dependencies
- **Consumers**: `CommandProcessor`, `Builtins`

---

### World & Room Management (3)

#### `IRoomManager`
- **Purpose**: Room and spatial relationship management
- **Implementation**: `RoomManagerInstance`
- **Key Members**: `GetStartingRoom()`, `GetAllRooms()`, `CreateExit()`, `GetExits()`
- **Dependencies**: `IDbProvider`, `ILogger`, `IObjectManager`
- **Consumers**: `ServerInitializer`, `CommandProcessor`, `ScriptEngine`

#### `IWorldInitializer`
- **Purpose**: Initialize game world (rooms, core classes)
- **Implementation**: `WorldInitializerInstance`
- **Key Members**: `InitializeWorld()`, `PrintWorldStatistics()`
- **Dependencies**: Many (logger, managers, initializers)
- **Consumers**: `ServerInitializer`

#### `ICoreClassFactory`
- **Purpose**: Create core classes (Room, Player, etc.)
- **Implementation**: `CoreClassFactoryInstance`
- **Key Members**: `CreateCoreClasses()`
- **Dependencies**: `IDbProvider`, `ILogger`
- **Consumers**: `IWorldInitializer`, `IObjectResolver`

---

### Initialization & Hot Reload (4)

#### `IPropertyInitializer`
- **Purpose**: Load property definitions from files
- **Implementation**: `PropertyInitializerInstance`
- **Key Members**: `LoadAndCreateProperties()`
- **Dependencies**: `IDbProvider`, `ILogger`, `IObjectManager`
- **Consumers**: `ServerInitializer`, `IHotReloadManager`

#### `IHotReloadManager`
- **Purpose**: Hot reload verbs and functions
- **Implementation**: `HotReloadManagerInstance`
- **Key Members**: `Initialize()`, `Shutdown()`, `ReloadVerbs()`, `ReloadFunctions()`
- **Dependencies**: `ILogger`, `IConfig`, `IVerbInitializer`, `IFunctionInitializer`, `IPlayerManager`
- **Consumers**: `ServerInitializer`

#### `ICoreHotReloadManager`
- **Purpose**: Hot reload core classes
- **Implementation**: `CoreHotReloadManagerInstance`
- **Key Members**: `Initialize()`, `Shutdown()`
- **Dependencies**: `ILogger`, `IPlayerManager`, `IPermissionManager`
- **Consumers**: `ServerInitializer`

---

### Permissions (1)

#### `IPermissionManager`
- **Purpose**: Permission and flag management
- **Implementation**: `PermissionManagerInstance`
- **Key Members**: `HasFlag()`, `AddFlag()`, `RemoveFlag()`, `InitializeAdminPermissions()`
- **Dependencies**: `IDbProvider`, `ILogger`
- **Consumers**: `CommandProcessor`, `GameObject`, `ServerInitializer`

---

### Core Utilities (1)

#### `IBuiltinsInstance`
- **Purpose**: Built-in functions for scripts
- **Implementation**: `BuiltinsInstance`
- **Key Members**: All Builtins static methods as instance methods
- **Dependencies**: Many (object manager, player manager, etc.)
- **Consumers**: Scripts (via static `Builtins` wrapper)

---

## Circular Dependencies

### Circular Dependency Graph

```
IObjectManager
    ↕ (circular via setters)
IPropertyManager
    ↕ (circular via setters)
IInstanceManager
```

**Resolution**: Setter injection after DI container creation

### Other Circular Dependencies

- `IDbProvider` ↔ `IObjectManager` (DbProvider caches objects, ObjectManager uses DbProvider)
- `IPlayerManager` ↔ `IObjectManager` (PlayerManager manages players, ObjectManager accessed)

**Resolution**: Setter injection

---

## Missing Contracts

### Potential Missing Interfaces

1. **Repository Pattern**: No `IRepository<T>` abstraction
   - Could abstract database operations further
   - Would make testing easier

2. **Command Handler**: No `ICommandHandler<TCommand>`
   - Commands currently handled ad-hoc in `CommandProcessor`
   - Could improve separation of concerns

3. **Event System**: No `IEventBus` or `IEventHandler<T>`
   - Could decouple components via events
   - Would enable plugin system

4. **Validation**: No `IValidator<T>`
   - Validation scattered throughout codebase
   - Could centralize validation logic

---

## Interface Design Patterns

### Manager Pattern

Most interfaces follow the "Manager" pattern:
- `*Manager` interfaces coordinate operations
- Single responsibility per manager
- Managers compose other services

**Examples**: `IObjectManager`, `IVerbManager`, `IFunctionManager`

### Resolver Pattern

Resolvers translate between representations:
- `IVerbResolver`: Commands → Verbs
- `IObjectResolver`: Strings → Objects
- `IFunctionResolver`: Names → Functions

### Factory Pattern

Factories create instances:
- `IScriptEngineFactory`: Creates ScriptEngine with dependencies
- `ICoreClassFactory`: Creates core classes

---

## Single Implementations

Interfaces with only one implementation (candidates for consolidation):

1. `IConfig` - Single implementation (`Config`)
2. `ILogger` - Single implementation (`LoggerInstance`)
3. `IClientConnection` - Two implementations (Telnet, WebSocket) - OK
4. `IGameDatabase` - Single implementation (`GameDatabase`)
5. Most managers - Single implementation each

**Recommendation**: Keep interfaces for:
- Testability (can mock)
- Future extensibility
- DI requirements

**Consider Removing**: If interface adds no value and will never have multiple implementations

---

## Contract Violations

### Design Issues

1. **Fat Interfaces**: Some interfaces are too large
   - `IObjectManager`: Too many responsibilities
   - Consider splitting into smaller interfaces

2. **Tight Coupling**: Some interfaces depend on concrete types
   - Better: Depend on abstractions

3. **Circular Dependencies**: Multiple circular dependencies
   - Acceptable for now, but consider refactoring

---

## Recommendations

1. **Split Large Interfaces**: Break `IObjectManager` into smaller interfaces
2. **Extract Repository Pattern**: Create repository interfaces
3. **Add Event System**: Introduce event interfaces for decoupling
4. **Reduce Circular Dependencies**: Refactor to eliminate cycles
5. **Document Contracts**: Add XML documentation to all interface members
