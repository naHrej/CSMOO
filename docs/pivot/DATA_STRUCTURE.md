# Data Structure: GameObject Properties for 3D

## Overview

GameObject properties (BsonDocument) store all 3D scene data, material definitions, texture generation parameters, and UI styling. Everything is defined in code and stored in the flexible property system.

## Core 3D Properties

### Position

```csharp
gameObject.Properties["position"] = new BsonDocument {
  { "x", 0.0 },
  { "y", 0.0 },
  { "z", 0.0 }
};
```

**Type**: `BsonDocument` with `x`, `y`, `z` (double)

### Rotation

```csharp
gameObject.Properties["rotation"] = new BsonDocument {
  { "x", 0.0 },
  { "y", 0.0 },
  { "z", 0.0 },
  { "w", 1.0 }
};
```

**Type**: `BsonDocument` with `x`, `y`, `z`, `w` (quaternion, double)

**Alternative**: Euler angles
```csharp
gameObject.Properties["rotation"] = new BsonDocument {
  { "x", 0.0 },  // Pitch
  { "y", 45.0 }, // Yaw
  { "z", 0.0 }   // Roll
};
```

### Scale

```csharp
gameObject.Properties["scale"] = new BsonDocument {
  { "x", 1.0 },
  { "y", 1.0 },
  { "z", 1.0 }
};
```

**Type**: `BsonDocument` with `x`, `y`, `z` (double)  
**Default**: `{ "x": 1, "y": 1, "z": 1 }`

### Model Path

```csharp
gameObject.Properties["modelPath"] = "/assets/models/bridge-console.stl";
```

**Type**: `string`  
**Format**: Path relative to server root

## Material Properties

### Basic Material

```csharp
gameObject.Properties["material"] = new BsonDocument {
  { "type", "MeshStandardMaterial" },  // or "MeshPhongMaterial", "MeshBasicMaterial"
  { "color", new BsonDocument { { "r", 0.1 }, { "g", 0.1 }, { "b", 0.15 } } },
  { "metalness", 0.8 },
  { "roughness", 0.2 },
  { "emissive", new BsonDocument { { "r", 0.05 }, { "g", 0.05 }, { "b", 0.1 } } },
  { "emissiveIntensity", 0.5 }
};
```

### Material Types

- **MeshStandardMaterial**: PBR material (recommended)
- **MeshPhongMaterial**: Phong shading
- **MeshBasicMaterial**: Unlit material
- **MeshLambertMaterial**: Lambert shading

### Material Properties

**color**: RGB color (0-1 range)
```csharp
{ "color", new BsonDocument { { "r", 0.1 }, { "g", 0.1 }, { "b", 0.15 } } }
```

**metalness**: 0.0 (non-metal) to 1.0 (metal)
```csharp
{ "metalness", 0.8 }
```

**roughness**: 0.0 (smooth) to 1.0 (rough)
```csharp
{ "roughness", 0.2 }
```

**emissive**: Emissive color (glow)
```csharp
{ "emissive", new BsonDocument { { "r", 0 }, { "g", 1 }, { "b", 0 } } }
```

**emissiveIntensity**: Emissive intensity
```csharp
{ "emissiveIntensity", 0.5 }
```

**transparent**: Enable transparency
```csharp
{ "transparent", true }
```

**opacity**: Opacity (0.0 to 1.0)
```csharp
{ "opacity", 0.8 }
```

## Texture Properties

See [TEXTURE_GENERATION.md](./TEXTURE_GENERATION.md) for detailed texture definition structure.

### Basic Texture

```csharp
gameObject.Properties["texture"] = new BsonDocument {
  { "type", "procedural" },
  { "generator", "metal" },
  { "parameters", new BsonDocument {
    { "baseColor", new BsonDocument { { "r", 0.3 }, { "g", 0.3 }, { "b", 0.35 } } },
    { "roughness", 0.2 },
    { "metalness", 0.9 }
  }},
  { "format", "png" },
  { "size", 512 }
};
```

## Room-Specific Properties

### Room Shape

**Initial Implementation: 3x3x3 Cubes**
```csharp
room.Properties["roomShape"] = new BsonDocument {
    { "type", "cube" },              // Shape type: "cube", "primitive", "mesh", "procedural"
    { "size", new BsonDocument {     // For cube: width, height, depth
        { "x", 3.0 },
        { "y", 3.0 },
        { "z", 3.0 }
    }},
    { "playerSpawn", new BsonDocument {  // Where player spawns in room
        { "x", 1.5 },
        { "y", 1.5 },
        { "z", 1.5 }
    }},
    { "bounds", new BsonDocument {    // Bounding box for collision
        { "min", new BsonDocument { { "x", 0.0 }, { "y", 0.0 }, { "z", 0.0 } } },
        { "max", new BsonDocument { { "x", 3.0 }, { "y", 3.0 }, { "z", 3.0 } } }
    }}
};
```

**Future: Custom Shapes**
- `type: "primitive"` - Built from primitives (like ship design)
- `type: "mesh"` - Custom STL mesh
- `type: "procedural"` - Algorithm-generated

See [ROOM_SHAPES.md](../game-design/ROOM_SHAPES.md) for detailed room shape documentation.

## Lighting Properties

### Room Lighting

```csharp
room.Properties["lighting"] = new BsonDocument {
  { "ambient", new BsonDocument { { "r", 0.3 }, { "g", 0.3 }, { "b", 0.3 } } },
  { "directional", new BsonDocument {
    { "direction", new BsonDocument { { "x", 0 }, { "y", -1 }, { "z", 0 } } },
    { "color", new BsonDocument { { "r", 1 }, { "g", 1 }, { "b", 1 } } },
    { "intensity", 1.0 }
  }},
  { "pointLights", new BsonArray {
    new BsonDocument {
      { "position", new BsonDocument { { "x", 5 }, { "y", 3 }, { "z", -2 } } },
      { "color", new BsonDocument { { "r", 1 }, { "g", 0.8 }, { "b", 0.6 } } },
      { "intensity", 0.5 },
      { "distance", 10 }
    }
  }}
};
```

## UI Styling Properties

### Room UI Style

```csharp
room.Properties["uiStyle"] = new BsonDocument {
  { "backgroundColor", "#1a1a2e" },
  { "textColor", "#0f3460" },
  { "fontFamily", "'Orbitron', sans-serif" },
  { "accentColor", "#00ff00" },
  { "borderColor", "#0f3460" }
};
```

**Properties:**
- **backgroundColor**: Background color (CSS color)
- **textColor**: Text color (CSS color)
- **fontFamily**: Font family (CSS font-family)
- **accentColor**: Accent color for highlights
- **borderColor**: Border color

## Complete Example

```csharp
// Create a bridge console
var console = CreateInstance("Item", "bridge-console");

// Basic properties
console.Properties["name"] = "Science Console";
console.Properties["description"] = "A sophisticated science console";

// 3D properties
console.Properties["modelPath"] = "/assets/models/console.stl";
console.Properties["position"] = new BsonDocument {
  { "x", 5.0 },
  { "y", 0.0 },
  { "z", -3.0 }
};
console.Properties["rotation"] = new BsonDocument {
  { "x", 0.0 },
  { "y", 45.0 },
  { "z", 0.0 }
};
console.Properties["scale"] = new BsonDocument {
  { "x", 1.0 },
  { "y", 1.0 },
  { "z", 1.0 }
};

// Material
console.Properties["material"] = new BsonDocument {
  { "type", "MeshStandardMaterial" },
  { "color", new BsonDocument { { "r", 0.1 }, { "g", 0.1 }, { "b", 0.15 } } },
  { "metalness", 0.9 },
  { "roughness", 0.1 },
  { "emissive", new BsonDocument { { "r", 0.05 }, { "g", 0.05 }, { "b", 0.1 } } }
};

// Texture
console.Properties["texture"] = new BsonDocument {
  { "type", "procedural" },
  { "generator", "composite" },
  { "parameters", new BsonDocument {
    { "layers", new BsonArray {
      new BsonDocument {
        { "generator", "material" },
        { "baseColor", new BsonDocument { { "r", 0.1 }, { "g", 0.1 }, { "b", 0.15 } } }
      },
      new BsonDocument {
        { "generator", "text" },
        { "text", "SCIENCE" },
        { "font", "Orbitron" },
        { "textColor", new BsonDocument { { "r", 0 }, { "g", 0.8 }, { "b", 1 } } }
      }
    }}
  }}
};

// Set location
console.Location = bridgeRoom;
```

## Helper Methods

### Get Position

```csharp
public Vector3 GetPosition(GameObject obj)
{
  if (!obj.Properties.ContainsKey("position"))
    return Vector3.Zero;
    
  var pos = obj.Properties["position"].AsBsonDocument;
  return new Vector3(
    pos["x"].AsDouble,
    pos["y"].AsDouble,
    pos["z"].AsDouble
  );
}
```

### Set Position

```csharp
public void SetPosition(GameObject obj, Vector3 position)
{
  obj.Properties["position"] = new BsonDocument {
    { "x", position.X },
    { "y", position.Y },
    { "z", position.Z }
  };
}
```

## Scene Description Format

When serialized to JSON for the API:

```json
{
  "id": "console-1",
  "name": "Science Console",
  "modelPath": "/assets/models/console.stl",
  "position": { "x": 5, "y": 0, "z": -3 },
  "rotation": { "x": 0, "y": 45, "z": 0 },
  "scale": { "x": 1, "y": 1, "z": 1 },
  "material": {
    "type": "MeshStandardMaterial",
    "color": { "r": 0.1, "g": 0.1, "b": 0.15 },
    "metalness": 0.9,
    "roughness": 0.1,
    "emissive": { "r": 0.05, "g": 0.05, "b": 0.1 }
  },
  "texture": {
    "type": "procedural",
    "generator": "composite",
    "parameters": { ... }
  }
}
```

## Validation

Properties should be validated when set:

- **position/rotation/scale**: Ensure numeric values
- **modelPath**: Ensure file exists (or will exist)
- **material**: Ensure valid material type and properties
- **texture**: Ensure valid generator and parameters

## Defaults

If properties are missing, use defaults:

- **position**: `{ "x": 0, "y": 0, "z": 0 }`
- **rotation**: `{ "x": 0, "y": 0, "z": 0, "w": 1 }` (identity quaternion)
- **scale**: `{ "x": 1, "y": 1, "z": 1 }`
- **material**: Default material (gray, non-metallic)
- **texture**: No texture (use material color only)
