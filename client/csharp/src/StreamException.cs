using System;
using System.Runtime.Serialization;

namespace KRPC.Client
{
    /// <summary>
    /// Thrown for a stream or event that has been removed, or that cannot be added.
    /// </summary>
    [Serializable]
    public class StreamException : RemoteException
    {
        /// <summary>
        /// Construct a StreamException with no message.
        /// </summary>
        public StreamException ()
        {
        }

        /// <summary>
        /// Construct a StreamException with the given message.
        /// </summary>
        public StreamException (string message) : base (message)
        {
        }

        /// <summary>
        /// Construct a StreamException with the given message and inner exception.
        /// </summary>
        public StreamException (string message, Exception inner) : base (message, inner)
        {
        }

#if !NET
        /// <summary>
        /// Construct a StreamException with the given serialization info and streaming context.
        /// </summary>
        protected StreamException (SerializationInfo info, StreamingContext context) : base (info, context)
        {
        }
#endif
    }
}
