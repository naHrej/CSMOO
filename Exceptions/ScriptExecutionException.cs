using CSMOO.Scripting;
namespace CSMOO.Exceptions;

/// <summary>
/// Custom exception that includes script stack trace information
/// </summary>
public class ScriptExecutionException : Exception
{
    public ScriptStackFrame[] ScriptStack { get; }
    public string SourceCode { get; }

    /// <summary>
    /// Create a ScriptExecutionException with just a message (automatically detects source code and inner exception)
    /// </summary>
    public ScriptExecutionException(string message) 
        : base(message)
    {
        ScriptStack = ScriptStackTrace.GetCallStack();
        SourceCode = GetSourceCodeFromCurrentFrame();
    }

    /// <summary>
    /// Create a ScriptExecutionException with a message and inner exception (automatically detects source code)
    /// </summary>
    public ScriptExecutionException(string message, Exception innerException) 
        : base(message, innerException)
    {
        ScriptStack = ScriptStackTrace.GetCallStack();
        SourceCode = GetSourceCodeFromCurrentFrame();
    }

    /// <summary>
    /// Create a ScriptExecutionException with explicit source code
    /// </summary>
    public ScriptExecutionException(string message, Exception innerException, string sourceCode) 
        : base(message, innerException)
    {
        ScriptStack = ScriptStackTrace.GetCallStack();
        SourceCode = sourceCode;
    }

    /// <summary>
    /// Create a ScriptExecutionException with just a message and explicit source code
    /// </summary>
    public ScriptExecutionException(string message, string sourceCode) 
        : base(message)
    {
        ScriptStack = ScriptStackTrace.GetCallStack();
        SourceCode = sourceCode;
    }

    /// <summary>
    /// Attempt to get source code from the current top frame in the call stack
    /// </summary>
    private static string GetSourceCodeFromCurrentFrame()
    {
        var frames = ScriptStackTrace.GetCallStack();
        if (frames.Length > 0)
        {
            var topFrame = frames.First();
            return topFrame.SourceCode ?? "";
        }
        return "";
    }

    public override string ToString()
    {
        return ScriptStackTrace.FormatError(this, SourceCode);
    }
}
