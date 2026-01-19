namespace CSMOO.Object;

/// <summary>
/// Interface for permission management operations
/// </summary>
public interface IPermissionManager
{
    /// <summary>
    /// Check if a player has a specific flag
    /// </summary>
    bool HasFlag(Player player, PermissionManager.Flag flag);
    
    /// <summary>
    /// Grant a flag to a player
    /// </summary>
    bool GrantFlag(Player targetPlayer, PermissionManager.Flag flag, Player grantingPlayer);
    
    /// <summary>
    /// Remove a flag from a player
    /// </summary>
    bool RemoveFlag(Player targetPlayer, PermissionManager.Flag flag, Player removingPlayer);
    
    /// <summary>
    /// Check if a player can grant a specific flag
    /// </summary>
    bool CanGrantFlag(Player player, PermissionManager.Flag flag);
    
    /// <summary>
    /// Check if a player can remove a specific flag
    /// </summary>
    bool CanRemoveFlag(Player player, PermissionManager.Flag flag);
    
    /// <summary>
    /// Get all flags a player has
    /// </summary>
    List<PermissionManager.Flag> GetPlayerFlags(GameObject go);
    
    /// <summary>
    /// Get a formatted string of all flags a player has
    /// </summary>
    string GetFlagsString(Player player);
    
    /// <summary>
    /// Initialize default permissions for the admin player
    /// </summary>
    void InitializeAdminPermissions(Player adminPlayer);
}
