namespace CSMOO.Exceptions;

/// <summary>
/// Object not found exception.
/// </summary>

public class NotFoundException : Exception
{
    public NotFoundException() : base("The requested object was not found.") { }

    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
}