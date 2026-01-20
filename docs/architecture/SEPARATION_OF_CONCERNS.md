# Separation of Concerns Analysis

This document identifies current violations of separation of concerns and areas for improvement in CSMOO's architecture.

## Current Violations

### 1. ObjectManager Doing Too Much

**Location**: `ObjectManagerInstance`

**Problem**: ObjectManager has multiple responsibilities:
- Object CRUD operations
- Property management (delegates but coordinates)
- Instance management (delegates but coordinates)
- Class management (delegates but coordinates)
- Caching
- Subtype conversion
- DBREF resolution

**Impact**: 
- Difficult to test
- Hard to modify one aspect without affecting others
- Large interface (55+ methods)

**Recommendation**: Split into:
- `IObjectRepository`: Basic CRUD
- `IObjectCache`: Caching operations
- `IObjectFactory`: Object creation/subtype conversion
- Keep `IObjectManager` as coordinator (facade pattern)

---

### 2. CommandProcessor as God Object

**Location**: `CommandProcessor`

**Problem**: Single class handles:
- Command parsing
- Command routing
- Authentication
- Login/logout
- Built-in commands
- Verb execution coordination
- Property editing
- Multi-line input handling
- Output formatting

**Impact**:
- Very large class (~2000+ lines)
- Difficult to test
- Hard to extend with new commands

**Recommendation**: 
- Extract command handlers: `ICommandHandler<TCommand>`
- Separate authentication: `IAuthenticationService`
- Extract built-in commands: `IBuiltInCommandHandler`
- Keep `CommandProcessor` as orchestrator

---

### 3. Static Wrappers Mixing Concerns

**Location**: Static wrapper classes (e.g., `ObjectManager`)

**Problem**: Static wrappers:
- Handle DI delegation
- Handle backward compatibility
- Provide singleton access
- Create default instances (EnsureInstance)

**Impact**:
- Confusing dual implementation
- Hard to understand code flow
- Maintenance burden

**Recommendation**: 
- Remove after DI migration complete
- For data classes, use service locator pattern instead

---

### 4. Network Servers Creating CommandProcessors Directly

**Location**: `TelnetServer`, `WebSocketServer`

**Problem**: Network servers:
- Create CommandProcessor instances
- Resolve all dependencies manually
- Mix network concerns with command processing

**Impact**:
- Tight coupling
- Code duplication
- Hard to test network layer

**Recommendation**:
- Use factory pattern: `ICommandProcessorFactory`
- Inject factory into network servers
- Network servers only handle protocol concerns

---

### 5. ScriptEngine Mixing Compilation and Execution

**Location**: `ScriptEngine`

**Problem**: ScriptEngine handles:
- Code preprocessing
- Script compilation
- Script execution
- Context management
- Error handling
- Timeout management

**Impact**:
- Complex class
- Hard to test individual aspects
- Difficult to add new preprocessing steps

**Recommendation**:
- Extract `IScriptPreprocessor`
- Extract `IScriptCompiler`
- Extract `IScriptExecutor`
- Keep `ScriptEngine` as coordinator

---

### 6. Database Layer Leaking into Domain

**Location**: `GameObject`, `ObjectClass`

**Problem**: Domain entities:
- Use `ObjectManager` static access
- Know about database operations
- Handle serialization concerns

**Impact**:
- Domain logic mixed with infrastructure
- Hard to test domain logic
- Violates dependency inversion

**Recommendation**:
- Use service locator or dependency injection
- Keep domain entities pure
- Move serialization to repository layer

---

### 7. Builtins as God Class

**Location**: `Builtins`, `BuiltinsInstance`

**Problem**: Builtins provides:
- Object operations
- Player operations
- Verb operations
- Function operations
- Messaging
- Utility functions
- Script execution

**Impact**:
- Large class (1700+ lines)
- Hard to navigate
- Everything depends on it

**Recommendation**:
- Split into focused modules:
  - `IObjectBuiltins`
  - `IPlayerBuiltins`
  - `IVerbBuiltins`
  - `IMessagingBuiltins`
  - `IUtilityBuiltins`

---

### 8. Circular Dependencies

**Problem**: Multiple circular dependencies:
- `IObjectManager` ↔ `IPropertyManager` ↔ `IInstanceManager`
- `IDbProvider` ↔ `IObjectManager`
- `IPlayerManager` ↔ `IObjectManager`

**Impact**:
- Complex initialization
- Hard to understand dependencies
- Testing difficulties

**Recommendation**:
- Extract shared concerns to separate interfaces
- Use events for cross-cutting concerns
- Consider mediator pattern

---

## Areas for Improvement

### 1. Command Processing

**Current**: Monolithic `CommandProcessor`

**Better**: Command pattern with handlers

```csharp
interface ICommandHandler<TCommand>
{
    Task<CommandResult> HandleAsync(TCommand command);
}

class LookCommandHandler : ICommandHandler<LookCommand> { }
class MoveCommandHandler : ICommandHandler<MoveCommand> { }
```

**Benefits**:
- Easy to add new commands
- Testable handlers
- Clear separation

---

### 2. Event System

**Current**: Direct method calls between components

**Better**: Event-driven architecture

```csharp
interface IEventBus
{
    void Publish<T>(T event);
    void Subscribe<T>(IEventHandler<T> handler);
}

// Events
class ObjectCreatedEvent { }
class VerbExecutedEvent { }
```

**Benefits**:
- Loose coupling
- Extensibility
- Better testability

---

### 3. Repository Pattern

**Current**: Direct database access via `IDbProvider`

**Better**: Repository abstraction

```csharp
interface IRepository<T>
{
    T? GetById(string id);
    IEnumerable<T> GetAll();
    void Save(T entity);
    void Delete(string id);
}

interface IObjectRepository : IRepository<GameObject> { }
```

**Benefits**:
- Abstraction over data access
- Easier testing
- Possible to swap data stores

---

### 4. Domain Services

**Current**: Managers doing business logic

**Better**: Domain services

```csharp
interface IObjectCreationService
{
    GameObject CreateObject(string classId, string? location);
    void ValidateCreation(GameObject obj);
}

interface IPropertyInheritanceService
{
    BsonValue ResolveProperty(GameObject obj, string name);
}
```

**Benefits**:
- Business logic in one place
- Reusable services
- Testable logic

---

## Testability Issues

### Current Problems

1. **Static Dependencies**: Hard to mock
2. **Tight Coupling**: Can't test in isolation
3. **Circular Dependencies**: Complex setup
4. **God Objects**: Too many dependencies to mock

### Improvements Needed

1. **Dependency Injection**: All dependencies injected
2. **Interface Segregation**: Small, focused interfaces
3. **Dependency Inversion**: Depend on abstractions
4. **Single Responsibility**: One reason to change

---

## Boundaries for Domain Separation

### Current Layer Structure

```
Network → Command → Verb → Script → Object → Database
```

### Proposed Domain Boundaries

1. **Network Domain**: Protocol handling, connections
2. **Command Domain**: Command parsing and routing
3. **Script Domain**: Script execution, compilation
4. **Object Domain**: Object lifecycle, properties, classes
5. **Player Domain**: Authentication, sessions, players
6. **Persistence Domain**: Database operations, serialization

**Benefits**:
- Clear boundaries
- Independent development
- Better testability
- Easier to understand

---

## Refactoring Priorities

### High Priority

1. **Extract Command Handlers**: Break up `CommandProcessor`
2. **Split ObjectManager**: Reduce responsibilities
3. **Remove Static Wrappers**: Complete DI migration

### Medium Priority

4. **Add Event System**: Decouple components
5. **Extract Repositories**: Abstract data access
6. **Split Builtins**: Modularize built-in functions

### Low Priority

7. **Resolve Circular Dependencies**: Refactor to events
8. **Domain Services**: Extract business logic
9. **Command Factory**: Abstract command processor creation

---

## Related Documentation

- `COMPONENTS.md` - Component responsibilities
- `DOMAIN_BOUNDARIES.md` - Domain identification
- `REFACTORING_ROADMAP.md` - Refactoring plan
- `CONTRACTS.md` - Interface analysis
