using System;

namespace KRPC.Client
{
    /// <summary>
    /// Thrown when a lambda expression cannot be compiled into a server side function.
    /// </summary>
    public class FunctionCompilationException : Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public FunctionCompilationException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public FunctionCompilationException (string message) : base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public FunctionCompilationException (string message, Exception innerException) : base (message, innerException)
        {
        }
    }
}
