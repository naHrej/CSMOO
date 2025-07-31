using LiteDB;

namespace CSMOO.Functions;

/// <summary>
/// Represents a function (method) that can be called on an object with strict type checking
/// </summary>
public class Function
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// The object this function is defined on
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the function (e.g., "getName", "calculateDamage", "display_login")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameter types in order (e.g., ["string", "int", "bool"])
    /// </summary>
    public string[] ParameterTypes { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Parameter names for documentation (e.g., ["username", "level", "isActive"])
    /// </summary>
    public string[] ParameterNames { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Return type of the function (e.g., "string", "int", "bool", "void")
    /// </summary>
    public string ReturnType { get; set; } = "void";
    
    /// <summary>
    /// The C# code to execute when this function is called
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Who can call this function (public, owner, wizard)
    /// </summary>
    public string Permissions { get; set; } = "public";
    
    /// <summary>
    /// Description of what this function does
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Who created this function
    /// </summary>
    public string CreatedBy { get; set; } = "system";
    
    /// <summary>
    /// When this function was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this function was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}



