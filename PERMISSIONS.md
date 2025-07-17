# CSMOO Permission System

## Overview

CSMOO now implements a PennMUSH-style permission flag system with three levels of access:

- **Admin (A)**: Full administrative privileges
- **Programmer (P)**: Can use programming commands (@program, @verb, @edit, @rmverb, etc.)
- **Moderator (M)**: Moderation privileges (reserved for future use)

## Permissions Model

### Flag Hierarchy
- Only Admin players can grant or remove any flags
- The original admin player (username: "admin") cannot have their Admin flag removed
- Programming commands require the Programmer flag
- Administrative commands require the Admin flag

### Protected Commands
The following commands now require the **Programmer** flag:
- `@program <object>:<verb>` - Create/edit verb code
- `@verb <object> <name>` - Create new verbs
- `@edit <object>:<verb>` - Edit existing verbs  
- `@rmverb <object>:<verb>` - Remove verbs

### Administrative Commands
The following commands require the **Admin** flag:
- `@flag <player> +/-<flag>` - Grant/remove flags
- `@update-permissions` - Migrate old permissions to new system

### Converted Commands
The following commands have been converted from built-in to verb-based:
- `examine` (alias: `ex`) - Examine objects, players, and environment. Shows admin info for Admin/Moderator users

## Command Reference

### Flag Management

#### View Flags
```
@flags              # Show your own flags
@flags <player>     # Show another player's flags
```

#### Grant/Remove Flags (Admin only)
```
@flag <player> +admin      # Grant Admin flag
@flag <player> +programmer # Grant Programmer flag  
@flag <player> +moderator  # Grant Moderator flag
@flag <player> -programmer # Remove Programmer flag
@flag <player> -moderator  # Remove Moderator flag
```

Note: The Admin flag cannot be removed from the original admin player.

### System Migration

#### Update Legacy Permissions (Admin only)
```
@update-permissions
```
This command converts old permission names:
- "admin" → Admin flag
- "builder" → Programmer flag

## Implementation Details

### Files Modified
- `Server/Database/PermissionManager.cs` - New permission management system
- `Server/Database/PlayerManager.cs` - Added FindPlayerByName method
- `Server/Commands/ProgrammingCommands.cs` - Added permission checks and flag commands
- `Server/ServerInitializer.cs` - Updated admin initialization

### Database Changes
The existing `Player.Permissions` List<string> property is used to store flags as lowercase strings:
- "admin" for Admin flag
- "programmer" for Programmer flag  
- "moderator" for Moderator flag

### Permission Checks
Permission checks are performed using the `PermissionManager` class:
```csharp
PermissionManager.HasFlag(player, PermissionManager.Flag.Admin)
PermissionManager.HasFlag(player, PermissionManager.Flag.Programmer)
```

## Security Features

1. **Original Admin Protection**: The admin user created during initialization cannot have their Admin flag removed
2. **Hierarchical Permissions**: Only Admins can grant/remove flags  
3. **Command Protection**: Programming commands are restricted to users with appropriate flags
4. **Audit Logging**: Flag changes are logged for security tracking

## Flag Display

Players' flags are displayed as single characters:
- **A** = Admin
- **P** = Programmer  
- **M** = Moderator

Example: A player with Admin and Programmer flags shows as "AP"

## Future Enhancements

The system is designed to be extensible. Additional flags can be added by:
1. Adding new entries to the `PermissionManager.Flag` enum
2. Updating the flag character mapping in `GetFlagsString()`
3. Adding permission checks to relevant commands
