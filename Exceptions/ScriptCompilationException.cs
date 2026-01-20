using CSMOO.Scripting;

namespace CSMOO.Exceptions;

/// <summary>
/// Exception thrown when script compilation fails
/// </summary>
public class ScriptCompilationException : ScriptExecutionException
{
    /// <summary>
    /// Compilation result containing diagnostics
    /// </summary>
    public CompilationResult CompilationResult { get; }

    public ScriptCompilationException(CompilationResult compilationResult)
        : base(FormatCompilationErrorMessage(compilationResult), compilationResult.Success ? string.Empty : compilationResult.Errors.FirstOrDefault()?.Message ?? "Compilation failed")
    {
        CompilationResult = compilationResult;
    }

    public ScriptCompilationException(CompilationResult compilationResult, string sourceCode)
        : base(FormatCompilationErrorMessage(compilationResult), sourceCode)
    {
        CompilationResult = compilationResult;
    }

    private static string FormatCompilationErrorMessage(CompilationResult result)
    {
        if (result.Errors.Count > 0)
        {
            var firstError = result.Errors[0];
            return $"Compilation error ({firstError.ErrorCode}) at line {firstError.Line}, column {firstError.Column}: {firstError.Message}";
        }
        
        if (result.Warnings.Count > 0)
        {
            var firstWarning = result.Warnings[0];
            return $"Compilation warning ({firstWarning.ErrorCode}) at line {firstWarning.Line}, column {firstWarning.Column}: {firstWarning.Message}";
        }
        
        return "Compilation failed";
    }

    public override string ToHtmlString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<div style='color: #ff6b6b; font-weight: bold; margin: 4px 0;'>Script Compilation Failed</div>");
        
        if (CompilationResult.Errors.Count > 0)
        {
            sb.AppendLine(DiagnosticFormatter.FormatCompilationErrors(CompilationResult.Errors, SourceCode ?? string.Empty));
        }
        
        if (CompilationResult.Warnings.Count > 0)
        {
            sb.AppendLine(DiagnosticFormatter.FormatCompilationWarnings(CompilationResult.Warnings, SourceCode ?? string.Empty));
        }
        
        return sb.ToString();
    }
}
