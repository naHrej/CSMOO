namespace CSMOO.Exceptions;

/// <summary>
/// Permission denied exception with enhanced script stack trace support.
/// </summary>
public class PermissionException : ScriptExecutionException
{
    public PermissionException() : base("Permission denied.") { }

    public PermissionException(string message) : base(message) { }

    public PermissionException(string message, Exception innerException) : base(message, innerException) { }
    
    public PermissionException(string message, string sourceCode) : base(message, sourceCode) { }
}