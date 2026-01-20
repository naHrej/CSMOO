# Domain Boundaries

This document identifies potential domain boundaries for refactoring CSMOO into a domain-based architecture.

## Domain Identification

### 1. Object Domain

**Purpose**: Core object system - objects, classes, properties, inheritance

**Responsibilities**:
- Object lifecycle (create, update, delete)
- Class definitions and inheritance
- Property management and inheritance
- Object relationships (location, ownership)
- DBREF system

**Key Entities**:
- `GameObject`
- `ObjectClass`
- Property definitions

**Key Services**:
- `IObjectManager`
- `IClassManager`
- `IPropertyManager`
- `IInstanceManager`

**Boundaries**:
- **Input**: Object operations from other domains
- **Output**: Object data to other domains
- **Independent**: Can function without scripting or network

**Dependencies**:
- Persistence Domain (for storage)

**Dependents**:
- All other domains

---

### 2. Script Domain

**Purpose**: Script compilation, execution, and management

**Responsibilities**:
- C# code compilation (Roslyn)
- Script execution context
- Script globals and helpers
- Script preprocessing
- Error handling and stack traces

**Key Entities**:
- Script definitions
- Execution contexts
- Compiled scripts

**Key Services**:
- `IScriptEngineFactory`
- `ScriptEngine`
- `ScriptGlobals`
- `ScriptPreprocessor`

**Boundaries**:
- **Input**: Script code, execution context
- **Output**: Execution results
- **Independent**: Can execute scripts without network

**Dependencies**:
- Object Domain (for object access)
- Verb Domain (for verb calls)
- Function Domain (for function calls)

**Dependents**:
- Verb Domain (executes verb code)
- Function Domain (executes function code)
- Command Domain (executes scripts)

---

### 3. Verb Domain

**Purpose**: Command/action system

**Responsibilities**:
- Verb definitions and storage
- Verb resolution (command → verb)
- Verb inheritance
- Pattern matching
- Verb execution coordination

**Key Entities**:
- `Verb` definitions
- Verb patterns
- Verb metadata

**Key Services**:
- `IVerbManager`
- `IVerbResolver`
- `IVerbInitializer`

**Boundaries**:
- **Input**: Commands from Command Domain
- **Output**: Verb execution results
- **Independent**: Verb definitions independent of execution

**Dependencies**:
- Object Domain (for object lookups)
- Script Domain (for code execution)

**Dependents**:
- Command Domain (uses verb resolution)

---

### 4. Function Domain

**Purpose**: Reusable script functions

**Responsibilities**:
- Function definitions
- Function resolution
- Function execution
- Function parameters and types

**Key Entities**:
- `GameFunction` definitions
- Function parameters

**Key Services**:
- `IFunctionManager`
- `IFunctionResolver`
- `IFunctionInitializer`

**Boundaries**:
- **Input**: Function calls from scripts
- **Output**: Function results
- **Independent**: Function definitions independent of execution

**Dependencies**:
- Script Domain (for code execution)

**Dependents**:
- Script Domain (functions called from scripts)

---

### 5. Player Domain

**Purpose**: Player accounts, sessions, and authentication

**Responsibilities**:
- Player account management
- Authentication and authorization
- Session tracking
- Player permissions
- Player connections

**Key Entities**:
- `Player` accounts
- Sessions
- Permissions/flags

**Key Services**:
- `IPlayerManager`
- `ISessionHandler`
- `IPermissionManager`

**Boundaries**:
- **Input**: Login requests, commands
- **Output**: Player data, session info
- **Independent**: Can manage players without network

**Dependencies**:
- Object Domain (Player extends GameObject)
- Persistence Domain (for storage)

**Dependents**:
- Command Domain (needs current player)
- Network Domain (manages sessions)

---

### 6. Network Domain

**Purpose**: Multi-protocol server support

**Responsibilities**:
- Connection management (Telnet, WebSocket, HTTP)
- Protocol handling
- Message routing
- Connection lifecycle

**Key Entities**:
- Connections
- Sessions (from Player Domain)
- Protocols

**Key Services**:
- `TelnetServer`
- `WebSocketServer`
- `HttpServer`
- `IClientConnection`

**Boundaries**:
- **Input**: Network connections
- **Output**: Messages to clients
- **Independent**: Protocol handling independent of game logic

**Dependencies**:
- Command Domain (routes messages)
- Player Domain (for session management)

**Dependents**:
- None (entry point)

---

### 7. Command Domain

**Purpose**: Command parsing and routing

**Responsibilities**:
- Command parsing
- Command routing
- Built-in command handling
- Command execution coordination
- Output formatting

**Key Entities**:
- Commands
- Command handlers
- Command results

**Key Services**:
- `CommandProcessor`
- Command handlers (proposed)

**Boundaries**:
- **Input**: User input from Network Domain
- **Output**: Formatted output to Network Domain
- **Independent**: Command logic independent of protocol

**Dependencies**:
- Verb Domain (for verb commands)
- Player Domain (for authentication)
- Script Domain (for script execution)
- Object Domain (for object operations)

**Dependents**:
- Network Domain (receives commands)

---

### 8. Persistence Domain

**Purpose**: Data storage and retrieval

**Responsibilities**:
- Database operations
- Data serialization
- Data caching (or delegates to Object Domain)
- Transaction management

**Key Entities**:
- Database collections
- Data models

**Key Services**:
- `IGameDatabase`
- `IDbProvider`
- `IDbCollection<T>`

**Boundaries**:
- **Input**: Data from all domains
- **Output**: Data to all domains
- **Independent**: Pure data access

**Dependencies**:
- None (foundation)

**Dependents**:
- All domains that need persistence

---

### 9. World Domain

**Purpose**: Spatial relationships and world structure

**Responsibilities**:
- Room management
- Exit management
- Spatial queries
- World initialization
- Starting room management

**Key Entities**:
- Rooms
- Exits
- Spatial relationships

**Key Services**:
- `IRoomManager`
- `IWorldInitializer`
- `ICoreClassFactory`

**Boundaries**:
- **Input**: World operations
- **Output**: Spatial data
- **Independent**: World structure independent of execution

**Dependencies**:
- Object Domain (rooms are GameObjects)
- Persistence Domain (for storage)

**Dependents**:
- Command Domain (movement commands)
- Script Domain (spatial queries)

---

## Domain Interaction

### Current Flow

```
Network Domain
    ↓
Command Domain
    ↓
Verb Domain → Script Domain → Object Domain
    ↓                              ↓
Function Domain              Persistence Domain
```

### Proposed Flow

```
Network Domain
    ↓ (messages)
Command Domain
    ↓ (commands)
Verb Domain / Function Domain
    ↓ (script execution)
Script Domain
    ↓ (object operations)
Object Domain / Player Domain / World Domain
    ↓ (data access)
Persistence Domain
```

## Domain Principles

### 1. Dependency Direction

Domains should depend only on:
- Lower-level domains (foundation)
- Shared abstractions (interfaces)

**Example**: Command Domain depends on Verb Domain interface, not implementation.

### 2. Domain Independence

Domains should be independently:
- Testable
- Developable
- Deployable (future)

**Example**: Object Domain can be tested without Network Domain.

### 3. Clear Boundaries

Domain boundaries should be:
- Explicit (via interfaces)
- Documented
- Enforced (via architecture)

**Example**: Command Domain only accesses Object Domain via `IObjectManager`.

---

## Domain Contracts

Each domain should expose:
- **Public Interface**: Services other domains can use
- **Domain Entities**: Core domain objects
- **Domain Events**: Events this domain publishes
- **Dependencies**: What this domain requires

---

## Migration Strategy

### Phase 1: Identify Boundaries
- ✅ Document current domains (this document)
- Define domain interfaces
- Identify cross-domain interactions

### Phase 2: Extract Domains
- Extract domain logic into separate namespaces/projects
- Define domain interfaces
- Set up dependency injection per domain

### Phase 3: Decouple Domains
- Replace direct calls with events
- Use domain events for cross-domain communication
- Reduce dependencies between domains

### Phase 4: Optimize
- Optimize domain boundaries
- Remove unnecessary dependencies
- Improve domain independence

---

## Benefits of Domain-Based Architecture

1. **Clarity**: Clear boundaries and responsibilities
2. **Testability**: Test domains independently
3. **Maintainability**: Changes isolated to domains
4. **Scalability**: Domains can be scaled independently (future)
5. **Extensibility**: Easy to add new domains or features

---

## Related Documentation

- `COMPONENTS.md` - Current component structure
- `SEPARATION_OF_CONCERNS.md` - Current violations
- `REFACTORING_ROADMAP.md` - Migration plan
- `ARCHITECTURE.md` - Overall architecture
