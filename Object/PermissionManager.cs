using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Configuration;

namespace CSMOO.Object;

/// <summary>
/// Static wrapper for PermissionManager (backward compatibility)
/// Delegates to PermissionManagerInstance for dependency injection support
/// </summary>
public static class PermissionManager
{
    private static IPermissionManager? _instance;
    
    /// <summary>
    /// Sets the permission manager instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IPermissionManager instance)
    {
        _instance = instance;
    }
    
    private static IPermissionManager Instance => _instance ?? throw new InvalidOperationException("PermissionManager instance not set. Call PermissionManager.SetInstance() first.");
    
    /// <summary>
    /// Ensures an instance exists (creates default if not set)
    /// </summary>
    private static void EnsureInstance()
    {
        if (_instance == null)
        {
            // Create default instances for backward compatibility
            var dbProvider = DbProvider.Instance;
            var logger = new LoggerInstance(Config.Instance);
            _instance = new PermissionManagerInstance(dbProvider, logger);
        }
    }
    
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
        EnsureInstance();
        return Instance.HasFlag(player, flag);
    }

    /// <summary>
    /// Grant a flag to a player
    /// </summary>
    public static bool GrantFlag(Player targetPlayer, Flag flag, Player grantingPlayer)
    {
        EnsureInstance();
        return Instance.GrantFlag(targetPlayer, flag, grantingPlayer);
    }

    /// <summary>
    /// Remove a flag from a player
    /// </summary>
    public static bool RemoveFlag(Player targetPlayer, Flag flag, Player removingPlayer)
    {
        EnsureInstance();
        return Instance.RemoveFlag(targetPlayer, flag, removingPlayer);
    }

    /// <summary>
    /// Check if a player can grant a specific flag
    /// </summary>
    public static bool CanGrantFlag(Player player, Flag flag)
    {
        EnsureInstance();
        return Instance.CanGrantFlag(player, flag);
    }

    /// <summary>
    /// Check if a player can remove a specific flag
    /// </summary>
    public static bool CanRemoveFlag(Player player, Flag flag)
    {
        EnsureInstance();
        return Instance.CanRemoveFlag(player, flag);
    }

    /// <summary>
    /// Get all flags a player has
    /// </summary>
    public static List<Flag> GetPlayerFlags(GameObject go)
    {
        EnsureInstance();
        return Instance.GetPlayerFlags(go);
    }

    /// <summary>
    /// Get a formatted string of all flags a player has
    /// </summary>
    public static string GetFlagsString(Player player)
    {
        EnsureInstance();
        return Instance.GetFlagsString(player);
    }

    /// <summary>
    /// Initialize default permissions for the admin player
    /// </summary>
    public static void InitializeAdminPermissions(Player adminPlayer)
    {
        EnsureInstance();
        Instance.InitializeAdminPermissions(adminPlayer);
    }
}



