# Space Mechanics: Dual-Space System

## Overview

The game uses a dual-space system combining traditional MUSH-style interior rooms with 3D space zones for spaceflight. Players can transition between being "on foot" in interior spaces and "in space" piloting ships.

## Core Concept

### Two Types of Spaces

1. **Interior Rooms** (Traditional MUSH)
   - Ship interiors (bridge, engineering, cargo bay, quarters)
   - Space stations
   - Planetary bases
   - Traditional room-based movement (exits, doors)
   - 2D/3D visualization but discrete locations

2. **Space Zones** (3D Space)
   - Star systems
   - Orbital space around planets
   - Deep space
   - 3D continuous movement
   - Orbital mechanics apply

### Player State

Players can be in one of two modes:

**On Foot (Interior Mode):**
- Located in an interior room
- Traditional MUSH movement (go north, enter door, etc.)
- Interact with objects, other players
- Use ship systems, consoles, equipment
- Cannot directly control ship movement

**In Space (Piloting Mode):**
- Located in a space zone
- 3D movement in space
- Orbital mechanics apply (when sublight)
- Ship-to-ship interactions
- Can dock/land to enter interior rooms

## Space Zones

### Definition

Space zones are like "rooms" but exist in 3D space with continuous coordinates:

```csharp
spaceZone.Properties["zoneType"] = "space";
spaceZone.Properties["bounds"] = new BsonDocument {
  { "center", new BsonDocument { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
  { "radius", 1e12 }  // meters (~6.7 AU)
};
spaceZone.Properties["parentBody"] = "star-1";
spaceZone.Properties["gravityWell"] = true;
spaceZone.Properties["gravityWellRadius"] = 1e11; // meters
spaceZone.Properties["ftlRestricted"] = true;
spaceZone.Properties["ftlRestrictionRadius"] = 1e10; // meters
```

### Zone Types

**Star System Zone:**
- Contains star, planets, moons, stations
- Large radius (multiple AU)
- Gravity well from star
- FTL restricted near star

**Planetary Orbit Zone:**
- Contains planet, moons, stations in orbit
- Smaller radius (few million km)
- Gravity well from planet
- FTL restricted near planet

**Deep Space Zone:**
- Between star systems
- Very large radius
- Minimal gravity
- FTL allowed

**Lagrange Point Zone:**
- Special zones at Lagrange points
- Stable orbits
- Good for stations
- FTL may be allowed (depending on distance)

## Orbital Mechanics

### Design Philosophy

**Real Physics, Simple Commands**

- **Under the Hood**: Full realistic orbital mechanics simulation
  - N-body physics
  - Elliptical orbits
  - Orbital elements (semi-major axis, eccentricity, inclination, etc.)
  - Gravitational forces
  - Realistic velocities and trajectories

- **Player Interface**: High-level, intuitive commands
  - "Enter equatorial orbit"
  - "Enter polar orbit"
  - "Enter geostationary orbit"
  - "Match orbit with [target]"
  - No manual trajectory plotting
  - No Kerbal Space Program complexity
  - Star Trek-style bridge commands

**Example:**
```
Player: "Enter equatorial orbit"
System: "Calculating orbital insertion..."
System: "Orbital insertion complete. We are now in equatorial orbit."
```

The system automatically:
1. Calculates required velocity and trajectory
2. Executes the maneuver
3. Places ship in the requested orbit
4. Maintains orbit automatically

### Orbital Data Structure

**Full Physics Data (Server-Side):**
```csharp
spaceObject.Properties["orbitalData"] = new BsonDocument {
  { "parentBody", "star-1" },              // What it orbits
  { "semiMajorAxis", 1.5e11 },            // meters (1 AU)
  { "eccentricity", 0.0167 },             // 0 = circular, 1 = parabolic
  { "inclination", 0.0 },                 // radians (orbital plane tilt)
  { "longitudeOfAscendingNode", 0.0 },    // radians (where orbit crosses reference plane)
  { "argumentOfPeriapsis", 0.0 },         // radians (orientation of ellipse)
  { "trueAnomaly", 0.0 },                // radians (current position in orbit)
  { "meanAnomaly", 0.0 },                // radians (time-based position)
  { "mass", 5.97e24 },                   // kg
  { "velocity", new BsonDocument {        // Current velocity vector (m/s)
    { "x", 0.0 },
    { "y", 29800.0 },  // Earth orbital velocity
    { "z", 0.0 }
  }},
  { "position", new BsonDocument {        // Current position vector (m)
    { "x", 1.5e11 },  // 1 AU from star
    { "y", 0.0 },
    { "z", 0.0 }
  }},
  { "orbitalPeriod", 31557600.0 }        // seconds (1 year)
};
```

**Simplified Player Data (What Players See):**
```csharp
ship.Properties["orbitInfo"] = new BsonDocument {
  { "orbiting", "Earth" },
  { "orbitType", "equatorial" },          // equatorial, polar, geostationary, custom
  { "altitude", 400000.0 },              // meters (400 km)
  { "orbitalPeriod", 5400.0 },           // seconds (~90 minutes)
  { "inclination", 0.0 },                // degrees (0 = equatorial)
  { "status", "stable" }                 // stable, decaying, maneuvering
};
```

### Physics Simulation

**Update Loop:**
- Calculate gravitational forces from parent bodies (N-body physics)
- Update velocity based on forces
- Update position based on velocity
- Apply thrust (for ships)
- Update orbital elements
- Maintain orbits automatically

**Time Step:**
- Real-time: 1 second = 1 second
- Physics updates: 60 Hz (60 times per second)
- Game updates: 20 Hz (20 times per second)
- Orbital calculations: Every physics tick

**Gravitational Force (N-Body):**
```csharp
// F = G * (m1 * m2) / r²
double G = 6.67430e-11; // Gravitational constant

Vector3 CalculateGravity(Vector3 position, List<GameObject> bodies) {
  Vector3 totalForce = Vector3.Zero;
  
  foreach (var body in bodies)
  {
    Vector3 direction = body.position - position;
    double distance = direction.Length();
    
    if (distance > 0.001) // Avoid division by zero
    {
      double force = G * (objectMass * body.mass) / (distance * distance);
      totalForce += direction.Normalize() * force;
    }
  }
  
  return totalForce;
}
```

**Orbital Maintenance:**
```csharp
void MaintainOrbit(GameObject ship)
{
  // If ship is in "orbit mode", automatically maintain orbit
  if (ship.Properties["orbitInfo"]["status"] == "stable")
  {
    // Calculate required velocity for current orbit
    Vector3 requiredVelocity = CalculateOrbitalVelocity(
      ship.position,
      ship.Properties["orbitInfo"]["orbiting"]
    );
    
    // Apply small corrections to maintain orbit
    Vector3 velocityError = requiredVelocity - ship.velocity;
    ApplyOrbitalCorrection(ship, velocityError);
  }
}
```

### Orbital Mechanics for Ships

**Sublight Movement:**
- Ships follow realistic orbital mechanics
- Thrust changes velocity
- Velocity changes orbit
- **Simplified Commands**: Players use high-level commands, system handles physics

**Orbital Commands:**
```csharp
// Player commands (simplified interface)
public verb EnterOrbit(string orbitType, string target = null)
{
  // orbitType: "equatorial", "polar", "geostationary", "custom"
  // target: Optional target to match orbit with
  
  var ship = Player.Location; // Assuming player is on ship
  
  if (target != null)
  {
    // Match orbit with target
    var targetObject = ResolveTarget(target);
    EnterMatchingOrbit(ship, targetObject, orbitType);
  }
  else
  {
    // Enter orbit around current parent body
    var parentBody = GetParentBody(ship);
    EnterOrbitAround(ship, parentBody, orbitType);
  }
  
  notify(Player, $"Entering {orbitType} orbit...");
  return true;
}

private void EnterOrbitAround(GameObject ship, GameObject parentBody, string orbitType)
{
  // Calculate orbital parameters based on orbit type
  OrbitalParameters orbitParams = CalculateOrbitParameters(
    parentBody,
    orbitType,
    ship.position
  );
  
  // Calculate required velocity
  Vector3 requiredVelocity = CalculateOrbitalVelocity(
    ship.position,
    parentBody,
    orbitParams
  );
  
  // Execute maneuver (automatic)
  ExecuteOrbitalInsertion(ship, requiredVelocity, orbitParams);
  
  // Update ship orbit info
  ship.Properties["orbitInfo"] = new BsonDocument {
    { "orbiting", parentBody.Name },
    { "orbitType", orbitType },
    { "altitude", orbitParams.altitude },
    { "orbitalPeriod", orbitParams.period },
    { "inclination", orbitParams.inclination },
    { "status", "stable" }
  };
}
```

**Orbit Type Calculations:**
```csharp
OrbitalParameters CalculateOrbitParameters(
  GameObject parentBody,
  string orbitType,
  Vector3 currentPosition)
{
  double parentRadius = parentBody.Properties["radius"].AsDouble;
  double parentMass = parentBody.Properties["mass"].AsDouble;
  double G = 6.67430e-11;
  
  switch (orbitType.ToLower())
  {
    case "equatorial":
      return new OrbitalParameters {
        altitude = CalculateAltitude(currentPosition, parentBody),
        inclination = 0.0, // Equatorial plane
        eccentricity = 0.0, // Circular
        // Calculate semi-major axis from altitude
        semiMajorAxis = parentRadius + CalculateAltitude(currentPosition, parentBody)
      };
      
    case "polar":
      return new OrbitalParameters {
        altitude = CalculateAltitude(currentPosition, parentBody),
        inclination = 90.0, // Polar orbit
        eccentricity = 0.0, // Circular
        semiMajorAxis = parentRadius + CalculateAltitude(currentPosition, parentBody)
      };
      
    case "geostationary":
      // Calculate altitude for geostationary orbit
      // T = 2π * sqrt(r³ / GM) where T = rotational period of body
      double rotationalPeriod = parentBody.Properties["rotationalPeriod"].AsDouble;
      double geostationaryRadius = Math.Pow(
        (G * parentMass * rotationalPeriod * rotationalPeriod) / (4 * Math.PI * Math.PI),
        1.0 / 3.0
      );
      
      return new OrbitalParameters {
        altitude = geostationaryRadius - parentRadius,
        inclination = 0.0, // Equatorial
        eccentricity = 0.0, // Circular
        semiMajorAxis = geostationaryRadius,
        orbitalPeriod = rotationalPeriod // Matches body rotation
      };
      
    default:
      // Custom orbit - use current position/velocity
      return CalculateOrbitFromState(currentPosition, ship.velocity, parentBody);
  }
}
```

**Automatic Orbital Insertion:**
```csharp
void ExecuteOrbitalInsertion(
  GameObject ship,
  Vector3 targetVelocity,
  OrbitalParameters orbitParams)
{
  // Calculate delta-V needed
  Vector3 currentVelocity = ship.Properties["orbitalData"]["velocity"];
  Vector3 deltaV = targetVelocity - currentVelocity;
  
  // Calculate burn time and direction
  double deltaVMagnitude = deltaV.Length();
  double thrust = CalculateTotalThrust(ship);
  double mass = CalculateShipMass(ship);
  double burnTime = (mass * deltaVMagnitude) / thrust;
  
  // Execute burn (automatic)
  StartOrbitalBurn(ship, deltaV.Normalize(), burnTime);
  
  // After burn, maintain orbit automatically
  ship.Properties["orbitInfo"]["status"] = "stable";
}
```

**FTL Movement:**
- Orbital mechanics disabled
- Ship moves in straight line (or plotted course)
- Cannot interact with gravity wells
- Must drop to sublight for orbital operations
- When dropping from FTL, ship enters space at current velocity (may need orbit correction)

## Player Location System

### Location Properties

```csharp
player.Properties["locationMode"] = "interior" | "space";
player.Properties["currentRoom"] = "bridge-room-id";      // If interior
player.Properties["currentShip"] = "ship-123";             // Ship player is on/in
player.Properties["spaceZone"] = "sol-system-zone";      // If in space
player.Properties["spacePosition"] = new BsonDocument {  // If in space
  { "x", 0.0 },
  { "y", 0.0 },
  { "z", 0.0 }
};
player.Properties["pilotMode"] = false;                    // Is player piloting?
```

### Transition: Interior → Space

**Process:**
1. Player is in interior room (e.g., bridge)
2. Player uses console/command to engage engines
3. Ship transitions to space mode
4. Player options:
   - Stay in interior (watch through viewport)
   - Move to other interior rooms
   - Take control (enter pilot mode)

**Example:**
```
Player: "engage engines"
System: "Engines engaged. Ship is now in space."
Player: "pilot"
System: "You take control of the ship."
```

### Transition: Space → Interior

**Process:**
1. Player is in space (piloting or on ship)
2. Ship approaches station/planet
3. Player docks/lands
4. Ship transitions to interior mode
5. Player enters interior room (docking bay, landing pad)

**Example:**
```
Player: "dock at station"
System: "Docking sequence initiated..."
System: "Docked. You are now in Station Docking Bay."
```

## Movement Commands

### Interior Movement (Traditional MUSH)

**Standard Commands:**
- `go north` / `north` / `n`
- `go south` / `south` / `s`
- `go east` / `east` / `e`
- `go west` / `west` / `w`
- `go up` / `up` / `u`
- `go down` / `down` / `d`
- `enter <room>`
- `exit`
- `open <door>`
- `close <door>`

**Ship-Specific:**
- `enter bridge`
- `enter engineering`
- `enter cargo bay`
- `go to <room>`

### Space Movement (New)

**Sublight Commands:**
- `thrust forward` / `thrust <direction>` (manual control)
- `thrust <x> <y> <z>` (vector thrust, manual)
- `set course [to] <target>` (automatic navigation)
- `enter orbit` / `enter equatorial orbit` / `enter polar orbit` / `enter geostationary orbit` (simplified orbital insertion)
- `match orbit [with] <target>` (match orbit with another object)
- `dock [at] <station>` (automatic docking)
- `land [on] <planet>` (automatic landing)
- `break orbit` (leave current orbit)

**FTL Commands:**
- `engage jump factor <n>`
- `plot course to <destination>`
- `engage course`
- `drop jump` / `disengage jump`
- `cancel course`

**Navigation:**
- `scan`
- `scan <target>`
- `show course`
- `show position`

## Ship-to-Ship Interaction

### Proximity Detection

Ships in same space zone can interact if close enough:

```csharp
double CalculateDistance(GameObject ship1, GameObject ship2) {
  var pos1 = GetPosition(ship1);
  var pos2 = GetPosition(ship2);
  return Vector3.Distance(pos1, pos2);
}

bool CanInteract(GameObject ship1, GameObject ship2) {
  double distance = CalculateDistance(ship1, ship2);
  double interactionRange = 1000.0; // meters (1 km)
  return distance <= interactionRange;
}
```

### Interaction Types

**Communication:**
- Radio/comm system
- Hail other ship
- Text chat
- Voice (future)

**Trading:**
- Cargo transfer
- Resource exchange
- Docking for trade

**Combat:**
- Weapons systems
- Shields
- Damage model
- Boarding (if close enough)

**Docking:**
- Connect ships
- Create temporary connection
- Transfer between ships
- Form fleet

## Space Object Types

### Planets

```csharp
planet.Properties["objectType"] = "planet";
planet.Properties["orbitalData"] = { ... };
planet.Properties["atmosphere"] = true;
planet.Properties["landable"] = true;
planet.Properties["gravity"] = 9.81; // m/s²
planet.Properties["interiorRooms"] = new BsonArray {
  "planet-surface-zone",
  "planet-base-1"
};
```

### Space Stations

```csharp
station.Properties["objectType"] = "station";
station.Properties["orbitalData"] = { ... };
station.Properties["dockingBays"] = new BsonArray { ... };
station.Properties["interiorRooms"] = new BsonArray {
  "station-docking-bay",
  "station-market",
  "station-bar"
};
```

### Ships

```csharp
ship.Properties["objectType"] = "ship";
ship.Properties["orbitalData"] = { ... }; // If in space
ship.Properties["interiorRooms"] = new BsonArray {
  "ship-bridge",
  "ship-engineering",
  "ship-cargo-bay"
};
ship.Properties["jumpDrive"] = { ... };
ship.Properties["shipData"] = { ... };
```

## Implementation Architecture

### Room Types

```csharp
// Base room (interior)
public class Room : GameObject 
{
  // Traditional MUSH room
  // Has exits
  // Contains objects/players
}

// Space zone (exterior 3D space)
public class SpaceZone : GameObject 
{
  // Has orbital mechanics
  // Contains ships, stations, planets
  // 3D boundaries
  // Gravity well
  // FTL restrictions
}

// Ship (can be both!)
public class Ship : GameObject
{
  // Has interior rooms (traditional)
  // Exists in space (3D position, orbital mechanics)
  // Can transition between modes
  // Has jump drive
  // Has ship systems
}
```

### Physics Engine

**Server-Side:**
- Calculate orbital mechanics
- Update positions/velocities
- Apply forces (gravity, thrust)
- Handle collisions (optional)
- Manage space zones

**Client-Side:**
- Render 3D space
- Show orbital paths
- Display ship positions
- Visual effects
- UI for navigation

## Real-Time Multiplayer Considerations

### Synchronization

- All players see same space state
- Server maintains authoritative positions
- Network updates sync ship positions
- Lag compensation for smooth movement
- Interpolation for smooth rendering

### Performance

- Update only active space zones
- Cull distant objects
- LOD (Level of Detail) for rendering
- Spatial partitioning for efficient queries
- Physics optimization (simplified for distant objects)

## Gameplay Examples

### Example 1: Player on Ship Bridge

1. Player is in "Bridge" room (interior)
2. Uses console to plot course to Proxima Centauri
3. Engages jump drive at Factor 5
4. Ship transitions to space mode
5. Player can:
   - Stay on bridge (watch through viewport)
   - Move to engineering (check systems)
   - Move to cargo bay (manage cargo)
   - Monitor navigation console

### Example 2: Player Piloting Ship

1. Player is in space (piloting ship)
2. Uses thrusters to change velocity
3. Orbital mechanics apply automatically
4. Approaches space station
5. Docks → enters "Station Docking Bay" room (interior)

### Example 3: Ship-to-Ship Interaction

1. Two ships in same space zone
2. Ships approach each other
3. Players can:
   - Hail other ship (communication)
   - Trade cargo
   - Engage in combat
   - Dock (if close enough, create connection)

## Future Enhancements

1. **Asteroid Fields**: Navigable, mineable
2. **Nebulae**: Visual effects, sensor interference
3. **Wormholes**: Fast travel between distant points
4. **Jump Gates**: Fixed FTL entry/exit points
5. **Space Weather**: Solar flares, radiation
6. **Gravity Slingshots**: Use planets for acceleration (automatic when using "set course")
7. **Lagrange Points**: Stable positions for stations
8. **Advanced Orbital Commands**: Optional manual control for advanced players
   - Custom altitude/inclination specification
   - Manual burn control
   - Trajectory visualization

## Related Documentation

- [ORBITAL_COMMANDS.md](./ORBITAL_COMMANDS.md) - Detailed command reference and implementation
