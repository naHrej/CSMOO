using System;
using LiteDB;

namespace CSMOO.Verbs;

/// <summary>
/// Represents a verb (command) that can be executed on an object
/// </summary>
public class Verb
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// The object this verb is defined on
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the verb (e.g., "look", "get", "open")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Aliases for this verb (space-separated, e.g., "l examine")
    /// </summary>
    public string Aliases { get; set; } = string.Empty;
    
    /// <summary>
    /// Pattern that this verb matches (e.g., "* at *", "* from *")
    /// </summary>
    public string Pattern { get; set; } = string.Empty;
    
    /// <summary>
    /// The C# code to execute when this verb is called
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Who can execute this verb (public, owner, wizard)
    /// </summary>
    public string Permissions { get; set; } = "public";
    
    /// <summary>
    /// Description of what this verb does
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Who created this verb
    /// </summary>
    public string CreatedBy { get; set; } = "system";
    
    /// <summary>
    /// When this verb was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this verb was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}



