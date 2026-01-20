# Dead Code Analysis

This document identifies dead code, legacy patterns, and code that may be candidates for removal or refactoring.

## Confirmed Dead Code

### 1. `Examples/ObjectSystemExample.cs`

**Status**: ✅ **Confirmed Dead Code**  
**Priority**: Low  
**Reason**: Example/demo code, not used in production runtime

**Evidence**:
- Only reference is in its own class definition
- Not called from any production code
- Located in `Examples/` directory (typically for documentation/examples)
- Uses legacy static access patterns (`DbProvider.Instance`, `WorldManager`)

**Usage Analysis**:
- File defines `ObjectSystemExample.RunExample()` method
- No calls to `RunExample()` found in codebase
- Used for demonstrating object system features

**Recommendation**: 
- **Keep for now**: Useful as documentation/example code
- Could be moved to documentation or tests
- If removed, document the example use cases elsewhere

---

### 2. `Scripting/Command.g4` (ANTLR Grammar)

**Status**: ⚠️ **Potentially Dead Code**  
**Priority**: Medium  
**Reason**: ANTLR grammar file with no active parser implementation

**Evidence**:
- Grammar file defines command parsing rules
- No code generation from this grammar found
- No ANTLR parser usage detected in codebase
- Command parsing is currently done via simple string splitting in `CommandProcessor`

**Grammar Contents**:
```antlr
grammar Command;
command : loginCommand | createPlayerCommand | gameCommand | scriptCommand ;
// ... more rules
```

**Recommendation**:
- **Verify usage**: Check if this is planned for future use
- If not used, **remove**: Dead weight in codebase
- If planned, **document**: Add comment explaining future usage
- README mentions "Advanced ANTLR4 command parsing (planned feature)" - may be intended for future

---

## Legacy Wrapper Code

### 3. `Database/WorldManager.cs`

**Status**: ⚠️ **Legacy Wrapper**  
**Priority**: Low (for backward compatibility)  
**Reason**: Static wrapper that delegates to `WorldInitializer` and `RoomManager`

**Evidence**:
- All methods delegate to `WorldInitializer` or `RoomManager`
- Only used by:
  - `Examples/ObjectSystemExample.cs` (example code)
  - `Scripting/ScriptWorldManager.cs` (script wrapper)

**Code Pattern**:
```csharp
public static class WorldManager
{
    public static void InitializeWorld() => WorldInitializer.InitializeWorld();
    public static GameObject? GetStartingRoom() => RoomManager.GetStartingRoom();
    // ... all methods delegate
}
```

**Current Usage**:
- 4 references total:
  - 1 in `ObjectSystemExample.cs` (example)
  - 3 in `ScriptWorldManager.cs` (script wrapper)

**Recommendation**:
- **Keep for now**: Used by `ScriptWorldManager` which provides script-safe access
- **Migration path**: Update `ScriptWorldManager` to use `RoomManager`/`WorldInitializer` directly
- **Remove after**: Script wrapper updated and example code removed/updated

---

### 4. `Scripting/ScriptWorldManager.cs`

**Status**: ⚠️ **Wrapper Code**  
**Priority**: Low  
**Reason**: Script-safe wrapper that may be redundant

**Evidence**:
- Wraps `WorldManager` static methods
- Returns string IDs instead of GameObject references (script-safe)
- Only used in `ScriptGlobals` as `WorldManager` property

**Code Pattern**:
```csharp
public class ScriptWorldManager
{
    public string? GetStartingRoom() => WorldManager.GetStartingRoom()?.Id;
    // ... wraps WorldManager methods
}
```

**Recommendation**:
- **Keep for now**: Provides script-safe API (returns IDs instead of objects)
- **Future improvement**: Could be refactored to use DI services directly
- **Low priority**: Works correctly, just adds an indirection layer

---

## Legacy Initialization Methods

### 5. `Init/ServerInitializer.Initialize()` (parameterless overload)

**Status**: ⚠️ **Legacy Code Path**  
**Priority**: Medium  
**Reason**: Non-DI initialization method for backward compatibility

**Evidence**:
- Parameterless `Initialize()` uses static singletons
- New DI version `Initialize(IServiceProvider)` is used in production
- Parameterless version documented as "for backward compatibility"
- Only used if someone manually calls it (not in `Program.Main()`)

**Current Usage**:
- `Program.Main()` calls `Initialize(serviceProvider)` (DI version)
- Parameterless version not called in normal execution

**Recommendation**:
- **Remove after DI migration complete**: Once all static wrappers removed
- **Document deprecation**: Add `[Obsolete]` attribute with message
- **Migration**: All code should use DI version

---

### 6. `Init/ServerInitializer.SetObjectOwners()` (parameterless overload)

**Status**: ⚠️ **Legacy Code Path**  
**Priority**: Low  
**Reason**: Overload for backward compatibility, delegates to DI version

**Evidence**:
- Parameterless version delegates to `SetObjectOwners(adminPlayer, null)`
- Only called from legacy `CreateDefaultAdminIfNeeded()` method
- Will be removed when legacy initialization removed

**Recommendation**:
- **Remove with legacy initialization**: When parameterless `Initialize()` is removed

---

### 7. `Init/ServerInitializer.CreateDefaultAdminIfNeeded()` (parameterless overload)

**Status**: ⚠️ **Legacy Code Path**  
**Priority**: Low  
**Reason**: Overload for backward compatibility

**Evidence**:
- Parameterless version uses static access patterns
- DI version with parameters is used in production
- Only called from parameterless `Initialize()` method

**Recommendation**:
- **Remove with legacy initialization**: When parameterless `Initialize()` is removed

---

### 8. `Init/ServerInitializer.Shutdown()` (parameterless overload)

**Status**: ⚠️ **Legacy Code Path**  
**Priority**: Low  
**Reason**: Overload for backward compatibility, delegates to DI version

**Evidence**:
- Parameterless version delegates to `Shutdown(null)`
- Used as fallback in production code
- DI version preferred

**Recommendation**:
- **Keep for now**: Used as safe fallback
- **Future**: Ensure all code paths use DI version

---

## Static Instance Access Patterns

### 9. Remaining `.Instance` Static Access

**Status**: ⚠️ **Legacy Pattern** (11 occurrences)  
**Priority**: Medium  
**Reason**: Static singleton access during DI migration

**Files with `.Instance` access**:
1. `Init/ServerInitializer.cs` (2 occurrences) - legacy initialization
2. `Object/GameObject.cs` (1 occurrence) - data class limitation
3. `Examples/ObjectSystemExample.cs` (3 occurrences) - example code
4. `Commands/ProgrammingCommands.cs` (2 occurrences) - needs investigation
5. `Core/Builtins.cs` (1 occurrence) - needs investigation
6. `Database/GameDatabase.cs` (1 occurrence) - singleton pattern
7. `Commands/CommandProcessor.cs` (1 occurrence) - needs investigation

**Recommendation**:
- **Investigate each usage**: Determine if it's in legacy path or necessary
- **Priority locations**: `ProgrammingCommands.cs`, `Builtins.cs`, `CommandProcessor.cs`
- **Migration target**: Replace with DI-injected dependencies

---

## Exception Types Usage

All exception types in `Exceptions/` directory are **actively used**:

- ✅ `ContextException` - Used in `GameObject.cs` (1 occurrence)
- ✅ `FunctionExecutionException` - Used in `GameObject.cs` (2 occurrences)
- ✅ `NotFoundException` - Base exception, extended by others
- ✅ `ObjectStateException` - Used in `GameObject.cs` (1 occurrence)
- ✅ `PermissionException` - Used extensively in `GameObject.cs` (7 occurrences)
- ✅ `PropertyAccessException` - Used extensively in `GameObject.cs` (6 occurrences)
- ✅ `ReturnTypeException` - Defined, likely used in scripting
- ✅ `ScriptExecutionException` - Base exception for all script errors
- ⚠️ `PrivateAccessException` - Defined but not directly used (may be obsolete)

**Recommendation for PrivateAccessException**:
- **Verify**: Check if this is superseded by `PermissionException`
- **If unused**: Remove or consolidate with `PermissionException`

---

## Code Excluded from Compilation

### 10. `Resources/**/*.cs` Files

**Status**: ✅ **Intentionally Excluded**  
**Reason**: Compile exclusion in `CSMOO.csproj`

**Evidence**:
```xml
<ItemGroup>
  <Compile Remove="Resources/**/*.cs" />
</ItemGroup>
```

**Recommendation**:
- **Keep exclusion**: These are resource files, not compiled code
- Files in `Resources/` are content files (copy to output), not source code

---

## Duplicate Functionality

### 11. Static Wrapper Classes

**Status**: ⚠️ **Temporary Duplication**  
**Priority**: High (for cleanup after DI migration)  
**Reason**: Dual implementation during DI migration

**Pattern**: Most managers have both:
- Static wrapper class (e.g., `ObjectManager`)
- Instance implementation (e.g., `ObjectManagerInstance`)

**Examples**:
- `ObjectManager` / `ObjectManagerInstance`
- `PropertyManager` / `PropertyManagerInstance`
- `InstanceManager` / `InstanceManagerInstance`
- `VerbManager` / `VerbManagerInstance`
- And many more...

**Recommendation**:
- **Remove after DI migration complete**: Once all code uses DI
- **Track in DI_MIGRATION_STATUS.md**: Current migration status
- **Priority**: Clean up after all components migrated

---

## Summary by Priority

### High Priority (Clean up after DI migration)
1. Remove static wrapper classes (after DI migration complete)
2. Remove legacy initialization methods (after DI migration complete)

### Medium Priority (Investigate and resolve)
1. Remove or document `Command.g4` (ANTLR grammar - unused)
2. Investigate remaining `.Instance` static access in:
   - `Commands/ProgrammingCommands.cs`
   - `Core/Builtins.cs`
   - `Commands/CommandProcessor.cs`
3. Verify `PrivateAccessException` usage

### Low Priority (Keep for now, document)
1. `Examples/ObjectSystemExample.cs` - Useful as example
2. `Database/WorldManager.cs` - Used by script wrapper
3. `Scripting/ScriptWorldManager.cs` - Provides script-safe API

## Removal Checklist

Before removing any code:

- [ ] Verify no active references (use IDE search across entire codebase)
- [ ] Check git history for context
- [ ] Update related documentation
- [ ] Run full test suite
- [ ] Check for any reflection-based access
- [ ] Verify no external dependencies

## Notes

- Most "dead code" is actually legacy code kept for backward compatibility during DI migration
- Example code in `Examples/` directory is intentionally not compiled but kept for documentation
- Static wrappers are temporary during DI migration and will be removed in Phase 3 of migration plan
