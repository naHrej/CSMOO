namespace CSMOO.Init;

/// <summary>
/// Interface for core hot reload management operations
/// </summary>
public interface ICoreHotReloadManager
{
    /// <summary>
    /// Initialize core application hot reload capabilities
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// Check if .NET hot reload is available and active
    /// </summary>
    bool IsHotReloadSupported();
    
    /// <summary>
    /// Get the current status of core hot reload
    /// </summary>
    string GetStatus();
    
    /// <summary>
    /// Force trigger a hot reload notification (for testing)
    /// </summary>
    void TriggerTestNotification();
    
    /// <summary>
    /// Shutdown the core hot reload manager
    /// </summary>
    void Shutdown();
}
