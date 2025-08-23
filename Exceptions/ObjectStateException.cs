namespace CSMOO.Exceptions;

/// <summary>
/// Exception thrown when there's an error with object state
/// </summary>
public class ObjectStateException : ScriptExecutionException
{
    public ObjectStateException(string message) : base(message) { }
    public ObjectStateException(string message, Exception innerException) : base(message, innerException) { }
    public ObjectStateException(string message, string sourceCode) : base(message, sourceCode) { }
    public ObjectStateException(string message, Exception innerException, string sourceCode) : base(message, innerException, sourceCode) { }
}
