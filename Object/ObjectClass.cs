using CSMOO.Scripting;
using LiteDB;

namespace CSMOO.Object;

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

    public Dictionary<string, List<Keyword>> PropAccessors { get; set; } = new Dictionary<string, List<Keyword>>();
    
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




