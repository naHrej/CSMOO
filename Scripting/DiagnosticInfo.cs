using Microsoft.CodeAnalysis;

namespace CSMOO.Scripting;

/// <summary>
/// Represents a diagnostic (error, warning, or info) from script compilation
/// </summary>
public class DiagnosticInfo
{
    /// <summary>
    /// Line number where the diagnostic occurs (1-based)
    /// </summary>
    public int Line { get; set; }
    
    /// <summary>
    /// Column number where the diagnostic occurs (1-based)
    /// </summary>
    public int Column { get; set; }
    
    /// <summary>
    /// Error code (e.g., "CS0103", "CS1002")
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Diagnostic message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Severity of the diagnostic (Error, Warning, Info)
    /// </summary>
    public DiagnosticSeverity Severity { get; set; }
    
    /// <summary>
    /// Optional file path where the diagnostic occurs
    /// </summary>
    public string? FilePath { get; set; }
    
    /// <summary>
    /// Whether this is an error (Severity == Error)
    /// </summary>
    public bool IsError => Severity == DiagnosticSeverity.Error;
    
    /// <summary>
    /// Whether this is a warning (Severity == Warning)
    /// </summary>
    public bool IsWarning => Severity == DiagnosticSeverity.Warning;
}
