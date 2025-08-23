namespace CSMOO.Exceptions;

/// <summary>
/// Exception thrown when there's an error during function execution
/// </summary>
public class FunctionExecutionException : ScriptExecutionException
{
    public FunctionExecutionException(string message) : base(message) { }
    public FunctionExecutionException(string message, Exception innerException) : base(message, innerException) { }
    public FunctionExecutionException(string message, string sourceCode) : base(message, sourceCode) { }
    public FunctionExecutionException(string message, Exception innerException, string sourceCode) : base(message, innerException, sourceCode) { }
}
