# FTL (Faster-Than-Light) Jump Drive System

## Overview

The game uses a "Jump Drive" system for faster-than-light travel, allowing ships to travel between star systems in real-time multiplayer. The system is designed to be legally distinct from Star Trek's "Warp" system while providing similar gameplay mechanics.

## Terminology (Legally Safe)

**Core Terms:**
- **Jump Drive**: FTL propulsion system
- **Jump Factor**: Speed multiplier (1-10 scale)
- **Jump Core**: Power source for jump drive
- **Jump Space**: The medium through which FTL travel occurs

**Avoided Terms (Star Trek IP):**
- ❌ "Warp" / "Warp Factor" / "Warp Core"
- ❌ "Transporter" / "Phaser" / "Starfleet"
- ❌ Other Star Trek-specific terminology

## Jump Factor System

### Speed Calculation

Jump factors use a **logarithmic scale** to make interstellar travel feasible in real-time:

```csharp
// Speed calculation
double lightSpeed = 299792458.0; // m/s (c)
double CalculateFTLSpeed(double factor) {
  return Math.Pow(10, factor - 1) * lightSpeed;
}
```

**Speed by Factor:**
- Factor 1: 1x light speed (299,792,458 m/s)
- Factor 2: 10x light speed (2,997,924,580 m/s)
- Factor 3: 100x light speed (29,979,245,800 m/s)
- Factor 4: 1,000x light speed (299,792,458,000 m/s)
- Factor 5: 10,000x light speed (2.998e12 m/s)
- Factor 6: 100,000x light speed (2.998e13 m/s)
- Factor 7: 1,000,000x light speed (2.998e14 m/s)
- Factor 8: 10,000,000x light speed (2.998e15 m/s)
- Factor 9: 100,000,000x light speed (2.998e16 m/s)
- Factor 10: 1,000,000,000x light speed (2.998e17 m/s)

### Travel Time Examples

**Proxima Centauri (4.24 light-years):**
- Factor 1: 4.24 years (too long)
- Factor 2: ~5 months (still long)
- Factor 3: ~15 days (feasible)
- Factor 4: ~1.5 days (good)
- Factor 5: ~3.6 hours (excellent)
- Factor 6: ~22 minutes (very fast)
- Factor 7: ~2.2 minutes (instant for gameplay)
- Factor 8: ~13 seconds (near-instant)
- Factor 9: ~1.3 seconds (instant)
- Factor 10: ~0.13 seconds (instant)

**Solar System (1 AU = 1.5e11 m):**
- Factor 1: ~500 seconds (~8 minutes)
- Factor 2: ~50 seconds
- Factor 3: ~5 seconds
- Factor 4+: <1 second (instant)

## Jump Drive Properties

### Ship Jump Drive Data

```csharp
ship.Properties["jumpDrive"] = new BsonDocument {
  { "maxFactor", 9.0 },           // Maximum jump factor capability
  { "currentFactor", 0.0 },       // Current speed (0 = sublight)
  { "basePowerConsumption", 1000.0 }, // Base power in Watts
  { "efficiency", 0.85 },        // Drive efficiency (0.0-1.0)
  { "cooldown", 0.0 },            // Cooldown timer (seconds)
  { "cooldownDuration", 60.0 },  // Cooldown after dropping from FTL
  { "status", "ready" },          // ready, engaged, cooling, damaged
  { "fuelType", "quantum" },      // Fuel type required
  { "fuelLevel", 100.0 },         // Current fuel level (%)
  { "fuelCapacity", 1000.0 }     // Maximum fuel capacity
};
```

### Power Consumption

Power consumption scales exponentially with jump factor:

```csharp
double CalculatePowerRequired(double factor, double basePower, double efficiency) {
  if (factor <= 0) return 0.0;
  double rawPower = basePower * Math.Pow(10, factor - 1);
  return rawPower / efficiency; // Less efficient = more power needed
}
```

**Power Requirements (Base 1000W, 85% efficiency):**
- Factor 1: ~1,176 W
- Factor 2: ~11,765 W
- Factor 3: ~117,647 W
- Factor 4: ~1,176,471 W
- Factor 5: ~11,764,706 W
- Factor 6: ~117,647,059 W
- Factor 7: ~1,176,470,588 W
- Factor 8: ~11,764,705,882 W
- Factor 9: ~117,647,058,824 W
- Factor 10: ~1,176,470,588,235 W

### Fuel Consumption

Fuel consumption also scales with factor and time:

```csharp
double CalculateFuelConsumption(double factor, double timeSeconds) {
  if (factor <= 0) return 0.0;
  double baseConsumption = 0.1; // % per hour at factor 1
  double factorMultiplier = Math.Pow(10, factor - 1);
  return (baseConsumption * factorMultiplier * timeSeconds) / 3600.0;
}
```

## FTL Mechanics

### Engaging Jump Drive

**Requirements:**
1. Ship must be in "safe" space (not in gravity well)
2. Sufficient power available
3. Sufficient fuel
4. Jump drive not on cooldown
5. Jump factor within ship's capability

**Process:**
1. Player commands: `engage jump factor 5`
2. System checks requirements
3. If valid, engage jump drive
4. Ship accelerates to FTL speed
5. Ship enters "jump space"
6. Power/fuel consumption begins

### During FTL Travel

**Limitations:**
- Cannot interact with objects in normal space
- Cannot change course (or very limited course changes)
- Cannot dock/land
- Cannot engage weapons (or limited)
- Cannot scan (or limited range)

**Allowed:**
- Players can move around ship interior
- Players can interact with ship systems
- Players can communicate (ship-to-ship radio)
- Players can monitor navigation
- Players can drop from FTL at any time

### Dropping from FTL

**Process:**
1. Player commands: `drop jump` or `disengage jump`
2. Ship decelerates from FTL
3. Ship returns to normal space
4. Cooldown period begins
5. Ship systems stabilize

**Cooldown:**
- Prevents immediate re-engagement
- Duration based on jump factor used
- Higher factors = longer cooldown
- During cooldown, ship is vulnerable

**Cooldown Calculation:**
```csharp
double CalculateCooldown(double factor) {
  double baseCooldown = 60.0; // seconds
  return baseCooldown * Math.Pow(1.5, factor - 1);
}
```

## Navigation System

### Course Plotting

Before engaging FTL, players can plot courses:

```csharp
// Course data
ship.Properties["course"] = new BsonDocument {
  { "destination", "proxima-centauri" },
  { "destinationType", "star" },  // star, planet, station, coordinates
  { "jumpFactor", 5.0 },
  { "distance", 4.24e16 },        // meters (4.24 light-years)
  { "estimatedTime", 12960.0 },   // seconds (~3.6 hours)
  { "waypoints", new BsonArray { } },
  { "status", "plotted" }          // plotted, active, completed, cancelled
};
```

### Navigation Commands

**Plot Course:**
```
plot course to <destination> at factor <n>
```

**Engage Course:**
```
engage course
```

**Cancel Course:**
```
cancel course
```

**Drop from FTL:**
```
drop jump
disengage jump
```

## FTL Restrictions

### Gravity Wells

Ships cannot engage FTL near massive objects:

```csharp
spaceZone.Properties["ftlRestricted"] = true;
spaceZone.Properties["gravityWellRadius"] = 1e10; // meters (~67 AU)
spaceZone.Properties["parentBody"] = "star-1";
spaceZone.Properties["parentBodyMass"] = 1.989e30; // kg (solar mass)
```

**Restriction Zones:**
- Near stars (gravity well interference)
- Near planets (gravity well interference)
- Inside asteroid fields (collision risk)
- Near space stations (safety zones)

**Safe Zones:**
- Deep space (far from gravity wells)
- Designated jump lanes
- Between star systems
- Lagrange points (some)

### Emergency Drop

If ship enters restricted zone while at FTL:
- Automatic emergency drop
- Extended cooldown (safety systems)
- Potential damage to jump drive
- Ship may be damaged

## Integration with Orbital Mechanics

### Two Movement Modes

**Sublight (Factor 0):**
- Orbital mechanics fully active
- Gravitational forces affect ship
- Can orbit planets/stars
- Can dock/land
- Can interact with objects
- Normal physics simulation

**FTL (Factor 1+):**
- Orbital mechanics disabled (or minimal)
- Ship moves in straight line (or along plotted course)
- Cannot interact with gravity wells
- Must drop to sublight to dock/land
- Limited interaction with normal space

### Transition Between Modes

**Sublight → FTL:**
1. Check if in safe zone
2. Engage jump drive
3. Accelerate to FTL speed
4. Enter jump space
5. Orbital mechanics disabled

**FTL → Sublight:**
1. Disengage jump drive
2. Decelerate from FTL
3. Return to normal space
4. Orbital mechanics re-enabled
5. Cooldown begins

## Real-Time Multiplayer Considerations

### Synchronization

- All players see same game time
- FTL travel happens in real-time for all
- Server maintains authoritative position
- Network updates sync ship positions
- Lag compensation for smooth movement

### Time Management

```csharp
server.Properties["gameTime"] = DateTime.UtcNow;  // Or game epoch
server.Properties["timeScale"] = 1.0;              // Always 1.0 (real-time)
server.Properties["tickRate"] = 20;                // Updates per second
server.Properties["physicsTickRate"] = 60;         // Physics updates per second
```

### Network Updates

**During FTL:**
- Update ship position based on velocity
- Update fuel/power consumption
- Broadcast to all players in same space zone
- Update course progress

**Frequency:**
- Position updates: Every physics tick (60 Hz)
- State updates: Every game tick (20 Hz)
- Course updates: On change or every 5 seconds

## Gameplay Implications

### Travel Time Creates Downtime

**Benefits:**
- Players can interact on ship during travel
- Time for roleplay, ship maintenance, planning
- Creates sense of distance and scale
- Adds strategic element (fuel management)

**Example Journey:**
- Proxima Centauri at Factor 5: ~3.6 hours
- Players can:
  - Roleplay on ship
  - Maintain systems
  - Plan mission
  - Interact with crew
  - Monitor ship status

### Fuel Management

- Higher factors = more fuel consumption
- Strategic decisions: fast but expensive vs. slow but efficient
- Need to refuel at stations
- Adds resource management gameplay

### Power Management

- Jump drive requires significant power
- May need to shut down other systems
- Strategic power allocation
- Emergency situations (power failure during FTL)

## Implementation Notes

### Server-Side

- Calculate FTL speed and position updates
- Enforce restrictions (gravity wells, safe zones)
- Manage fuel/power consumption
- Handle cooldown timers
- Validate course plotting

### Client-Side

- Display jump factor and speed
- Show course progress
- Display fuel/power levels
- Visual effects for FTL (optional)
- Navigation interface

### Database

- Store jump drive properties in GameObject
- Store course data
- Track fuel/power consumption
- Log FTL events (for debugging)

## Future Enhancements

1. **Jump Lanes**: Predefined safe routes between systems
2. **Jump Gates**: Fixed points for FTL entry/exit
3. **Interdiction**: Ability to force ships out of FTL
4. **Jump Drive Upgrades**: Improve max factor, efficiency
5. **Emergency Jump**: Risky high-factor jump with damage risk
6. **Jump Drive Damage**: Malfunctions, repairs needed
