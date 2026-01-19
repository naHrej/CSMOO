namespace CSMOO.Database;

/// <summary>
/// Interface for world initialization operations
/// </summary>
public interface IWorldInitializer
{
    /// <summary>
    /// Initializes the basic world structure with core classes
    /// </summary>
    void InitializeWorld();
    
    /// <summary>
    /// Gets basic world statistics for display
    /// </summary>
    void PrintWorldStatistics();
}
