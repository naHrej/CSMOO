namespace CSMOO.Functions;

/// <summary>
/// Interface for function initialization operations
/// </summary>
public interface IFunctionInitializer
{
    /// <summary>
    /// Loads and creates all functions from C# class definitions
    /// </summary>
    void LoadAndCreateFunctions();
    
    /// <summary>
    /// Hot reload all function definitions (removes old, loads new)
    /// Only removes functions that were created from resources folder (CreatedBy == "system")
    /// </summary>
    void ReloadFunctions();
    
    /// <summary>
    /// Force reload ALL function definitions (removes all functions, loads new)
    /// Use with caution - this will delete in-game created functions too!
    /// </summary>
    void ForceReloadAllFunctions();
    
    /// <summary>
    /// Loads and creates functions from all C# definitions in Resources directory
    /// </summary>
    (int Loaded, int Skipped) LoadFunctions();
}
