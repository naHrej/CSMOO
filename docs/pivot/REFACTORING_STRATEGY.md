# Refactoring Strategy: Domain-Specific Projects

## Overview

Refactor the monolithic codebase into domain-specific projects with intelligent namespace organization. Break up megalithic classes into smaller, focused classes grouped by domain.

## Goals

1. **Domain-Specific Projects**: Separate concerns into distinct projects
2. **Intelligent Namespaces**: Organize code logically by domain/feature
3. **Smaller Classes**: Break up large classes into focused, single-responsibility classes
4. **Clear Boundaries**: Well-defined project boundaries and dependencies
5. **Better Organization**: Easier to navigate, understand, and maintain

## Current Structure

**Monolithic Project:**
```
CSMOO/
├── Commands/
├── Configuration/
├── Core/
├── Database/
├── Exceptions/
├── Functions/
├── Init/
├── Logging/
├── Network/
├── Object/
├── Scripting/
├── Sessions/
├── Verbs/
└── CSMOO.csproj (single project)
```

## Target Structure

**Domain-Specific Projects:**
```
CSMOO/
├── CSMOO.Core/              # Core domain models and interfaces
│   ├── Object/
│   ├── Exceptions/
│   └── Interfaces/
├── CSMOO.Database/          # Database layer
│   ├── Persistence/
│   ├── Repositories/
│   └── Migrations/
├── CSMOO.Scripting/         # Scripting system
│   ├── Compilation/
│   ├── Execution/
│   └── Preprocessing/
├── CSMOO.Network/           # Network layer
│   ├── Http/
│   ├── WebSocket/
│   └── Telnet/
├── CSMOO.Game/              # Game logic
│   ├── Commands/
│   ├── Verbs/
│   ├── Functions/
│   └── Sessions/
├── CSMOO.Infrastructure/    # Infrastructure concerns
│   ├── Configuration/
│   ├── Logging/
│   └── Initialization/
└── CSMOO.Server/            # Server entry point
    └── Program.cs
```

## Domain Breakdown

### CSMOO.Core

**Purpose:** Core domain models, interfaces, and shared abstractions

**Namespaces:**
```
CSMOO.Core.Object
CSMOO.Core.Exceptions
CSMOO.Core.Interfaces
CSMOO.Core.Abstractions
```

**Classes:**
- `GameObject`, `ObjectClass`, `Room`, `Player`, `Exit`, `Item`, `Container`
- Exception classes
- Core interfaces (`IObjectManager`, `IPlayerManager`, etc.)
- Shared abstractions

**Dependencies:** None (base project)

### CSMOO.Database

**Purpose:** Database persistence and data access

**Namespaces:**
```
CSMOO.Database.Persistence
CSMOO.Database.Repositories
CSMOO.Database.Managers
```

**Classes:**
- `GameDatabase`, `DbProvider`
- `ObjectManagerInstance`, `ClassManagerInstance`, `InstanceManagerInstance`
- `PropertyManagerInstance`, `RoomManagerInstance`
- Database-specific implementations

**Dependencies:** `CSMOO.Core`

### CSMOO.Scripting

**Purpose:** Script compilation, execution, and preprocessing

**Namespaces:**
```
CSMOO.Scripting.Compilation
CSMOO.Scripting.Execution
CSMOO.Scripting.Preprocessing
CSMOO.Scripting.Caching
```

**Classes:**
- `ScriptEngine`, `ScriptEngineFactory`
- `ScriptPrecompiler`, `CompilationInitializer`
- `CompilationCache`
- Script execution logic

**Dependencies:** `CSMOO.Core`, `CSMOO.Database`

### CSMOO.Network

**Purpose:** Network protocols and communication

**Namespaces:**
```
CSMOO.Network.Http
CSMOO.Network.WebSocket
CSMOO.Network.Telnet
CSMOO.Network.Protocols
```

**Classes:**
- `HttpServer`
- `WebSocketServer`, `WebSocketSession`
- `TelnetServer`
- Protocol handlers

**Dependencies:** `CSMOO.Core`, `CSMOO.Game`

### CSMOO.Game

**Purpose:** Game logic, commands, verbs, functions

**Namespaces:**
```
CSMOO.Game.Commands
CSMOO.Game.Verbs
CSMOO.Game.Functions
CSMOO.Game.Sessions
CSMOO.Game.Resolution
```

**Classes:**
- `CommandProcessor`, `ProgrammingCommands`, `ScriptCommands`
- `VerbResolver`, `VerbManager`, `VerbInitializer`
- `FunctionResolver`, `FunctionManager`, `FunctionInitializer`
- `SessionHandler`, session management
- `ObjectResolver`

**Dependencies:** `CSMOO.Core`, `CSMOO.Database`, `CSMOO.Scripting`

### CSMOO.Infrastructure

**Purpose:** Cross-cutting concerns and infrastructure

**Namespaces:**
```
CSMOO.Infrastructure.Configuration
CSMOO.Infrastructure.Logging
CSMOO.Infrastructure.Initialization
```

**Classes:**
- `Config`, configuration classes
- `Logger`, logging infrastructure
- `ServerInitializer`
- Hot reload managers

**Dependencies:** `CSMOO.Core`

### CSMOO.Server

**Purpose:** Application entry point and composition

**Namespaces:**
```
CSMOO.Server
```

**Classes:**
- `Program.cs` - Main entry point
- DI container setup
- Server startup/shutdown

**Dependencies:** All other projects

## Breaking Up Megalithic Classes

### Strategy

**Identify Large Classes:**
- Classes with 500+ lines
- Classes with many responsibilities
- Classes that violate Single Responsibility Principle

**Break Down By:**
1. **Feature/Functionality**: Split by what the class does
2. **Responsibility**: Each class has one clear responsibility
3. **Domain**: Group related functionality together
4. **Layer**: Separate concerns by architectural layer

### Example: Breaking Up Large Classes

**Before (Megalithic):**
```csharp
// ScriptPrecompiler.cs - 2000+ lines, does everything
public class ScriptPrecompiler
{
    // Variable injection
    // Method call rewriting
    // Type detection
    // Diagnostic conversion
    // Line offset calculation
    // ... many more responsibilities
}
```

**After (Focused Classes):**
```csharp
// CSMOO.Scripting.Preprocessing.VariableInjector.cs
public class VariableInjector
{
    public string InjectVariables(string script, ...);
    public int CalculateLineOffset(string script);
}

// CSMOO.Scripting.Preprocessing.MethodCallRewriter.cs
public class MethodCallRewriter
{
    public string RewriteMethodCalls(string code, ...);
}

// CSMOO.Scripting.Preprocessing.TypeDetector.cs
public class TypeDetector
{
    public bool IsGameObjectType(string identifier, ...);
    public HashSet<string> BuildTypeNameCache();
}

// CSMOO.Scripting.Preprocessing.DiagnosticConverter.cs
public class DiagnosticConverter
{
    public Diagnostic ConvertDiagnostic(Diagnostic diagnostic, int lineOffset);
}

// CSMOO.Scripting.Preprocessing.ScriptPrecompiler.cs (orchestrator)
public class ScriptPrecompiler
{
    private readonly VariableInjector _variableInjector;
    private readonly MethodCallRewriter _methodCallRewriter;
    private readonly TypeDetector _typeDetector;
    private readonly DiagnosticConverter _diagnosticConverter;
    
    public PrecompilationResult Precompile(string script, ...)
    {
        // Orchestrate the process
    }
}
```

## Namespace Organization Principles

### 1. Domain-First Organization

**Structure by domain, then by feature:**
```
CSMOO.Database.Managers.ObjectManager
CSMOO.Database.Managers.ClassManager
CSMOO.Database.Repositories.ObjectRepository
```

### 2. Feature Grouping

**Group related classes by feature:**
```
CSMOO.Scripting.Compilation.Initialization
CSMOO.Scripting.Compilation.Caching
CSMOO.Scripting.Execution.Engine
CSMOO.Scripting.Execution.Factory
```

### 3. Layer Separation

**Separate by architectural layer:**
```
CSMOO.Core.Object              # Domain models
CSMOO.Database.Persistence    # Data access
CSMOO.Game.Commands           # Application logic
```

### 4. Avoid Deep Nesting

**Keep namespaces shallow (2-3 levels max):**
```
✅ Good: CSMOO.Scripting.Preprocessing
❌ Bad: CSMOO.Scripting.Preprocessing.MethodCall.Rewriting
```

## Refactoring Process

### Step 1: Identify Domains

1. Analyze current codebase
2. Identify logical domains
3. Group related classes
4. Define project boundaries

### Step 2: Create Project Structure

1. Create new .csproj files for each domain
2. Set up project references
3. Create namespace structure
4. Move classes to appropriate projects

### Step 3: Break Up Large Classes

1. Identify megalithic classes
2. Analyze responsibilities
3. Extract focused classes
4. Update references

### Step 4: Organize Namespaces

1. Apply namespace organization principles
2. Update using statements
3. Ensure consistent naming
4. Verify no circular dependencies

### Step 5: Update Dependencies

1. Update project references
2. Fix broken references
3. Update DI registration
4. Test compilation

### Step 6: Verify and Test

1. Build all projects
2. Run tests
3. Verify functionality
4. Check for circular dependencies

## Project Reference Strategy

**Dependency Graph:**
```
CSMOO.Server
  ├── CSMOO.Game
  │     ├── CSMOO.Core
  │     ├── CSMOO.Database
  │     └── CSMOO.Scripting
  ├── CSMOO.Network
  │     ├── CSMOO.Core
  │     └── CSMOO.Game
  └── CSMOO.Infrastructure
        └── CSMOO.Core

CSMOO.Database
  └── CSMOO.Core

CSMOO.Scripting
  ├── CSMOO.Core
  └── CSMOO.Database
```

**Rules:**
- Core has no dependencies (base layer)
- Infrastructure depends only on Core
- Database depends on Core
- Scripting depends on Core and Database
- Game depends on Core, Database, Scripting
- Network depends on Core and Game
- Server depends on all (composition root)

## Example: Refactoring ScriptPrecompiler

### Current State

**Single large class:**
- `ScriptPrecompiler.cs` - 1000+ lines
- Handles variable injection, method rewriting, type detection, diagnostics, etc.

### Target State

**Focused classes in appropriate namespaces:**
```
CSMOO.Scripting.Preprocessing/
├── ScriptPrecompiler.cs (orchestrator, ~100 lines)
├── VariableInjector.cs (~200 lines)
├── MethodCallRewriter.cs (~300 lines)
├── TypeDetector.cs (~200 lines)
├── DiagnosticConverter.cs (~150 lines)
└── LineOffsetCalculator.cs (~100 lines)
```

**Benefits:**
- Each class has single responsibility
- Easier to test individually
- Easier to understand
- Easier to modify
- Better namespace organization

## Migration Strategy

### Incremental Approach

1. **Phase 1**: Create new project structure (empty projects)
2. **Phase 2**: Move classes to new projects (one domain at a time)
3. **Phase 3**: Break up large classes (one at a time)
4. **Phase 4**: Organize namespaces
5. **Phase 5**: Update all references

### Order of Migration

1. **CSMOO.Core** (no dependencies)
2. **CSMOO.Infrastructure** (depends on Core)
3. **CSMOO.Database** (depends on Core)
4. **CSMOO.Scripting** (depends on Core, Database)
5. **CSMOO.Game** (depends on Core, Database, Scripting)
6. **CSMOO.Network** (depends on Core, Game)
7. **CSMOO.Server** (depends on all)

## Benefits

### Maintainability
- Easier to find code (organized by domain)
- Easier to understand (smaller classes)
- Easier to modify (clear boundaries)

### Testability
- Smaller classes = easier to test
- Clear dependencies = easier to mock
- Domain separation = focused tests

### Scalability
- Easy to add new domains
- Easy to add new features
- Clear extension points

### Team Development
- Multiple developers can work on different domains
- Clear ownership boundaries
- Reduced merge conflicts

## Risks and Mitigation

### Risk: Breaking Changes
- **Mitigation**: Move incrementally, test frequently

### Risk: Circular Dependencies
- **Mitigation**: Careful dependency planning, use interfaces

### Risk: Large Refactoring Effort
- **Mitigation**: Do incrementally, one domain at a time

### Risk: Temporary Broken State
- **Mitigation**: Keep old structure until new is complete, then switch

## Success Criteria

- [ ] All code organized into domain-specific projects
- [ ] No classes over 500 lines (ideally under 300)
- [ ] Clear namespace organization (2-3 levels max)
- [ ] No circular dependencies
- [ ] All tests pass
- [ ] Build succeeds
- [ ] Code is easier to navigate and understand

## Tools and Techniques

### Analysis Tools
- **NDepend** (if available): Analyze class sizes, dependencies
- **Visual Studio**: Code metrics (lines of code, cyclomatic complexity)
- **Manual Review**: Identify large classes, analyze responsibilities

### Refactoring Tools
- **Visual Studio**: Extract Class, Move Type, etc.
- **ReSharper/Rider**: Advanced refactoring tools
- **Manual Refactoring**: For complex cases

## Notes

- This refactoring should happen **before** adding new features
- Do it incrementally to avoid breaking everything at once
- Keep old structure until new is complete (if needed)
- Test frequently during refactoring
- Update documentation as you go
