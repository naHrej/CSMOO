using System.Runtime.Serialization;

namespace CSMOO.Exceptions
{
    /// <summary>
    /// Base class for all exceptions in the CSMOO project.
    /// </summary>
    public class Exception : System.Exception
    {
        public Exception()
        {
        }

        public Exception(string message) : base(message) { }
        public Exception(string message, Exception innerException) : base(message, innerException) { }

        public Exception(string? message, System.Exception? innerException) : base(message, innerException)
        {
        }

    }
}