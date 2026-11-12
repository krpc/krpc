using System;
using System.Runtime.Serialization;

namespace KRPC.Client
{
    /// <summary>
    /// Thrown when a lambda expression cannot be compiled into a server side function or expression.
    /// </summary>
    [Serializable]
    public class CompilationException : RemoteException
    {
        /// <summary>
        /// Construct a CompilationException with no message.
        /// </summary>
        public CompilationException ()
        {
        }

        /// <summary>
        /// Construct a CompilationException with the given message.
        /// </summary>
        public CompilationException (string message) : base (message)
        {
        }

        /// <summary>
        /// Construct a CompilationException with the given message and inner exception.
        /// </summary>
        public CompilationException (string message, Exception inner) : base (message, inner)
        {
        }

#if !NET
        /// <summary>
        /// Construct a CompilationException with the given serialization info and streaming context.
        /// </summary>
        protected CompilationException (SerializationInfo info, StreamingContext context) : base (info, context)
        {
        }
#endif
    }
}
