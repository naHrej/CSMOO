# Refactoring Roadmap

This document provides a prioritized roadmap for refactoring CSMOO toward a domain-based architecture with proper separation of concerns.

## Current State Assessment

### Strengths
- ✅ Dependency Injection migration nearly complete
- ✅ Clear interface/implementation separation
- ✅ Well-structured components
- ✅ Comprehensive documentation

### Weaknesses
- ⚠️ Large god objects (CommandProcessor, ObjectManager, Builtins)
- ⚠️ Mixed concerns in multiple classes
- ⚠️ Circular dependencies
- ⚠️ Static wrappers still present
- ⚠️ Tight coupling between components

---

## Refactoring Phases

### Phase 1: Complete DI Migration (Priority: High)

**Goal**: Remove all static wrappers and legacy code

#### Tasks
1. Remove legacy initialization methods
   - Remove `ServerInitializer.Initialize()` (parameterless)
   - Remove `ServerInitializer.CreateDefaultAdminIfNeeded()` (parameterless)
   - Remove `ServerInitializer.Shutdown()` (parameterless)
   - **Effort**: 2-4 hours

2. Investigate and convert remaining static access
   - `Commands/ProgrammingCommands.cs` (2 occurrences)
   - `Core/Builtins.cs` (1 occurrence)
   - `Commands/CommandProcessor.cs` (1 occurrence)
   - **Effort**: 4-8 hours

3. Remove `EnsureInstance()` methods from static wrappers
   - Remove from all static wrapper classes
   - Update tests to use DI
   - **Effort**: 4-8 hours

4. Document decision on static wrappers for data classes
   - Keep wrappers for `GameObject`/`ObjectClass` (acceptable)
   - OR migrate to service locator pattern
   - **Effort**: 2-4 hours

**Total Effort**: 12-24 hours  
**Dependencies**: None  
**Benefits**: Cleaner codebase, easier testing

---

### Phase 2: Extract Command Handlers (Priority: High)

**Goal**: Break up monolithic `CommandProcessor`

#### Tasks
1. Define command handler interfaces
   ```csharp
   interface ICommandHandler<TCommand>
   {
       Task<CommandResult> HandleAsync(TCommand command);
   }
   ```

2. Extract built-in command handlers
   - `LookCommandHandler`
   - `MoveCommandHandler`
   - `QuitCommandHandler`
   - `LoginCommandHandler`
   - **Effort**: 8-16 hours

3. Create command dispatcher
   ```csharp
   interface ICommandDispatcher
   {
       Task<CommandResult> DispatchAsync(string command, Player player);
   }
   ```

4. Refactor `CommandProcessor` to use dispatcher
   - Keep `CommandProcessor` thin (orchestrator only)
   - **Effort**: 8-16 hours

**Total Effort**: 16-32 hours  
**Dependencies**: Phase 1  
**Benefits**: Better testability, easier to add commands

---

### Phase 3: Split ObjectManager (Priority: Medium)

**Goal**: Reduce ObjectManager responsibilities

#### Tasks
1. Extract `IObjectRepository`
   - Basic CRUD operations
   - Move from `IObjectManager`
   - **Effort**: 4-8 hours

2. Extract `IObjectCache`
   - Caching operations
   - Subtype conversion
   - **Effort**: 4-8 hours

3. Extract `IObjectFactory`
   - Object creation
   - Subtype conversion logic
   - **Effort**: 4-8 hours

4. Refactor `IObjectManager` as facade
   - Coordinate repositories, cache, factory
   - Keep existing API for backward compatibility
   - **Effort**: 8-16 hours

**Total Effort**: 20-40 hours  
**Dependencies**: Phase 1  
**Benefits**: Better separation, easier to test

---

### Phase 4: Add Event System (Priority: Medium)

**Goal**: Decouple components via events

#### Tasks
1. Define event interfaces
   ```csharp
   interface IEventBus
   {
       void Publish<T>(T event);
       void Subscribe<T>(IEventHandler<T> handler);
   }
   ```

2. Define domain events
   - `ObjectCreatedEvent`
   - `VerbExecutedEvent`
   - `PlayerConnectedEvent`
   - `PropertyChangedEvent`
   - **Effort**: 4-8 hours

3. Implement event bus
   - Simple in-memory implementation
   - Thread-safe
   - **Effort**: 8-16 hours

4. Migrate cross-domain communication to events
   - Replace direct calls with events
   - Start with non-critical paths
   - **Effort**: 16-32 hours

**Total Effort**: 28-56 hours  
**Dependencies**: Phase 2, Phase 3  
**Benefits**: Loose coupling, extensibility

---

### Phase 5: Extract Repositories (Priority: Low)

**Goal**: Abstract data access

#### Tasks
1. Define repository interfaces
   ```csharp
   interface IRepository<T>
   {
       T? GetById(string id);
       IEnumerable<T> GetAll();
       void Save(T entity);
       void Delete(string id);
   }
   ```

2. Implement repositories
   - `IObjectRepository` for GameObjects
   - `IClassRepository` for ObjectClasses
   - `IVerbRepository` for Verbs
   - **Effort**: 16-32 hours

3. Refactor `IDbProvider` to use repositories
   - Or replace `IDbProvider` with repositories
   - **Effort**: 8-16 hours

**Total Effort**: 24-48 hours  
**Dependencies**: Phase 3  
**Benefits**: Better abstraction, easier testing

---

### Phase 6: Split Builtins (Priority: Low)

**Goal**: Modularize built-in functions

#### Tasks
1. Define focused interfaces
   - `IObjectBuiltins`
   - `IPlayerBuiltins`
   - `IVerbBuiltins`
   - `IMessagingBuiltins`
   - `IUtilityBuiltins`
   - **Effort**: 4-8 hours

2. Split `BuiltinsInstance` into modules
   - One class per interface
   - Keep `Builtins` as aggregator
   - **Effort**: 16-32 hours

3. Update script globals
   - Inject specific builtin modules
   - **Effort**: 8-16 hours

**Total Effort**: 28-56 hours  
**Dependencies**: None  
**Benefits**: Better organization, easier to navigate

---

### Phase 7: Resolve Circular Dependencies (Priority: Low)

**Goal**: Eliminate circular dependencies

#### Tasks
1. Identify all circular dependencies
   - Document in dependency graph
   - **Effort**: 2-4 hours

2. Extract shared concerns
   - Create new interfaces for shared functionality
   - **Effort**: 8-16 hours

3. Use events for cross-cutting concerns
   - Replace direct calls with events
   - **Effort**: 16-32 hours

4. Refactor to eliminate cycles
   - May require domain boundary changes
   - **Effort**: 16-32 hours

**Total Effort**: 42-84 hours  
**Dependencies**: Phase 4  
**Benefits**: Cleaner dependencies, easier to understand

---

### Phase 8: Domain-Based Organization (Priority: Low)

**Goal**: Organize code by domain boundaries

#### Tasks
1. Create domain namespaces/projects
   - Object.Domain
   - Script.Domain
   - Verb.Domain
   - Command.Domain
   - etc.
   - **Effort**: 8-16 hours

2. Move code to appropriate domains
   - Organize by domain boundaries
   - **Effort**: 16-32 hours

3. Define domain contracts
   - Document public interfaces
   - Define domain events
   - **Effort**: 8-16 hours

**Total Effort**: 32-64 hours  
**Dependencies**: Phase 4, Phase 7  
**Benefits**: Clear domain boundaries, better organization

---

## Implementation Guidelines

### Principles

1. **Incremental**: Make small, safe changes
2. **Testable**: Add tests before refactoring
3. **Backward Compatible**: Maintain APIs during migration
4. **Documented**: Update documentation as you go

### Process

1. **Plan**: Document the change
2. **Test**: Write/update tests
3. **Refactor**: Make the change
4. **Verify**: Run all tests
5. **Document**: Update documentation

### Risk Management

- **High Risk**: Large refactorings (ObjectManager split)
- **Medium Risk**: Event system, command handlers
- **Low Risk**: DI cleanup, repository extraction

**Mitigation**:
- Do refactorings incrementally
- Keep old code working during migration
- Have rollback plan

---

## Success Criteria

### Phase 1 Complete When
- ✅ No static wrapper `EnsureInstance()` methods
- ✅ No legacy initialization methods
- ✅ All static access converted or documented

### Phase 2 Complete When
- ✅ CommandProcessor < 500 lines
- ✅ All built-in commands extracted to handlers
- ✅ New commands easy to add

### Phase 3 Complete When
- ✅ ObjectManager < 300 lines
- ✅ Repository, Cache, Factory extracted
- ✅ Clear separation of concerns

### Overall Success When
- ✅ No god objects (>1000 lines)
- ✅ No circular dependencies
- ✅ Clear domain boundaries
- ✅ High test coverage (>80%)
- ✅ Easy to add new features

---

## Estimated Timeline

- **Phase 1**: 1-3 weeks
- **Phase 2**: 2-4 weeks
- **Phase 3**: 2.5-5 weeks
- **Phase 4**: 3.5-7 weeks
- **Phase 5**: 3-6 weeks
- **Phase 6**: 3.5-7 weeks
- **Phase 7**: 5-10 weeks
- **Phase 8**: 4-8 weeks

**Total**: ~24-50 weeks (6-12 months)

**Note**: Phases can overlap, timeline is approximate.

---

## Quick Wins

These can be done immediately with minimal risk:

1. **Remove dead code** (2-4 hours)
   - `Examples/ObjectSystemExample.cs` (if not needed)
   - `Command.g4` (if not used)

2. **Add XML documentation** (8-16 hours)
   - Document all interfaces
   - Document all public methods

3. **Improve error messages** (4-8 hours)
   - Better exception messages
   - More context in errors

---

## Related Documentation

- `ARCHITECTURE.md` - Current architecture
- `SEPARATION_OF_CONCERNS.md` - Current violations
- `DOMAIN_BOUNDARIES.md` - Domain identification
- `DEAD_CODE.md` - Code to remove
- `DI_MIGRATION_STATUS.md` - DI migration status
