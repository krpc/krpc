using System;
using System.Runtime.Serialization;

namespace KRPC.Client
{
    /// <summary>
    /// Thrown for an error the server reports, through a call, a stream or an event, that has no .NET exception type. The exception classes of the services derive it.
    /// </summary>
    [Serializable]
    public class ServerException : RemoteException
    {
        /// <summary>
        /// Construct a ServerException with no message.
        /// </summary>
        public ServerException ()
        {
        }

        /// <summary>
        /// Construct a ServerException with the given message.
        /// </summary>
        public ServerException (string message) : base (message)
        {
        }

        /// <summary>
        /// Construct a ServerException with the given message and inner exception.
        /// </summary>
        public ServerException (string message, Exception inner) : base (message, inner)
        {
        }

#if !NET
        /// <summary>
        /// Construct a ServerException with the given serialization info and streaming context.
        /// </summary>
        protected ServerException (SerializationInfo info, StreamingContext context) : base (info, context)
        {
        }
#endif
    }
}
