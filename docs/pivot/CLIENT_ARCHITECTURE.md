# Client Architecture (Web-Based - Original Plan)

> **Note**: This document describes the original web-based client architecture. See [CLIENT_ARCHITECTURE_CSharp.md](./CLIENT_ARCHITECTURE_CSharp.md) for the C# native client architecture with HTML/CSS/JS texture rendering.

## Overview

The client is a pure HTML5/CSS/JavaScript application that renders the game world based entirely on data provided by the server. It contains zero game assets and acts as a viewer with input capability.

## Technology Stack

- **HTML5**: Structure and UI overlay
- **CSS**: Styling (can be server-controlled via properties)
- **JavaScript**: Client logic and WebGL rendering
- **WebGL**: 3D rendering (Three.js or similar)
- **WebSocket**: Real-time communication with server
- **Fetch API**: HTTP requests for scene data and assets

## Client Responsibilities

### ✅ What the Client Does

1. **Rendering**
   - Load scene description from server
   - Request and load STL models from server
   - Request and load generated textures from server
   - Render 3D scene using WebGL
   - Render UI overlay (HTML/CSS)

2. **Input Handling**
   - Capture user input (mouse, keyboard, touch)
   - Send input to server via WebSocket
   - Wait for server response

3. **State Synchronization**
   - Receive real-time updates via WebSocket
   - Update scene based on server messages
   - Apply animations and transitions

4. **Asset Management**
   - Cache loaded models and textures
   - Request assets on-demand
   - Handle loading states and errors

### ❌ What the Client Does NOT Do

1. **Game Logic**: No game rules, validation, or state management
2. **Asset Storage**: No bundled models, textures, or sounds
3. **Scene Definition**: Does not define what exists in the world
4. **Authority**: Does not make authoritative decisions about game state

## Communication Protocol

### Initial Connection

1. **WebSocket Connection**
   ```javascript
   const ws = new WebSocket('ws://server:1702/api');
   ```

2. **Request Initial Scene**
   ```javascript
   // After authentication/login
   const response = await fetch('/api/scene/current-room-id');
   const sceneData = await response.json();
   ```

3. **Load Assets**
   ```javascript
   // For each object in scene
   const modelLoader = new THREE.STLLoader();
   modelLoader.load(`/assets/models/${object.modelPath}`, (geometry) => {
     // Apply material from object properties
     // Add to scene
   });
   ```

### Real-Time Updates (WebSocket)

**Server → Client Messages:**

```javascript
// Object moved
{
  type: "objectMoved",
  objectId: "player-123",
  position: { x: 5, y: 0, z: -3 },
  rotation: { x: 0, y: 45, z: 0 }
}

// Object added
{
  type: "objectAdded",
  object: {
    id: "console-1",
    modelPath: "/assets/models/console.stl",
    position: { x: 0, y: 0, z: 0 },
    material: { ... },
    texture: { ... }
  }
}

// Object removed
{
  type: "objectRemoved",
  objectId: "item-456"
}

// Animation event
{
  type: "animation",
  objectId: "door-1",
  animation: "open",
  duration: 1000
}

// UI update
{
  type: "uiUpdate",
  element: "status",
  content: "Systems operational",
  style: { color: "#00ff00" }
}
```

**Client → Server Messages:**

```javascript
// User action
{
  type: "action",
  verb: "activate",
  target: "console-1",
  parameters: []
}

// Movement input
{
  type: "move",
  direction: { x: 1, y: 0, z: 0 },
  speed: 1.0
}

// Interaction
{
  type: "interact",
  objectId: "door-1",
  interaction: "open"
}
```

## Rendering Pipeline

### 1. Scene Loading

```javascript
async function loadScene(roomId) {
  // Request scene data
  const sceneData = await fetch(`/api/scene/${roomId}`).then(r => r.json());
  
  // Set up lighting
  setupLighting(sceneData.lighting);
  
  // Load each object
  for (const object of sceneData.objects) {
    await loadObject(object);
  }
}

async function loadObject(object) {
  // Load model
  const geometry = await loadSTL(object.modelPath);
  
  // Generate/load texture
  const texture = await loadTexture(object.id, object.texture);
  
  // Create material
  const material = createMaterial(object.material, texture);
  
  // Create mesh
  const mesh = new THREE.Mesh(geometry, material);
  mesh.position.set(object.position.x, object.position.y, object.position.z);
  mesh.rotation.set(object.rotation.x, object.rotation.y, object.rotation.z);
  mesh.scale.set(object.scale.x, object.scale.y, object.scale.z);
  
  // Store object reference
  objectMap.set(object.id, mesh);
  
  // Add to scene
  scene.add(mesh);
}
```

### 2. Texture Loading

```javascript
async function loadTexture(objectId, textureDef) {
  // Request texture from server
  const format = getPreferredFormat(); // webp, png, jpg
  const size = getMaxTextureSize(); // 512, 1024, 2048
  
  const textureUrl = `/api/texture/${objectId}?format=${format}&size=${size}`;
  const texture = await new THREE.TextureLoader().load(textureUrl);
  
  return texture;
}
```

### 3. Real-Time Updates

```javascript
ws.onmessage = (event) => {
  const message = JSON.parse(event.data);
  
  switch (message.type) {
    case 'objectMoved':
      const mesh = objectMap.get(message.objectId);
      if (mesh) {
        // Animate to new position
        animateTo(mesh, message.position, message.rotation);
      }
      break;
      
    case 'objectAdded':
      loadObject(message.object);
      break;
      
    case 'objectRemoved':
      const removedMesh = objectMap.get(message.objectId);
      if (removedMesh) {
        scene.remove(removedMesh);
        objectMap.delete(message.objectId);
      }
      break;
  }
};
```

## UI Overlay

The UI overlay is HTML/CSS that can be styled by server properties:

```html
<div id="uiOverlay">
  <div id="status" class="status-bar"></div>
  <div id="inventory" class="inventory-panel"></div>
  <div id="chat" class="chat-window"></div>
</div>
```

Server can inject CSS via `uiStyle` properties:

```javascript
// Apply server-defined styles
function applyUIStyles(room) {
  if (room.uiStyle) {
    const style = document.createElement('style');
    style.textContent = `
      body { background-color: ${room.uiStyle.backgroundColor}; }
      .status-bar { color: ${room.uiStyle.textColor}; }
    `;
    document.head.appendChild(style);
  }
}
```

## Error Handling

```javascript
// Handle connection errors
ws.onerror = (error) => {
  console.error('WebSocket error:', error);
  // Attempt reconnection
  setTimeout(connect, 5000);
};

// Handle asset loading errors
async function loadObject(object) {
  try {
    // ... load logic
  } catch (error) {
    console.error(`Failed to load object ${object.id}:`, error);
    // Show placeholder or error indicator
    showErrorIndicator(object.id);
  }
}
```

## Performance Considerations

1. **Model Caching**: Cache loaded STL models to avoid re-downloading
2. **Texture Caching**: Cache generated textures (browser cache + local storage)
3. **LOD (Level of Detail)**: Request lower-resolution textures for distant objects
4. **Frustum Culling**: Only render objects in view
5. **Object Pooling**: Reuse mesh objects when possible

## Browser Compatibility

- **WebGL**: Required (all modern browsers)
- **WebSocket**: Required (all modern browsers)
- **Fetch API**: Required (all modern browsers)
- **ES6+**: Use Babel if needed for older browsers

## Security Considerations

1. **Input Validation**: Server validates all input (client is untrusted)
2. **CORS**: Configure CORS properly for asset serving
3. **XSS Prevention**: Sanitize any server-provided HTML/CSS
4. **WebSocket Security**: Use WSS in production
