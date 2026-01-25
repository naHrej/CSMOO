# Model Format: STL (Stereolithography)

## Decision

**Format**: STL (Stereolithography)  
**Rationale**: Direct CAD export capability, native Three.js support, simplicity

## Why STL?

### Advantages

1. **CAD Compatibility**
   - Standard export format in all major CAD software
   - Direct export → serve → render workflow
   - No conversion step required

2. **Web Support**
   - Three.js has built-in `STLLoader`
   - No additional dependencies
   - Binary and ASCII formats supported

3. **Simplicity**
   - Simple format (just geometry/triangles)
   - Easy to understand and debug
   - Widely supported

### Limitations

1. **No Materials**
   - STL contains only geometry (triangles)
   - Materials must be defined separately in GameObject properties
   - This is actually a benefit for our code-driven approach

2. **No Hierarchy**
   - Single mesh per file
   - No scene graph structure
   - Can be worked around with multiple STL files

3. **File Size**
   - Can be larger than optimized formats
   - Binary STL is more efficient than ASCII
   - Compression can be applied (gzip)

## Workflow

```
CAD Software → Export STL → Place in /assets/models/ → Server serves → Client loads
```

### Example

1. **Design in CAD** (Fusion 360, SolidWorks, FreeCAD, etc.)
2. **Export as STL**
   - Binary format preferred (smaller file size)
   - Resolution appropriate for game (not too high, not too low)
3. **Place in server assets**
   - `/assets/models/bridge-console.stl`
4. **Reference in GameObject**
   ```csharp
   console.Properties["modelPath"] = "/assets/models/bridge-console.stl";
   ```
5. **Client loads and renders**
   ```javascript
   const loader = new THREE.STLLoader();
   loader.load('/assets/models/bridge-console.stl', (geometry) => {
     // Apply material from properties
     const mesh = new THREE.Mesh(geometry, material);
     scene.add(mesh);
   });
   ```

## Material Application

Since STL has no materials, we define them in GameObject properties:

```csharp
console.Properties["material"] = new BsonDocument {
  { "type", "MeshStandardMaterial" },
  { "color", new BsonDocument { { "r", 0.1 }, { "g", 0.1 }, { "b", 0.15 } } },
  { "metalness", 0.8 },
  { "roughness", 0.2 },
  { "emissive", new BsonDocument { { "r", 0.05 }, { "g", 0.05 }, { "b", 0.1 } } }
};
```

Client applies material after loading geometry:

```javascript
const material = new THREE.MeshStandardMaterial({
  color: new THREE.Color(materialData.color.r, materialData.color.g, materialData.color.b),
  metalness: materialData.metalness,
  roughness: materialData.roughness,
  emissive: new THREE.Color(materialData.emissive.r, materialData.emissive.g, materialData.emissive.b)
});
```

## Alternative Formats (Future Consideration)

### 3MF (3D Manufacturing Format)

**Pros:**
- Can include materials, colors, textures
- Modern format
- Good for CAD workflows

**Cons:**
- No native Three.js support
- Would need custom loader or conversion
- Less common in web rendering

**If Needed:**
- Export 3MF from CAD
- Convert 3MF → GLTF server-side (using `Microsoft.3MF` or similar)
- Serve GLTF (excellent web support)
- Client loads GLTF with materials included

### GLTF/GLB

**Pros:**
- Excellent web support
- Can include materials, textures, animations
- Optimized for web

**Cons:**
- Not direct CAD export (requires conversion)
- Extra conversion step

**If Needed:**
- Use as conversion target from 3MF
- Or use tools like Blender to convert STL → GLTF

## Best Practices

1. **Use Binary STL**: Smaller file sizes than ASCII
2. **Appropriate Resolution**: Not too high (large files), not too low (poor quality)
3. **Optimize Geometry**: Remove unnecessary triangles in CAD before export
4. **Naming Convention**: Use descriptive names (`bridge-console.stl`, not `model1.stl`)
5. **Organize by Category**: `/assets/models/furniture/`, `/assets/models/vehicles/`, etc.

## Server Implementation

### Asset Serving

```csharp
// In HttpServer.cs
if (requestPath.StartsWith("/assets/models/"))
{
  var filePath = Path.Combine("assets", "models", requestPath.Replace("/assets/models/", ""));
  if (File.Exists(filePath))
  {
    context.Response.ContentType = "application/sla"; // STL MIME type
    context.Response.Headers.Add("Content-Disposition", $"inline; filename=\"{Path.GetFileName(filePath)}\"");
    await context.Response.OutputStream.WriteAsync(File.ReadAllBytes(filePath));
    return;
  }
}
```

### CORS Headers

```csharp
context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
context.Response.Headers.Add("Access-Control-Allow-Methods", "GET");
```

## Client Implementation

### Three.js STL Loader

```javascript
import { STLLoader } from 'three/examples/jsm/loaders/STLLoader.js';

const loader = new STLLoader();
loader.load(
  '/assets/models/bridge-console.stl',
  (geometry) => {
    // Geometry loaded
    const material = createMaterialFromProperties(object.material);
    const mesh = new THREE.Mesh(geometry, material);
    scene.add(mesh);
  },
  (progress) => {
    // Loading progress
    console.log('Loading:', progress.loaded / progress.total * 100 + '%');
  },
  (error) => {
    // Error handling
    console.error('Failed to load STL:', error);
  }
);
```

## File Size Optimization

1. **Compression**: Serve STL files with gzip compression
2. **LOD**: Multiple resolution versions for distance-based loading
3. **Caching**: Browser cache + service worker for offline capability

## Conclusion

STL is the right choice for this project because:
- Direct CAD export (no conversion)
- Native web support (Three.js)
- Materials defined in code (flexible, scriptable)
- Simple workflow

If materials embedded in files become important later, we can add 3MF → GLTF conversion as an enhancement.
