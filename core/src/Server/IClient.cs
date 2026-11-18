using System;

namespace KRPC.Server
{
    /// <summary>
    /// Non-generic client interface.
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// A string identifying the client. Should be human readable.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A the globally unique identifier for the client.
        /// </summary>
        Guid Guid { get; }

        /// <summary>
        /// The address of the client. Depends on the underlying communication method.
        /// </summary>
        /// <remarks>For example, could be an IP address when client
        /// communication is over a network.</remarks>
        string Address { get; }

        /// <summary>
        /// Returns true if the client is actively connected to the server.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Close the connection to the client and free the connections resources.
        /// </summary>
        void Close ();
    }

    /// <summary>
    /// A client, and the stream it communicates over: of bytes, or of messages.
    /// </summary>
    public interface IClient<TStream> : IEquatable<IClient<TStream>>, IClient
        where TStream : IStream
    {
        /// <summary>
        /// A stream for communicating with the client.
        /// </summary>
        TStream Stream { get; }
    }
}
