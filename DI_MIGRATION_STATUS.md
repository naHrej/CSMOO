# Dependency Injection Migration Status

## ✅ Converted Components (39)

All of these have been converted to DI with backward-compatible static wrappers:

1. `IConfig` / `Config`
2. `ILogger` / `LoggerInstance`
3. `IGameDatabase` / `GameDatabase`
4. `IDbProvider` / `DbProvider`
5. `IPlayerManager` / `PlayerManager`
6. `IObjectManager` / `ObjectManager`
7. `IClassManager` / `ClassManager`
8. `IInstanceManager` / `InstanceManager`
9. `IPropertyManager` / `PropertyManager`
10. `IRoomManager` / `RoomManager`
11. `IWorldInitializer` / `WorldInitializer`
12. `IPermissionManager` / `PermissionManager`
13. `ICoreClassFactory` / `CoreClassFactory`
14. `IVerbInitializer` / `VerbInitializer`
15. `IFunctionInitializer` / `FunctionInitializer`
16. `IPropertyInitializer` / `PropertyInitializer`
17. `IHotReloadManager` / `HotReloadManager`
18. `ICoreHotReloadManager` / `CoreHotReloadManager`
19. `IVerbManager` / `VerbManager`
20. `IFunctionManager` / `FunctionManager`
21. `IVerbResolver` / `VerbResolver`
22. `IObjectResolver` / `ObjectResolver`
23. `IFunctionResolver` / `FunctionResolver`
24. `IScriptEngineFactory` / `ScriptEngineFactory`
25. `ISessionHandler` / `SessionHandler`
26. `Commands/CommandProcessor` - Now accepts DI dependencies
27. `Commands/ProgrammingCommands` - Now accepts DI dependencies
28. `Network/HttpServer` - Now accepts DI dependencies
29. `Scripting/ScriptHelpers` - Now accepts DI dependencies
30. `Scripting/ScriptEngine` - Now accepts `IObjectManager`, `ILogger`, `IConfig`, `IObjectResolver` via constructor
31. `Scripting/ScriptObject` - Now accepts `IObjectManager`, `IFunctionResolver`, `IDbProvider` via constructor
32. `Scripting/ScriptObjectManager` - Now accepts `IObjectManager` via constructor
33. `Scripting/ScriptObjectFactory` - Now accepts `IObjectManager`, `IFunctionResolver`, `IDbProvider` via constructor
34. `Scripting/ScriptPlayerManager` - Now accepts `IPlayerManager`, `IObjectManager` via constructor
35. `Scripting/IScriptEngineFactory` / `ScriptEngineFactory` - Updated to inject dependencies into ScriptEngine
36. `Scripting/ScriptGlobals` - Now accepts `IObjectManager`, `IVerbResolver`, `IFunctionResolver` via constructor
37. `Scripting/AdminScriptGlobals` - Updated to accept DI dependencies via constructor
38. `Core/Builtins.cs` - Now uses DI via `IBuiltinsInstance` / `BuiltinsInstance` with static wrapper for backward compatibility
39. `Core/IBuiltinsInstance.cs` - Interface for Builtins instance
40. `Core/BuiltinsInstance.cs` - Instance implementation with DI dependencies
41. `Scripting/ScriptEngineFactoryStatic` - Static wrapper for ScriptEngineFactory with DI support
42. `Object/GameObject.cs` - Updated to use `ScriptEngineFactoryStatic` instead of creating ScriptEngine directly

## ❌ Components Still Using Static Access

These components need to be converted to use DI before we can remove backward compatibility:

### High Priority (Core Runtime Components)

1. ~~**`Scripting/ScriptEngine.cs`**~~ ✅ **CONVERTED**
   - ~~Uses: `ObjectManager`, `Logger`, `Config`, `ObjectResolver`~~
   - **Status**: Now accepts `IObjectManager`, `ILogger`, `IConfig`, `IObjectResolver` via constructor, maintains backward compatibility

2. ~~**`Scripting/ScriptGlobals.cs`**~~ ✅ **CONVERTED**
   - ~~Uses: `ObjectManager`, `VerbResolver`, `FunctionResolver`~~
   - **Status**: Now accepts `IObjectManager`, `IVerbResolver`, `IFunctionResolver` via constructor, maintains backward compatibility

3. ~~**`Scripting/ScriptHelpers.cs`**~~ ✅ **CONVERTED**
   - ~~Uses: `ObjectManager`, `PlayerManager`~~
   - **Status**: Now accepts `IObjectManager`, `IPlayerManager`, `IDbProvider`, `ILogger`, `IVerbManager`, and `IRoomManager` via constructor, maintains backward compatibility

4. ~~**`Scripting/ScriptObject.cs`**~~ ✅ **CONVERTED**
   - ~~Uses: `ObjectManager`, `FunctionResolver`~~
   - **Status**: Now accepts `IObjectManager`, `IFunctionResolver`, `IDbProvider` via constructor, maintains backward compatibility

5. ~~**`Scripting/ScriptObjectManager.cs`**~~ ✅ **CONVERTED**
   - ~~Uses: `ObjectManager`~~
   - **Status**: Now accepts `IObjectManager` via constructor, maintains backward compatibility

6. ~~**`Scripting/ScriptObjectFactory.cs`**~~ ✅ **CONVERTED**
   - ~~Uses: `ObjectManager`~~
   - **Status**: Now accepts `IObjectManager`, `IFunctionResolver`, `IDbProvider` via constructor, maintains backward compatibility

7. ~~**`Scripting/ScriptPlayerManager.cs`**~~ ✅ **CONVERTED**
   - ~~Uses: `PlayerManager`, `ObjectManager`~~
   - **Status**: Now accepts `IPlayerManager`, `IObjectManager` via constructor, maintains backward compatibility

8. ~~**`Core/Builtins.cs`**~~ ✅ **CONVERTED**
    - ~~Uses: `ObjectManager`, `PlayerManager`, `PermissionManager`, `FunctionResolver`, `VerbManager`, `RoomManager`~~
    - **Status**: Now uses `IBuiltinsInstance` / `BuiltinsInstance` with static wrapper for backward compatibility. All static calls replaced with instance calls.

### Medium Priority (Supporting Components)

1. ~~**`Object/GameObject.cs`**~~ ✅ **ACCEPTABLE AS-IS**
    - Uses: `ObjectManager`, `ObjectResolver`, `FunctionResolver`, `ScriptEngineFactoryStatic`
    - **Status**: GameObject is a serialized data class, so it can't have dependencies injected. However, it uses static wrappers (`ObjectManager`, `FunctionResolver`, `ObjectResolver`, `ScriptEngineFactoryStatic`) that already delegate to DI instances when set via `SetInstance()`. Updated to use `ScriptEngineFactoryStatic` instead of creating `ScriptEngine` directly.

2. ~~**`Object/ObjectClass.cs`**~~ ✅ **ACCEPTABLE AS-IS**
    - Uses: `ObjectManager`
    - **Status**: ObjectClass is a serialized data class, so it can't have dependencies injected. However, it uses the static wrapper `ObjectManager` which already delegates to DI instances when set via `SetInstance()`. This is acceptable for data classes.

### Low Priority (Utility Classes)

1. **`Core/Html.cs`** - Pure utility, no dependencies
2. **`Scripting/ScriptPreprocessor.cs`** - Pure utility, no dependencies
3. **`Scripting/ScriptStackTrace.cs`** - Utility with ThreadLocal storage

## Migration Strategy

### Phase 1: Convert High Priority Components
1. ✅ Convert `CommandProcessor` to accept DI dependencies
2. ✅ Convert `ProgrammingCommands` to accept DI dependencies
3. ✅ Convert `ScriptEngine` to accept DI dependencies
4. ✅ Convert `ScriptObject`, `ScriptObjectManager`, `ScriptObjectFactory`, `ScriptPlayerManager` to accept DI
5. ✅ Convert `ScriptGlobals` to accept DI dependencies
6. ✅ Convert `Builtins` to instance class with DI dependencies

### Phase 2: Convert Medium Priority Components
1. ✅ Convert `SessionHandler` to accept DI
2. ✅ Convert `HttpServer` to accept DI
3. ✅ Handle `GameObject` and `ObjectClass` - Updated to use static wrappers that support DI (acceptable for data classes)

### Phase 3: Remove Backward Compatibility
9. Remove all `EnsureInstance()` methods from static wrappers
10. Remove static wrapper classes entirely
11. Update all remaining references to use DI

## Key Challenges

1. **Scripting System**: Scripts execute in a sandbox and need access to managers. We'll need to inject dependencies into `ScriptEngine` and pass them through to script globals.

2. ~~**GameObject/ObjectClass**: These are data classes that are serialized. We can't easily inject dependencies into them. Options:
   - Use a service locator pattern
   - Pass dependencies through method calls
   - Use a static service locator that gets the DI container~~ ✅ **RESOLVED** - These classes use static wrappers that delegate to DI instances. This is acceptable for serialized data classes.

3. ~~**Builtins**: This is a large static utility class used extensively in scripts. Converting it will require careful refactoring.~~ ✅ **COMPLETED** - Now uses `IBuiltinsInstance` with static wrapper for backward compatibility.

4. ~~**CommandProcessor**: Created per connection, needs access to DI container. Should be created via DI factory.~~ ✅ **COMPLETED**

## Estimated Remaining Work

- **High Priority**: ✅ **ALL COMPLETE!**
- **Medium Priority**: ✅ **ALL COMPLETE!** (GameObject and ObjectClass use static wrappers that support DI - acceptable for data classes)
- **Low Priority**: 3 components (may not need conversion - pure utilities)

**Total**: All critical components are now DI-compatible! The remaining low-priority utilities may not need conversion as they have no dependencies.
