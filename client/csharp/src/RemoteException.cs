using System;
using System.Runtime.Serialization;

namespace KRPC.Client
{
    /// <summary>
    /// The base of every exception the client throws of its own. A server exception with a mapped type, such as <c>ArgumentException</c>, throws the .NET exception it stands for.
    /// </summary>
    [Serializable]
    public class RemoteException : Exception
    {
        /// <summary>
        /// Construct a RemoteException with no message.
        /// </summary>
        public RemoteException ()
        {
        }

        /// <summary>
        /// Construct a RemoteException with the given message.
        /// </summary>
        public RemoteException (string message) : base (message)
        {
        }

        /// <summary>
        /// Construct a RemoteException with the given message and inner exception.
        /// </summary>
        public RemoteException (string message, Exception inner) : base (message, inner)
        {
        }

#if !NET
        /// <summary>
        /// Construct a RemoteException with the given serialization info and streaming context.
        /// </summary>
        protected RemoteException (SerializationInfo info, StreamingContext context) : base (info, context)
        {
        }
#endif
    }
}
