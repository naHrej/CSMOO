# Procedural Texture Generation

## Overview

Textures are generated procedurally on the server based on definitions stored in GameObject properties. No texture files are stored; everything is generated on-demand.

## Philosophy

- **Code-Driven**: Texture definitions in GameObject properties
- **On-Demand**: Generate when requested, cache by parameters
- **Format Negotiation**: Client requests preferred format (WebP, PNG, JPEG)
- **Flexible**: Infinite variations through parameter changes
- **Hot-Reloadable**: Change properties → regenerate → client updates

## Texture Definition Structure

### Basic Structure

```csharp
gameObject.Properties["texture"] = new BsonDocument {
  { "type", "procedural" },
  { "generator", "metal" },  // or "fabric", "plastic", "noise", "text", etc.
  { "parameters", new BsonDocument {
    // Generator-specific parameters
  }},
  { "format", "png" },  // Default format (client can override)
  { "size", 512 }       // Default size (client can override)
};
```

### Generator Types

#### 1. Material-Based (Simple)

Solid color with material properties:

```csharp
{
  "type": "procedural",
  "generator": "material",
  "parameters": {
    "color": { "r": 0.2, "g": 0.4, "b": 0.8 },
    "roughness": 0.7,
    "metalness": 0.1,
    "noise": 0.05  // Subtle variation
  }
}
```

#### 2. Pattern-Based

Predefined patterns (brushed metal, fabric weave, circuit board, etc.):

```csharp
{
  "type": "procedural",
  "generator": "pattern",
  "parameters": {
    "pattern": "brushed",  // or "weave", "circuit", "grid", etc.
    "baseColor": { "r": 0.1, "g": 0.1, "b": 0.15 },
    "patternColor": { "r": 0.15, "g": 0.15, "b": 0.2 },
    "scale": 0.5,
    "rotation": 45
  }
}
```

#### 3. Noise-Based (Procedural)

Perlin/Simplex noise for organic textures:

```csharp
{
  "type": "procedural",
  "generator": "noise",
  "parameters": {
    "noiseType": "perlin",  // or "simplex"
    "scale": 0.1,
    "octaves": 4,
    "color1": { "r": 0.8, "g": 0.7, "b": 0.6 },
    "color2": { "r": 0.6, "g": 0.5, "b": 0.4 }
  }
}
```

#### 4. Text-Based

For signs, displays, UI elements:

```csharp
{
  "type": "procedural",
  "generator": "text",
  "parameters": {
    "text": "BRIDGE",
    "font": "Orbitron",
    "fontSize": 48,
    "textColor": { "r": 1, "g": 0.8, "b": 0 },
    "backgroundColor": { "r": 0.1, "g": 0.1, "b": 0.15 },
    "border": true,
    "borderColor": { "r": 0.2, "g": 0.2, "b": 0.3 }
  }
}
```

#### 5. Composite

Combine multiple generators:

```csharp
{
  "type": "procedural",
  "generator": "composite",
  "parameters": {
    "layers": [
      {
        "generator": "noise",
        "opacity": 1.0,
        "blendMode": "normal"
      },
      {
        "generator": "pattern",
        "pattern": "circuit",
        "opacity": 0.5,
        "blendMode": "multiply"
      }
    ]
  }
}
```

## API Endpoint

### GET /api/texture/{objectId}

**Query Parameters:**
- `format` (optional): `webp`, `png`, `jpg` (default: `png`)
- `size` (optional): Texture resolution (default: `512`)

**Response:**
- Content-Type: `image/png`, `image/webp`, or `image/jpeg`
- Body: Image bytes

**Example:**
```
GET /api/texture/console-1?format=webp&size=1024
```

## Implementation

### Texture Generator Service

```csharp
public class TextureGenerator
{
    public byte[] GenerateTexture(
        BsonDocument textureDef, 
        string format = "png", 
        int size = 512)
    {
        var generator = textureDef["generator"]?.AsString ?? "material";
        var parameters = textureDef["parameters"]?.AsBsonDocument ?? new BsonDocument();
        
        return generator switch
        {
            "material" => GenerateMaterialTexture(parameters, format, size),
            "pattern" => GeneratePatternTexture(parameters, format, size),
            "noise" => GenerateNoiseTexture(parameters, format, size),
            "text" => GenerateTextTexture(parameters, format, size),
            "composite" => GenerateCompositeTexture(parameters, format, size),
            _ => GenerateMaterialTexture(parameters, format, size)
        };
    }
    
    private byte[] GenerateMaterialTexture(BsonDocument parameters, string format, int size)
    {
        // Generate solid color texture with optional noise
        // Use SkiaSharp or ImageSharp
    }
    
    // ... other generator methods
}
```

### Caching Strategy

```csharp
// Cache key: hash of texture definition + format + size
private string GetCacheKey(BsonDocument textureDef, string format, int size)
{
    var json = textureDef.ToJson();
    var hash = ComputeHash(json + format + size);
    return $"texture_{hash}";
}

public byte[] GetOrGenerateTexture(BsonDocument textureDef, string format, int size)
{
    var cacheKey = GetCacheKey(textureDef, format, size);
    
    // Check cache
    if (_textureCache.TryGetValue(cacheKey, out var cached))
        return cached;
    
    // Generate
    var generated = GenerateTexture(textureDef, format, size);
    
    // Cache
    _textureCache[cacheKey] = generated;
    
    return generated;
}
```

## Library Options

### Option 1: SkiaSharp (Recommended)

**Pros:**
- Cross-platform (Windows, Linux, macOS)
- Good performance
- Supports PNG, WebP, JPEG output
- Modern API
- Good for procedural generation

**Cons:**
- Larger dependency

### Option 2: ImageSharp

**Pros:**
- Cross-platform
- Good performance
- Modern API
- Pure .NET (no native dependencies)

**Cons:**
- Less mature than SkiaSharp

### Option 3: System.Drawing (Not Recommended)

**Pros:**
- Simple API
- Built-in to .NET

**Cons:**
- Windows-only
- Legacy API

## Noise Library

For procedural noise generation:

- **FastNoiseLite**: C# port available
- **LibNoise**: Perlin/Simplex noise library
- **Custom**: Implement Perlin noise algorithm

## Example: Star Trek Bridge Console

```csharp
var console = CreateInstance("Item", "bridge-console");
console.Properties["modelPath"] = "/assets/models/console.stl";
console.Properties["texture"] = new BsonDocument {
  { "type", "procedural" },
  { "generator", "composite" },
  { "parameters", new BsonDocument {
    { "layers", new BsonArray {
      new BsonDocument {
        { "generator", "material" },
        { "baseColor", new BsonDocument { { "r", 0.1 }, { "g", 0.1 }, { "b", 0.15 } } },
        { "metalness", 0.9 },
        { "roughness", 0.1 }
      },
      new BsonDocument {
        { "generator", "text" },
        { "text", "SCIENCE" },
        { "font", "Orbitron" },
        { "textColor", new BsonDocument { { "r", 0 }, { "g", 0.8 }, { "b", 1 } } },
        { "opacity", 0.9 }
      },
      new BsonDocument {
        { "generator", "pattern" },
        { "pattern", "circuit" },
        { "color", new BsonDocument { { "r", 0 }, { "g", 0.3 }, { "b", 0.5 } } },
        { "opacity", 0.3 },
        { "blendMode", "screen" }
      }
    }}
  }}
};
```

## Client Format Negotiation

```javascript
// Client capabilities
const textureFormats = {
  preferred: 'webp',  // Better compression
  fallback: 'png',    // Universal support
  maxSize: 1024       // Max texture resolution
};

// Request texture
const textureUrl = `/api/texture/${objectId}?format=${textureFormats.preferred}&size=${textureFormats.maxSize}`;
const texture = await new THREE.TextureLoader().load(textureUrl);
```

## Benefits

1. **Fully Code-Driven**: Textures defined in scripts/properties
2. **No Asset Management**: No texture files to organize
3. **Dynamic**: Change properties → regenerate
4. **Flexible**: Client chooses format/size
5. **Cacheable**: Generate once, serve many times
6. **Procedural**: Infinite variations possible
7. **Hot-Reloadable**: Update texture definition → regenerate

## Future Enhancements

1. **Texture Atlases**: Combine multiple textures into one
2. **Normal Maps**: Generate normal maps procedurally
3. **Animation**: Animated textures (e.g., flickering lights)
4. **UV Mapping**: Procedural UV coordinate generation
5. **Material PBR**: Full PBR material generation
