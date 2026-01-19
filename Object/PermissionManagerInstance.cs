using CSMOO.Database;
using CSMOO.Logging;

namespace CSMOO.Object;

/// <summary>
/// Instance-based permission manager implementation for dependency injection
/// </summary>
public class PermissionManagerInstance : IPermissionManager
{
    private readonly IDbProvider _dbProvider;
    private readonly ILogger _logger;
    
    public PermissionManagerInstance(IDbProvider dbProvider, ILogger logger)
    {
        _dbProvider = dbProvider;
        _logger = logger;
    }
    
    /// <summary>
    /// Check if a player has a specific flag
    /// </summary>
    public bool HasFlag(Player player, PermissionManager.Flag flag)
    {
        if (player?.Permissions == null) return false;
        return player.Permissions.Contains(flag.ToString().ToLower());
    }

    /// <summary>
    /// Grant a flag to a player
    /// </summary>
    public bool GrantFlag(Player targetPlayer, PermissionManager.Flag flag, Player grantingPlayer)
    {
        if (targetPlayer?.Permissions == null)
        {
            _logger.Error("Cannot grant flag: target player or permissions list is null");
            return false;
        }

        // Check if granting player has permission to grant this flag
        if (!CanGrantFlag(grantingPlayer, flag))
        {
            return false;
        }

        var flagStr = flag.ToString().ToLower();
        if (!targetPlayer.Permissions.Contains(flagStr))
        {
            var perms = targetPlayer.Permissions;
            perms.Add(flagStr);
            targetPlayer.Permissions = perms;
            _dbProvider.Update("players", targetPlayer);
            _logger.Info($"Flag {flag} granted to player {targetPlayer.Name} by {grantingPlayer?.Name}");
            return true;
        }

        return false; // Already has the flag
    }

    /// <summary>
    /// Remove a flag from a player
    /// </summary>
    public bool RemoveFlag(Player targetPlayer, PermissionManager.Flag flag, Player removingPlayer)
    {
        if (targetPlayer?.Permissions == null)
        {
            _logger.Error("Cannot remove flag: target player or permissions list is null");
            return false;
        }

        // Protect the original admin from having their Admin flag removed
        if (flag == PermissionManager.Flag.Admin && targetPlayer.Name.Equals(PermissionManager.ORIGINAL_ADMIN_NAME, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Warning($"Attempt to remove Admin flag from original admin player {targetPlayer.Name} was blocked");
            return false;
        }

        // Check if removing player has permission to remove this flag
        if (!CanRemoveFlag(removingPlayer, flag))
        {
            return false;
        }

        var flagStr = flag.ToString().ToLower();
        if (targetPlayer.Permissions.Contains(flagStr))
        {
            var flags = targetPlayer.Permissions;
            flags.Remove(flagStr);
            targetPlayer.Permissions = flags;
            _dbProvider.Update("players", targetPlayer);
            _logger.Info($"Flag {flag} removed from player {targetPlayer.Name} by {removingPlayer?.Name}");
            return true;
        }

        return false; // Didn't have the flag
    }

    /// <summary>
    /// Check if a player can grant a specific flag
    /// </summary>
    public bool CanGrantFlag(Player player, PermissionManager.Flag flag)
    {
        if (player?.Permissions == null) return false;

        return flag switch
        {
            PermissionManager.Flag.Admin => HasFlag(player, PermissionManager.Flag.Admin),        // Only admins can grant admin
            PermissionManager.Flag.Programmer => HasFlag(player, PermissionManager.Flag.Admin),   // Only admins can grant programmer
            PermissionManager.Flag.Moderator => HasFlag(player, PermissionManager.Flag.Admin),    // Only admins can grant moderator
            _ => false
        };
    }

    /// <summary>
    /// Check if a player can remove a specific flag
    /// </summary>
    public bool CanRemoveFlag(Player player, PermissionManager.Flag flag)
    {
        if (player?.Permissions == null) return false;

        return flag switch
        {
            PermissionManager.Flag.Admin => HasFlag(player, PermissionManager.Flag.Admin),        // Only admins can remove admin
            PermissionManager.Flag.Programmer => HasFlag(player, PermissionManager.Flag.Admin),   // Only admins can remove programmer
            PermissionManager.Flag.Moderator => HasFlag(player, PermissionManager.Flag.Admin),    // Only admins can remove moderator
            _ => false
        };
    }

    /// <summary>
    /// Get all flags a player has
    /// </summary>
    public List<PermissionManager.Flag> GetPlayerFlags(GameObject go)
    {
        // Use the Permissions property directly, do not reload from DB
        if (go?.Permissions == null) return new List<PermissionManager.Flag>();

        var flags = new List<PermissionManager.Flag>();
        foreach (var permission in go.Permissions)
        {
            if (Enum.TryParse<PermissionManager.Flag>(permission, true, out var flag))
            {
                flags.Add(flag);
            }
        }
        return flags;
    }

    /// <summary>
    /// Get a formatted string of all flags a player has
    /// </summary>
    public string GetFlagsString(Player player)
    {
        var flags = GetPlayerFlags(player);
        if (!flags.Any()) return "none";
        
        var flagChars = flags.Select(f => f switch
        {
            PermissionManager.Flag.Admin => "A",
            PermissionManager.Flag.Programmer => "P",
            PermissionManager.Flag.Moderator => "M",
            _ => "?"
        });
        
        return string.Join("", flagChars);
    }

    /// <summary>
    /// Initialize default permissions for the admin player
    /// </summary>
    public void InitializeAdminPermissions(Player adminPlayer)
    {
        if (adminPlayer == null)
        {
            _logger.Error("Cannot initialize admin permissions: adminPlayer is null");
            return;
        }

        // Initialize Permissions list if it's null
        if (adminPlayer.Permissions == null)
        {
            adminPlayer.Permissions = new List<string>();
            _logger.Info($"Initializing null Permissions list for player {adminPlayer.Name}");
        }

        adminPlayer.Permissions = new List<string>
        {
            PermissionManager.Flag.Admin.ToString().ToLower(),
            PermissionManager.Flag.Programmer.ToString().ToLower()
        };
        
        _dbProvider.Update("players", adminPlayer);
        _logger.Info($"Initialized admin permissions for player {adminPlayer.Name}: {GetFlagsString(adminPlayer)}");
        
        // Verify the permissions were saved correctly
        var savedPlayer = _dbProvider.FindById<Player>("players", adminPlayer.Id);
        if (savedPlayer != null)
        {
            var savedFlags = GetFlagsString(savedPlayer);
            _logger.Info($"Verified saved permissions for player {adminPlayer.Name}: {savedFlags}");
        }
        else
        {
            _logger.Warning($"Could not verify saved permissions for player {adminPlayer.Name} - player not found in database");
        }
    }
}
