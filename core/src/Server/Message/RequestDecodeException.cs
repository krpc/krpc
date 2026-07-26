using System;

namespace KRPC.Server.Message
{
    /// <summary>
    /// Thrown when a request was received in full, but could not be turned into the call
    /// it names - for example because it names an object the server no longer has.
    /// </summary>
    /// <remarks>
    /// Carries the number of bytes the request occupied, so that the stream can drop them
    /// and stay in step with the client. Without that, the same bytes are decoded again on
    /// the next poll, and the client never gets another call through.
    /// </remarks>
    sealed class RequestDecodeException : Exception
    {
        public RequestDecodeException (int bytesRead, Exception innerException) :
            base (innerException == null ? string.Empty : innerException.Message, innerException)
        {
            BytesRead = bytesRead;
        }

        /// <summary>
        /// The number of bytes the request that could not be decoded occupied.
        /// </summary>
        public int BytesRead { get; private set; }
    }
}
