using LiteDB;

namespace CSMOO.Functions;

/// <summary>
/// Represents a user-defined function that can be called from scripts
/// </summary>
public class GameFunction
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Name of the function
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// C# code for the function body
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this function does
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Function parameters
    /// </summary>
    public List<FunctionParameter> Parameters { get; set; } = new List<FunctionParameter>();
    
    /// <summary>
    /// Return type of the function
    /// </summary>
    public string ReturnType { get; set; } = "void";
    
    /// <summary>
    /// Who can call this function (public, wizard, etc.)
    /// </summary>
    public string Permissions { get; set; } = "public";
    
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



