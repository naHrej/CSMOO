# Orbital Commands: Simplified Interface

## Overview

Players interact with orbital mechanics through high-level, intuitive commands. The system handles all complex physics calculations automatically, allowing players to focus on gameplay rather than orbital mechanics.

## Design Philosophy

**Not Kerbal Space Program, More Like Star Trek**

- Players give bridge-style commands
- System calculates and executes maneuvers
- No manual trajectory plotting
- No delta-V calculations
- No burn timing
- Automatic orbit maintenance

## Command Reference

### Enter Orbit

**Command:** `enter orbit [type] [around <target>]`

**Orbit Types:**
- `equatorial` - Orbit in equatorial plane (0° inclination)
- `polar` - Orbit over poles (90° inclination)
- `geostationary` - Synchronous orbit matching body rotation
- `custom` - Use current position/velocity (if already in orbit)

**Examples:**
```
enter orbit equatorial
enter orbit polar around Mars
enter geostationary orbit
enter orbit around station
```

**What Happens:**
1. System identifies target body (current parent or specified)
2. Calculates orbital parameters for requested orbit type
3. Calculates required velocity
4. Executes automatic orbital insertion burn
5. Maintains orbit automatically

**Response:**
```
"Calculating orbital insertion..."
"Orbital insertion burn in 3... 2... 1..."
"Burn complete. We are now in equatorial orbit at 400 km altitude."
"Orbital period: 90 minutes. Orbit is stable."
```

### Match Orbit

**Command:** `match orbit [with] <target>`

**Examples:**
```
match orbit with station
match orbit station
match orbit with player-123
```

**What Happens:**
1. System identifies target object
2. Gets target's orbital parameters
3. Calculates matching orbit (same altitude, period, phase)
4. Executes orbital insertion
5. Matches relative velocity (0 m/s relative to target)

**Response:**
```
"Target identified: Space Station Alpha"
"Calculating matching orbit..."
"Orbital insertion complete. We are now matching orbit with Space Station Alpha."
"Relative velocity: 0 m/s. Ready for docking."
```

### Break Orbit

**Command:** `break orbit`

**What Happens:**
1. System calculates escape velocity
2. Executes burn to escape orbit
3. Ship enters free flight (no longer in orbit)

**Response:**
```
"Breaking orbit..."
"Escape burn complete. We are now in free flight."
```

### Set Course

**Command:** `set course [to] <target>`

**Examples:**
```
set course to Mars
set course station
set course to player-123
```

**What Happens:**
1. System calculates intercept trajectory
2. Executes automatic navigation
3. Ship follows calculated course
4. Adjusts as needed (automatic)

**Response:**
```
"Course plotted to Mars. Estimated arrival: 45 minutes."
"Engaging engines..."
```

### Dock

**Command:** `dock [at] <target>`

**Examples:**
```
dock at station
dock station
dock
```

**What Happens:**
1. System identifies nearest docking port
2. Calculates approach trajectory
3. Matches velocity with target
4. Executes docking maneuver
5. Transitions to interior room

**Response:**
```
"Initiating docking sequence..."
"Approaching docking port..."
"Docking complete. You are now in Station Docking Bay."
```

### Land

**Command:** `land [on] <target>`

**Examples:**
```
land on Mars
land Mars
land
```

**What Happens:**
1. System identifies landing site
2. Calculates descent trajectory
3. Executes landing burn
4. Lands at designated location
5. Transitions to surface/landing pad room

**Response:**
```
"Calculating landing trajectory..."
"Beginning descent..."
"Landing complete. You are now on the surface."
```

## Implementation Details

### Orbital Insertion Algorithm

```csharp
public class OrbitalCommandHandler
{
    public void EnterOrbit(GameObject ship, string orbitType, GameObject targetBody = null)
    {
        // Determine target body
        var parentBody = targetBody ?? GetParentBody(ship);
        
        // Calculate orbital parameters
        var orbitParams = CalculateOrbitParameters(parentBody, orbitType, ship.position);
        
        // Calculate required velocity
        var requiredVelocity = CalculateOrbitalVelocity(
            ship.position,
            parentBody,
            orbitParams
        );
        
        // Execute insertion
        ExecuteOrbitalInsertion(ship, requiredVelocity, orbitParams);
    }
    
    private OrbitalParameters CalculateOrbitParameters(
        GameObject parentBody,
        string orbitType,
        Vector3 currentPosition)
    {
        double parentRadius = parentBody.Properties["radius"].AsDouble;
        double parentMass = parentBody.Properties["mass"].AsDouble;
        double G = 6.67430e-11;
        double altitude = CalculateAltitude(currentPosition, parentBody);
        
        switch (orbitType.ToLower())
        {
            case "equatorial":
                return new OrbitalParameters {
                    altitude = altitude,
                    inclination = 0.0,
                    eccentricity = 0.0,
                    semiMajorAxis = parentRadius + altitude
                };
                
            case "polar":
                return new OrbitalParameters {
                    altitude = altitude,
                    inclination = 90.0,
                    eccentricity = 0.0,
                    semiMajorAxis = parentRadius + altitude
                };
                
            case "geostationary":
                double rotationalPeriod = parentBody.Properties["rotationalPeriod"].AsDouble;
                double geostationaryRadius = Math.Pow(
                    (G * parentMass * rotationalPeriod * rotationalPeriod) / (4 * Math.PI * Math.PI),
                    1.0 / 3.0
                );
                
                return new OrbitalParameters {
                    altitude = geostationaryRadius - parentRadius,
                    inclination = 0.0,
                    eccentricity = 0.0,
                    semiMajorAxis = geostationaryRadius,
                    orbitalPeriod = rotationalPeriod
                };
                
            default:
                // Use current state
                return CalculateOrbitFromState(currentPosition, ship.velocity, parentBody);
        }
    }
    
    private Vector3 CalculateOrbitalVelocity(
        Vector3 position,
        GameObject parentBody,
        OrbitalParameters orbitParams)
    {
        double G = 6.67430e-11;
        double parentMass = parentBody.Properties["mass"].AsDouble;
        double r = orbitParams.semiMajorAxis;
        
        // Circular orbit velocity: v = sqrt(GM / r)
        double speed = Math.Sqrt((G * parentMass) / r);
        
        // Calculate direction (tangent to orbit)
        Vector3 direction = CalculateOrbitalDirection(position, parentBody.position, orbitParams);
        
        return direction * speed;
    }
    
    private void ExecuteOrbitalInsertion(
        GameObject ship,
        Vector3 targetVelocity,
        OrbitalParameters orbitParams)
    {
        Vector3 currentVelocity = ship.Properties["orbitalData"]["velocity"];
        Vector3 deltaV = targetVelocity - currentVelocity;
        
        // Calculate burn
        double deltaVMagnitude = deltaV.Length();
        double thrust = CalculateTotalThrust(ship);
        double mass = CalculateShipMass(ship);
        double burnTime = (mass * deltaVMagnitude) / thrust;
        
        // Execute burn
        StartOrbitalBurn(ship, deltaV.Normalize(), burnTime);
        
        // Update orbit info
        ship.Properties["orbitInfo"] = new BsonDocument {
            { "orbiting", GetParentBody(ship).Name },
            { "orbitType", orbitParams.type },
            { "altitude", orbitParams.altitude },
            { "orbitalPeriod", orbitParams.period },
            { "inclination", orbitParams.inclination },
            { "status", "stable" }
        };
    }
}
```

### Automatic Orbit Maintenance

```csharp
public class OrbitMaintainer
{
    public void UpdateOrbit(GameObject ship, double deltaTime)
    {
        if (ship.Properties["orbitInfo"]["status"] != "stable")
            return;
        
        // Calculate required velocity for current orbit
        var orbitInfo = ship.Properties["orbitInfo"];
        var parentBody = GetParentBody(ship);
        var orbitParams = GetOrbitalParameters(ship);
        
        Vector3 requiredVelocity = CalculateOrbitalVelocity(
            ship.position,
            parentBody,
            orbitParams
        );
        
        // Check for drift
        Vector3 currentVelocity = ship.Properties["orbitalData"]["velocity"];
        Vector3 velocityError = requiredVelocity - currentVelocity;
        
        // If drift is significant, apply correction
        if (velocityError.Length() > 1.0) // 1 m/s threshold
        {
            ApplyOrbitalCorrection(ship, velocityError);
        }
    }
    
    private void ApplyOrbitalCorrection(GameObject ship, Vector3 correction)
    {
        // Small automatic correction burn
        double correctionMagnitude = correction.Length();
        double thrust = CalculateTotalThrust(ship);
        double mass = CalculateShipMass(ship);
        double burnTime = (mass * correctionMagnitude) / thrust;
        
        // Execute small correction burn
        StartOrbitalBurn(ship, correction.Normalize(), burnTime * 0.1); // 10% of calculated time
    }
}
```

## User Experience

### Command Flow

1. **Player gives command**: "Enter equatorial orbit"
2. **System acknowledges**: "Calculating orbital insertion..."
3. **System executes**: Automatic burn
4. **System confirms**: "We are now in equatorial orbit."
5. **System maintains**: Orbit automatically maintained

### No Manual Calculations

Players never need to:
- Calculate delta-V
- Plot trajectories
- Time burns
- Calculate orbital parameters
- Understand orbital mechanics

Players just give commands, system handles physics.

## Advanced Options (Optional)

For players who want more control (future enhancement):

- `enter orbit custom altitude <n> km` - Specify altitude
- `enter orbit custom inclination <n> degrees` - Specify inclination
- `manual burn <direction> <duration>` - Manual control mode

But these are optional - default is always simplified.
