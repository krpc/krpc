using System;

namespace KRPC.Server.Message
{
    /// <summary>
    /// Thrown when a request was received in full, but could not be turned into the call
    /// it names - for example because it names an object the server no longer has.
    /// </summary>
    sealed class RequestDecodeException : Exception
    {
        public RequestDecodeException (ulong id, Exception innerException) :
            base (innerException == null ? string.Empty : innerException.Message, innerException)
        {
            Id = id;
        }

        /// <summary>
        /// The id of the request, for the error response.
        /// </summary>
        public ulong Id { get; private set; }
    }
}
