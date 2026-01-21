using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace CSMOO.Scripting;

/// <summary>
/// Result of script precompilation
/// </summary>
public class CompilationResult
{
    /// <summary>
    /// Whether compilation was successful (no errors and no warnings when warnings are treated as errors)
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// List of compilation errors
    /// </summary>
    public List<DiagnosticInfo> Errors { get; set; } = new List<DiagnosticInfo>();
    
    /// <summary>
    /// List of compilation warnings
    /// </summary>
    public List<DiagnosticInfo> Warnings { get; set; } = new List<DiagnosticInfo>();
    
    /// <summary>
    /// Compiled script (available if Success is true)
    /// </summary>
    public Script<object>? CompiledScript { get; set; }
    
    /// <summary>
    /// SHA256 hash of the source code for cache invalidation
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;
}
