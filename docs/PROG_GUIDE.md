# CSMOO Programming Guide

A comprehensive guide to writing verbs and programming in the CSMOO environment.

## Table of Contents

- [Getting Started](#getting-started)
- [Verb Basics](#verb-basics)
- [JSON Verb Definitions](#json-verb-definitions)
- [Scripting Environment](#scripting-environment)
- [Available Variables](#available-variables)
- [Helper Methods](#helper-methods)
- [Movement System](#movement-system)
- [Object Management](#object-management)
- [Advanced Patterns](#advanced-patterns)
- [Best Practices](#best-practices)
- [Examples](#examples)

## Getting Started

CSMOO supports three ways to execute and manage scripts:

1. **Quick scripts** - Single-line commands using `;`, `th`, `think`, or `script`
2. **Multi-line scripts** - Using `@script` for complex code blocks (like `@program`)
3. **JSON verbs** - External file-based definitions with hot-reloading

All approaches use the same C# scripting engine and have access to the same functionality.

## Script Execution Methods

### Quick Single-Line Scripts

For quick testing and simple commands:
```bash
; notify(player, "Hello!");                    # Very short alias
th player.Location = "new-room-id";            # Short alias  
think here.description = "A modified room";    # Natural alias
script return player.Name;                     # Full command
```

### Multi-Line Scripts (`@script`)

For complex scripts with multiple statements:
```bash
@script
# Enters multi-line mode, then type:
var greeting = $"Hello, {player.Name}!";
notify(player, greeting);
here.description = "This room was modified by script";
SayToRoom($"{player.Name} just ran a script!");
.
# Type '.' on its own line to execute, or '.abort' to cancel
```

### JSON Verb Files

For permanent, reusable functionality - see [JSON Verb Definitions](#json-verb-definitions) section.

## Verb Basics

### What is a Verb?

A verb is a piece of C# code attached to an object that can be executed by players. Verbs define the interactive behavior of objects in the world.

### Verb Resolution Order

When a player types a command, CSMOO searches for matching verbs in this order:

1. **Objects in the room** (including the player)
2. **The room itself**
3. **The player object**
4. **Global system verbs**
5. **Movement fallback** (for direction commands)

### Verb Inheritance

Objects inherit verbs from their class hierarchy:
- **Instance verbs** override class verbs
- **Child class verbs** override parent class verbs
- Search goes from most specific to most general

## JSON Verb Definitions

### File Structure

JSON verbs are stored in the `resources/verbs/` directory:
- `resources/verbs/system/` - Global system verbs
- `resources/verbs/classes/` - Class-specific verbs

### Basic JSON Format

```json
{
  "name": "verbName",
  "aliases": "alias1 alias2",
  "pattern": "*",
  "description": "What this verb does",
  "targetClass": "ClassName",
  "code": [
    "// C# code goes here",
    "notify(player, \"Hello world!\");",
    "return \"Success\";"
  ]
}
```

### Required Fields

- **name**: The primary name of the verb
- **code**: Array of C# code lines

### Optional Fields

- **aliases**: Space-separated list of alternative names
- **pattern**: Command pattern for argument matching
- **description**: Human-readable description
- **targetClass**: For class verbs, specifies which class

### Hot Reloading

Use the `@verbreload` command to reload all JSON verb definitions without restarting the server.

## Scripting Environment

### Language Support

CSMOO verbs support full C# 12 syntax including:
- All control structures (`if`, `for`, `foreach`, `while`, `switch`)
- LINQ queries and method syntax
- Lambda expressions (`=>`)
- Type inference (`var`)
- Exception handling (`try/catch/finally`)
- Async/await (where applicable)

### Imports Available

The following namespaces are automatically imported:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using CSMOO.Server.Database;
using CSMOO.Server.Commands;
```

## Available Variables

### Core Variables

```csharp
player      // The player executing the command (Player object)
here        // The player's current location (ScriptObject)
me          // Alias for player (ScriptObject)
this        // The object the verb is running on (ScriptObject)
Args        // List<string> of command arguments
Input       // Complete input string that triggered the verb
Verb        // Name of the verb being executed
```

**Note**: For the `script` command, these variables work slightly differently:
- `player` - The player executing the script
- `here` - The player's current room 
- `me` - Alias for the player (same as `player`)
- `this` - The system object (since script runs in system context)
- `Args` - The script code arguments
- Standard C# variables and LINQ are also available

### Global Objects

```csharp
Helpers              // ScriptHelpers instance with utility methods
CommandProcessor     // Command processor for sending messages
```

## Helper Methods

### Communication

```csharp
// Send message to current player
notify(player, "Message text");
Say("Message to current player");

// Send to all players in room except current player
SayToRoom("Message to room");

// Send to specific player
notify(targetPlayer, "Private message");
```

### Object Interaction

```csharp
// Get/set object properties
var value = Helpers.GetProperty(obj, "propertyName");
Helpers.SetProperty(obj, "propertyName", value);

// Move objects
Helpers.MoveObject(object, destinationId);

// Update player location (use after moving player)
Helpers.UpdatePlayerLocation(player, newLocationId);
```

### Room and Location

```csharp
// Show current room description
Helpers.ShowRoom();

// Get exits from a room
var exits = Helpers.GetExitsFromRoom(roomId);

// Look at an object
Helpers.LookAtObject("objectName");
```

### Object Discovery

```csharp
// Find objects in current room
var objects = ObjectManager.GetObjectsInLocation(here.ObjectId);

// Find object by name in room
var obj = Helpers.FindObjectInRoom("objectName");
```

## Movement System

### Natural Movement

Players can move using unprefixed direction commands:
```bash
north    # instead of 'go north'
n        # abbreviation for north
south
southeast
se       # abbreviation for southeast
up
down
```

### How It Works

The movement system:
1. Checks if the word matches any available exit direction
2. Supports common abbreviations (`n`, `s`, `e`, `w`, `ne`, `nw`, `se`, `sw`, `u`, `d`)
3. Calls the existing `go` verb for consistency
4. Only triggers if no other verb matches the command

### Custom Exit Names

Exits can have custom names and aliases:
```json
{
  "direction": "portal",
  "aliases": "gate gateway",
  "destination": "room-id"
}
```

## Object Management

### Creating Objects

```csharp
// Create a new object instance
var obj = ObjectManager.CreateInstance(classId, locationId);

// Set initial properties
ObjectManager.SetProperty(obj, "name", "A shiny sword");
ObjectManager.SetProperty(obj, "description", "This sword gleams in the light.");
```

### Property Management

```csharp
// Get property (returns BsonValue)
var name = ObjectManager.GetProperty(objectId, "name")?.AsString;
var weight = ObjectManager.GetProperty(objectId, "weight")?.AsInt32 ?? 0;

// Set property
ObjectManager.SetProperty(objectId, "name", "New Name");
ObjectManager.SetProperty(objectId, "weight", 10);
```

### Object Relationships

```csharp
// Get objects in a location
var contents = ObjectManager.GetObjectsInLocation(locationId);

// Move object to new location
ObjectManager.MoveObject(objectId, newLocationId);
```

## Advanced Patterns

### State Management

```csharp
// Store complex state in object properties
var state = new Dictionary<string, object>
{
    ["health"] = 100,
    ["mana"] = 50,
    ["inventory"] = new List<string>()
};
ObjectManager.SetProperty(this.ObjectId, "gameState", state);
```

### Cross-Object Communication

```csharp
// Call verbs on other objects (if available)
var result = CallVerb(targetObjectId, "verbName", arg1, arg2);

// Check if object has a verb
if (HasVerb(objectId, "verbName"))
{
    CallVerb(objectId, "verbName");
}
```

### Error Handling

```csharp
try
{
    // Risky operation
    var result = SomeOperation();
    notify(player, $"Success: {result}");
}
catch (Exception ex)
{
    notify(player, $"Error: {ex.Message}");
    // Log errors for debugging
    Logger.Error($"Verb error: {ex.Message}");
}
```

## Best Practices

### Code Organization

1. **Keep verbs focused** - One verb should do one thing well
2. **Use meaningful names** - Verb names should be clear and descriptive
3. **Handle edge cases** - Check for null values and invalid input
4. **Provide feedback** - Always give the player feedback about what happened

### Performance

1. **Avoid expensive operations** in frequently-called verbs
2. **Cache lookups** when possible
3. **Use LINQ judiciously** - It's powerful but can be slow for large datasets
4. **Clean up resources** - Dispose of objects when done

### Security

1. **Validate input** - Never trust user input
2. **Check permissions** - Ensure players can perform the action
3. **Sanitize output** - Prevent injection attacks
4. **Use safe defaults** - Fail securely when things go wrong

### Documentation

1. **Add comments** to complex logic
2. **Use descriptive variable names**
3. **Include usage examples** in verb descriptions
4. **Document expected arguments** and return values

## Examples

### Simple Greeting Verb

```json
{
  "name": "greet",
  "aliases": "hello hi",
  "description": "Greet someone warmly",
  "code": [
    "if (Args.Count == 0)",
    "{",
    "    Say(\"Hello everyone!\");",
    "    SayToRoom($\"{player.Name} waves hello.\");",
    "}",
    "else",
    "{",
    "    var target = Args[0];",
    "    Say($\"Hello, {target}!\");",
    "    SayToRoom($\"{player.Name} greets {target}.\");",
    "}",
    "return \"Greeting complete.\";"
  ]
}
```

### Look at Object Verb

```json
{
  "name": "examine",
  "aliases": "look exam",
  "pattern": "*",
  "description": "Examine an object in detail",
  "code": [
    "if (Args.Count == 0)",
    "{",
    "    Helpers.ShowRoom();",
    "    return;",
    "}",
    "",
    "var target = string.Join(\" \", Args);",
    "var obj = Helpers.FindObjectInRoom(target);",
    "",
    "if (obj == null)",
    "{",
    "    notify(player, $\"You don't see '{target}' here.\");",
    "    return;",
    "}",
    "",
    "var name = Helpers.GetProperty(obj, \"name\")?.AsString ?? \"something\";",
    "var desc = Helpers.GetProperty(obj, \"description\")?.AsString ?? \"You see nothing special.\";",
    "",
    "notify(player, $\"=== {name} ===\");",
    "notify(player, desc);"
  ]
}
```

### Combat System Verb

```json
{
  "name": "attack",
  "description": "Attack a target",
  "code": [
    "if (Args.Count == 0)",
    "{",
    "    notify(player, \"Attack what?\");",
    "    return;",
    "}",
    "",
    "var targetName = Args[0];",
    "var target = Helpers.FindObjectInRoom(targetName);",
    "",
    "if (target == null)",
    "{",
    "    notify(player, $\"You don't see '{targetName}' here.\");",
    "    return;",
    "}",
    "",
    "// Get player's weapon",
    "var weapon = Helpers.GetProperty(player, \"weapon\")?.AsString ?? \"fists\";",
    "var damage = weapon == \"sword\" ? 10 : 5;",
    "",
    "// Apply damage",
    "var currentHealth = Helpers.GetProperty(target, \"health\")?.AsInt32 ?? 100;",
    "var newHealth = Math.Max(0, currentHealth - damage);",
    "Helpers.SetProperty(target, \"health\", newHealth);",
    "",
    "// Messages",
    "Say($\"You attack {targetName} with your {weapon} for {damage} damage!\");",
    "SayToRoom($\"{player.Name} attacks {targetName}!\");",
    "",
    "if (newHealth <= 0)",
    "{",
    "    SayToRoom($\"{targetName} has been defeated!\");",
    "    // Move defeated object to 'limbo' or handle death",
    "    Helpers.MoveObject(target, \"limbo-room-id\");",
    "}",
    "else",
    "{",
    "    notify(player, $\"{targetName} has {newHealth} health remaining.\");",
    "}"
  ]
}
```

### Movement Verb (Custom)

```json
{
  "name": "teleport",
  "aliases": "tp",
  "description": "Teleport to a room by name",
  "code": [
    "if (Args.Count == 0)",
    "{",
    "    notify(player, \"Teleport where?\");",
    "    return;",
    "}",
    "",
    "var roomName = string.Join(\" \", Args).ToLower();",
    "",
    "// Find room by name (simplified - you might want a proper room registry)",
    "var rooms = ObjectManager.GetObjectsOfClass(\"Room\");",
    "GameObject targetRoom = null;",
    "",
    "foreach (var room in rooms)",
    "{",
    "    var name = Helpers.GetProperty(room, \"name\")?.AsString?.ToLower();",
    "    if (name != null && name.Contains(roomName))",
    "    {",
    "        targetRoom = room;",
    "        break;",
    "    }",
    "}",
    "",
    "if (targetRoom == null)",
    "{",
    "    notify(player, $\"No room found matching '{roomName}'.\");",
    "    return;",
    "}",
    "",
    "// Teleport the player",
    "SayToRoom($\"{player.Name} disappears in a flash of light!\");",
    "Helpers.MoveObject(player, targetRoom.Id);",
    "Helpers.UpdatePlayerLocation(player, targetRoom.Id);",
    "SayToRoom($\"{player.Name} appears in a flash of light!\");",
    "Helpers.ShowRoom();"
  ]
}
```

### Hot Reload System Verb

```json
{
  "name": "verbreload",
  "aliases": "@verbreload",
  "description": "Hot reload all verb definitions from JSON files",
  "code": [
    "// Check admin privileges",
    "if (player.Name != \"admin\")",
    "{",
    "    notify(player, \"Permission denied. This command requires admin privileges.\");",
    "    return;",
    "}",
    "",
    "notify(player, \"Reloading verb definitions...\");",
    "",
    "try",
    "{",
    "    CSMOO.Server.Database.World.VerbInitializer.ReloadVerbs();",
    "    notify(player, \"Verb definitions reloaded successfully!\");",
    "}",
    "catch (Exception ex)",
    "{",
    "    notify(player, $\"Error reloading verbs: {ex.Message}\");",
    "}"
  ]
}
```

### Script Execution Verb

```json
{
  "name": "script",
  "aliases": "@script",
  "description": "Execute C# script code directly with access to player, me, here, and this",
  "code": [
    "// Check if any code was provided",
    "if (Args.Count == 0 || string.Join(\" \", Args).Trim().Length == 0)",
    "{",
    "    notify(player, \"Usage: script { C# code here }\");",
    "    notify(player, \"Available variables: player, me, here, this, Args, Input, Verb\");",
    "    notify(player, \"Example: script { notify(player, $\\\"Hello {player.Name}!\\\"); }\");",
    "    return;",
    "}",
    "",
    "// Join all arguments to reconstruct the script code",
    "var scriptCode = string.Join(\" \", Args);",
    "",
    "try",
    "{",
    "    // Use the script engine to execute the code with full globals access",
    "    var scriptEngine = new CSMOO.Server.Scripting.ScriptEngine();",
    "    var result = scriptEngine.ExecuteScript(scriptCode, player, CommandProcessor);",
    "    ",
    "    if (!string.IsNullOrEmpty(result))",
    "    {",
    "        notify(player, $\"Script result: {result}\");",
    "    }",
    "}",
    "catch (Exception ex)",
    "{",
    "    notify(player, $\"Script error: {ex.Message}\");",
    "}"
  ]
}
```

---

This guide covers the fundamental concepts and advanced patterns for programming in CSMOO. For more examples, check the `resources/verbs/` directory and the existing system verbs.

Happy coding! 🚀
