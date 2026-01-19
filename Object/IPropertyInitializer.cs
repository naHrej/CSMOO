namespace CSMOO.Object;

/// <summary>
/// Interface for property initialization operations
/// </summary>
public interface IPropertyInitializer
{
    /// <summary>
    /// Loads and sets all properties from C# class definitions
    /// </summary>
    void LoadAndSetProperties();
    
    /// <summary>
    /// Hot reload all property definitions
    /// </summary>
    void ReloadProperties();
    
    /// <summary>
    /// Loads and creates properties from all C# class definitions in Resources directory
    /// </summary>
    (int Loaded, int Skipped) LoadProperties();
}
