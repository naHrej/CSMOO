namespace CSMOO.Exceptions
{
    /// <summary>
    /// Exception thrown when a return type does not match the expected type.
    /// </summary>
    public class ReturnTypeException : Exception
    {
        public ReturnTypeException() : base("The return type does not match the expected type.") { }

        public ReturnTypeException(string message) : base(message) { }

        public ReturnTypeException(string message, Exception innerException) : base(message, innerException) { }
    }
}