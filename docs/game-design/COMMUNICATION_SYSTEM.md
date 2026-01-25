# Communication System

## Overview

The game features a multi-level communication system allowing players to communicate at different scopes and through different channels. Communication ranges from local room chat to long-range subspace messages with time delays.

## Communication Levels

### 1. Room Level (Local)

**Scope:** Players in the same room  
**Range:** Same physical location  
**Delay:** Instant  
**Access:** Direct chat interface

**Description:**
- Players in the same room can communicate instantly
- Messages are visible to all players in the room
- No special equipment required
- Standard chat interface

**Example:**
```
Player A (in Bridge): "Hey, anyone see that anomaly?"
Player B (in Bridge): "Yeah, it's on the sensors"
Player C (in Bridge): "Let me check the readings"
```

**Implementation:**
```csharp
public void SendRoomMessage(GameObject player, string message)
{
    var room = player.Location as Room;
    if (room == null) return;
    
    // Get all players in room
    var playersInRoom = GetPlayersInRoom(room);
    
    // Send message to all players in room
    foreach (var recipient in playersInRoom)
    {
        SendToPlayer(recipient, $"[{player.Name}]: {message}");
    }
}
```

### 2. Object Level (Local to Object)

**Scope:** Players on the same object (ship, station, planet)  
**Range:** Same object, may be in different rooms  
**Delay:** Instant (within object)  
**Access:** Via communication consoles

**Description:**
- Players on the same ship/station/planet can communicate
- Requires access to a communication console
- Messages broadcast to all players on the object
- Can be used even if players are in different rooms

**Example:**
```
Player A (in Bridge, on Ship Alpha): Uses console → "Engineering, we need more power"
Player B (in Engineering, on Ship Alpha): Hears message on console
Player C (in Cargo Bay, on Ship Alpha): Hears message on console
```

**Console Interface:**
- Players interact with communication console
- Console shows available channels
- Select channel → type message → send
- Messages appear on all consoles on the object

**Implementation:**
```csharp
public void SendObjectMessage(GameObject player, GameObject targetObject, string message)
{
    // Verify player is on the object
    if (!IsPlayerOnObject(player, targetObject))
        return;
    
    // Get all players on the object
    var playersOnObject = GetPlayersOnObject(targetObject);
    
    // Send message to all players on object
    foreach (var recipient in playersOnObject)
    {
        SendToPlayer(recipient, $"[{targetObject.Name} - {player.Name}]: {message}");
    }
}
```

**Console Access:**
```csharp
public class CommunicationConsole
{
    public void OpenConsole(GameObject player, GameObject console)
    {
        // Show console UI
        var ui = new CommunicationConsoleUI
        {
            ObjectId = GetObjectId(console),
            AvailableChannels = GetAvailableChannels(console),
            RecentMessages = GetRecentMessages(console)
        };
        
        ShowUI(player, ui);
    }
    
    public void SendMessage(GameObject player, GameObject console, string channel, string message)
    {
        var objectId = GetObjectId(console);
        var targetObject = GetObject(objectId);
        
        // Send object-level message
        SendObjectMessage(player, targetObject, message);
        
        // Log message on console
        LogMessage(console, channel, player, message);
    }
}
```

### 3. Subspace Communication (Long-Range)

**Scope:** Between different objects (ships, stations, planets)  
**Range:** Any distance  
**Delay:** Time-delayed based on distance  
**Access:** Via communication consoles with subspace capability

**Description:**
- Communication between objects not in the same physical space
- Messages travel at subspace speed (faster than light, but not instant)
- Delay calculated based on distance between objects
- Requires subspace communication equipment on both objects

**Time Delay Calculation:**
```csharp
public TimeSpan CalculateSubspaceDelay(Vector3 sourcePosition, Vector3 destPosition)
{
    // Calculate distance
    var distance = Vector3.Distance(sourcePosition, destPosition);
    
    // Subspace speed (configurable, e.g., 1000x light speed)
    var subspaceSpeed = 1000.0; // times light speed
    var lightSpeed = 299792458.0; // m/s
    var effectiveSpeed = lightSpeed * subspaceSpeed; // m/s
    
    // Calculate time delay
    var timeSeconds = distance / effectiveSpeed;
    
    return TimeSpan.FromSeconds(timeSeconds);
}
```

**Example:**
```
Player A (on Ship Alpha, near Earth): Sends message to Ship Beta (near Mars)
Distance: ~225 million km
Subspace delay: ~12.5 minutes

Player A: "Ship Beta, this is Ship Alpha, requesting docking permission"
[12.5 minutes later]
Player B (on Ship Beta): Receives message
Player B: "Ship Alpha, this is Ship Beta, permission granted"
[12.5 minutes later]
Player A: Receives response
```

**Subspace Message Queue:**
```csharp
public class SubspaceCommunication
{
    private Queue<SubspaceMessage> _messageQueue = new();
    
    public void SendSubspaceMessage(
        GameObject source,
        GameObject destination,
        string message,
        GameObject sender)
    {
        // Calculate delay
        var sourcePos = GetObjectPosition(source);
        var destPos = GetObjectPosition(destination);
        var delay = CalculateSubspaceDelay(sourcePos, destPos);
        
        // Create message
        var subspaceMessage = new SubspaceMessage
        {
            Source = source.Id,
            Destination = destination.Id,
            Message = message,
            Sender = sender.Id,
            SendTime = DateTime.UtcNow,
            ArrivalTime = DateTime.UtcNow + delay
        };
        
        // Queue message
        _messageQueue.Enqueue(subspaceMessage);
    }
    
    public void ProcessSubspaceMessages()
    {
        var now = DateTime.UtcNow;
        var messagesToDeliver = _messageQueue
            .Where(m => m.ArrivalTime <= now)
            .ToList();
        
        foreach (var message in messagesToDeliver)
        {
            DeliverMessage(message);
            _messageQueue.Dequeue();
        }
    }
    
    private void DeliverMessage(SubspaceMessage message)
    {
        var destination = GetObject(message.Destination);
        var sender = GetObject(message.Sender);
        
        // Get all players on destination object
        var players = GetPlayersOnObject(destination);
        
        // Deliver message
        foreach (var player in players)
        {
            SendToPlayer(player, 
                $"[Subspace - {sender.Name}]: {message.Message}");
        }
    }
}
```

## Communication Channels

### Channel Types

**1. Room Channel (Local)**
- Scope: Same room
- No equipment needed
- Instant delivery

**2. Object Channel (Ship/Station/Planet)**
- Scope: Same object
- Requires communication console
- Instant delivery within object

**3. Subspace Channel (Long-Range)**
- Scope: Between objects
- Requires subspace communication equipment
- Time-delayed based on distance

### Channel Selection

**Console Interface:**
```
┌─────────────────────────────┐
│ Communication Console       │
├─────────────────────────────┤
│ Channel: [Object ▼]         │
│                             │
│ [Room] - Local chat        │
│ [Ship] - Ship-wide         │
│ [Subspace] - Long-range    │
│                             │
│ Message:                    │
│ [_________________________]│
│                             │
│ [Send]                      │
└─────────────────────────────┘
```

## Communication Equipment

### Room Level
**No Equipment Required:**
- Players can talk directly
- No special equipment needed
- Always available

### Object Level
**Communication Console:**
- Required for object-level communication
- Found in various rooms (bridge, engineering, etc.)
- Allows communication across object
- Can be accessed by multiple players

**Console Properties:**
```csharp
console.Properties["communication"] = new BsonDocument {
    { "type", "object" },
    { "objectId", "ship-123" },  // Object this console belongs to
    { "channels", new BsonArray { "room", "object" } },
    { "powerRequired", 10.0 },
    { "operational", true }
};
```

### Subspace Communication
**Subspace Communication Array:**
- Required for long-range communication
- Must be installed on object
- Requires power
- Can be damaged/destroyed

**Subspace Array Properties:**
```csharp
subspaceArray.Properties["communication"] = new BsonDocument {
    { "type", "subspace" },
    { "objectId", "ship-123" },
    { "range", 1e15 },  // Maximum range (meters)
    { "powerRequired", 100.0 },
    { "operational", true },
    { "damage", 0.0 }  // 0.0 = fully operational, 1.0 = destroyed
};
```

**Subspace Array Requirements:**
- Must be installed on object
- Requires power to operate
- Can be damaged in combat
- Range limited by equipment quality

## Communication UI

### Chat Interface

**Room Chat:**
```
┌─────────────────────────────┐
│ Chat                        │
├─────────────────────────────┤
│ [Room]                      │
│                             │
│ Player A: Hey everyone     │
│ Player B: What's up?       │
│ Player C: Not much         │
│                             │
│ [Type message...]          │
│ [Send]                      │
└─────────────────────────────┘
```

**Object Chat (via Console):**
```
┌─────────────────────────────┐
│ Communication Console       │
├─────────────────────────────┤
│ Channel: [Ship Alpha]       │
│                             │
│ [Ship] Player A: Need power│
│ [Ship] Player B: On it     │
│                             │
│ [Type message...]          │
│ [Send]                      │
└─────────────────────────────┘
```

**Subspace Chat:**
```
┌─────────────────────────────┐
│ Subspace Communication      │
├─────────────────────────────┤
│ To: [Ship Beta ▼]           │
│ Distance: 225M km           │
│ Delay: ~12.5 minutes        │
│                             │
│ [12:00] You: Request dock  │
│ [12:12] Ship Beta: Granted │
│                             │
│ [Type message...]          │
│ [Send]                      │
└─────────────────────────────┘
```

## Message Formatting

### Message Types

**Room Message:**
```
[Player Name]: Message text
```

**Object Message:**
```
[Object Name - Player Name]: Message text
```

**Subspace Message:**
```
[Subspace - Sender Name]: Message text
[Arrival Time] [Sender Name]: Message text
```

### Timestamps

**Subspace Messages:**
- Show send time
- Show arrival time (when received)
- Show delay information

**Example:**
```
[12:00:00] [Subspace - Ship Alpha]: Requesting docking
[12:12:30] [Subspace - Ship Beta]: Permission granted
```

## Communication Commands

### Player Commands (via UI/Keybinds)

**Room Chat:**
- Press `Enter` → Opens chat input
- Type message → Press `Enter` → Sends to room
- Or: Click chat input → Type → Click Send

**Object Communication:**
- Interact with console (`E` key)
- Select "Communication" option
- Select channel
- Type message → Send

**Subspace Communication:**
- Interact with subspace console
- Select destination object
- Type message → Send
- Message queued with delay

### Command Structure

**No Text Commands:**
- All communication via UI
- Chat input field for typing messages
- Channel selection via UI
- Destination selection via UI

## Implementation Details

### Message Routing

```csharp
public class CommunicationRouter
{
    public void RouteMessage(
        GameObject sender,
        string channel,
        string destination,
        string message)
    {
        switch (channel.ToLower())
        {
            case "room":
                SendRoomMessage(sender, message);
                break;
                
            case "object":
                var objectId = GetObjectId(sender);
                SendObjectMessage(sender, objectId, message);
                break;
                
            case "subspace":
                var destObject = GetObject(destination);
                SendSubspaceMessage(sender, destObject, message);
                break;
        }
    }
}
```

### Message History

**Store Recent Messages:**
- Room: Last 50 messages
- Object: Last 100 messages
- Subspace: All messages (with timestamps)

**Message Storage:**
```csharp
room.Properties["chatHistory"] = new BsonArray {
    new BsonDocument {
        { "sender", "player-123" },
        { "message", "Hello everyone" },
        { "timestamp", DateTime.UtcNow }
    }
};
```

### Message Filtering

**Player Can Filter:**
- Show/hide room chat
- Show/hide object chat
- Show/hide subspace chat
- Mute specific players
- Mute specific objects

## Gameplay Examples

### Example 1: Room Chat

**Scenario:** Three players in Bridge room

```
Player A types: "Anyone see that anomaly?"
→ Message sent to room
→ Player B sees: "[Player A]: Anyone see that anomaly?"
→ Player C sees: "[Player A]: Anyone see that anomaly?"

Player B types: "Yeah, it's on sensors"
→ Message sent to room
→ All players in room see message
```

### Example 2: Object Communication

**Scenario:** Player on bridge, another in engineering

```
Player A (Bridge):
1. Interacts with communication console
2. Selects "Ship" channel
3. Types: "Engineering, we need more power"
4. Sends message

→ Message broadcast to all players on ship
→ Player B (Engineering) sees: "[Ship Alpha - Player A]: Engineering, we need more power"
→ Player C (Cargo Bay) sees: "[Ship Alpha - Player A]: Engineering, we need more power"
```

### Example 3: Subspace Communication

**Scenario:** Ship Alpha near Earth, Ship Beta near Mars

```
Player A (Ship Alpha):
1. Interacts with subspace console
2. Selects "Ship Beta" as destination
3. Types: "Ship Beta, requesting docking permission"
4. Sends message

→ Message queued with 12.5 minute delay
→ [12.5 minutes later]
→ Player B (Ship Beta) receives: "[Subspace - Ship Alpha]: Ship Beta, requesting docking permission"

Player B (Ship Beta):
1. Types response: "Ship Alpha, permission granted"
2. Sends message

→ Message queued with 12.5 minute delay
→ [12.5 minutes later]
→ Player A receives: "[Subspace - Ship Beta]: Ship Alpha, permission granted"
```

## Technical Considerations

### Performance

**Message Processing:**
- Room messages: Process immediately
- Object messages: Process immediately
- Subspace messages: Process in background queue

**Message Queue:**
- Background thread processes subspace messages
- Check queue every second
- Deliver messages when arrival time reached

### Scalability

**Message Limits:**
- Room: 50 messages stored
- Object: 100 messages stored
- Subspace: All messages stored (with cleanup)

**Rate Limiting:**
- Limit messages per second per player
- Prevent spam
- Configurable limits

### Security

**Message Validation:**
- Sanitize message content
- Check player permissions
- Verify object access
- Validate subspace destination

## Related Documentation

- [PLAYER_INTERACTION_MODEL.md](../pivot/PLAYER_INTERACTION_MODEL.md) - Player interaction (no text commands)
- [SPACE_MECHANICS.md](./SPACE_MECHANICS.md) - Space zones and object positioning
- [SHIP_COMPONENTS.md](./SHIP_COMPONENTS.md) - Communication equipment as ship components
