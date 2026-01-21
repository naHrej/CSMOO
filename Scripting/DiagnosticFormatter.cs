using System.Text;

namespace CSMOO.Scripting;

/// <summary>
/// Formats compilation diagnostics for display to users
/// </summary>
public static class DiagnosticFormatter
{
    /// <summary>
    /// Formats compilation errors as HTML for player display
    /// </summary>
    public static string FormatCompilationErrors(List<DiagnosticInfo> errors, string code)
    {
        if (errors.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("<section class=\"compiler\">");
        sb.AppendLine("<span class=\"error\">Compilation Errors:</span>");

        var codeLines = code.Split('\n');

        foreach (var error in errors)
        {
            sb.AppendLine("<div>");
            sb.AppendLine($"  <span class=\"error\">Line {error.Line}, Column {error.Column}: {error.ErrorCode}</span>");
            sb.AppendLine($"  <span class=\"error\">{HtmlEncode(error.Message)}</span>");
            
            // Show the actual line of code with error highlighted
            if (error.Line > 0 && error.Line <= codeLines.Length)
            {
                var errorLine = codeLines[error.Line - 1];
                sb.AppendLine($"  <pre>");
                sb.AppendLine($"  {HtmlEncode(errorLine)}");
                
                // Add caret to point at error column
                if (error.Column > 0)
                {
                    var spaces = new string(' ', Math.Max(0, error.Column - 1));
                    sb.AppendLine($"  {spaces}^</pre>");
                }
                else
                {
                    sb.AppendLine("  </pre>");
                }
            }
            
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</section>");
        return sb.ToString();
    }

    /// <summary>
    /// Formats compilation warnings as HTML for player display
    /// </summary>
    public static string FormatCompilationWarnings(List<DiagnosticInfo> warnings, string code)
    {
        if (warnings.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("<section class=\"compiler\">");
        sb.AppendLine("<span class=\"warning\">Compilation Warnings (treated as errors):</span>");

        var codeLines = code.Split('\n');

        foreach (var warning in warnings)
        {
            sb.AppendLine("<div>");
            sb.AppendLine($"  <span class=\"warning\">Line {warning.Line}, Column {warning.Column}: {warning.ErrorCode}</span>");
            sb.AppendLine($"  <span class=\"warning\">{HtmlEncode(warning.Message)}</span>");
            
            // Show the actual line of code with warning highlighted
            if (warning.Line > 0 && warning.Line <= codeLines.Length)
            {
                var warningLine = codeLines[warning.Line - 1];
                sb.AppendLine($"  <pre>");
                sb.AppendLine($"  {HtmlEncode(warningLine)}");
                
                // Add caret to point at warning column
                if (warning.Column > 0)
                {
                    var spaces = new string(' ', Math.Max(0, warning.Column - 1));
                    sb.AppendLine($"  {spaces}^</pre>");
                }
                else
                {
                    sb.AppendLine("  </pre>");
                }
            }
            
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</section>");
        return sb.ToString();
    }

    /// <summary>
    /// Formats a single diagnostic as plain text for console logging
    /// </summary>
    public static string FormatDiagnosticPlainText(DiagnosticInfo diagnostic)
    {
        return $"Line {diagnostic.Line}, Column {diagnostic.Column}: {diagnostic.ErrorCode} - {diagnostic.Message}";
    }

    /// <summary>
    /// HTML-encodes text to safely display in HTML clients
    /// </summary>
    private static string HtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
