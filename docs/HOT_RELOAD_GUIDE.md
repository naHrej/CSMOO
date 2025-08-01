# Hot Reload System for CSMOO

The CSMOO server now supports comprehensive hot reloading of both application components and core code without requiring a full server restart, keeping all players connected while you make changes.

## Types of Hot Reload

### 1. Verb Hot Reload (Always Available)
- **What**: JSON verb definition files
- **Path**: `resources/verbs/**/*.json`
- **Trigger**: Automatic file watching + manual commands
- **Scope**: Verb definitions, patterns, and code

### 2. Core Application Hot Reload (Development Mode)
- **What**: Core C# application code files
- **Path**: `Server/**/*.cs` and other C# files
- **Trigger**: .NET Hot Reload (when using `dotnet watch`)
- **Scope**: Server logic, commands, database operations, etc.

## Features

### Automatic File Watching
The hot reload system automatically monitors:
- **Verb definitions**: `resources/verbs/**/*.json` 
- **Function definitions**: `resources/functions/**/*.json`
- **Property definitions**: `resources/properties/**/*.json`
- **Core C# code**: `Server/**/*.cs` and other C# files (Development mode only)
- **C# scripts**: `Scripts/**/*.cs` (if the Scripts directory exists)

### Manual Commands
Administrators can trigger hot reloads manually using these commands:

#### @reload [target]
Manually trigger a hot reload of specific components:
- `@reload verbs` - Reload all verb definitions from JSON files
- `@reload functions` - Reload all function definitions from JSON files
- `@reload properties` - Reload all property definitions from JSON files
- `@reload scripts` - Reload the script engine
- `@reload status` - Show current hot reload status

#### @hotreload [action]
Control the verb/script hot reload system:
- `@hotreload enable` - Enable automatic file watching for verbs/scripts
- `@hotreload disable` - Disable automatic file watching (manual reloads still work)
- `@hotreload status` - Show detailed status information

#### @corehot [action]
Core application hot reload information and control:
- `@corehot status` - Show core hot reload status and capabilities
- `@corehot test` - Test core hot reload notifications

## JSON File Syntax Reference

The hot reload system supports three types of JSON configurations for different game components.

### Verb Definitions (`resources/verbs/**/*.json`)

Verbs define player commands and their behavior. Each JSON file contains an array of verb objects.

#### Basic Verb Example
```json
[
  {
    "aliases": ["look", "l"],
    "pattern": "look",
    "description": "Look around your current location",
    "code": "return this.GetProperty('description') || 'You see nothing special.';",
    "overwrite": true
  }
]
```

#### Advanced Verb with Pattern Matching
```json
[
  {
    "aliases": ["get", "take"],
    "pattern": "get <object>",
    "description": "Pick up an object",
    "code": [
      "var target = args.object;",
      "if (!target) {",
      "  return 'Get what?';",
      "}",
      "// Move object to player's inventory",
      "target.SetProperty('location', player.id);",
      "return `You take the ${target.GetProperty('name')}.`;"
    ],
    "overwrite": true
  }
]
```

#### Verb Properties
- **`aliases`** (array): Command names that trigger this verb
- **`pattern`** (string): Pattern for argument parsing (supports `<object>`, `<string>`, etc.)
- **`description`** (string): Help text for the verb
- **`code`** (string or array): JavaScript code to execute (arrays become multiline)
- **`overwrite`** (boolean): Whether to replace existing verbs with same aliases

### Function Definitions (`resources/functions/**/*.json`)

Functions define reusable JavaScript functions available to all scripts and verbs.

#### Basic Function Example
```json
[
  {
    "name": "formatName",
    "description": "Format an object's display name",
    "code": [
      "function formatName(obj) {",
      "  var name = obj.GetProperty('name') || 'something';",
      "  var article = obj.GetProperty('article') || 'a';",
      "  return article + ' ' + name;",
      "}"
    ],
    "overwrite": true
  }
]
```

#### Utility Function Example
```json
[
  {
    "name": "findObjectInRoom",
    "description": "Find an object in the current room by name",
    "code": [
      "function findObjectInRoom(player, objectName) {",
      "  var room = ObjectManager.GetObject(player.GetProperty('location'));",
      "  var contents = room.GetProperty('contents') || [];",
      "  ",
      "  for (var i = 0; i < contents.length; i++) {",
      "    var obj = ObjectManager.GetObject(contents[i]);",
      "    var name = obj.GetProperty('name') || '';",
      "    if (name.toLowerCase().includes(objectName.toLowerCase())) {",
      "      return obj;",
      "    }",
      "  }",
      "  return null;",
      "}"
    ],
    "overwrite": true
  }
]
```

#### Function Properties
- **`name`** (string): Function name (must be valid JavaScript identifier)
- **`description`** (string): Documentation for the function
- **`code`** (string or array): JavaScript function code (arrays become multiline)
- **`overwrite`** (boolean): Whether to replace existing functions with same name

### Property Definitions (`resources/properties/**/*.json`)

Properties define default values and configurations for game objects. Properties can be system-wide, class-based, or instance-specific.

#### System Properties (`resources/properties/system/*.json`)
```json
[
  {
    "name": "welcomeMessage",
    "type": "string",
    "value": "Welcome to CSMOO! Type 'help' for commands.",
    "description": "Message shown to new players",
    "overwrite": true
  },
  {
    "name": "serverRules",
    "type": "array",
    "lines": [
      "1. Be respectful to other players",
      "2. No spam or excessive shouting",
      "3. Report bugs to administrators",
      "4. Have fun!"
    ],
    "description": "Server rules displayed with 'rules' command",
    "overwrite": true
  }
]
```

#### Property Loading from File
```json
[
  {
    "name": "stylesheet",
    "type": "array",
    "filename": "stylesheet.css",
    "description": "CSS stylesheet loaded from file",
    "overwrite": true
  }
]
```

#### Class Properties (`resources/properties/classes/*.json`)
```json
[
  {
    "name": "maxHealth",
    "type": "int", 
    "value": 100,
    "targetClass": "Player",
    "description": "Maximum health for player characters",
    "overwrite": true
  },
  {
    "name": "description",
    "type": "string",
    "value": "A typical room with plain walls.",
    "targetClass": "Room",
    "description": "Default room description",
    "overwrite": false
  }
]
```

#### Instance Properties (`resources/properties/instances/*.json`)
```json
[
  {
    "name": "description",
    "type": "array",
    "lines": [
      "You are standing in the town square.",
      "A fountain bubbles merrily in the center,",
      "surrounded by shops and houses.",
      "Roads lead north, south, east, and west."
    ],
    "targetObject": "2",
    "description": "Description for the starting room",
    "overwrite": true
  },
  {
    "name": "exits",
    "type": "string",
    "value": "north,south,east,west",
    "targetObject": "town-square",
    "description": "Available exits from town square",
    "overwrite": true
  }
]
```

#### Property Types and Values
- **`name`** (string): Property name
- **`type`** (string): Data type - `string`, `int`, `bool`, `array`, etc.
- **`value`** (any): Simple property value
- **`lines`** (array): Multi-line string array (for `array` type)
- **`filename`** (string): Load content from file (relative to JSON file location)
- **`targetClass`** (string): Target object class (for class properties)
- **`targetObject`** (string): Target object ID or name (for instance properties)
- **`description`** (string): Documentation for the property
- **`overwrite`** (boolean): Whether to replace existing properties

#### Property Resolution Priority
1. **Instance properties** - Specific to individual objects
2. **Class properties** - Default values for all objects of a class
3. **System properties** - Global game settings

## How It Works

### Verb Hot Reload
When a `.json` file in the `resources/verbs/` directory is modified:
1. The file watcher detects the change
2. After a 500ms debounce period, the reload is triggered
3. All existing verbs are cleared from the database
4. All verb JSON files are re-processed and loaded
5. Online players are notified of the reload

### Function Hot Reload
When a `.json` file in the `resources/functions/` directory is modified:
1. The file watcher detects the change
2. After a 500ms debounce period, the reload is triggered
3. All existing functions are cleared from the script engine
4. All function JSON files are re-processed and loaded
5. Functions become immediately available to verbs and scripts

### Property Hot Reload
When a `.json` file in the `resources/properties/` directory is modified:
1. The file watcher detects the change
2. After a 500ms debounce period, the reload is triggered
3. Properties are loaded and applied based on their target:
   - **System properties**: Applied to the system object
   - **Class properties**: Applied as defaults for all instances of that class
   - **Instance properties**: Applied to specific objects by ID or name
4. Existing properties are only overwritten if `"overwrite": true`

### Core Application Hot Reload
When a `.cs` file in your project is modified:
1. **With `dotnet watch`**: .NET automatically compiles and hot reloads the changes
2. **File watching**: The system detects changes and notifies administrators
3. **Live updates**: Changes take effect immediately without restarting the server
4. **State preservation**: All player connections and game state remain intact

### Requirements for Core Hot Reload
- **Development mode**: Must be running in development environment
- **dotnet watch**: Best experience with `dotnet watch run`
- **Compatible changes**: Some changes (like adding new classes) may require restart

### Debouncing
The system uses debounce timers to prevent multiple rapid reloads:
- **Verb changes**: 500ms debounce
- **Function changes**: 500ms debounce  
- **Property changes**: 500ms debounce
- **Core code changes**: 1 second debounce (for notifications)

### Statistics
The reload process reports how many items were:
- **Created**: New items loaded from JSON files
- **Skipped**: Items that already existed and weren't recreated
- **Applied**: Properties successfully set on objects

## Security

Hot reload commands require specific permission flags:
- `@reload` commands require the **Admin** flag
- `@hotreload` commands require the **Admin** flag

## Configuration

The server configuration is managed via `config.json`. You can set the server port, database file locations, logging options, and hot reload features.

### Example config.json
```jsonc
{
  "Server": {
    "Port": 1701,
    "ShowDebugInConsole": false
  },
  "HotReload": {
    "Enabled": false // Set to true to enable hot reload features
  },
  "Database": {
    "GameDataFile": "gamedata.db",
    "LogDataFile": "gamedata-log.db"
  },
  "Logging": {
    "EnableConsoleLogging": true,
    "EnableFileLogging": true,
    "GameLogFile": "logs/game.log",
    "DebugLogFile": "logs/debug.log",
    "LogLevel": "Debug",
    "MaxLogFiles": 5
  }
}
```

## Hot Reload Manager

The Hot Reload Manager allows you to update verb and script files without restarting the server. By default, it is disabled. To enable, set `"HotReload": { "Enabled": true }` in your `config.json`.

**Note:** When disabled, changes to verb or script files will not be automatically reloaded until the server is restarted.

## Benefits

1. **No Downtime**: Keep all players connected while making changes
2. **Rapid Development**: Test verb changes immediately without restart
3. **Automatic**: File changes trigger reloads automatically
4. **Safe**: Debouncing prevents reload spam
5. **Informative**: Clear logging and status information

## Getting Started with Core Hot Reload

### Option 1: Run with Hot Reload Scripts (Recommended)
```bash
# Windows
run-with-hotreload.bat

# Linux/Mac
chmod +x run-with-hotreload.sh
./run-with-hotreload.sh
```

### Option 2: Manual dotnet watch
```bash
# Set development environment
set DOTNET_ENVIRONMENT=Development  # Windows
export DOTNET_ENVIRONMENT=Development  # Linux/Mac

# Run with hot reload
dotnet watch run
```

### Option 3: Regular run (Verb hot reload only)
```bash
dotnet run
```

## Example Usage

```
> @corehot status
🔥 Core Hot Reload Status:

🔥 Core hot reload enabled (Development mode)
✅ .NET Hot Reload APIs available
👀 Watching core C# files for changes

💡 For best experience, run with: dotnet watch run

> @hotreload status
✅ Hot Reload Status: ENABLED
📁 Monitoring the following directories:
  • resources/verbs/ (*.json) - Verb definitions
  • resources/functions/ (*.json) - Function definitions
  • resources/properties/ (*.json) - Property definitions
  • Scripts/ (*.cs) - C# script files [if present]
🔄 Changes to these files will trigger automatic reloads

> @reload verbs
🔄 Initiating manual verb reload...
✅ Verb reload completed successfully!

> @reload functions
🔄 Initiating manual function reload...
✅ Function reload completed successfully!

> @reload properties
🔄 Initiating manual property reload...
✅ Property reload completed successfully!

> @reload status
Hot Reload Status: ENABLED
Monitored paths:
  • resources/verbs/ (*.json)
  • resources/functions/ (*.json) 
  • resources/properties/ (*.json)
  • Scripts/ (*.cs) [if present]
```

## Development Workflow

### With Core Hot Reload (Recommended)
1. Start server with `dotnet watch run` or use the provided scripts
2. Edit any C# file in your project (commands, database operations, etc.)
3. Save the file
4. Watch the console for ".NET Hot Reload completed successfully!"
5. Test your changes immediately in-game - **no restart needed!**

### With Verb/Function/Property Hot Reload
1. Edit JSON files in `resources/verbs/`, `resources/functions/`, or `resources/properties/`
2. Save the file
3. Watch the server console for reload confirmation
4. Test your changes immediately in-game
5. No need to restart the server or reconnect players

### What Can Be Hot Reloaded?

#### ✅ Always Supported (JSON Hot Reload):
- **Verb definitions**: Commands, patterns, aliases, and code
- **Function definitions**: Reusable JavaScript functions
- **Property definitions**: Object properties and default values
- **File-based properties**: CSS, text files, and other resources

#### ✅ Supported (Core Hot Reload):
- Method implementations
- Property implementations
- Adding new methods to existing classes
- Changing method logic
- Updating command handlers
- Modifying database operations
- Changes to existing classes

#### ❌ Not Supported (Requires Restart):
- Adding new classes
- Changing class inheritance
- Adding/removing fields
- Changing method signatures
- Major structural changes

#### ✅ Property Hot Reload Features:
- **File loading**: Properties can load content from external files
- **Type conversion**: Automatic conversion to appropriate data types
- **Hierarchical application**: System → Class → Instance property precedence
- **Selective overwriting**: Only overwrite when explicitly requested
- **Multi-line support**: Arrays of strings for complex text properties

#### ✅ Always Supported (Verb Hot Reload):
- Verb JSON definitions
- Verb code and patterns
- Verb aliases and descriptions

## Logging

Hot reload operations are logged at the Info level:
- Reload start and completion
- Statistics (created vs skipped)
- Error information if reloads fail
- File change notifications (Debug level)

This makes it easy to monitor the hot reload system and troubleshoot any issues.
