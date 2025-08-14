namespace CSMOO.Exceptions
{
    /// <summary>
    /// Exception thrown when a return type does not match the expected type, with enhanced script stack trace support.
    /// </summary>
    public class ReturnTypeException : ScriptExecutionException
    {
        public ReturnTypeException() : base("The return type does not match the expected type.") { }

        public ReturnTypeException(string message) : base(message) { }

        public ReturnTypeException(string message, Exception innerException) : base(message, innerException) { }
        
        public ReturnTypeException(string message, string sourceCode) : base(message, sourceCode) { }
    }
}