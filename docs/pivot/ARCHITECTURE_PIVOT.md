# Architecture Pivot: From Text MU* to Graphical MMORPG

## Vision

Transform CSMOO from a text-based Multi-User Shared Object-Oriented Environment (LambdaMOO-style) into a graphical MMORPG while maintaining the core server-first, code-driven architecture.

## Core Principles

### 1. Server-First Architecture
- **Server is authoritative**: All game logic, state, and validation happens on the server
- **Client is a viewer**: The client renders what the server tells it to render
- **No client-side game logic**: Client only handles rendering and input forwarding

### 2. World Describable in Code
- **Everything defined in code**: Objects, models, textures, materials, UI styling - all defined in GameObject properties and scripts
- **No asset management**: Models and textures are either served by the server or generated procedurally
- **Hot-reloadable**: Change properties in code → server updates → client reflects changes

### 3. Zero Client Assets
- **No bundled assets**: Client contains zero game assets (models, textures, sounds)
- **Everything served by server**: Models, textures, and other assets are served via HTTP
- **Dynamic loading**: Client loads assets on-demand based on server scene descriptions

### 4. Client as Pure Viewer
- **HTML5 + CSS + JavaScript**: Modern web technologies
- **WebGL rendering**: Three.js or similar for 3D rendering
- **Input forwarding**: User interactions sent to server, server responds with updates

## What We're Keeping

### Core Systems (Highly Reusable)

1. **Object System** (~80% reusable)
   - `GameObject` / `ObjectClass` with inheritance and properties
   - Flexible property system (BsonDocument)
   - Location/spatial relationships
   - Works for any game type

2. **Database Layer** (~95% reusable)
   - LiteDB persistence
   - Flexible schema
   - Object/class storage
   - No changes needed

3. **Scripting System** (~90% reusable)
   - C# Roslyn compilation
   - Hot-reload support
   - Function/verb execution
   - Needs API exposure, not replacement

4. **Network Infrastructure** (~70% reusable)
   - WebSocket server already present
   - JSON API channel (`/api`)
   - Session management
   - Can drop Telnet; WebSocket is sufficient

5. **Server Architecture** (~85% reusable)
   - Dependency Injection container
   - Service layer separation
   - Logging, configuration
   - Solid foundation

## What We're Changing

### Major Changes

1. **Command Processing** (~30% reusable)
   - **From**: Text parsing ("go north", "look", "examine")
   - **To**: Structured JSON API calls
   - **Keep**: Verb/function execution logic
   - **Change**: Input/output format

2. **Output Formatting** (~10% reusable)
   - **From**: Text descriptions ("You see a room...")
   - **To**: Structured scene data (models, positions, materials, lighting)
   - **From**: Room descriptions
   - **To**: Scene graphs with model references

3. **Client Communication** (~50% reusable)
   - **Extend**: WebSocket JSON channel for real-time updates
   - **Add**: Scene sync, model references, position updates, animation events
   - **Add**: HTTP REST API for scene descriptions

### New Components

1. **Scene Description API**
   - `GET /api/scene/{roomId}` - Complete scene with all objects
   - Returns JSON with models, positions, materials, lighting

2. **Asset Serving**
   - `/assets/models/*.stl` - STL model files
   - `/assets/textures/*` - Generated textures (on-demand)
   - Static file serving via HTTP

3. **Texture Generation Service**
   - Procedural texture generation
   - On-demand generation based on GameObject properties
   - Format negotiation (WebP, PNG, JPEG)
   - Caching by parameters

4. **3D Data Properties**
   - Add to GameObject.Properties:
     - `modelPath` - Path to STL file
     - `position` - {x, y, z}
     - `rotation` - Quaternion {x, y, z, w}
     - `scale` - {x, y, z}
     - `material` - Material properties
     - `texture` - Texture generation definition
     - `uiStyle` - CSS styling for UI overlay
     - `lighting` - Scene lighting data

## Architecture Benefits

1. **Fully Code-Driven**: World defined in scripts, no manual asset placement
2. **Hot-Reloadable**: Change properties → regenerate → client updates
3. **Flexible**: Add new properties without client changes
4. **Server-Authoritative**: All logic on server, secure and consistent
5. **No Asset Management**: Models served by server, textures generated
6. **Procedural**: Infinite variations possible through code

## Migration Strategy

See [MIGRATION_PLAN.md](./MIGRATION_PLAN.md) for detailed implementation phases.

**Estimated Timeline**: 6-8 weeks to working prototype

## Related Documentation

- [CLIENT_ARCHITECTURE.md](./CLIENT_ARCHITECTURE.md) - Client implementation details
- [MODEL_FORMAT.md](./MODEL_FORMAT.md) - STL format decisions
- [TEXTURE_GENERATION.md](./TEXTURE_GENERATION.md) - Procedural texture generation
- [API_DESIGN.md](./API_DESIGN.md) - API endpoint specifications
- [DATA_STRUCTURE.md](./DATA_STRUCTURE.md) - GameObject property structure
- [DECISIONS.md](./DECISIONS.md) - Decision log
