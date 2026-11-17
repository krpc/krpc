using KRPC.Service.Messages;

namespace KRPC.Server
{
    /// <summary>
    /// A client's connection, which carries its requests and responses, and its stream updates.
    /// </summary>
    public interface IMessageStream : IStream
    {
        /// <summary>
        /// Take the request that has arrived. Throws an exception if none has.
        /// </summary>
        Request Read ();

        /// <summary>
        /// Write a response to the client.
        /// </summary>
        void Write (Response value);

        /// <summary>
        /// Write a stream update to the client.
        /// </summary>
        void Write (StreamUpdate value);

        /// <summary>
        /// Free the state kept for a stream that has been removed.
        /// </summary>
        void StreamRemoved (ulong id);

        /// <summary>
        /// End the connection, giving the client a reason.
        /// </summary>
        void Disconnect (DisconnectReason reason, string message);
    }
}
