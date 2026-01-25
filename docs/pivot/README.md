# Pivot Documentation

This directory contains documentation for the architectural pivot from text-based MU* to graphical MMORPG.

## Overview

CSMOO is being transformed from a text-based Multi-User Shared Object-Oriented Environment into a graphical MMORPG while maintaining the core server-first, code-driven architecture.

## Documentation Index

### Core Documents

1. **[ARCHITECTURE_PIVOT.md](./ARCHITECTURE_PIVOT.md)**
   - Main decision document
   - Vision and core principles
   - What we're keeping vs. changing
   - Architecture benefits

2. **[DECISIONS.md](./DECISIONS.md)**
   - Decision log with rationale
   - Alternatives considered
   - Trade-offs and status

3. **[MIGRATION_PLAN.md](./MIGRATION_PLAN.md)**
   - Phased implementation plan
   - Timeline and deliverables
   - Risk mitigation
   - Success criteria

4. **[CLEANUP_CHECKLIST.md](./CLEANUP_CHECKLIST.md)**
   - Codebase cleanup process
   - Backward compatibility removal
   - Duplicate code cleanup
   - Wrapper removal
   - Old scripting engine removal

5. **[REFACTORING_STRATEGY.md](./REFACTORING_STRATEGY.md)**
   - Domain-specific project structure
   - Namespace organization principles
   - Breaking up megalithic classes
   - Project dependency strategy
   - Refactoring process and examples

6. **[SCRIPT_PROCESSOR_UNIFICATION.md](./SCRIPT_PROCESSOR_UNIFICATION.md)**
   - Consolidate verbs and functions into single script processor
   - Verbs = Functions + Command Hooks
   - Unified script model and execution
   - Migration strategy
   - Benefits and implementation details

7. **[PLAYER_INTERACTION_MODEL.md](./PLAYER_INTERACTION_MODEL.md)**
   - No text command input - all interaction via UI/keybinds
   - Verbs become object command interfaces (clickable actions)
   - Player object commands (inventory, equipment, combat)
   - Key binding system
   - UI interaction design
   - Input handling architecture

8. **[DATABASE_PERFORMANCE.md](./DATABASE_PERFORMANCE.md)**
   - LiteDB performance analysis
   - Scaling concerns (50-100 players, 10K-100K objects)
   - Performance bottlenecks and mitigation strategies
   - Alternative database options
   - Migration recommendations and timeline

9. **[DATABASE_ABSTRACTION.md](./DATABASE_ABSTRACTION.md)**
   - Database abstraction layer design
   - Interface definitions (`IDatabase`, `ICollection<T>`)
   - Implementation strategy (LiteDB, PostgreSQL)
   - Database factory pattern
   - Migration path and testing strategy

10. **[DATABASE_ISSUES.md](./DATABASE_ISSUES.md)**
   - Analysis of current database implementation issues
   - Identified hacks and anti-patterns
   - Performance problems
   - Architectural issues
   - Refactoring priorities and recommendations

### Technical Specifications

4. **[CLIENT_ARCHITECTURE.md](./CLIENT_ARCHITECTURE.md)**
   - Web-based client (original plan)
   - HTML5/CSS/JavaScript stack
   - Responsibilities and communication protocol
   - Rendering pipeline
   - UI overlay design

5. **[CLIENT_ARCHITECTURE_CSharp.md](./CLIENT_ARCHITECTURE_CSharp.md)**
   - C# native client (current plan)
   - 3D engine with HTML/CSS/JS texture rendering
   - Technology stack options
   - Architecture design
   - Implementation approach

5. **[API_DESIGN.md](./API_DESIGN.md)**
   - HTTP REST API endpoints
   - WebSocket protocol extensions
   - Message formats
   - Error handling

6. **[DATA_STRUCTURE.md](./DATA_STRUCTURE.md)**
   - GameObject properties for 3D
   - Material and texture definitions
   - Lighting and UI styling
   - Helper methods

7. **[MODEL_FORMAT.md](./MODEL_FORMAT.md)**
   - STL format decision
   - Workflow and best practices
   - Material application
   - Server/client implementation

8. **[TEXTURE_GENERATION.md](./TEXTURE_GENERATION.md)**
   - Procedural texture generation
   - Generator types
   - API endpoint
   - Implementation details

## Quick Start

1. **Read [ARCHITECTURE_PIVOT.md](./ARCHITECTURE_PIVOT.md)** to understand the overall vision
2. **Review [DECISIONS.md](./DECISIONS.md)** to see key decisions and rationale
3. **Follow [MIGRATION_PLAN.md](./MIGRATION_PLAN.md)** for implementation phases
4. **Reference technical docs** as needed during development

## Key Principles

1. **Server-First**: Server is authoritative, client is viewer
2. **Code-Driven**: World describable entirely in code
3. **Zero Client Assets**: Everything served by server
4. **Hot-Reloadable**: Change properties → regenerate → client updates

## Timeline

**Estimated**: 6-8 weeks to working prototype

- **Phase 1**: Scene Data Structure (1 week)
- **Phase 2**: API Endpoints (1 week)
- **Phase 3**: Client Prototype (2 weeks)
- **Phase 4**: Full Integration (2 weeks)

## Status

- ✅ Architecture decisions made
- ✅ Documentation complete
- ⏳ Implementation pending

## Related Documentation

### Game Design
- [Game Design Overview](../game-design/README.md) - Game mechanics overview
- [FTL System](../game-design/FTL_SYSTEM.md) - Jump Drive and FTL travel
- [Space Mechanics](../game-design/SPACE_MECHANICS.md) - Dual-space system and orbital mechanics
- [Ship Design](../game-design/SHIP_DESIGN.md) - Primitive-based ship construction

## Questions?

Refer to the relevant documentation file, or update [DECISIONS.md](./DECISIONS.md) with new decisions as they're made.
