# Player Interaction Model

## Overview

The game uses a **graphical interface with key bindings and UI clicks** for player interaction. Players **no longer type text commands**. Instead, interactions are handled through:

1. **Key Bindings** - Keyboard shortcuts for actions
2. **UI Clicks** - Clicking on objects, buttons, menus
3. **Object Command Interfaces** - Verbs become clickable actions on objects
4. **Player Object Commands** - RPG-like commands accessible via UI/keybinds

## Design Philosophy

**No Text Input:**
- Players do not type commands
- All interaction is graphical/UI-based
- Commands are triggered by key presses or clicks
- Text input only for chat/communication

**Verbs as Object Interfaces:**
- Verbs remain in the system
- Verbs become **command interfaces on objects**
- Players click objects to see available actions (verbs)
- Verbs are not typed by players

**Player Object Commands:**
- Player object has RPG-like commands
- Accessible via key bindings or UI
- Examples: inventory, equipment, combat actions

## Interaction Methods

### 1. Key Bindings

**Keyboard Shortcuts:**
- Press key → Execute action
- Configurable key bindings
- Context-sensitive (different keys in different contexts)

**Examples:**
- `E` - Interact with object (opens interaction menu)
- `I` - Open inventory
- `Tab` - Open player menu
- `Space` - Jump / Use / Action
- `R` - Reload weapon
- `F` - Use item
- `Q` - Quick action menu

### 2. UI Clicks

**Clickable Elements:**
- Click object → See available actions
- Click button → Execute action
- Click menu item → Navigate/execute
- Click inventory item → Use/equip/drop

**Object Interaction:**
- Right-click object → Context menu with verbs
- Left-click object → Select/highlight
- Double-click object → Primary action (if available)

### 3. Object Command Interfaces

**Verbs as Clickable Actions:**
- Objects have verbs (command interfaces)
- Players see verbs as clickable buttons/actions
- Click verb → Execute action
- Verbs are not typed, they're clicked

**Example:**
```
Player clicks on "Console" object
→ UI shows available actions:
  - [Activate]
  - [Configure]
  - [Examine]
  - [Repair]
  
Player clicks [Activate]
→ Executes "activate" verb on console
```

### 4. Player Object Commands

**RPG-Like Commands:**
- Player object has special commands
- Accessible via key bindings or UI
- Not typed by players
- Examples:
  - "Who is online" - Social menu
  - "What's in my inventory" - Inventory UI
  - "What am I holding?" - Equipment UI
  - "Draw my weapon" - Combat action
  - "Shoot at target" - Combat action

## Command Architecture

### Verb System (Object Commands)

**Verbs Remain, But Usage Changes:**

**Before (Text-Based):**
```
Player types: "look at console"
→ System finds "look" verb
→ Executes verb
```

**After (Graphical):**
```
Player clicks "console" object
→ UI shows verbs: [Look] [Activate] [Examine]
→ Player clicks [Look]
→ System executes "look" verb on console
```

**Verb Storage (Unchanged):**
- Verbs still stored on objects/classes
- Verbs still have patterns, code, permissions
- Verbs still execute scripts

**Verb Resolution (Changed):**
- No longer resolves from typed text
- Resolves from UI selection
- Player selects object + verb from UI

### Player Object Commands

**Special Commands on Player Object:**

```csharp
player.Properties["playerCommands"] = new BsonArray {
    "inventory",      // Show inventory
    "equipment",      // Show equipment
    "status",         // Show player status
    "who",            // Who is online
    "social",         // Social menu
    "drawWeapon",     // Combat: draw weapon
    "holsterWeapon",  // Combat: holster weapon
    "shoot",          // Combat: shoot at target
    "reload",         // Combat: reload weapon
    "useItem",        // Use item from inventory
    "dropItem",       // Drop item
    "pickupItem"      // Pick up item
};
```

**Command Execution:**
- Triggered by key bindings or UI clicks
- Not typed by players
- Execute as scripts/functions on player object

**Example:**
```csharp
// Player presses 'I' key
→ Execute "inventory" command on player object
→ Opens inventory UI
→ Shows items in player's inventory
```

## Input Handling

### Client-Side Input

**Keyboard Input:**
```csharp
public class InputHandler
{
    private Dictionary<Keys, string> _keyBindings = new()
    {
        { Keys.E, "interact" },
        { Keys.I, "inventory" },
        { Keys.Tab, "playerMenu" },
        { Keys.Space, "action" },
        { Keys.R, "reload" },
        { Keys.F, "useItem" }
    };
    
    public void HandleKeyPress(Keys key)
    {
        if (_keyBindings.TryGetValue(key, out var command))
        {
            ExecuteCommand(command);
        }
    }
    
    private void ExecuteCommand(string command)
    {
        // Send command to server
        _networkClient.SendCommand(command);
    }
}
```

**Mouse Input:**
```csharp
public class MouseHandler
{
    public void HandleClick(Vector2 screenPosition, MouseButton button)
    {
        // Raycast to find clicked object
        var hit = Raycast(screenPosition);
        
        if (hit != null)
        {
            if (button == MouseButton.Left)
            {
                SelectObject(hit.Object);
            }
            else if (button == MouseButton.Right)
            {
                ShowContextMenu(hit.Object);
            }
        }
    }
    
    private void ShowContextMenu(GameObject obj)
    {
        // Get verbs on object
        var verbs = GetVerbsOnObject(obj);
        
        // Show context menu with verb actions
        _uiManager.ShowContextMenu(obj, verbs);
    }
}
```

### Server-Side Command Processing

**Command Types:**

1. **Object Interaction Commands:**
   - `interact <objectId>` - Interact with object
   - `executeVerb <objectId> <verbName>` - Execute verb on object
   - `select <objectId>` - Select object

2. **Player Commands:**
   - `inventory` - Open inventory
   - `equipment` - Open equipment
   - `who` - Who is online
   - `status` - Player status
   - `drawWeapon` - Draw weapon
   - `shoot <targetId>` - Shoot at target

3. **UI Commands:**
   - `openMenu <menuName>` - Open menu
   - `closeMenu` - Close menu
   - `navigateMenu <direction>` - Navigate menu

**Command Processing:**
```csharp
public class CommandProcessor
{
    public void ProcessCommand(string command, GameObject player)
    {
        var parts = command.Split(' ');
        var commandType = parts[0];
        
        switch (commandType)
        {
            case "interact":
                HandleInteract(parts[1], player);
                break;
                
            case "executeVerb":
                HandleExecuteVerb(parts[1], parts[2], player);
                break;
                
            case "inventory":
                HandleInventory(player);
                break;
                
            case "drawWeapon":
                HandleDrawWeapon(player);
                break;
                
            // ... more commands
        }
    }
    
    private void HandleExecuteVerb(string objectId, string verbName, GameObject player)
    {
        var obj = _objectManager.GetObject(objectId);
        if (obj == null) return;
        
        // Find verb on object
        var verb = _scriptResolver.ResolveVerb(obj, verbName);
        if (verb == null) return;
        
        // Execute verb
        _scriptProcessor.ExecuteScript(verb, player, obj);
    }
}
```

## UI Design

### Object Interaction UI

**Context Menu:**
```
[Object: Console]
├── [Look] - Examine object
├── [Activate] - Activate console
├── [Configure] - Configure settings
└── [Repair] - Repair console
```

**Interaction Panel:**
```
┌─────────────────────┐
│ Console             │
├─────────────────────┤
│ [Activate]          │
│ [Configure]         │
│ [Examine]           │
│ [Repair]            │
└─────────────────────┘
```

### Player Menu UI

**Player Menu (Tab Key):**
```
┌─────────────────────┐
│ Player Menu         │
├─────────────────────┤
│ [Inventory] (I)     │
│ [Equipment]         │
│ [Status]            │
│ [Social]            │
│ [Settings]          │
└─────────────────────┘
```

**Inventory UI:**
```
┌─────────────────────┐
│ Inventory           │
├─────────────────────┤
│ [Item 1] [Use] [Drop]│
│ [Item 2] [Use] [Drop]│
│ [Item 3] [Use] [Drop]│
└─────────────────────┘
```

### Combat UI

**Combat Actions:**
```
┌─────────────────────┐
│ Combat              │
├─────────────────────┤
│ [Draw Weapon] (D)   │
│ [Shoot] (Left Click)│
│ [Reload] (R)        │
│ [Holster] (H)       │
└─────────────────────┘
```

## Migration Impact

### What Changes

1. **Command Input:**
   - ❌ Remove text command input
   - ✅ Add key binding system
   - ✅ Add mouse/click input
   - ✅ Add UI interaction

2. **Verb Resolution:**
   - ❌ No longer resolves from typed text
   - ✅ Resolves from UI selection
   - ✅ Player selects object + verb

3. **Command Processing:**
   - ❌ No longer parses text commands
   - ✅ Processes structured commands (objectId + verbName)
   - ✅ Processes player commands (inventory, etc.)

4. **Client Architecture:**
   - ❌ No text input field
   - ✅ Add input handler (keyboard/mouse)
   - ✅ Add UI system
   - ✅ Add context menus
   - ✅ Add key binding configuration

### What Stays the Same

1. **Verb System:**
   - Verbs still stored on objects
   - Verbs still execute scripts
   - Verb patterns still exist (for internal use)

2. **Script Execution:**
   - Scripts still execute the same way
   - Script engine unchanged
   - Script globals unchanged

3. **Object System:**
   - Objects still have verbs
   - Objects still have properties
   - Object relationships unchanged

## Implementation Phases

### Phase 1: Remove Text Input

**Tasks:**
1. Remove text command input from client
2. Remove command parsing from server
3. Update command processing to handle structured commands
4. Add key binding system
5. Add mouse input handling

### Phase 2: Add UI System

**Tasks:**
1. Add context menu UI
2. Add object interaction UI
3. Add player menu UI
4. Add inventory UI
5. Add equipment UI

### Phase 3: Add Player Commands

**Tasks:**
1. Add player object commands
2. Implement inventory system
3. Implement equipment system
4. Implement combat actions
5. Implement social commands

### Phase 4: Polish

**Tasks:**
1. Key binding configuration UI
2. UI customization
3. Tooltips and help
4. Accessibility features

## Examples

### Example 1: Interacting with Object

**Player Action:**
1. Player sees "Console" object in 3D scene
2. Player right-clicks on "Console"
3. Context menu appears with verbs: [Look] [Activate] [Configure]
4. Player clicks [Activate]
5. System executes "activate" verb on console

**Server Processing:**
```
Client sends: "executeVerb console-123 activate"
Server:
  1. Gets console object
  2. Finds "activate" verb
  3. Executes verb script
  4. Returns result
```

### Example 2: Opening Inventory

**Player Action:**
1. Player presses 'I' key
2. Inventory UI opens
3. Shows items in player's inventory
4. Player clicks item → [Use] [Drop] [Examine]

**Server Processing:**
```
Client sends: "inventory"
Server:
  1. Gets player object
  2. Executes "inventory" command
  3. Returns inventory data
  4. Client displays in UI
```

### Example 3: Combat Action

**Player Action:**
1. Player has weapon equipped
2. Player presses 'D' key (draw weapon)
3. Weapon is drawn
4. Player left-clicks on target
5. System executes "shoot" command

**Server Processing:**
```
Client sends: "drawWeapon"
Server:
  1. Executes "drawWeapon" on player
  2. Updates player state
  3. Returns success

Client sends: "shoot target-456"
Server:
  1. Executes "shoot" command on player
  2. Calculates hit/miss
  3. Applies damage
  4. Returns result
```

## Key Bindings Configuration

**Configurable Key Bindings:**
```json
{
  "keyBindings": {
    "interact": "E",
    "inventory": "I",
    "playerMenu": "Tab",
    "action": "Space",
    "reload": "R",
    "useItem": "F",
    "drawWeapon": "D",
    "holsterWeapon": "H"
  }
}
```

**Player Can Customize:**
- All key bindings are configurable
- UI for key binding configuration
- Save preferences per player
- Default bindings provided

## Notes

- **No Text Commands**: Players never type commands
- **Verbs as Interfaces**: Verbs become clickable actions
- **Player Commands**: Special commands on player object
- **Key Bindings**: All actions accessible via keys
- **UI Clicks**: All actions accessible via UI
- **Chat Interface**: Text input for communication only
  - Room chat (local, instant)
  - Object communication (via consoles)
  - Subspace communication (long-range, time-delayed)
  - See [COMMUNICATION_SYSTEM.md](../game-design/COMMUNICATION_SYSTEM.md) for details

## Related Documentation

- [CLIENT_ARCHITECTURE_CSharp.md](./CLIENT_ARCHITECTURE_CSharp.md) - Client architecture
- [SCRIPT_PROCESSOR_UNIFICATION.md](./SCRIPT_PROCESSOR_UNIFICATION.md) - Verb/function system
- [API_DESIGN.md](./API_DESIGN.md) - API for commands
