namespace CSMOO.Verbs;

/// <summary>
/// Interface for verb initialization operations
/// </summary>
public interface IVerbInitializer
{
    /// <summary>
    /// Loads and creates all verbs from C# class definitions
    /// </summary>
    void LoadAndCreateVerbs();
    
    /// <summary>
    /// Hot reload all verb definitions (removes old, loads new)
    /// Only removes verbs that were created from resources folder (CreatedBy == "system")
    /// </summary>
    void ReloadVerbs();
    
    /// <summary>
    /// Force reload ALL verb definitions (removes all verbs, loads new)
    /// Use with caution - this will delete in-game created verbs too!
    /// </summary>
    void ForceReloadAllVerbs();
}
