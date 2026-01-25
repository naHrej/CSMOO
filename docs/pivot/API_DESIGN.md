# API Design

## Overview

The API consists of HTTP REST endpoints for scene data and asset serving, plus WebSocket protocol extensions for real-time updates.

## HTTP REST API

### Base URL

```
http://server:1703/api
```

### Endpoints

#### GET /api/scene/{roomId}

Get complete scene description for a room.

**Parameters:**
- `roomId` (path): Room/object ID

**Response:**
```json
{
  "roomId": "bridge-room",
  "room": {
    "id": "bridge-room",
    "name": "Bridge",
    "modelPath": "/assets/models/bridge.stl",
    "position": { "x": 0, "y": 0, "z": 0 },
    "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 },
    "scale": { "x": 1, "y": 1, "z": 1 },
    "material": { ... },
    "texture": { ... },
    "lighting": {
      "ambient": { "r": 0.3, "g": 0.3, "b": 0.3 },
      "directional": {
        "direction": { "x": 0, "y": -1, "z": 0 },
        "color": { "r": 1, "g": 1, "b": 1 },
        "intensity": 1.0
      },
      "pointLights": [
        {
          "position": { "x": 5, "y": 3, "z": -2 },
          "color": { "r": 1, "g": 0.8, "b": 0.6 },
          "intensity": 0.5,
          "distance": 10
        }
      ]
    },
    "uiStyle": {
      "backgroundColor": "#1a1a2e",
      "textColor": "#0f3460",
      "fontFamily": "'Orbitron', sans-serif"
    }
  },
  "objects": [
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
    },
    // ... more objects
  ]
}
```

**Status Codes:**
- `200 OK`: Scene data returned
- `404 Not Found`: Room not found
- `500 Internal Server Error`: Server error

#### GET /api/texture/{objectId}

Generate and return texture for an object.

**Parameters:**
- `objectId` (path): Object ID
- `format` (query, optional): `webp`, `png`, `jpg` (default: `png`)
- `size` (query, optional): Texture resolution (default: `512`)

**Response:**
- Content-Type: `image/png`, `image/webp`, or `image/jpeg`
- Body: Image bytes

**Example:**
```
GET /api/texture/console-1?format=webp&size=1024
```

**Status Codes:**
- `200 OK`: Texture generated and returned
- `404 Not Found`: Object not found
- `400 Bad Request`: Invalid format or size
- `500 Internal Server Error`: Generation error

#### GET /api/object/{objectId}

Get single object data.

**Response:**
```json
{
  "id": "console-1",
  "name": "Science Console",
  "modelPath": "/assets/models/console.stl",
  "position": { "x": 5, "y": 0, "z": -3 },
  "rotation": { "x": 0, "y": 45, "z": 0 },
  "scale": { "x": 1, "y": 1, "z": 1 },
  "material": { ... },
  "texture": { ... }
}
```

#### POST /api/action

Execute a verb/function on an object.

**Request:**
```json
{
  "verb": "activate",
  "target": "console-1",
  "parameters": ["science", "scan"],
  "playerId": "player-123"
}
```

**Response:**
```json
{
  "success": true,
  "result": "Scanning complete. No anomalies detected.",
  "sceneUpdates": [
    {
      "type": "objectChanged",
      "objectId": "console-1",
      "changes": {
        "material": {
          "emissive": { "r": 0, "g": 1, "b": 0 }
        }
      }
    }
  ]
}
```

**Status Codes:**
- `200 OK`: Action executed
- `400 Bad Request`: Invalid request
- `404 Not Found`: Object or verb not found
- `403 Forbidden`: Permission denied
- `500 Internal Server Error`: Execution error

### Asset Serving

#### GET /assets/models/{filename}

Serve STL model files.

**Response:**
- Content-Type: `application/sla` (STL MIME type)
- Body: STL file bytes

**Example:**
```
GET /assets/models/bridge-console.stl
```

#### GET /assets/{path}

Generic asset serving (for future use: sounds, etc.).

## WebSocket Protocol

### Connection

```
ws://server:1702/api
```

### Message Format

All messages are JSON.

### Server → Client Messages

#### objectMoved

Object position/rotation changed.

```json
{
  "type": "objectMoved",
  "objectId": "player-123",
  "position": { "x": 5, "y": 0, "z": -3 },
  "rotation": { "x": 0, "y": 45, "z": 0 },
  "timestamp": "2026-01-22T12:00:00Z"
}
```

#### objectAdded

New object added to scene.

```json
{
  "type": "objectAdded",
  "object": {
    "id": "console-1",
    "name": "Science Console",
    "modelPath": "/assets/models/console.stl",
    "position": { "x": 0, "y": 0, "z": 0 },
    "rotation": { "x": 0, "y": 0, "z": 0 },
    "scale": { "x": 1, "y": 1, "z": 1 },
    "material": { ... },
    "texture": { ... }
  },
  "timestamp": "2026-01-22T12:00:00Z"
}
```

#### objectRemoved

Object removed from scene.

```json
{
  "type": "objectRemoved",
  "objectId": "item-456",
  "timestamp": "2026-01-22T12:00:00Z"
}
```

#### objectChanged

Object properties changed.

```json
{
  "type": "objectChanged",
  "objectId": "console-1",
  "changes": {
    "material": {
      "emissive": { "r": 0, "g": 1, "b": 0 }
    },
    "texture": {
      "parameters": {
        "text": "SCANNING..."
      }
    }
  },
  "timestamp": "2026-01-22T12:00:00Z"
}
```

#### animation

Animation event.

```json
{
  "type": "animation",
  "objectId": "door-1",
  "animation": "open",
  "duration": 1000,
  "parameters": {
    "targetPosition": { "x": 0, "y": 0, "z": 2 }
  },
  "timestamp": "2026-01-22T12:00:00Z"
}
```

#### uiUpdate

UI overlay update.

```json
{
  "type": "uiUpdate",
  "element": "status",
  "content": "Systems operational",
  "style": {
    "color": "#00ff00"
  },
  "timestamp": "2026-01-22T12:00:00Z"
}
```

#### error

Error message.

```json
{
  "type": "error",
  "code": "OBJECT_NOT_FOUND",
  "message": "Object 'console-999' not found",
  "timestamp": "2026-01-22T12:00:00Z"
}
```

### Client → Server Messages

#### action

Execute verb/function.

```json
{
  "type": "action",
  "verb": "activate",
  "target": "console-1",
  "parameters": ["science", "scan"],
  "requestId": "req-123"
}
```

**Response:**
```json
{
  "type": "actionResult",
  "requestId": "req-123",
  "success": true,
  "result": "Scanning complete.",
  "sceneUpdates": [ ... ]
}
```

#### move

Player movement input.

```json
{
  "type": "move",
  "direction": { "x": 1, "y": 0, "z": 0 },
  "speed": 1.0,
  "requestId": "req-124"
}
```

#### interact

Object interaction.

```json
{
  "type": "interact",
  "objectId": "door-1",
  "interaction": "open",
  "requestId": "req-125"
}
```

#### subscribe

Subscribe to updates for specific objects/rooms.

```json
{
  "type": "subscribe",
  "channels": ["room:bridge-room", "object:console-1"]
}
```

#### unsubscribe

Unsubscribe from updates.

```json
{
  "type": "unsubscribe",
  "channels": ["room:bridge-room"]
}
```

## Error Handling

### HTTP Errors

Standard HTTP status codes:
- `200 OK`: Success
- `400 Bad Request`: Invalid request
- `401 Unauthorized`: Authentication required
- `403 Forbidden`: Permission denied
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

### WebSocket Errors

Error messages sent via WebSocket:

```json
{
  "type": "error",
  "code": "ERROR_CODE",
  "message": "Human-readable error message",
  "details": { ... },
  "timestamp": "2026-01-22T12:00:00Z"
}
```

## CORS

All API endpoints should include CORS headers:

```
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, OPTIONS
Access-Control-Allow-Headers: Content-Type
```

## Authentication

(To be determined - may need session-based auth or token-based)

## Rate Limiting

(To be determined - prevent abuse)

## Caching

- **Scene data**: Cache with ETag/Last-Modified
- **Textures**: Cache with long expiration (generated content)
- **Models**: Cache with long expiration (static assets)
