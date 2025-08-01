namespace CSMOO.Exceptions;

class PermissionException : Exception
{
    public PermissionException() : base("Permission denied.") { }

    public PermissionException(string message) : base(message) { }

    public PermissionException(string message, Exception innerException) : base(message, innerException) { }
}