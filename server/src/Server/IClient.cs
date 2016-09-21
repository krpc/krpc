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
    /// Generic client interface.
    /// </summary>
    public interface IClient<TIn,TOut> : IEquatable<IClient<TIn,TOut>>, IClient
    {
        /// <summary>
        /// A stream for communicating with the client.
        /// </summary>
        IStream<TIn,TOut> Stream { get; }
    }
}
