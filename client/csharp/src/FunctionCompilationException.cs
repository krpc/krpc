using System;
using System.Runtime.Serialization;

namespace KRPC.Client
{
    /// <summary>
    /// Thrown when a lambda expression cannot be compiled into a server side function.
    /// </summary>
    [Serializable]
    public class FunctionCompilationException : Exception
    {
        /// <summary>
        /// Construct a FunctionCompilationException with no message.
        /// </summary>
        public FunctionCompilationException ()
        {
        }

        /// <summary>
        /// Construct a FunctionCompilationException with the given message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public FunctionCompilationException (string message) : base (message)
        {
        }

        /// <summary>
        /// Construct a FunctionCompilationException with the given message and inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="inner">The exception that caused this one.</param>
        public FunctionCompilationException (string message, Exception inner) : base (message, inner)
        {
        }

#if !NET
        /// <summary>
        /// Construct a FunctionCompilationException with the given serialization info and
        /// streaming context.
        /// </summary>
        protected FunctionCompilationException (SerializationInfo info, StreamingContext context) :
            base (info, context)
        {
        }
#endif
    }
}
