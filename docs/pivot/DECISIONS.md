# Decision Log

This document records key architectural and technical decisions made during the pivot from text-based MU* to graphical MMORPG.

## Decision: Adapt vs. Rebuild

**Date**: 2026-01-22  
**Decision**: Adapt existing codebase rather than rebuild from scratch  
**Rationale**:
- Core systems (object system, database, scripting) are highly reusable (~80-95%)
- WebSocket infrastructure already exists
- Estimated 6-8 weeks vs. 12-16 weeks for rebuild
- Existing architecture is solid foundation

**Alternatives Considered**:
- Complete rebuild: Too time-consuming, would rebuild same systems
- Hybrid approach: Keep text, add graphics: Too complex, two codebases

**Status**: ✅ Accepted

## Decision: Model Format - STL

**Date**: 2026-01-22  
**Decision**: Use STL (Stereolithography) format for 3D models  
**Rationale**:
- Direct CAD export capability (no conversion step)
- Native Three.js support (STLLoader built-in)
- Simple format, easy to work with
- Materials defined in code (flexible, scriptable)

**Alternatives Considered**:
- OBJ: Less common in CAD, similar limitations
- GLTF: Requires conversion, not direct CAD export
- 3MF: No native web support, would need conversion

**Trade-offs**:
- ✅ Direct CAD export
- ✅ Native web support
- ❌ No embedded materials (but this is actually a benefit for code-driven approach)
- ❌ No hierarchy (single mesh per file)

**Status**: ✅ Accepted  
**Future Consideration**: Add 3MF → GLTF conversion if embedded materials become important

## Decision: Procedural Texture Generation

**Date**: 2026-01-22  
**Decision**: Generate textures procedurally on server, no texture files  
**Rationale**:
- Fits "world describable in code" philosophy
- No asset management overhead
- Infinite variations through parameters
- Hot-reloadable (change properties → regenerate)
- Client format negotiation (WebP, PNG, JPEG)

**Alternatives Considered**:
- Texture files: Requires asset management, not code-driven
- Hybrid (files + procedural): More complex, less consistent

**Trade-offs**:
- ✅ Fully code-driven
- ✅ No asset management
- ✅ Dynamic and flexible
- ❌ Requires texture generation library
- ❌ Generation performance considerations

**Status**: ✅ Accepted

## Decision: Client Technology - HTML5/WebGL (Original)

**Date**: 2026-01-22  
**Decision**: HTML5 + CSS + JavaScript + WebGL (Three.js)  
**Rationale**:
- Zero installation (web-based)
- Cross-platform (works everywhere)
- Modern web technologies
- Three.js is mature and well-supported
- Easy deployment (just serve HTML)

**Alternatives Considered**:
- Unity WebGL: Larger download, more complex
- Native client: Requires installation, platform-specific
- Unreal Engine WebGL: Too heavy, overkill

**Trade-offs**:
- ✅ Zero installation
- ✅ Cross-platform
- ✅ Easy deployment
- ❌ WebGL compatibility concerns (but all modern browsers support)
- ❌ Performance limitations (but acceptable for small-scale MMORPG)

**Status**: ⚠️ Superseded (see below)

## Decision: Client Technology - C# Native with HTML/CSS/JS Textures

**Date**: 2026-01-25  
**Decision**: C# native 3D engine (MonoGame/Unity) with HTML/CSS/JavaScript rendered as textures  
**Rationale**:
- Both developers are C# native, faster development
- Better performance (native code)
- Full 3D engine capabilities
- HTML/CSS/JS for rich, interactive UI on 3D objects
- Familiar tooling and ecosystem
- Can render HTML displays on ship consoles, screens, etc.

**Alternatives Considered**:
- Web-based client: Zero install, but JavaScript development, WebGL limitations
- Pure C# UI: Less flexible, more development time
- Hybrid: C# engine + HTML textures = best of both worlds

**Trade-offs**:
- ✅ Native C# development (familiar)
- ✅ Better performance
- ✅ Rich 3D engine features
- ✅ HTML/CSS/JS flexibility for UI
- ✅ Interactive displays on 3D objects
- ❌ Requires installation (not zero-install)
- ❌ Platform-specific builds
- ❌ Larger download size

**Technology Stack**:
- 3D Engine: MonoGame (Unity ruled out)
- HTML Renderer: CefSharp or WebView2 (TBD)
- Language: C# (.NET)

**Status**: ✅ Accepted

## Decision: Server-First Architecture

**Date**: 2026-01-22  
**Decision**: Server is authoritative, client is viewer  
**Rationale**:
- Security (no client-side game logic)
- Consistency (single source of truth)
- Flexibility (change server without client updates)
- Prevents cheating

**Alternatives Considered**:
- Client-authoritative: Security risk, allows cheating
- Hybrid: Complex, inconsistent

**Trade-offs**:
- ✅ Secure
- ✅ Consistent
- ✅ Flexible
- ❌ Network latency (but acceptable for turn-based or slow-paced gameplay)
- ❌ Server load (but manageable for small-scale)

**Status**: ✅ Accepted

## Decision: Zero Client Assets

**Date**: 2026-01-22  
**Decision**: Client contains zero game assets, everything served by server  
**Rationale**:
- World describable in code (assets defined server-side)
- No asset versioning issues
- Easy updates (change server, client reflects)
- Consistent with server-first architecture

**Alternatives Considered**:
- Bundled assets: Versioning issues, larger client, harder updates
- CDN assets: Still requires asset management

**Trade-offs**:
- ✅ Code-driven
- ✅ Easy updates
- ✅ No versioning issues
- ❌ Network bandwidth (but models/textures can be cached)
- ❌ Initial load time (but can be optimized with progressive loading)

**Status**: ✅ Accepted

## Decision: Keep Existing Systems

**Date**: 2026-01-22  
**Decision**: Keep object system, database, scripting, server architecture  
**Rationale**:
- These systems are game-agnostic and highly reusable
- Well-tested and working
- Would take significant time to rebuild
- No need to change what works

**What We're Keeping**:
- Object system (GameObject/ObjectClass) - 80% reusable
- Database layer (LiteDB) - 95% reusable
- Scripting system (C# Roslyn) - 90% reusable
- Network infrastructure (WebSocket) - 70% reusable
- Server architecture (DI, services) - 85% reusable

**Status**: ✅ Accepted

## Decision: Drop Telnet Support

**Date**: 2026-01-22  
**Decision**: Remove Telnet server, keep only WebSocket  
**Rationale**:
- Graphical client doesn't need Telnet
- WebSocket is sufficient
- Simplifies codebase
- Can add back later if needed for admin/debug

**Alternatives Considered**:
- Keep Telnet: Unnecessary complexity, not needed for graphical client
- Keep for admin: Can add back later if needed

**Status**: ✅ Accepted (can reconsider if needed)

## Decision: Texture Generation Library

**Date**: 2026-01-22  
**Decision**: TBD - SkiaSharp or ImageSharp  
**Rationale**: To be determined during Phase 2 implementation  
**Considerations**:
- SkiaSharp: Cross-platform, good performance, supports WebP
- ImageSharp: Pure .NET, no native dependencies, modern API

**Status**: ⏳ Pending (Phase 2)

## Decision: Noise Library

**Date**: 2026-01-22  
**Decision**: TBD - FastNoiseLite C# port or custom implementation  
**Rationale**: To be determined during Phase 2 implementation  
**Considerations**:
- FastNoiseLite: Well-tested, good performance
- Custom: Full control, no dependencies

**Status**: ⏳ Pending (Phase 2)

## Decision: License Change

**Date**: 2026-01-22  
**Decision**: Change from open source to proprietary license  
**Rationale**: Both authors consent, moving to private repository  
**Status**: ✅ Completed

## Decision: Private Repository

**Date**: 2026-01-22  
**Decision**: Create new private repository for proprietary version  
**Rationale**: License change, proprietary development  
**Status**: ✅ Completed

## Decision: RNG/Probability System

**Date**: 2026-01-25  
**Decision**: Deliberately leave RNG/deterministic aspects blank for now  
**Rationale**:
- Have a probability engine that will be imported eventually
- Don't want to design around a system we don't have yet
- Will integrate probability engine when available
- For now, use placeholders or simple implementations

**Status**: ⏳ Deferred - Will integrate probability engine later

**Note**: All game design documents that mention RNG/probability should note this is a placeholder for future probability engine integration. See [RNG_NOTES.md](../game-design/RNG_NOTES.md) for details.

## Decision: Unify Verbs and Functions

**Date**: 2026-01-25  
**Decision**: Consolidate verbs and functions into single script processor  
**Rationale**:
- Verbs are just functions with command hooks
- Eliminates duplication (two separate systems doing the same thing)
- Simplifies architecture (one resolver, one processor, one execution path)
- More flexible (functions can become verbs by adding command patterns)
- Easier to maintain (changes in one place)

**Current State**:
- Separate `IVerbResolver` and `IFunctionResolver`
- Separate verb and function storage
- Separate execution paths

**Target State**:
- Unified `IScriptResolver` and `IScriptProcessor`
- Single script storage (scripts can have optional command patterns)
- Single execution path
- Verbs = Scripts with command patterns

**Alternatives Considered**:
- Keep separate: More complex, duplicate code, harder to maintain
- Unify: Simpler, cleaner, more flexible

**Trade-offs**:
- ✅ Simpler architecture
- ✅ Less code to maintain
- ✅ More flexible
- ❌ Requires migration effort
- ❌ Breaking change (but we're doing cleanup anyway)

**Status**: ✅ Accepted  
**Implementation**: During Phase 0 refactoring  
**Reference**: [SCRIPT_PROCESSOR_UNIFICATION.md](./SCRIPT_PROCESSOR_UNIFICATION.md)

## Decision: No Text Command Input

**Date**: 2026-01-25  
**Decision**: Players do not type commands - all interaction via UI/keybinds  
**Rationale**:
- Graphical MMORPG, not text-based MUSH
- Modern game interaction model
- Better UX with UI/keybinds
- Verbs become object command interfaces
- Player object has RPG-like commands

**Interaction Methods:**
- Key bindings (keyboard shortcuts)
- UI clicks (buttons, menus, context menus)
- Object command interfaces (verbs as clickable actions)
- Player object commands (inventory, equipment, combat)

**What Changes:**
- ❌ Remove text command input
- ✅ Add key binding system
- ✅ Add mouse/click input
- ✅ Add UI interaction system
- ✅ Verbs become clickable actions on objects
- ✅ Player object has special commands (inventory, combat, etc.)

**What Stays:**
- Verb system (verbs still stored on objects)
- Script execution (unchanged)
- Object system (unchanged)

**Status**: ✅ Accepted  
**Reference**: [PLAYER_INTERACTION_MODEL.md](./PLAYER_INTERACTION_MODEL.md)

## Decision: Database Abstraction Layer

**Date**: 2026-01-25  
**Decision**: Create database abstraction layer to make database swappable  
**Rationale**:
- Need to migrate from LiteDB to PostgreSQL eventually
- Abstraction allows swapping databases without changing application code
- Better testability (in-memory implementations)
- Future-proof (can add other databases)
- Cleaner separation of concerns

**Implementation:**
- Create `IDatabase` and `ICollection<T>` interfaces
- Implement LiteDB version (wraps existing code)
- Remove direct database dependencies from application code
- Add PostgreSQL implementation when ready to migrate

**Benefits:**
- ✅ Easy migration path
- ✅ Can test with different databases
- ✅ Better testability
- ✅ Future-proof

**Trade-offs:**
- ❌ Additional abstraction layer (slight overhead)
- ❌ Need to maintain multiple implementations
- ✅ But: Worth it for flexibility

**Status**: ✅ Accepted  
**Implementation**: Phase 0 refactoring  
**Reference**: [DATABASE_ABSTRACTION.md](./DATABASE_ABSTRACTION.md)

## Open Questions

1. **Authentication**: Session-based or token-based?
2. **Rate Limiting**: What limits to implement?
3. **CDN**: Use CDN for assets or serve directly?
4. **Compression**: What compression for STL files?
5. **LOD**: How many LOD levels to support?
6. **HTML Renderer**: CefSharp or WebView2?
7. **Probability Engine**: Integration details TBD when engine is available

## Decision Process

Decisions are made by consensus between both authors. Major architectural decisions are documented here with rationale and alternatives considered.
