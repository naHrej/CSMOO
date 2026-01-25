# Codebase Cleanup Checklist

## Overview

Before beginning the migration to graphical MMORPG, we need to clean up the codebase by removing all backward compatibility code, duplicate implementations, and wrappers that exist only for compatibility.

## Philosophy

**Clean Slate Approach:**
- Strip out anything that looks like backward compatibility
- Remove duplicate code
- Remove wrappers that just forward calls
- No "old way" vs "new way" - just the new way
- Modern patterns only (DI, proper architecture)

## Categories of Code to Remove

### 1. Backward Compatibility Constructors

**Pattern to Find:**
```csharp
// Old constructor for backward compatibility
public ClassName()
    : this(CreateDefaultX(), CreateDefaultY())
{
}

private static X CreateDefaultX() { ... }
```

**Action:** Remove these constructors. All code should use DI or proper constructors.

**Example Locations:**
- `HttpServer` - backward compatibility constructor
- Any class with "CreateDefault" helper methods for old constructors

### 2. Wrapper/Adapter Classes

**Pattern to Find:**
```csharp
// Wrapper that just forwards to new implementation
public class OldWrapper
{
    private NewImplementation _impl;
    public void Method() => _impl.Method(); // Just forwarding
}
```

**Action:** Remove wrapper, use implementation directly.

**Example Locations:**
- Old scripting engine wrappers
- Any adapter pattern that's just for compatibility

### 3. Dual Code Paths

**Pattern to Find:**
```csharp
// Code that checks for old vs new
if (UseOldSystem || _oldSystem != null)
{
    OldSystem.DoSomething();
}
else
{
    NewSystem.DoSomething();
}
```

**Action:** Remove old path, keep only new path.

**Example Locations:**
- Scripting engine (old vs new)
- Any feature that has been refactored but old code remains

### 4. Static Singleton Compatibility Shims

**Pattern to Find:**
```csharp
// Static instance for backward compatibility
public static class OldManager
{
    private static IManager _instance;
    public static IManager Instance 
    {
        get => _instance ?? throw new Exception("Not initialized");
        set => _instance = value;
    }
}
```

**Action:** Remove static singleton, ensure all code uses DI.

**Example Locations:**
- `ObjectManager.Instance`
- `DbProvider.Instance`
- `Logger.Instance`
- Any static "Instance" property that's a compatibility shim

### 5. Old Scripting Engine

**Specific Target:**
- Old scripting engine code
- Wrappers around new scripting system
- Compatibility layers for old script format
- Dual execution paths (old vs new)

**Action:** Complete removal. New scripting system only.

### 6. Duplicate Implementations

**Pattern to Find:**
- Same functionality in multiple places
- Two classes doing the same thing
- Redundant abstractions

**Action:** Consolidate to single implementation.

### 7. Unused Code

**Pattern to Find:**
- Dead code (never called)
- Commented-out code
- Unused methods/classes
- Obsolete attributes (if we're not maintaining compatibility)

**Action:** Delete it.

## Search Patterns

### Find Backward Compatibility Code

```bash
# Search for common patterns
grep -r "backward\|compat\|legacy\|deprecated\|obsolete" --include="*.cs"
grep -r "CreateDefault" --include="*.cs"
grep -r "Instance\s*=" --include="*.cs"  # Static Instance setters
grep -r "UseOld\|OldSystem\|OldEngine" --include="*.cs"
```

### Find Wrapper Classes

```bash
# Look for classes that just forward calls
grep -r "=> _.*\." --include="*.cs"  # Lambda forwarding
grep -r "class.*Wrapper\|class.*Adapter" --include="*.cs"
```

### Find Duplicate Code

```bash
# Use tools like:
# - Visual Studio Code Clone Detection
# - NDepend (if available)
# - Manual code review
```

## Cleanup Process

### Step 1: Identify

1. Run search patterns above
2. Review codebase for patterns
3. Create list of files/classes to clean up
4. Prioritize (high impact first)

### Step 2: Analyze Dependencies

1. For each item to remove:
   - Find all references
   - Check if anything still uses it
   - If used, update callers first

### Step 3: Remove

1. Update all callers to use new implementation
2. Remove old code
3. Test to ensure nothing breaks
4. Commit changes

### Step 4: Verify

1. Build succeeds
2. Tests pass (if applicable)
3. No references to removed code
4. Codebase is cleaner

## Specific Targets

### Old Scripting Engine

**Files to Review:**
- Any files with "Old" or "Legacy" in name
- Scripting engine files that have dual paths
- Wrappers around `ScriptEngine` or `ScriptPrecompiler`
- Old execution paths in scripting system

**Action:**
- Remove old execution paths
- Remove compatibility layers
- Remove wrappers that just forward to new system
- Keep only new scripting system (Roslyn-based)

### Static Singleton Wrappers

**Files to Review:**
- `ObjectManager.cs` - static `Instance` property
- `DbProvider.cs` - static `Instance` property
- `Logger.cs` - static `Instance` property
- `RoomManager.cs` - static `Instance` property
- `PermissionManager.cs` - static `Instance` property
- `VerbResolver.cs` - static `Instance` property
- `ObjectResolver.cs` - static `Instance` property
- `GameDatabase.cs` - static `Instance` property
- `SessionHandler.cs` - static `Instance` property
- Any other static `Instance` properties

**Action:**
- Remove static `Instance` properties
- Remove `SetInstance` methods
- Ensure all code uses DI (already done in `Program.cs`)
- Update any remaining static access to use DI

**Reference:** See `docs/architecture/DEAD_CODE.md` for detailed analysis of static wrappers.

### Backward Compatibility Constructors

**Files to Review:**
- `HttpServer.cs` - backward compatibility constructor
- `Network/TelnetServer.cs` - check for compatibility constructors
- `Network/WebSocketServer.cs` - check for compatibility constructors
- Any class with parameterless constructor that calls `CreateDefault*`

**Action:**
- Remove parameterless constructors
- Remove `CreateDefault*` helper methods
- Update all callers to use proper constructors (DI)

### Legacy Initialization

**Files to Review:**
- `Init/ServerInitializer.cs` - parameterless `Initialize()` method
- `Init/ServerInitializer.cs` - parameterless `Shutdown()` method
- `Init/ServerInitializer.cs` - `CreateDefaultAdminIfNeeded()` parameterless overload
- `Init/ServerInitializer.cs` - `SetObjectOwners()` parameterless overload

**Action:**
- Remove parameterless overloads
- All code should use DI version with `IServiceProvider`
- `Program.Main()` already uses DI version - remove legacy paths

**Reference:** See `docs/architecture/DEAD_CODE.md` for detailed analysis.

### Wrapper Classes

**Files to Review:**
- `Database/WorldManager.cs` - static wrapper (delegates to `WorldInitializer`/`RoomManager`)
- `Scripting/ScriptWorldManager.cs` - script wrapper (may be needed, review)
- Any adapter/wrapper that just forwards calls

**Action:**
- Remove wrappers that just forward
- Update callers to use implementation directly
- Keep only if provides real value (e.g., script-safe API)

**Reference:** See `docs/architecture/DEAD_CODE.md` for detailed analysis.

### Static Singletons

**Files to Review:**
- `ObjectManager.cs` - `Instance` property
- `DbProvider.cs` - `Instance` property
- `Logger.cs` - `Instance` property
- Any class with static `Instance` property

**Action:**
- Ensure all code uses DI
- Remove static `Instance` properties
- Remove `SetInstance` methods

### Backward Compatibility Constructors

**Files to Review:**
- `HttpServer.cs` - backward compatibility constructor
- Any class with parameterless constructor that calls `CreateDefault*`

**Action:**
- Remove parameterless constructors
- Remove `CreateDefault*` helper methods
- Update all callers to use proper constructors

## Testing After Cleanup

1. **Build Test:**
   - Ensure project compiles
   - No missing references

2. **Runtime Test:**
   - Server starts
   - Basic functionality works
   - No errors from removed code

3. **Integration Test:**
   - Scripts compile and run
   - Objects can be created
   - Database operations work
   - Network connections work

## Benefits

**After Cleanup:**
- Cleaner codebase
- Easier to understand
- Less maintenance burden
- No confusion about which code path to use
- Modern patterns throughout
- Easier migration to new architecture

## Risks

**Potential Issues:**
- May break some code that still uses old patterns
- Need to update all callers
- May reveal hidden dependencies

**Mitigation:**
- Thorough testing after each removal
- Update callers before removing
- Keep commits small and focused
- Test frequently

## Timeline

**Estimated:** 1-2 weeks

- Week 1: Identify and analyze
- Week 2: Remove and test

## Related Documentation

- **[DEAD_CODE.md](../architecture/DEAD_CODE.md)** - Existing analysis of dead code and legacy patterns
- **[DI_MIGRATION_STATUS.md](../../DI_MIGRATION_STATUS.md)** - Current DI migration status
- **[DEPENDENCY_INJECTION.md](../architecture/DEPENDENCY_INJECTION.md)** - DI architecture documentation

## Notes

- This is a one-time cleanup before migration
- Don't add new backward compatibility code
- If something breaks, fix it properly (don't add compatibility shims)
- Clean codebase = easier migration
- Reference existing `DEAD_CODE.md` for detailed analysis of what to remove
- Most cleanup targets are already identified in `DEAD_CODE.md`
