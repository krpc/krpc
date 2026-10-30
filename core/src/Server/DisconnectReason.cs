namespace KRPC.Server
{
    /// <summary>
    /// The reason the server gives a client for ending its connection.
    /// </summary>
    public enum DisconnectReason
    {
        /// <summary>
        /// A normal close, or no more specific reason.
        /// </summary>
        Closed,
        /// <summary>
        /// The server stopped.
        /// </summary>
        ServerStopped,
        /// <summary>
        /// The player removed the client from the server window.
        /// </summary>
        Removed,
        /// <summary>
        /// A message failed to parse, or a request failed to read.
        /// </summary>
        MalformedMessage,
        /// <summary>
        /// A valid message the receiver did not expect.
        /// </summary>
        ProtocolError,
        /// <summary>
        /// A partial message stalled.
        /// </summary>
        Timeout
    }
}
