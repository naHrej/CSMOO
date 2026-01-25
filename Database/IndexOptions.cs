namespace CSMOO.Database;

/// <summary>
/// Options for index creation
/// </summary>
public class IndexOptions
{
    /// <summary>
    /// Whether the index should be unique
    /// </summary>
    public bool Unique { get; set; } = false;
    
    /// <summary>
    /// Whether the index should be sparse (ignore null values)
    /// </summary>
    public bool Sparse { get; set; } = false;
    
    /// <summary>
    /// Optional name for the index
    /// </summary>
    public string? Name { get; set; }
}
