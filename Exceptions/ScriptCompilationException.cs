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
        sb.AppendLine("<section class=\"compiler\">");
        sb.AppendLine("<span class=\"error\">Script Compilation Failed</span>");
        
        if (CompilationResult.Errors.Count > 0)
        {
            // FormatCompilationErrors already wraps in <section class="compiler">, extract content
            var errorContent = DiagnosticFormatter.FormatCompilationErrors(CompilationResult.Errors, SourceCode ?? string.Empty);
            var startTag = "<section class=\"compiler\">";
            var endTag = "</section>";
            if (errorContent.StartsWith(startTag) && errorContent.EndsWith(endTag))
            {
                errorContent = errorContent.Substring(startTag.Length, errorContent.Length - startTag.Length - endTag.Length).Trim();
            }
            sb.AppendLine(errorContent);
        }
        
        if (CompilationResult.Warnings.Count > 0)
        {
            // FormatCompilationWarnings already wraps in <section class="compiler">, extract content
            var warningContent = DiagnosticFormatter.FormatCompilationWarnings(CompilationResult.Warnings, SourceCode ?? string.Empty);
            var startTag = "<section class=\"compiler\">";
            var endTag = "</section>";
            if (warningContent.StartsWith(startTag) && warningContent.EndsWith(endTag))
            {
                warningContent = warningContent.Substring(startTag.Length, warningContent.Length - startTag.Length - endTag.Length).Trim();
            }
            sb.AppendLine(warningContent);
        }
        
        sb.AppendLine("</section>");
        return sb.ToString();
    }
}
