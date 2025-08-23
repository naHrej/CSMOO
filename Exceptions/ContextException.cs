namespace CSMOO.Exceptions;

/// <summary>
/// Exception thrown when there's an error with context access
/// </summary>
public class ContextException : ScriptExecutionException
{
    public ContextException(string message) : base(message) { }
    public ContextException(string message, Exception innerException) : base(message, innerException) { }
    public ContextException(string message, string sourceCode) : base(message, sourceCode) { }
    public ContextException(string message, Exception innerException, string sourceCode) : base(message, innerException, sourceCode) { }
}
