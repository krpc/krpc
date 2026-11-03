using System;
using System.Runtime.Serialization;

namespace KRPC.Client
{
    /// <summary>
    /// Thrown when the server returns null for a value that cannot be null.
    /// </summary>
    [Serializable]
    public class EncodingException : Exception
    {
        /// <summary>
        /// Construct an EncodingException with no message.
        /// </summary>
        public EncodingException ()
        {
        }

        /// <summary>
        /// Construct an EncodingException with the given message.
        /// </summary>
        public EncodingException (string message) : base (message)
        {
        }

        /// <summary>
        /// Construct an EncodingException with the given message and inner exception.
        /// </summary>
        public EncodingException (string message, Exception inner) : base (message, inner)
        {
        }

#if !NET
        /// <summary>
        /// Construct an EncodingException with the given serialization info and streaming context.
        /// </summary>
        protected EncodingException (SerializationInfo info, StreamingContext context) : base (info, context)
        {
        }
#endif
    }
}
