# Hot Reload System for CSMOO

The CSMOO server now supports comprehensive hot reloading of both application components and core code without requiring a full server restart, keeping all players connected while you make changes.

## Types of Hot Reload

### 1. Verb Hot Reload (Always Available)
- **What**: JSON verb definition files
- **Path**: `Resources/verbs/**/*.json`
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
- **Verb definitions**: `Resources/verbs/**/*.json` 
- **Core C# code**: `Server/**/*.cs` and other C# files (Development mode only)
- **C# scripts**: `Scripts/**/*.cs` (if the Scripts directory exists)

### Manual Commands
Administrators can trigger hot reloads manually using these commands:

#### @reload [target]
Manually trigger a hot reload of specific components:
- `@reload verbs` - Reload all verb definitions from JSON files
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

## How It Works

### Verb Hot Reload
When a `.json` file in the `Resources/verbs/` directory is modified:
1. The file watcher detects the change
2. After a 500ms debounce period, the reload is triggered
3. All existing verbs are cleared from the database
4. All verb JSON files are re-processed and loaded
5. Online players are notified of the reload

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
- **Core code changes**: 1 second debounce (for notifications)

### Statistics
The reload process reports how many verbs were:
- **Created**: New verbs loaded from JSON files
- **Skipped**: Verbs that already existed and weren't recreated

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
  • Resources/verbs/ (*.json) - Verb definitions
  • Scripts/ (*.cs) - C# script files [if present]
🔄 Changes to these files will trigger automatic reloads

> @reload verbs
🔄 Initiating manual verb reload...
✅ Verb reload completed successfully!
```

## Development Workflow

### With Core Hot Reload (Recommended)
1. Start server with `dotnet watch run` or use the provided scripts
2. Edit any C# file in your project (commands, database operations, etc.)
3. Save the file
4. Watch the console for ".NET Hot Reload completed successfully!"
5. Test your changes immediately in-game - **no restart needed!**

### With Verb Hot Reload
1. Edit verb JSON files in `Resources/verbs/`
2. Save the file
3. Watch the server console for reload confirmation
4. Test your changes immediately in-game
5. No need to restart the server or reconnect players

### What Can Be Hot Reloaded?

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
