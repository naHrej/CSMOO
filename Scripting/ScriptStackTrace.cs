using System.Text;
using CSMOO.Object;
using CSMOO.Functions;
using CSMOO.Verbs;
using CSMOO.Exceptions;
using CSMOO.Logging;

namespace CSMOO.Scripting;

/// <summary>
/// Manages the script call stack for better error reporting
/// </summary>
public static class ScriptStackTrace
{
    private static readonly ThreadLocal<Stack<ScriptStackFrame>> _callStack = 
        new ThreadLocal<Stack<ScriptStackFrame>>(() => new Stack<ScriptStackFrame>());

    /// <summary>
    /// Push a new frame onto the script call stack
    /// </summary>
    public static void PushFrame(string type, string name, string objectId, string objectName = "")
    {
        var frame = new ScriptStackFrame
        {
            Type = type,
            Name = name,
            ObjectId = objectId,
            ObjectName = objectName
        };
        _callStack.Value!.Push(frame);
    }

    /// <summary>
    /// Push a verb frame onto the call stack
    /// </summary>
    public static void PushVerbFrame(Verb verb, GameObject? obj)
    {
        var objectName = obj?.Name ?? "unknown";
        PushFrame("verb", verb.Name, verb.ObjectId, objectName);
    }

    /// <summary>
    /// Push a function frame onto the call stack
    /// </summary>
    public static void PushFunctionFrame(Function function, GameObject? obj)
    {
        var objectName = obj?.Name ?? "unknown";
        PushFrame("function", function.Name, function.ObjectId, objectName);
    }

    /// <summary>
    /// Pop the top frame from the call stack
    /// </summary>
    public static void PopFrame()
    {
        if (_callStack.Value!.Count > 0)
        {
            _callStack.Value.Pop();
        }
    }

    /// <summary>
    /// Get the current call stack
    /// </summary>
    public static ScriptStackFrame[] GetCallStack()
    {
        return _callStack.Value!.ToArray();
    }

    /// <summary>
    /// Clear the call stack (useful for cleanup after errors)
    /// </summary>
    public static void Clear()
    {
        _callStack.Value!.Clear();
    }

    /// <summary>
    /// Try to determine the line number where an error occurred based on the C# exception
    /// </summary>
    public static int TryGetLineNumber(Exception ex, string sourceCode)
    {
        try
        {
            // Handle compilation errors specifically - these have precise line information
            if (ex is Microsoft.CodeAnalysis.Scripting.CompilationErrorException compilationEx)
            {
                var diagnostics = compilationEx.Diagnostics;
                var firstError = diagnostics.FirstOrDefault();
                if (firstError != null)
                {
                    var lineSpan = firstError.Location.GetLineSpan();
                    return lineSpan.StartLinePosition.Line + 1; // Convert to 1-based
                }
            }
            
            // For runtime exceptions, try to find line number from stack trace
            var stackTrace = ex.StackTrace ?? "";
            
            // Look for line number patterns in the stack trace
            var patterns = new[]
            {
                @"Submission#\d+\.<<Initialize>>.*line (\d+)",
                @"at.*line (\d+)",
                @"\((\d+),\d+\)",
                @"line (\d+)",
                @"Submission#\d+.*MoveNext\(\)" // This pattern indicates we're in user script code
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(stackTrace, pattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    // Special handling for Submission MoveNext - this means we're in user script
                    if (pattern.Contains("MoveNext"))
                    {
                        // For script submissions, try to find the relevant line
                        if (!string.IsNullOrEmpty(sourceCode))
                        {
                            var lines = sourceCode.Split('\n');
                            
                            // First priority: look for throw statements (actual error locations)
                            for (int i = 0; i < lines.Length; i++)
                            {
                                var line = lines[i].Trim();
                                if (line.Contains("throw"))
                                {
                                    return i + 1;
                                }
                            }
                            
                            // Second priority: look for function calls that might be causing the error
                            // But be more specific - look for actual method calls, not just any function call
                            for (int i = 0; i < lines.Length; i++)
                            {
                                var line = lines[i].Trim();
                                // Look for specific patterns that indicate method calls
                                if (line.Contains(".Description()") || 
                                    line.Contains(".Players()") ||
                                    line.Contains(".Contents()") ||
                                    line.Contains(".Exits()") ||
                                    (line.Contains("notify(") && line.Contains(".")))
                                {
                                    return i + 1;
                                }
                            }
                            
                            // If no specific patterns found, don't guess
                            return 0;
                        }
                    }
                    else if (int.TryParse(match.Groups[1].Value, out int lineNum))
                    {
                        // Validate that this line number makes sense for our source code
                        if (!string.IsNullOrEmpty(sourceCode))
                        {
                            var lines = sourceCode.Split('\n');
                            if (lineNum > 0 && lineNum <= lines.Length)
                            {
                                return lineNum;
                            }
                        }
                        else
                        {
                            return lineNum;
                        }
                    }
                }
            }
            
            // If we can't find line number from stack trace, try to infer from source code
            // Look for patterns that might indicate where the error occurred
            if (!string.IsNullOrEmpty(sourceCode) && ex.Message.Contains("throw"))
            {
                var lines = sourceCode.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.Contains("throw"))
                    {
                        return i + 1; // Line numbers are 1-based
                    }
                }
            }
        }
        catch
        {
            // If anything goes wrong with line number detection, return 0
        }

        return 0; // Unable to determine line number
    }

    /// <summary>
    /// Get the source line that caused the error with surrounding context
    /// </summary>
    public static string GetErrorContext(string sourceCode, int lineNumber)
    {
        if (lineNumber <= 0 || string.IsNullOrEmpty(sourceCode))
            return "";

        try
        {
            var lines = sourceCode.Split('\n');
            if (lineNumber <= lines.Length)
            {
                var sb = new StringBuilder();
                
                // Show up to 2 lines before and after the error line
                var startLine = Math.Max(1, lineNumber - 2);
                var endLine = Math.Min(lines.Length, lineNumber + 2);
                
                for (int i = startLine; i <= endLine; i++)
                {
                    var prefix = i == lineNumber ? ">>> " : "    ";
                    var lineContent = i <= lines.Length ? lines[i - 1].Trim() : "";
                    sb.AppendLine($"{prefix}{i}: {lineContent}");
                }
                
                return sb.ToString().TrimEnd();
            }
        }
        catch
        {
            // If we can't get the line, return empty
        }

        return "";
    }

    /// <summary>
    /// Update the current frame with line number and error context
    /// </summary>
    public static void UpdateCurrentFrame(Exception ex, string sourceCode)
    {
        if (_callStack.Value!.Count > 0)
        {
            var currentFrame = _callStack.Value.Peek();
            
            // Only update if we don't have a line number yet
            if (currentFrame.LineNumber <= 0)
            {
                var lineNumber = TryGetLineNumber(ex, sourceCode);
                currentFrame.LineNumber = lineNumber;
                currentFrame.SourceCode = sourceCode;
                currentFrame.ErrorContext = GetErrorContext(sourceCode, currentFrame.LineNumber);
            }
        }
    }

    /// <summary>
    /// Create a formatted error message with custom stack trace and HTML styling
    /// </summary>
    public static string FormatError(Exception ex, string sourceCode = "")
    {
        var sb = new StringBuilder();
        
        // Update current frame with error details
        if (!string.IsNullOrEmpty(sourceCode))
        {
            UpdateCurrentFrame(ex, sourceCode);
        }

        // Get the root cause message (unwrap nested ScriptExecutionExceptions)
        var rootMessage = ex.Message;
        var currentEx = ex;
        while (currentEx.InnerException != null && currentEx is ScriptExecutionException)
        {
            currentEx = currentEx.InnerException;
            if (!currentEx.Message.Contains("Error in function") && !currentEx.Message.Contains("Error in verb"))
            {
                rootMessage = currentEx.Message;
                break;
            }
        }

        // Get the script stack trace - use pre-captured frames from ScriptExecutionException if available
        ScriptStackFrame[] frames;
        if (ex is ScriptExecutionException scriptEx && scriptEx.ScriptStack?.Length > 0)
        {
            frames = scriptEx.ScriptStack;
        }
        else
        {
            frames = GetCallStack();
        }
        
        if (frames.Length > 0)
        {
            // Header with exception type and message
            sb.Append($"<div style='color: #ff6b6b; font-weight: bold; margin: 4px 0;'>");
            sb.Append($"<span style='color: #ffa8a8;'>{HtmlEncode(ex.GetType().Name)}: {HtmlEncode(rootMessage)}</span>");
            sb.Append("</div>");
            
            // Stack trace
            sb.Append("<div style='color: #adb5bd; margin: 4px 0;'>");
            
            for (int i = 0; i < frames.Length && i < 4; i++) // Limit to 4 frames total
            {
                var frame = frames[i];
                var lineInfo = frame.LineNumber > 0 ? $"(line {frame.LineNumber})" : "";
                
                // Use different separator for verbs vs functions
                var separator = frame.Type == "verb" ? ":" : ".";
                var color = i == 0 ? "#ffd43b" : "#74c0fc"; // First frame is highlighted
                
                sb.Append($"<span style='color: {color};'>{HtmlEncode(frame.ObjectName)}</span>");
                sb.Append($"{separator}<span style='color: #e9ecef;'>{HtmlEncode(frame.Name)}</span>");
                if (!string.IsNullOrEmpty(lineInfo))
                {
                    sb.Append($"<span style='color: #ff8cc8;'> {lineInfo}</span>");
                }
                if (i < frames.Length - 1 && i < 3) // Don't add newline after last item
                {
                    sb.AppendLine();
                }
            }
            
            sb.AppendLine("</div>");
        }
        else
        {
            // Fallback if no stack frames available
            sb.Append($"<div style='color: #ff6b6b; font-weight: bold; margin: 4px 0;'>");
            sb.Append($"<span style='color: #ffa8a8;'>{HtmlEncode(ex.GetType().Name)}: {HtmlEncode(rootMessage)}</span>");
            sb.Append("</div>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// HTML-encode text to safely display in HTML clients
    /// </summary>
    private static string HtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? "";
        
        return text.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("'", "&#39;");
    }
}
