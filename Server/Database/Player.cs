using System;
using System.Collections.Generic;
using System.Dynamic;
using LiteDB;

namespace CSMOO.Server.Database;/// <summary>
/// Player-specific data that extends GameObject
/// </summary>
public class Player : GameObject
{
    
    /// <summary>
    /// Password hash for authentication
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Current session GUID (if online)
    /// </summary>
    public Guid? SessionGuid { get; set; }
    
    /// <summary>
    /// Last login time
    /// </summary>
    public DateTime? LastLogin { get; set; }
    
    /// <summary>
    /// Whether the player is currently online
    /// </summary>
    public bool IsOnline { get; set; } = false;
    
    /// <summary>
    /// Player permissions/privileges
    /// </summary>
    public List<string> Permissions { get; set; } = new List<string>();
}
