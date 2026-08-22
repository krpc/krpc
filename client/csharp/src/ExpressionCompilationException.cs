using System;

namespace KRPC.Client
{
    /// <summary>
    /// Thrown when a lambda expression cannot be compiled into a server side expression.
    /// </summary>
    public class ExpressionCompilationException : Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ExpressionCompilationException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ExpressionCompilationException (string message) : base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ExpressionCompilationException (string message, Exception innerException) : base (message, innerException)
        {
        }
    }
}
