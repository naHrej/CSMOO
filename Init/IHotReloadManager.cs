namespace CSMOO.Init;

/// <summary>
/// Interface for hot reload management operations
/// </summary>
public interface IHotReloadManager
{
    /// <summary>
    /// Initialize hot reload functionality
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// Manually trigger a hot reload of verbs
    /// </summary>
    void ManualReloadVerbs();
    
    /// <summary>
    /// Manually trigger a hot reload of functions
    /// </summary>
    void ManualReloadFunctions();
    
    /// <summary>
    /// Manually trigger a hot reload of scripts
    /// </summary>
    void ManualReloadScripts();
    
    /// <summary>
    /// Enable or disable hot reload functionality
    /// </summary>
    void SetEnabled(bool enabled);
    
    /// <summary>
    /// Get the current status of hot reload
    /// </summary>
    bool IsEnabled { get; }
    
    /// <summary>
    /// Shutdown the hot reload manager
    /// </summary>
    void Shutdown();
}
