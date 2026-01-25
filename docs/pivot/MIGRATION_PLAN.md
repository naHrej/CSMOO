# Migration Plan

## Overview

This document outlines the phased approach to migrating CSMOO from a text-based MU* to a graphical MMORPG.

## Timeline

**Estimated Total**: 7-10 weeks to working prototype (includes cleanup)

## Phase 0: Codebase Cleanup and Refactoring (2-3 weeks)

**MUST BE DONE FIRST** - Clean up and refactor before adding new features.

See [CLEANUP_CHECKLIST.md](./CLEANUP_CHECKLIST.md) for detailed cleanup process.
See [REFACTORING_STRATEGY.md](./REFACTORING_STRATEGY.md) for domain-specific project structure.

### Part A: Cleanup (1-2 weeks)

**Key Tasks:**
- Remove all backward compatibility code
- Remove old scripting engine and wrappers
- Remove duplicate implementations
- Remove static singleton compatibility shims
- Clean up wrapper classes

**Deliverables:**
- Clean codebase with no compatibility code
- All code uses modern patterns (DI, etc.)
- Single code paths (no old vs new)

### Part B: Refactoring (1 week)

**Key Tasks:**
- Break codebase into domain-specific projects
- Organize namespaces intelligently
- Split megalithic classes into smaller, focused classes
- Group related functionality by domain
- Create proper project boundaries

**Target Projects:**
- `CSMOO.Core` - Core domain models and interfaces
- `CSMOO.Database` - Database layer
- `CSMOO.Scripting` - Scripting system
- `CSMOO.Network` - Network layer
- `CSMOO.Game` - Game logic (commands, verbs, functions)
- `CSMOO.Infrastructure` - Infrastructure concerns
- `CSMOO.Server` - Server entry point

**Large Classes to Break Up:**
- `CommandProcessor.cs` (1273 lines) - Extract command handlers
- `ProgrammingCommands.cs` (2608 lines) - Split by command type
- `Builtins.cs` (1782 lines) - Split by functionality
- `ScriptEngine.cs` (1677 lines) - Extract execution, compilation, etc.
- `ScriptPrecompiler.cs` (1274 lines) - Extract preprocessing components
- `ScriptGlobals.cs` (795 lines) - Split by concern

**System Unification:**
- Consolidate verbs and functions into single script processor
- Verbs = Functions + Command Hooks
- Remove `IVerbResolver` and `IFunctionResolver`
- Create unified `IScriptResolver` and `IScriptProcessor`
- See [SCRIPT_PROCESSOR_UNIFICATION.md](./SCRIPT_PROCESSOR_UNIFICATION.md) for details

**Database Abstraction:**
- Create database abstraction layer (`IDatabase`, `ICollection<T>`)
- Make database swappable (LiteDB → PostgreSQL)
- Remove direct database dependencies from application code
- Enable easy migration path
- See [DATABASE_ABSTRACTION.md](./DATABASE_ABSTRACTION.md) for details

**Deliverables:**
- Domain-specific projects with clear boundaries
- Well-organized namespaces (2-3 levels max)
- Smaller, focused classes (ideally <300 lines)
- No circular dependencies
- Better separation of concerns

## Phase 1: Scene Data Structure (1 week)

### Goals
- Add 3D data properties to GameObject
- Create helper methods for position/rotation/scale
- Update ObjectClass to support 3D properties

### Tasks

1. **Extend GameObject Properties**
   - Add `modelPath` property support
   - Add `position`, `rotation`, `scale` properties
   - Add `material` property structure
   - Add `texture` property structure
   - Add `lighting` property for rooms
   - Add `uiStyle` property for UI styling

2. **Create Helper Methods**
   - `GetPosition(GameObject)` → `Vector3`
   - `SetPosition(GameObject, Vector3)`
   - `GetRotation(GameObject)` → `Quaternion` or `Vector3` (Euler)
   - `SetRotation(GameObject, Quaternion)`
   - `GetScale(GameObject)` → `Vector3`
   - `SetScale(GameObject, Vector3)`
   - `GetMaterial(GameObject)` → `MaterialData`
   - `SetMaterial(GameObject, MaterialData)`

3. **Update ObjectClass**
   - Support 3D properties in class defaults
   - Inheritance of 3D properties

4. **Create Data Models**
   - `Vector3` class (or use existing)
   - `Quaternion` class (or use existing)
   - `MaterialData` class
   - `TextureDefinition` class
   - `LightingData` class

### Deliverables
- GameObject can store 3D properties
- Helper methods available
- Data models defined

## Phase 2: API Endpoints (1 week)

### Goals
- Add scene description API endpoint
- Add texture generation API endpoint
- Add asset serving for STL files
- Extend WebSocket protocol

### Tasks

1. **Scene API Endpoint**
   - `GET /api/scene/{roomId}`
   - Serialize GameObject to scene JSON
   - Include all objects in room
   - Include lighting data
   - Include UI styling

2. **Texture API Endpoint**
   - `GET /api/texture/{objectId}?format=png&size=512`
   - Read texture definition from GameObject
   - Generate texture (basic implementation)
   - Return image bytes
   - Support format negotiation
   - Implement caching

3. **Asset Serving**
   - Serve `/assets/models/*.stl` files
   - Set proper MIME types
   - Add CORS headers
   - Handle 404 errors

4. **WebSocket Protocol Extension**
   - Add `objectMoved` message type
   - Add `objectAdded` message type
   - Add `objectRemoved` message type
   - Add `objectChanged` message type
   - Add `animation` message type
   - Add `uiUpdate` message type

5. **Action API**
   - `POST /api/action` or WebSocket message
   - Execute verb/function
   - Return result and scene updates

### Deliverables
- Scene API working
- Texture API working (basic)
- Asset serving working
- WebSocket protocol extended

## Phase 3: Client Prototype (2 weeks)

### Goals
- Basic HTML5 client
- Three.js integration
- WebSocket connection
- Model loading from server
- Basic rendering

### Tasks

1. **Client Setup**
   - Create HTML5 page structure
   - Set up Three.js scene
   - Configure WebGL renderer
   - Set up camera and controls

2. **WebSocket Connection**
   - Connect to `ws://server:1702/api`
   - Handle connection/disconnection
   - Implement reconnection logic

3. **Scene Loading**
   - Request scene from `/api/scene/{roomId}`
   - Parse scene JSON
   - Load STL models using STLLoader
   - Apply materials from properties
   - Add objects to scene

4. **Texture Loading**
   - Request textures from `/api/texture/{objectId}`
   - Apply textures to materials
   - Handle loading states

5. **Basic Rendering**
   - Render scene
   - Set up lighting
   - Handle camera movement
   - Basic input (mouse look, WASD)

6. **WebSocket Updates**
   - Listen for `objectMoved` messages
   - Update object positions
   - Handle `objectAdded` / `objectRemoved`
   - Basic animation

### Deliverables
- Working client prototype
- Can load and render a scene
- Can receive real-time updates
- Basic input handling

## Phase 4: Full Integration (2 weeks)

### Goals
- Complete texture generation
- Full WebSocket protocol
- Input handling and action execution
- UI overlay
- Polish and optimization

### Tasks

1. **Texture Generation**
   - Implement all generator types:
     - Material-based
     - Pattern-based
     - Noise-based
     - Text-based
     - Composite
   - Choose library (SkiaSharp or ImageSharp)
   - Implement noise library integration
   - Optimize generation performance
   - Improve caching

2. **Input Handling**
   - Mouse interactions (click objects)
   - Keyboard input (movement, actions)
   - Send actions to server
   - Handle server responses
   - Update scene based on responses

3. **UI Overlay**
   - HTML/CSS overlay
   - Apply server-defined styles
   - Status bar
   - Inventory panel
   - Chat window
   - Update via WebSocket messages

4. **Animation System**
   - Interpolate position changes
   - Handle animation events
   - Smooth transitions

5. **Optimization**
   - Model caching
   - Texture caching
   - LOD (Level of Detail)
   - Frustum culling
   - Object pooling

6. **Error Handling**
   - Connection errors
   - Asset loading errors
   - Server errors
   - User-friendly error messages

7. **Testing**
   - Test with multiple objects
   - Test with multiple players
   - Test texture generation
   - Test WebSocket updates
   - Performance testing

### Deliverables
- Complete client
- Full texture generation
- Working multiplayer
- Polished UI
- Optimized performance

## Phase 0: Codebase Cleanup (Before Phase 1)

### Goals
- Remove all backward compatibility code
- Clean up duplicate code
- Remove wrappers that exist only for compatibility
- Strip out old systems that are no longer needed

### Tasks

1. **Identify Backward Compatibility Code**
   - Search for "backward", "compat", "legacy", "deprecated", "obsolete"
   - Find wrapper classes that just forward to new implementations
   - Identify duplicate code paths
   - Find old scripting engine remnants

2. **Remove Old Scripting Engine**
   - Old scripting engine is a prime example of code to remove
   - Remove any wrappers around new scripting system
   - Clean up any dual-code paths (old vs new)
   - Remove compatibility layers

3. **Remove Wrapper Classes**
   - Find adapter/wrapper patterns that exist only for compatibility
   - Remove if they're just forwarding calls
   - Consolidate to single implementation

4. **Remove Duplicate Code**
   - Find code that does the same thing in multiple places
   - Consolidate to single implementation
   - Remove redundant abstractions

5. **Clean Up Static Singletons**
   - Remove static singleton patterns if DI is in place
   - Clean up any "Instance" static properties that are compatibility shims
   - Ensure all code uses DI properly

6. **Remove Unused Code**
   - Dead code that's no longer called
   - Commented-out code
   - Unused methods/classes

### Examples of What to Remove

**Backward Compatibility Constructors:**
```csharp
// REMOVE: Old constructor kept for compatibility
public HttpServer()
    : this(CreateDefaultConfig(), CreateDefaultLogger(), CreateDefaultObjectManager())
{
}
```

**Wrapper Classes:**
```csharp
// REMOVE: Wrapper that just forwards to new implementation
public class OldScriptEngine
{
    private NewScriptEngine _engine;
    public void Execute(string code) => _engine.Execute(code); // Just forwarding
}
```

**Dual Code Paths:**
```csharp
// REMOVE: Code that checks for old vs new system
if (UseOldSystem)
{
    OldSystem.DoSomething();
}
else
{
    NewSystem.DoSomething();
}
```

**Static Compatibility Shims:**
```csharp
// REMOVE: Static instance that's just for backward compatibility
public static class OldManager
{
    private static IManager _instance;
    public static IManager Instance => _instance ?? throw new Exception("Not initialized");
}
```

### Deliverables
- Clean codebase with no backward compatibility code
- No duplicate implementations
- No wrapper classes for compatibility
- Single code paths (no old vs new)
- All code uses modern patterns (DI, etc.)

### Estimated Time
- 1-2 weeks (depending on codebase size)

## Post-MVP Enhancements

### Future Phases (Not in initial 6-8 weeks)

1. **Advanced Features**
   - Normal maps
   - Shadow mapping
   - Post-processing effects
   - Particle systems
   - Audio integration

2. **Performance**
   - WebGL optimization (if web client)
   - Network optimization
   - Asset compression
   - CDN integration

3. **Features**
   - Inventory system UI
   - Character customization
   - Chat system
   - Social features
   - Quest system

4. **Tools**
   - World editor
   - Model preview tool
   - Texture preview tool
   - Debug tools

## Risk Mitigation

### Potential Risks

1. **Texture Generation Performance**
   - **Risk**: Generation too slow
   - **Mitigation**: Aggressive caching, async generation, pre-generation

2. **WebGL Compatibility**
   - **Risk**: Browser compatibility issues
   - **Mitigation**: Feature detection, fallbacks, polyfills

3. **Network Latency**
   - **Risk**: Laggy updates
   - **Mitigation**: Client-side prediction, interpolation, delta compression

4. **Asset Loading**
   - **Risk**: Slow initial load
   - **Mitigation**: Progressive loading, LOD, compression, CDN

5. **Scope Creep**
   - **Risk**: Trying to do too much
   - **Mitigation**: Strict MVP definition, phased approach

## Success Criteria

### MVP Success Criteria

- [ ] Client can load and render a scene with multiple objects
- [ ] Objects can be positioned, rotated, scaled
- [ ] Materials and textures are applied correctly
- [ ] Real-time updates work (object movement)
- [ ] User can interact with objects (click, execute verbs)
- [ ] Texture generation works for all generator types
- [ ] WebSocket communication is stable
- [ ] Performance is acceptable (30+ FPS)

## Dependencies

### External Libraries

- **Three.js**: 3D rendering (client)
- **STLLoader**: STL model loading (client)
- **SkiaSharp or ImageSharp**: Texture generation (server)
- **Noise Library**: Procedural noise (server)

### Internal Dependencies

- Existing GameObject system
- Existing property system
- Existing WebSocket infrastructure
- Existing verb/function system

## Notes

- Keep existing text-based functionality during migration (if desired)
- Can run both clients simultaneously
- Gradual migration possible
- Backward compatibility not required (clean break)
