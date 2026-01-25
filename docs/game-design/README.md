# Game Design Documentation

This directory contains detailed design documents for the space sim MUSH game mechanics.

## Overview

The game combines traditional MUSH-style room-based gameplay with space simulation mechanics, including:
- FTL (Faster-Than-Light) travel system
- Orbital mechanics
- Ship design and construction
- Dual-space system (interior rooms + 3D space)

## Documents

### Core Mechanics

1. **[FTL_SYSTEM.md](./FTL_SYSTEM.md)**
   - Jump Drive system (legally distinct from Star Trek)
   - Jump Factor speed system (logarithmic scale)
   - FTL travel mechanics
   - Navigation and course plotting
   - Power and fuel consumption
   - Real-time multiplayer considerations

2. **[SPACE_MECHANICS.md](./SPACE_MECHANICS.md)**
   - Dual-space system (interior rooms + space zones)
   - Orbital mechanics
   - Player location and transitions
   - Movement commands (interior vs space)
   - Ship-to-ship interactions
   - Space object types (planets, stations, ships)

3. **[SHIP_DESIGN.md](./SHIP_DESIGN.md)**
   - Primitive-based ship construction
   - Primitive types and attachment system
   - Ship structure and hierarchy
   - System integration
   - Ship statistics calculation
   - Design interface and workflow

4. **[SHIP_COMPONENTS.md](./SHIP_COMPONENTS.md)**
   - Physical component system
   - Physics properties (mass, density, etc.)
   - Component types (engines, shields, power, etc.)
   - Ship physics calculation
   - Balance systems (power, processing, heat)
   - Component placement effects
   - Damage model

5. **[ORBITAL_COMMANDS.md](./ORBITAL_COMMANDS.md)**
   - Simplified orbital command interface
   - High-level commands (enter orbit, match orbit, etc.)
   - Automatic orbital insertion and maintenance
   - Implementation details
   - User experience design

6. **[RNG_NOTES.md](./RNG_NOTES.md)**
   - RNG/probability system status
   - Placeholder implementations
   - Future probability engine integration
   - Areas that will use probability

7. **[ROOM_SHAPES.md](./ROOM_SHAPES.md)**
   - Room shape system
   - Initial implementation: 3x3x3 cubes
   - Future: Custom room shapes (primitive-based, mesh-based, procedural)
   - Door system and room connections
   - Object placement and collision
   - Implementation phases

8. **[COMMUNICATION_SYSTEM.md](./COMMUNICATION_SYSTEM.md)**
   - Multi-level communication system
   - Room level (local, instant)
   - Object level (ship/station-wide, via consoles)
   - Subspace communication (long-range, time-delayed)
   - Communication equipment and channels
   - UI design and implementation

## Design Principles

### Real-Time Multiplayer
- All players experience same game time
- No time acceleration (would break synchronization)
- Server maintains authoritative state
- Network synchronization for smooth gameplay

### Legally Safe Terminology
- "Jump Drive" instead of "Warp Drive"
- "Jump Factor" instead of "Warp Factor"
- "Jump Core" instead of "Warp Core"
- Avoids Star Trek IP while maintaining similar gameplay

### Code-Driven World
- Ships defined in GameObject properties
- Primitives stored as data structures
- Systems configured via properties
- Hot-reloadable designs

### Dual-Space System
- Interior rooms: Traditional MUSH movement
- Space zones: 3D continuous movement
- Seamless transitions between modes
- Ships can be both (interior + space presence)

## Integration with Architecture

These game mechanics integrate with the overall architecture documented in `../pivot/`:

- **Scene Data**: Ships, space zones use 3D properties
- **API**: FTL, navigation, ship design exposed via API
- **Client**: Renders 3D space, ships, orbital mechanics
- **Database**: Stores ship designs, orbital data, FTL state

## Implementation Phases

### Phase 1: Foundation
- Basic space zones
- Simple ship objects (static)
- Interior rooms (existing)

### Phase 2: Movement
- Orbital mechanics (simplified)
- Ship movement in space
- Docking/landing transitions

### Phase 3: FTL System
- Jump drive mechanics
- Navigation system
- Course plotting

### Phase 4: Ship Design
- Primitive system
- Ship builder interface
- System integration

### Phase 5: Advanced
- Realistic orbital mechanics
- Complex star systems
- Multi-crew ships

## Related Documentation

- [Architecture Pivot](../pivot/ARCHITECTURE_PIVOT.md) - Overall architecture
- [API Design](../pivot/API_DESIGN.md) - API specifications
- [Data Structure](../pivot/DATA_STRUCTURE.md) - GameObject properties
- [Migration Plan](../pivot/MIGRATION_PLAN.md) - Implementation timeline

## Questions?

Refer to specific design documents, or update this documentation as design evolves.
