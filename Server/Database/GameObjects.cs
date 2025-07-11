using System;
using System.Collections.Generic;
using LiteDB;

namespace CSMOO.Server.Database;

/// <summary>
/// Base class definition that all game objects inherit from
/// This is the "template" or "prototype" that defines behavior and default properties
/// </summary>
public class ObjectClass
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable name of this class (e.g., "Room", "Player", "Sword", "Door")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Parent class this inherits from (null for root classes)
    /// </summary>
    public string? ParentClassId { get; set; }
    
    /// <summary>
    /// Default properties that instances of this class will have
    /// These can be overridden in individual instances
    /// </summary>
    public BsonDocument Properties { get; set; } = new BsonDocument();
    
    /// <summary>
    /// Methods/functions defined on this class (stored as code strings)
    /// Could be C# code, script code, etc.
    /// </summary>
    public BsonDocument Methods { get; set; } = new BsonDocument();
    
    /// <summary>
    /// Description of what this class represents
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this class can be instantiated directly
    /// (abstract classes cannot be instantiated)
    /// </summary>
    public bool IsAbstract { get; set; } = false;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// An actual instance of an ObjectClass - this is what exists in the game world
/// </summary>
public class GameObject
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Numeric database reference (like #1, #2, #3, etc.) for easy user addressing
    /// </summary>
    public int DbRef { get; set; } = 0;
    
    /// <summary>
    /// The class this object is an instance of
    /// </summary>
    public string ClassId { get; set; } = string.Empty;
    
    /// <summary>
    /// Instance-specific properties that override or extend the class defaults
    /// </summary>
    public BsonDocument Properties { get; set; } = new BsonDocument();
    
    /// <summary>
    /// Location of this object (ID of the room/container it's in)
    /// Null means it's not in the game world currently
    /// </summary>
    public string? Location { get; set; }
    
    /// <summary>
    /// Objects contained within this object (inventory, room contents, etc.)
    /// </summary>
    public List<string> Contents { get; set; } = new List<string>();
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Player-specific data that extends GameObject
/// </summary>
public class Player : GameObject
{
    /// <summary>
    /// Player's login name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
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
