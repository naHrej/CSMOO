namespace CSMOO.Exceptions;
public class PrivateAccessException : ScriptExecutionException
{
    public PrivateAccessException() : base("Access to this property is private and cannot be modified.") { }

    public PrivateAccessException(string message) : base(message) { }

    public PrivateAccessException(string message, Exception innerException) : base(message, innerException) { }
    
    public PrivateAccessException(string message, string sourceCode) : base(message, sourceCode) { }
}