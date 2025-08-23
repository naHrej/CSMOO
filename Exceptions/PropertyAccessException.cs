namespace CSMOO.Exceptions;

/// <summary>
/// Exception thrown when there's an error accessing object properties
/// </summary>
public class PropertyAccessException : ScriptExecutionException
{
    public PropertyAccessException(string message) : base(message) { }
    public PropertyAccessException(string message, Exception innerException) : base(message, innerException) { }
    public PropertyAccessException(string message, string sourceCode) : base(message, sourceCode) { }
    public PropertyAccessException(string message, Exception innerException, string sourceCode) : base(message, innerException, sourceCode) { }
}
