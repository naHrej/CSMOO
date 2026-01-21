namespace CSMOO.Scripting;

/// <summary>
/// Statistics from compilation initialization
/// </summary>
public class CompilationStatistics
{
    public int VerbsCompiled { get; set; }
    public int VerbsFailed { get; set; }
    public int FunctionsCompiled { get; set; }
    public int FunctionsFailed { get; set; }
}

/// <summary>
/// Interface for initializing compilation cache on server startup
/// </summary>
public interface ICompilationInitializer
{
    /// <summary>
    /// Recompiles all verbs and functions on server startup
    /// </summary>
    /// <returns>Task that completes when recompilation is done</returns>
    Task RecompileAllAsync();
    
    /// <summary>
    /// Gets compilation statistics
    /// </summary>
    /// <returns>Statistics about compiled/failed items</returns>
    CompilationStatistics GetStatistics();
}
