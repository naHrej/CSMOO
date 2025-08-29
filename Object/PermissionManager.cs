using CSMOO.Database;
using CSMOO.Logging;

namespace CSMOO.Object;

/// <summary>
/// Manages player permissions and flags in a PennMUSH-style system
/// </summary>
public static class PermissionManager
{
    /// <summary>
    /// Available permission flags
    /// </summary>
    public enum Flag
    {
        Admin,      // Full administrative privileges - cannot be removed from original admin
        Programmer, // Can use @program and other programming commands
        Moderator   // Moderation privileges (future use)
    }

    /// <summary>
    /// The original admin player name - this player's Admin flag cannot be removed
    /// </summary>
    public const string ORIGINAL_ADMIN_NAME = "admin";

    /// <summary>
    /// Check if a player has a specific flag
    /// </summary>
    public static bool HasFlag(Player player, Flag flag)
    {
        if (player?.Permissions == null) return false;
        return player.Permissions.Contains(flag.ToString().ToLower());
    }

    /// <summary>
    /// Grant a flag to a player
    /// </summary>
    public static bool GrantFlag(Player targetPlayer, Flag flag, Player grantingPlayer)
    {
        if (targetPlayer?.Permissions == null)
        {
            Logger.Error("Cannot grant flag: target player or permissions list is null");
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
            DbProvider.Instance.Update("players", targetPlayer);
            Logger.Info($"Flag {flag} granted to player {targetPlayer.Name} by {grantingPlayer?.Name}");
            return true;
        }

        return false; // Already has the flag
    }

    /// <summary>
    /// Remove a flag from a player
    /// </summary>
    public static bool RemoveFlag(Player targetPlayer, Flag flag, Player removingPlayer)
    {
        if (targetPlayer?.Permissions == null)
        {
            Logger.Error("Cannot remove flag: target player or permissions list is null");
            return false;
        }

        // Protect the original admin from having their Admin flag removed
        if (flag == Flag.Admin && targetPlayer.Name.Equals(ORIGINAL_ADMIN_NAME, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Warning($"Attempt to remove Admin flag from original admin player {targetPlayer.Name} was blocked");
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
            DbProvider.Instance.Update("players", targetPlayer);
            Logger.Info($"Flag {flag} removed from player {targetPlayer.Name} by {removingPlayer?.Name}");
            return true;
        }

        return false; // Didn't have the flag
    }

    /// <summary>
    /// Check if a player can grant a specific flag
    /// </summary>
    public static bool CanGrantFlag(Player player, Flag flag)
    {
        if (player?.Permissions == null) return false;

        return flag switch
        {
            Flag.Admin => HasFlag(player, Flag.Admin),        // Only admins can grant admin
            Flag.Programmer => HasFlag(player, Flag.Admin),   // Only admins can grant programmer
            Flag.Moderator => HasFlag(player, Flag.Admin),    // Only admins can grant moderator
            _ => false
        };
    }

    /// <summary>
    /// Check if a player can remove a specific flag
    /// </summary>
    public static bool CanRemoveFlag(Player player, Flag flag)
    {
        if (player?.Permissions == null) return false;

        return flag switch
        {
            Flag.Admin => HasFlag(player, Flag.Admin),        // Only admins can remove admin
            Flag.Programmer => HasFlag(player, Flag.Admin),   // Only admins can remove programmer
            Flag.Moderator => HasFlag(player, Flag.Admin),    // Only admins can remove moderator
            _ => false
        };
    }

    /// <summary>
    /// Get all flags a player has
    /// </summary>
public static List<Flag> GetPlayerFlags(GameObject go)
{
    // Use the Permissions property directly, do not reload from DB
    if (go?.Permissions == null) return new List<Flag>();

    var flags = new List<Flag>();
    foreach (var permission in go.Permissions)
    {
        if (Enum.TryParse<Flag>(permission, true, out var flag))
        {
            flags.Add(flag);
        }
    }
    return flags;
}

    /// <summary>
    /// Get a formatted string of all flags a player has
    /// </summary>
    public static string GetFlagsString(Player player)
    {
        var flags = GetPlayerFlags(player);
        if (!flags.Any()) return "none";
        
        var flagChars = flags.Select(f => f switch
        {
            Flag.Admin => "A",
            Flag.Programmer => "P",
            Flag.Moderator => "M",
            _ => "?"
        });
        
        return string.Join("", flagChars);
    }

    /// <summary>
    /// Initialize default permissions for the admin player
    /// </summary>
    public static void InitializeAdminPermissions(Player adminPlayer)
    {
        if (adminPlayer?.Permissions == null) return;

        adminPlayer.Permissions = new List<string>
        {
            Flag.Admin.ToString().ToLower(),
            Flag.Programmer.ToString().ToLower()
        };
        
        DbProvider.Instance.Update("players", adminPlayer);
        Logger.Info($"Initialized admin permissions for player {adminPlayer.Name}: {GetFlagsString(adminPlayer)}");
    }
}



