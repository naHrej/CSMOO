
namespace CSMOO.Exceptions;

/// <summary>
/// Object not found exception with enhanced script stack trace support.
/// </summary>
public class NotFoundException : ScriptExecutionException
{
    public NotFoundException() : base("The requested object was not found.") { }

    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
    
    public NotFoundException(string message, string sourceCode) : base(message, sourceCode) { }
}