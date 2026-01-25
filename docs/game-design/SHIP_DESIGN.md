# Ship Design System: Primitive-Based Construction

## Overview

Ships are designed by players using primitive STL models that are "stitched" together. This allows for custom ship designs while keeping asset management simple (only primitives needed, not complete ships).

## Design Philosophy

### Primitive-Based Approach

**Concept:**
- Ships are composed of primitive building blocks
- Primitives are STL models (hull sections, engines, weapons, etc.)
- Players combine primitives to create unique ships
- Ship structure stored in GameObject properties
- Client renders ship by combining primitives

**Benefits:**
- Infinite ship variations from limited primitives
- Easy to add new primitives
- Players can design custom ships
- Code-driven (ship structure in properties)
- No need for complete ship models

## Primitive Types

### Hull Primitives

**Basic Shapes:**
- Cylinder hull
- Sphere hull
- Box hull
- Cone hull
- Wedge hull

**Specialized:**
- Bridge section
- Cargo section
- Engine section
- Weapon mount
- Docking port

### System Primitives

**Engines:**
- Thruster (small)
- Main engine (large)
- Maneuvering thruster
- Jump drive core (visual)

**Weapons:**
- Turret mount
- Missile launcher
- Beam weapon
- Railgun

**Systems:**
- Sensor array
- Shield generator (visual)
- Life support module
- Power generator

### Attachment Points

Primitives have attachment points where other primitives can connect:

```csharp
primitive.Properties["attachmentPoints"] = new BsonArray {
  new BsonDocument {
    { "name", "front" },
    { "position", new BsonDocument { { "x", 1 }, { "y", 0 }, { "z", 0 } } },
    { "rotation", new BsonDocument { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
    { "type", "hull" }  // What can attach here
  },
  new BsonDocument {
    { "name", "rear" },
    { "position", new BsonDocument { { "x", -1 }, { "y", 0 }, { "z", 0 } } },
    { "rotation", new BsonDocument { { "x", 0 }, { "y", 180 }, { "z", 0 } } },
    { "type", "hull" }
  },
  new BsonDocument {
    { "name", "hardpoint-1" },
    { "position", new BsonDocument { { "x", 0.5 }, { "y", 0 }, { "z", 0.5 } } },
    { "rotation", new BsonDocument { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
    { "type", "weapon" }
  }
};
```

## Ship Structure

### Ship Definition

```csharp
ship.Properties["primitives"] = new BsonArray {
  new BsonDocument {
    { "id", "hull-1" },
    { "type", "hull" },
    { "primitiveType", "cylinder-hull" },
    { "modelPath", "/assets/models/primitives/hull-cylinder.stl" },
    { "position", new BsonDocument { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
    { "rotation", new BsonDocument { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
    { "scale", new BsonDocument { { "x", 1 }, { "y", 1 }, { "z", 2 } } },
    { "material", { ... } },
    { "texture", { ... } },
    { "attachmentPoint", null }  // Root primitive
  },
  new BsonDocument {
    { "id", "engine-1" },
    { "type", "engine" },
    { "primitiveType", "main-engine" },
    { "modelPath", "/assets/models/primitives/engine-main.stl" },
    { "position", new BsonDocument { { "x", 0 }, { "y", 0 }, { "z", -1 } } },
    { "rotation", new BsonDocument { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
    { "scale", new BsonDocument { { "x", 1 }, { "y", 1 }, { "z", 1 } } },
    { "material", { ... } },
    { "texture", { ... } },
    { "attachmentPoint", "rear" },  // Attached to hull-1's "rear" point
    { "parentId", "hull-1" }
  },
  new BsonDocument {
    { "id", "weapon-1" },
    { "type", "weapon" },
    { "primitiveType", "turret" },
    { "modelPath", "/assets/models/primitives/weapon-turret.stl" },
    { "position", new BsonDocument { { "x", 1 }, { "y", 0 }, { "z", 0 } } },
    { "rotation", new BsonDocument { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
    { "scale", new BsonDocument { { "x", 0.5 }, { "y", 0.5 }, { "z", 0.5 } } },
    { "material", { ... } },
    { "texture", { ... } },
    { "attachmentPoint", "hardpoint-1" },
    { "parentId", "hull-1" }
  }
};
```

### Ship Hierarchy

Ships form a tree structure:
- Root primitive (usually main hull)
- Child primitives attached to root
- Grandchild primitives attached to children
- etc.

**Example:**
```
hull-1 (root)
├── engine-1 (attached to "rear")
├── engine-2 (attached to "rear")
├── weapon-1 (attached to "hardpoint-1")
├── weapon-2 (attached to "hardpoint-2")
└── bridge-1 (attached to "front")
    └── sensor-1 (attached to "top")
```

## Ship Systems Integration

### System Mapping

Primitives can represent ship systems:

```csharp
primitive.Properties["systemType"] = "engine";
primitive.Properties["systemData"] = new BsonDocument {
  { "thrust", 5000.0 },      // Newtons
  { "powerConsumption", 1000.0 }, // Watts
  { "fuelConsumption", 0.1 },    // % per hour
  { "efficiency", 0.85 }
};
```

**System Types:**
- `engine`: Provides thrust
- `weapon`: Provides combat capability
- `shield`: Provides defense
- `sensor`: Provides detection/scanning
- `power`: Provides energy
- `lifeSupport`: Provides oxygen/temperature
- `jumpDrive`: Provides FTL capability
- `cargo`: Provides storage

### Ship Statistics

Ship statistics calculated from primitives:

```csharp
ship.Properties["shipData"] = new BsonDocument {
  { "mass", CalculateMass(ship.primitives) },
  { "thrust", CalculateThrust(ship.primitives) },
  { "maxVelocity", CalculateMaxVelocity(ship) },
  { "hullIntegrity", 100.0 },
  { "power", CalculatePower(ship.primitives) },
  { "fuel", 100.0 },
  { "cargoCapacity", CalculateCargo(ship.primitives) },
  { "jumpDrive", CalculateJumpDrive(ship.primitives) }
};
```

**Calculation Functions:**
```csharp
double CalculateMass(BsonArray primitives) {
  double totalMass = 0.0;
  foreach (var prim in primitives) {
    totalMass += prim["mass"]?.AsDouble ?? 0.0;
  }
  return totalMass;
}

double CalculateThrust(BsonArray primitives) {
  double totalThrust = 0.0;
  foreach (var prim in primitives) {
    if (prim["systemType"]?.AsString == "engine") {
      totalThrust += prim["systemData"]["thrust"]?.AsDouble ?? 0.0;
    }
  }
  return totalThrust;
}
```

## Primitive Snapping System

### Standardized Primitive Size

**Hard Rule: All primitives must occupy a 1x1x1 unit footprint**

This standardization enables:
- Consistent snapping behavior
- Predictable attachment points
- Easier mesh merging
- Simplified calculations

**Primitive Definition:**
```csharp
primitive.Properties["footprint"] = new BsonDocument {
  { "width", 1.0 },   // Always 1.0
  { "height", 1.0 },  // Always 1.0
  { "depth", 1.0 }    // Always 1.0
};
```

**Note:** Primitives can have curved/angled surfaces within the 1x1x1 volume, but the bounding box is always 1x1x1.

### Snapping Rules

**Edge Snapping:**
- Primitives snap along edges of other primitives
- Snapping occurs when primitive is within snap distance (e.g., 0.1 units)
- Snaps to nearest edge/face
- Aligns rotation to match snapped surface

**Surface Types:**
- **Flat surfaces**: Standard 90-degree alignment
- **Curved surfaces**: Snaps to tangent plane
- **Angled surfaces**: Snaps along angle
- **Complex surfaces**: Averages nearby faces for alignment

**Snapping Algorithm:**
```csharp
public class PrimitiveSnapper
{
    private const double SnapDistance = 0.1; // units
    private const double SnapAngle = 5.0;   // degrees
    
    public SnapResult SnapPrimitive(
        Primitive primitive, 
        Primitive target, 
        Vector3 position)
    {
        // Find nearest edge/face on target
        var nearestFace = FindNearestFace(target, position);
        
        if (nearestFace.Distance < SnapDistance)
        {
            // Calculate snap position
            Vector3 snapPosition = CalculateSnapPosition(
                primitive, 
                target, 
                nearestFace
            );
            
            // Calculate snap rotation
            Quaternion snapRotation = CalculateSnapRotation(
                primitive,
                target,
                nearestFace
            );
            
            return new SnapResult
            {
                Position = snapPosition,
                Rotation = snapRotation,
                Snapped = true,
                TargetFace = nearestFace
            };
        }
        
        return new SnapResult { Snapped = false };
    }
    
    private Face FindNearestFace(Primitive target, Vector3 position)
    {
        // Find the face on target primitive closest to position
        // Consider all 6 faces of the 1x1x1 cube
        // Return face with normal, center point, distance
    }
    
    private Vector3 CalculateSnapPosition(
        Primitive primitive, 
        Primitive target, 
        Face targetFace)
    {
        // Calculate where primitive should be positioned
        // to snap to target face
        
        // Get primitive's attachment face (opposite of target face)
        Face attachmentFace = GetAttachmentFace(primitive, targetFace);
        
        // Calculate offset to align faces
        Vector3 offset = targetFace.Center - attachmentFace.Center;
        
        return targetFace.Center + offset;
    }
    
    private Quaternion CalculateSnapRotation(
        Primitive primitive,
        Primitive target,
        Face targetFace)
    {
        // For flat surfaces: align normals
        if (targetFace.IsFlat)
        {
            Vector3 targetNormal = targetFace.Normal;
            Vector3 primitiveNormal = GetPrimitiveNormal(primitive, targetFace);
            
            return Quaternion.FromToRotation(primitiveNormal, targetNormal);
        }
        
        // For curved/angled surfaces: average nearby faces
        if (targetFace.IsCurved || targetFace.IsAngled)
        {
            return CalculateAveragedRotation(primitive, target, targetFace);
        }
        
        return Quaternion.Identity;
    }
}
```

### Face Averaging for Curved/Angled Surfaces

**Problem:** Curved or angled surfaces don't have a single normal vector.

**Solution:** Average nearby faces to determine alignment.

```csharp
private Quaternion CalculateAveragedRotation(
    Primitive primitive,
    Primitive target,
    Face targetFace)
{
    // Get nearby faces on target primitive
    var nearbyFaces = GetNearbyFaces(target, targetFace, radius: 0.2);
    
    // Calculate average normal
    Vector3 averageNormal = Vector3.Zero;
    foreach (var face in nearbyFaces)
    {
        averageNormal += face.Normal;
    }
    averageNormal = averageNormal.Normalize();
    
    // Calculate average tangent (for orientation)
    Vector3 averageTangent = Vector3.Zero;
    foreach (var face in nearbyFaces)
    {
        averageTangent += face.Tangent;
    }
    averageTangent = averageTangent.Normalize();
    
    // Build rotation from averaged vectors
    return Quaternion.LookRotation(averageTangent, averageNormal);
}

private List<Face> GetNearbyFaces(Primitive target, Face centerFace, double radius)
{
    // Get all faces within radius of center face
    // For 1x1x1 primitives, this typically means adjacent faces
    // on the same primitive or connected primitives
}
```

### Snapping Implementation

**Snapping Modes:**
1. **Edge Snap**: Snaps to edges (corners of 1x1x1 cubes)
2. **Face Snap**: Snaps to face centers
3. **Surface Snap**: Snaps to any point on surface (for curved surfaces)

**Visual Feedback:**
- Highlight snap target when primitive is near
- Show preview of snapped position
- Display snap distance
- Show alignment indicator

**User Interaction:**
- Drag primitive near target → auto-snaps
- Hold modifier key to disable snapping
- Click to confirm snap
- Undo/redo snap operations

## Mesh Merging for Performance

### Merging Strategy

**Problem:** Many individual primitives = many draw calls = poor performance.

**Solution:** Merge completed meshes while retaining metadata.

**Process:**
1. User places primitives (individual meshes)
2. User marks section as "complete" or auto-merges after delay
3. System merges meshes into single mesh
4. Retains metadata for editing (which vertices belong to which primitive)
5. Renders merged mesh (single draw call)

### Mesh Merging Algorithm

```csharp
public class MeshMerger
{
    public MergedMesh MergePrimitives(List<Primitive> primitives)
    {
        var mergedMesh = new MergedMesh();
        var vertexMap = new Dictionary<int, PrimitiveMetadata>();
        
        // Combine all vertices
        int vertexIndex = 0;
        foreach (var primitive in primitives)
        {
            var mesh = primitive.GetMesh();
            int startIndex = vertexIndex;
            
            foreach (var vertex in mesh.Vertices)
            {
                // Transform vertex to world space
                Vector3 worldVertex = TransformVertex(vertex, primitive.Transform);
                
                mergedMesh.Vertices.Add(worldVertex);
                mergedMesh.Normals.Add(TransformNormal(vertex.Normal, primitive.Transform));
                mergedMesh.UVs.Add(vertex.UV);
                
                // Track which primitive this vertex belongs to
                vertexMap[vertexIndex] = new PrimitiveMetadata
                {
                    PrimitiveId = primitive.Id,
                    OriginalVertexIndex = vertex.Index,
                    StartIndex = startIndex
                };
                
                vertexIndex++;
            }
            
            // Add indices (offset by start index)
            foreach (var index in mesh.Indices)
            {
                mergedMesh.Indices.Add(startIndex + index);
            }
        }
        
        // Remove duplicate vertices (optional optimization)
        RemoveDuplicateVertices(mergedMesh, vertexMap);
        
        return new MergedMesh
        {
            Vertices = mergedMesh.Vertices,
            Normals = mergedMesh.Normals,
            UVs = mergedMesh.UVs,
            Indices = mergedMesh.Indices,
            Metadata = vertexMap
        };
    }
    
    private void RemoveDuplicateVertices(
        MergedMesh mesh, 
        Dictionary<int, PrimitiveMetadata> vertexMap)
    {
        // Find vertices that are very close (within epsilon)
        // Merge them and update indices
        // Update vertexMap to track merged vertices
    }
}
```

### Metadata Retention

**Purpose:** Allow editing of merged meshes.

**Stored Information:**
```csharp
public class PrimitiveMetadata
{
    public string PrimitiveId { get; set; }
    public int OriginalVertexIndex { get; set; }
    public int StartIndex { get; set; }
    public List<int> VertexIndices { get; set; } // All vertices for this primitive
    public Transform OriginalTransform { get; set; }
    public ComponentData ComponentData { get; set; }
}

public class MergedMesh
{
    public List<Vector3> Vertices { get; set; }
    public List<Vector3> Normals { get; set; }
    public List<Vector2> UVs { get; set; }
    public List<int> Indices { get; set; }
    public Dictionary<int, PrimitiveMetadata> Metadata { get; set; }
}
```

### Editing Merged Meshes

**When user wants to edit:**
1. Select primitive (by clicking or metadata lookup)
2. System extracts primitive from merged mesh using metadata
3. User edits primitive (move, rotate, delete)
4. System re-merges affected section
5. Update metadata

**Extraction Algorithm:**
```csharp
public Primitive ExtractPrimitive(MergedMesh mergedMesh, string primitiveId)
{
    // Find all vertices belonging to this primitive
    var primitiveVertices = mergedMesh.Metadata
        .Where(kvp => kvp.Value.PrimitiveId == primitiveId)
        .Select(kvp => kvp.Key)
        .ToList();
    
    // Extract vertices, normals, UVs
    var vertices = primitiveVertices
        .Select(i => mergedMesh.Vertices[i])
        .ToList();
    
    // Rebuild primitive mesh
    var primitive = new Primitive
    {
        Id = primitiveId,
        Vertices = vertices,
        // ... other properties from metadata
    };
    
    return primitive;
}
```

### Performance Benefits

**Before Merging:**
- 100 primitives = 100 draw calls
- High CPU overhead
- Poor performance

**After Merging:**
- 100 primitives = 1 draw call (if all merged)
- Low CPU overhead
- Good performance

**Hybrid Approach:**
- Merge completed sections
- Keep active/editing primitives separate
- Merge when editing complete

## Ship Design Interface

### Design Modes

**In-Game Editor:**
- Place primitives in 3D space with snapping
- Snap to edges/faces of other primitives
- Rotate, scale (within 1x1x1 constraint), position
- Preview ship
- Merge meshes for performance
- Edit merged sections
- Save design

**External Tool:**
- Standalone ship designer
- Export to game format
- More advanced features
- Better visualization

**Script-Based:**
- Define ships in code
- Programmatic generation
- Template ships
- Procedural generation

### Recommended: Hybrid Approach

**Phase 1: Script-Based**
- Define ships in code
- Simple templates
- Quick to implement
- No snapping needed (code-defined positions)

**Phase 2: In-Game Editor**
- Basic placement with snapping
- 1x1x1 primitive constraint
- Edge/face snapping
- Save/load designs

**Phase 3: Advanced Editor**
- Full 3D manipulation
- Curved/angled surface snapping
- Face averaging
- Mesh merging
- Edit merged meshes
- System configuration

## Primitive Library

### Standard Primitives

**Hull:**
- `hull-cylinder-small` (1m radius, 2m length)
- `hull-cylinder-medium` (2m radius, 4m length)
- `hull-cylinder-large` (4m radius, 8m length)
- `hull-sphere-small` (1m radius)
- `hull-sphere-medium` (2m radius)
- `hull-box-small` (1x1x1m)
- `hull-box-medium` (2x2x2m)
- `hull-wedge` (triangular section)

**Engines:**
- `engine-thruster-small`
- `engine-thruster-medium`
- `engine-thruster-large`
- `engine-main-small`
- `engine-main-medium`
- `engine-main-large`
- `engine-maneuvering`

**Weapons:**
- `weapon-turret-small`
- `weapon-turret-medium`
- `weapon-turret-large`
- `weapon-missile-launcher`
- `weapon-beam-emitter`
- `weapon-railgun`

**Systems:**
- `system-sensor-array`
- `system-shield-generator`
- `system-life-support`
- `system-power-generator`
- `system-jump-core`

**Special:**
- `bridge-section`
- `cargo-bay`
- `docking-port`
- `landing-gear`

## Ship Rendering

### Client-Side Assembly (MonoGame)

Client loads and combines primitives, using merged meshes when available:

```csharp
public class ShipRenderer
{
    public Model RenderShip(GameObject ship, GraphicsDevice device)
    {
        var primitives = ship.Properties["primitives"].AsBsonArray;
        
        // Check if ship has merged mesh
        if (ship.Properties.ContainsKey("mergedMesh"))
        {
            return RenderMergedMesh(ship, device);
        }
        
        // Otherwise render individual primitives
        return RenderIndividualPrimitives(primitives, device);
    }
    
    private Model RenderMergedMesh(GameObject ship, GraphicsDevice device)
    {
        var mergedMeshData = ship.Properties["mergedMesh"].AsBsonDocument;
        
        // Create vertex buffer from merged mesh
        var vertices = ParseVertices(mergedMeshData["vertices"]);
        var indices = ParseIndices(mergedMeshData["indices"]);
        
        var vertexBuffer = new VertexBuffer(
            device, 
            typeof(VertexPositionNormalTexture), 
            vertices.Length, 
            BufferUsage.WriteOnly
        );
        vertexBuffer.SetData(vertices);
        
        var indexBuffer = new IndexBuffer(
            device,
            IndexElementSize.ThirtyTwoBits,
            indices.Length,
            BufferUsage.WriteOnly
        );
        indexBuffer.SetData(indices);
        
        // Single draw call for entire merged mesh
        device.SetVertexBuffer(vertexBuffer);
        device.Indices = indexBuffer;
        
        return new Model
        {
            VertexBuffer = vertexBuffer,
            IndexBuffer = indexBuffer,
            PrimitiveCount = indices.Length / 3
        };
    }
    
    private Model RenderIndividualPrimitives(BsonArray primitives, GraphicsDevice device)
    {
        // Render each primitive separately
        // Used during editing or before merging
        var models = new List<Model>();
        
        foreach (var prim in primitives)
        {
            var geometry = LoadSTL(prim["modelPath"].AsString);
            var mesh = CreateMesh(geometry, prim, device);
            models.Add(mesh);
        }
        
        return new CompositeModel(models);
    }
}
```

### Position Calculation

When primitives are attached, positions are relative to parent:

```csharp
public Vector3 CalculateWorldPosition(Primitive primitive, Primitive parent)
{
    if (parent == null)
    {
        return primitive.Position; // Root primitive
    }
    
    // Transform relative position by parent's transform
    Matrix parentMatrix = Matrix.CreateScale(parent.Scale) *
                          Matrix.CreateFromQuaternion(parent.Rotation) *
                          Matrix.CreateTranslation(parent.Position);
    
    Vector3 relativePos = primitive.Position;
    Vector3 worldPos = Vector3.Transform(relativePos, parentMatrix);
    
    return worldPos;
}
```

### Snapping Visualization

```csharp
public class SnappingVisualizer
{
    public void DrawSnapPreview(
        SpriteBatch spriteBatch,
        Primitive primitive,
        SnapResult snapResult)
    {
        if (snapResult.Snapped)
        {
            // Draw preview of snapped position
            DrawWireframeBox(snapResult.Position, Color.Green);
            
            // Draw snap indicator
            DrawSnapIndicator(snapResult.TargetFace);
            
            // Draw distance indicator
            DrawDistanceText(snapResult.Distance);
        }
    }
    
    private void DrawWireframeBox(Vector3 position, Color color)
    {
        // Draw 1x1x1 wireframe box at position
        // Shows where primitive will snap to
    }
    
    private void DrawSnapIndicator(Face targetFace)
    {
        // Draw visual indicator on target face
        // Shows which face will be snapped to
    }
}
```

## Ship Design Workflow

### Example: Building a Simple Ship

1. **Start with hull:**
   - Place `hull-cylinder-medium` at origin
   - This is the root primitive

2. **Add engines:**
   - Attach `engine-main-medium` to "rear" attachment point
   - Attach `engine-thruster-small` to "rear" (for maneuvering)

3. **Add weapons:**
   - Attach `weapon-turret-small` to "hardpoint-1"
   - Attach `weapon-turret-small` to "hardpoint-2"

4. **Add systems:**
   - Attach `bridge-section` to "front"
   - Attach `system-sensor-array` to "top"
   - Attach `system-shield-generator` to hull interior (hidden)

5. **Configure systems:**
   - Set engine thrust values
   - Set weapon damage values
   - Set shield capacity

6. **Save ship:**
   - Ship structure saved to GameObject properties
   - Can be loaded/used in game

## Ship Templates

### Pre-Built Ships

Common ship designs as templates:

**Fighter:**
- Small hull
- High thrust
- Light weapons
- Fast, agile

**Freighter:**
- Large hull
- Medium thrust
- Cargo bays
- Slow, durable

**Explorer:**
- Medium hull
- Medium thrust
- Sensors, jump drive
- Balanced

**Destroyer:**
- Large hull
- High thrust
- Heavy weapons
- Combat-focused

Players can:
- Use templates as-is
- Modify templates
- Build from scratch

## Validation and Constraints

### Design Rules

**Mass Limits:**
- Maximum ship mass based on total thrust
- Minimum thrust-to-mass ratio for movement

**Power Limits:**
- Systems require power
- Total power consumption cannot exceed generation
- Power allocation priorities

**Attachment Rules:**
- Primitives can only attach to compatible attachment points
- Cannot exceed attachment point capacity
- Must maintain structural integrity

**System Limits:**
- Maximum number of engines
- Maximum number of weapons
- Maximum cargo capacity
- Maximum jump drive factor

### Validation Function

```csharp
ValidationResult ValidateShipDesign(GameObject ship) {
  var result = new ValidationResult();
  
  // Check mass vs thrust
  double mass = CalculateMass(ship.primitives);
  double thrust = CalculateThrust(ship.primitives);
  if (thrust / mass < 0.1) {
    result.AddError("Insufficient thrust for ship mass");
  }
  
  // Check power
  double powerGen = CalculatePowerGeneration(ship.primitives);
  double powerCons = CalculatePowerConsumption(ship.primitives);
  if (powerCons > powerGen) {
    result.AddError("Power consumption exceeds generation");
  }
  
  // Check attachment points
  if (!ValidateAttachments(ship.primitives)) {
    result.AddError("Invalid primitive attachments");
  }
  
  return result;
}
```

## Future Enhancements

1. **Procedural Generation**: Generate ships algorithmically
2. **Ship Classes**: Define ship types with rules
3. **Upgrades**: Modify existing ships with new primitives
4. **Damage Model**: Primitives can be damaged/destroyed
5. **Modular Systems**: Swap systems in/out
6. **Ship Sharing**: Players can share ship designs
7. **Marketplace**: Buy/sell ship designs
