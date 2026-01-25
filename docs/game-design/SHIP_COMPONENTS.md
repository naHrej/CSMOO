# Ship Components and Physics System

## Overview

All ship subsystems are physical components with real physics properties (mass, density, volume, etc.) that directly influence ship movement and behavior. Balance is maintained through abstract resource systems (power, processing, etc.) while physics provides realism.

## Component Philosophy

### Physical Components

**Core Principle:**
- Every subsystem is a physical object in 3D space
- Components have real physics properties
- Physics properties directly affect ship performance
- Components can be damaged, destroyed, or removed
- Visual representation matches physical presence

**Examples:**
- Engine = Physical thruster component
- Shield Generator = Physical device on ship
- Power Generator = Physical reactor component
- Weapon = Physical turret/mount
- Sensor Array = Physical antenna/dish
- Life Support = Physical module

## Component Properties

### Physics Properties

Every component has physics properties that contribute to ship behavior:

```csharp
component.Properties["physics"] = new BsonDocument {
  { "mass", 100.0 },              // kg
  { "density", 2700.0 },          // kg/m³ (aluminum-like)
  { "volume", 0.037 },            // m³ (calculated or defined)
  { "centerOfMass", new BsonDocument {
    { "x", 0.0 },
    { "y", 0.0 },
    { "z", 0.0 }
  }},
  { "momentOfInertia", new BsonDocument {
    { "xx", 1.0 },
    { "yy", 1.0 },
    { "zz", 1.0 }
  }},
  { "dragCoefficient", 0.5 },     // Air resistance (for atmosphere)
  { "structuralIntegrity", 100.0 } // % (damage model)
};
```

### Component Types

**Engines:**
```csharp
component.Properties["componentType"] = "engine";
component.Properties["engineData"] = new BsonDocument {
  { "thrust", 5000.0 },           // Newtons
  { "thrustVector", new BsonDocument {
    { "x", 0.0 },
    { "y", 0.0 },
    { "z", -1.0 }  // Direction of thrust
  }},
  { "specificImpulse", 300.0 },    // seconds (fuel efficiency)
  { "maxThrottle", 1.0 },          // 0.0 to 1.0
  { "throttleResponse", 0.1 },     // seconds (how fast throttle changes)
  { "heatGeneration", 1000.0 },   // Watts (heat produced)
  { "coolingRequired", 500.0 }    // Watts (cooling needed)
};
```

**Shield Generators:**
```csharp
component.Properties["componentType"] = "shield";
component.Properties["shieldData"] = new BsonDocument {
  { "maxCapacity", 10000.0 },      // Joules (shield strength)
  { "rechargeRate", 100.0 },      // Joules/second
  { "coverage", 360.0 },          // degrees (full sphere = 360)
  { "efficiency", 0.85 },        // 0.0-1.0 (power efficiency)
  { "powerConsumption", 5000.0 }, // Watts
  { "frequency", 1.0e9 }          // Hz (shield frequency, for tuning)
};
```

**Power Generators:**
```csharp
component.Properties["componentType"] = "power";
component.Properties["powerData"] = new BsonDocument {
  { "maxOutput", 10000.0 },       // Watts
  { "efficiency", 0.90 },        // 0.0-1.0
  { "fuelConsumption", 0.1 },    // % per hour at max output
  { "heatGeneration", 2000.0 },  // Watts (waste heat)
  { "coolingRequired", 1500.0 }, // Watts (cooling needed)
  { "fuelType", "quantum" },     // Fuel type required
  { "fuelCapacity", 100.0 }      // % (fuel storage)
};
```

**Weapons:**
```csharp
component.Properties["componentType"] = "weapon";
component.Properties["weaponData"] = new BsonDocument {
  { "weaponType", "turret" },     // turret, beam, missile, railgun
  { "damage", 100.0 },            // Damage per shot
  { "fireRate", 1.0 },            // Shots per second
  { "range", 10000.0 },           // meters
  { "powerConsumption", 2000.0 }, // Watts per shot
  { "ammoCapacity", 100 },       // Shots (if applicable)
  { "reloadTime", 5.0 },         // seconds
  { "trackingSpeed", 90.0 },     // degrees/second
  { "arcOfFire", 180.0 }         // degrees (firing arc)
};
```

**Sensors:**
```csharp
component.Properties["componentType"] = "sensor";
component.Properties["sensorData"] = new BsonDocument {
  { "range", 100000.0 },         // meters
  { "resolution", 1.0 },          // meters (detection resolution)
  { "scanSpeed", 10.0 },         // scans per second
  { "powerConsumption", 500.0 }, // Watts
  { "processingRequired", 10.0 }, // Processing units
  { "sensorType", "active" }     // active, passive, both
};
```

**Life Support:**
```csharp
component.Properties["componentType"] = "lifeSupport";
component.Properties["lifeSupportData"] = new BsonDocument {
  { "crewCapacity", 10 },        // Number of crew supported
  { "oxygenGeneration", 100.0 }, // liters/hour
  { "oxygenStorage", 1000.0 },  // liters
  { "atmosphereControl", true },
  { "temperatureControl", true },
  { "powerConsumption", 200.0 }, // Watts
  { "efficiency", 0.95 }
};
```

**Processing Units:**
```csharp
component.Properties["componentType"] = "processor";
component.Properties["processorData"] = new BsonDocument {
  { "processingPower", 1000.0 }, // Processing units
  { "powerConsumption", 100.0 }, // Watts
  { "heatGeneration", 50.0 },    // Watts
  { "coolingRequired", 30.0 },   // Watts
  { "efficiency", 0.90 }
};
```

**Cooling Systems:**
```csharp
component.Properties["componentType"] = "cooling";
component.Properties["coolingData"] = new BsonDocument {
  { "coolingCapacity", 5000.0 }, // Watts (heat removal)
  { "powerConsumption", 500.0 }, // Watts (to run cooling)
  { "efficiency", 0.80 },
  { "coolantType", "liquid" },   // liquid, gas, radiative
  { "coolantCapacity", 100.0 }   // % (coolant level)
};
```

**Jump Drive:**
```csharp
component.Properties["componentType"] = "jumpDrive";
component.Properties["jumpDriveData"] = new BsonDocument {
  { "maxFactor", 9.0 },          // Maximum jump factor
  { "efficiency", 0.85 },       // Drive efficiency
  { "powerConsumption", 1000000.0 }, // Watts (at factor 1)
  { "fuelConsumption", 0.1 },    // % per hour at factor 1
  { "cooldownDuration", 60.0 },  // seconds
  { "heatGeneration", 50000.0 }, // Watts (waste heat)
  { "coolingRequired", 40000.0 } // Watts (cooling needed)
};
```

## Ship Physics Calculation

### Total Mass

```csharp
double CalculateShipMass(GameObject ship) {
  double totalMass = 0.0;
  foreach (var component in ship.primitives) {
    totalMass += component.physics.mass;
  }
  return totalMass;
}
```

### Center of Mass

```csharp
Vector3 CalculateCenterOfMass(GameObject ship) {
  double totalMass = 0.0;
  Vector3 weightedPosition = Vector3.Zero;
  
  foreach (var component in ship.primitives) {
    double mass = component.physics.mass;
    Vector3 position = GetComponentPosition(component);
    totalMass += mass;
    weightedPosition += position * mass;
  }
  
  return weightedPosition / totalMass;
}
```

### Moment of Inertia

```csharp
Matrix3 CalculateMomentOfInertia(GameObject ship) {
  Vector3 centerOfMass = CalculateCenterOfMass(ship);
  Matrix3 inertia = Matrix3.Zero;
  
  foreach (var component in ship.primitives) {
    double mass = component.physics.mass;
    Vector3 position = GetComponentPosition(component) - centerOfMass;
    Matrix3 componentInertia = component.physics.momentOfInertia;
    
    // Parallel axis theorem
    double r2 = position.LengthSquared();
    Matrix3 offset = new Matrix3(
      r2 - position.X * position.X, -position.X * position.Y, -position.X * position.Z,
      -position.Y * position.X, r2 - position.Y * position.Y, -position.Y * position.Z,
      -position.Z * position.X, -position.Z * position.Y, r2 - position.Z * position.Z
    );
    
    inertia += componentInertia + offset * mass;
  }
  
  return inertia;
}
```

### Total Thrust

```csharp
Vector3 CalculateTotalThrust(GameObject ship, double throttle) {
  Vector3 totalThrust = Vector3.Zero;
  
  foreach (var component in ship.primitives) {
    if (component.componentType == "engine") {
      Vector3 thrustVector = component.engineData.thrustVector;
      double thrust = component.engineData.thrust * throttle;
      totalThrust += thrustVector * thrust;
    }
  }
  
  return totalThrust;
}
```

### Ship Movement

```csharp
void UpdateShipPhysics(GameObject ship, double deltaTime) {
  // Calculate forces
  Vector3 thrust = CalculateTotalThrust(ship, ship.currentThrottle);
  Vector3 gravity = CalculateGravity(ship.position);
  Vector3 drag = CalculateDrag(ship.velocity, ship.components);
  
  Vector3 totalForce = thrust + gravity + drag;
  
  // Update velocity (F = ma, so a = F/m)
  double mass = CalculateShipMass(ship);
  Vector3 acceleration = totalForce / mass;
  ship.velocity += acceleration * deltaTime;
  
  // Update position
  ship.position += ship.velocity * deltaTime;
  
  // Update rotation (torque from off-center thrust)
  Vector3 centerOfMass = CalculateCenterOfMass(ship);
  Vector3 torque = CalculateTorque(ship, centerOfMass);
  Matrix3 inertia = CalculateMomentOfInertia(ship);
  Vector3 angularAcceleration = inertia.Inverse() * torque;
  ship.angularVelocity += angularAcceleration * deltaTime;
  ship.rotation = UpdateRotation(ship.rotation, ship.angularVelocity, deltaTime);
}
```

## Balance Systems

### Power System

**Power Generation:**
```csharp
double CalculatePowerGeneration(GameObject ship) {
  double totalPower = 0.0;
  foreach (var component in ship.primitives) {
    if (component.componentType == "power") {
      totalPower += component.powerData.maxOutput * component.powerData.efficiency;
    }
  }
  return totalPower;
}
```

**Power Consumption:**
```csharp
double CalculatePowerConsumption(GameObject ship) {
  double totalConsumption = 0.0;
  
  foreach (var component in ship.primitives) {
    double consumption = 0.0;
    
    switch (component.componentType) {
      case "engine":
        consumption = component.engineData.powerConsumption * ship.currentThrottle;
        break;
      case "shield":
        if (component.shieldData.active) {
          consumption = component.shieldData.powerConsumption;
        }
        break;
      case "weapon":
        if (component.weaponData.firing) {
          consumption = component.weaponData.powerConsumption * component.weaponData.fireRate;
        }
        break;
      case "sensor":
        consumption = component.sensorData.powerConsumption;
        break;
      case "lifeSupport":
        consumption = component.lifeSupportData.powerConsumption;
        break;
      case "processor":
        consumption = component.processorData.powerConsumption;
        break;
      case "cooling":
        consumption = component.coolingData.powerConsumption;
        break;
      case "jumpDrive":
        if (component.jumpDriveData.engaged) {
          double factor = component.jumpDriveData.currentFactor;
          consumption = CalculateJumpDrivePower(factor, component.jumpDriveData);
        }
        break;
    }
    
    totalConsumption += consumption;
  }
  
  return totalConsumption;
}
```

**Power Balance:**
```csharp
bool CheckPowerBalance(GameObject ship) {
  double generation = CalculatePowerGeneration(ship);
  double consumption = CalculatePowerConsumption(ship);
  return generation >= consumption;
}

// If power insufficient, systems shut down in priority order
void ManagePower(GameObject ship) {
  double generation = CalculatePowerGeneration(ship);
  double consumption = CalculatePowerConsumption(ship);
  
  if (consumption > generation) {
    double deficit = consumption - generation;
    ShutdownSystemsByPriority(ship, deficit);
  }
}
```

### Processing System

**Processing Generation:**
```csharp
double CalculateProcessingPower(GameObject ship) {
  double totalProcessing = 0.0;
  foreach (var component in ship.primitives) {
    if (component.componentType == "processor") {
      totalProcessing += component.processorData.processingPower;
    }
  }
  return totalProcessing;
}
```

**Processing Consumption:**
```csharp
double CalculateProcessingConsumption(GameObject ship) {
  double totalConsumption = 0.0;
  
  foreach (var component in ship.primitives) {
    if (component.componentType == "sensor") {
      totalConsumption += component.sensorData.processingRequired;
    }
    // Other systems may require processing
  }
  
  return totalConsumption;
}
```

**Processing Balance:**
- Sensors require processing to function
- Insufficient processing = reduced sensor range/accuracy
- Can prioritize which sensors get processing

### Heat Management

**Heat Generation:**
```csharp
double CalculateHeatGeneration(GameObject ship) {
  double totalHeat = 0.0;
  
  foreach (var component in ship.primitives) {
    double heat = 0.0;
    
    switch (component.componentType) {
      case "engine":
        heat = component.engineData.heatGeneration * ship.currentThrottle;
        break;
      case "power":
        heat = component.powerData.heatGeneration * (component.powerData.currentOutput / component.powerData.maxOutput);
        break;
      case "weapon":
        if (component.weaponData.firing) {
          heat = component.weaponData.heatGeneration * component.weaponData.fireRate;
        }
        break;
      case "processor":
        heat = component.processorData.heatGeneration;
        break;
      case "jumpDrive":
        if (component.jumpDriveData.engaged) {
          heat = component.jumpDriveData.heatGeneration;
        }
        break;
    }
    
    totalHeat += heat;
  }
  
  return totalHeat;
}
```

**Cooling Capacity:**
```csharp
double CalculateCoolingCapacity(GameObject ship) {
  double totalCooling = 0.0;
  foreach (var component in ship.primitives) {
    if (component.componentType == "cooling") {
      totalCooling += component.coolingData.coolingCapacity * component.coolingData.efficiency;
    }
  }
  return totalCooling;
}
```

**Heat Management:**
```csharp
void UpdateHeat(GameObject ship, double deltaTime) {
  double heatGen = CalculateHeatGeneration(ship);
  double cooling = CalculateCoolingCapacity(ship);
  double netHeat = heatGen - cooling;
  
  ship.heatLevel += netHeat * deltaTime;
  
  // Overheating effects
  if (ship.heatLevel > ship.maxHeatLevel) {
    ApplyOverheatEffects(ship);
  }
}
```

**Overheat Effects:**
- Reduced efficiency
- System failures
- Component damage
- Emergency shutdowns

## Component Placement Effects

### Thrust Balance

**Off-Center Thrust:**
- Engines not aligned with center of mass create torque
- Ship will rotate when thrusting
- Must balance engine placement or use reaction control

**Example:**
```csharp
Vector3 CalculateTorque(GameObject ship, Vector3 centerOfMass) {
  Vector3 torque = Vector3.Zero;
  
  foreach (var component in ship.primitives) {
    if (component.componentType == "engine") {
      Vector3 enginePosition = GetComponentPosition(component);
      Vector3 offset = enginePosition - centerOfMass;
      Vector3 thrust = component.engineData.thrustVector * component.engineData.thrust;
      torque += Vector3.Cross(offset, thrust);
    }
  }
  
  return torque;
}
```

### Mass Distribution

**Effects:**
- Affects moment of inertia
- Affects rotation speed
- Affects stability
- Affects fuel efficiency

**Optimization:**
- Place heavy components near center of mass
- Distribute mass evenly for stability
- Consider rotation axes

### Shield Coverage

**Physical Placement Matters:**
- Shield generators have coverage arcs
- Must place generators to cover all angles
- Gaps in coverage = vulnerable areas
- Overlapping coverage = redundancy

## Component Interaction

### Dependencies

**Power Dependencies:**
- All systems require power
- Power generators must supply all consumers
- Power distribution can be damaged

**Cooling Dependencies:**
- Heat-generating systems need cooling
- Insufficient cooling = overheating
- Cooling systems need power

**Processing Dependencies:**
- Sensors need processing
- Advanced systems may need processing
- Processing can be overloaded

### Synergies

**Multiple Engines:**
- More engines = more thrust
- Redundancy (if one fails)
- Better thrust vectoring

**Multiple Shield Generators:**
- Overlapping coverage
- Redundancy
- Higher total capacity

**Multiple Power Generators:**
- More total power
- Redundancy
- Can run at lower load (more efficient)

## Component Damage Model

### Structural Integrity

```csharp
component.Properties["structuralIntegrity"] = 100.0; // %

void ApplyDamage(Component component, double damage) {
  component.structuralIntegrity -= damage;
  
  if (component.structuralIntegrity <= 0) {
    component.destroyed = true;
    component.functional = false;
  } else if (component.structuralIntegrity < 50) {
    component.efficiency *= 0.5; // Reduced efficiency
  }
}
```

### Component Failure

**Effects:**
- Destroyed component = no function
- Damaged component = reduced efficiency
- Can affect other components (power loss, etc.)

**Repair:**
- Requires resources
- Takes time
- May require specialized tools/crew

## Design Constraints

### Physics Constraints

**Mass Limits:**
- More mass = slower acceleration
- More mass = more fuel consumption
- More mass = harder to maneuver

**Thrust-to-Mass Ratio:**
- Minimum ratio for useful acceleration
- Affects maneuverability
- Affects escape velocity

**Power Requirements:**
- Must generate enough power
- Must have enough cooling
- Must have enough processing

### Balance Constraints

**Resource Management:**
- Power generation vs consumption
- Heat generation vs cooling
- Processing generation vs consumption
- Fuel capacity vs consumption

**Trade-offs:**
- More engines = more thrust but more mass/power
- More shields = more defense but more power
- More weapons = more offense but more power/mass
- More power = can run more systems but more mass/heat

## Example: Balanced Ship Design

**Small Fighter:**
- Light hull (low mass)
- 2x small engines (good thrust-to-mass)
- 1x small power generator (sufficient for systems)
- 1x small shield generator
- 2x small weapons
- 1x basic sensor
- 1x basic cooling
- Result: Fast, agile, but limited systems

**Heavy Freighter:**
- Large hull (high mass, high cargo)
- 4x medium engines (adequate for mass)
- 2x large power generators (for all systems)
- 2x medium shield generators (coverage)
- 1x small weapon (defensive)
- 1x advanced sensor
- 2x large cooling systems
- Result: Slow, durable, high capacity

## Implementation Notes

### Server-Side

- Calculate physics properties from components
- Update ship physics every tick
- Manage power/heat/processing balance
- Handle component damage
- Validate ship designs

### Client-Side

- Render components in 3D
- Show power/heat/processing status
- Display component health
- Visual effects for active systems

### Database

- Store component properties
- Store ship designs
- Track component damage
- Log system status
