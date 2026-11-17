using System;

namespace KRPC.Server
{
    /// <summary>
    /// Abstract base class for a client event.
    /// </summary>
    public abstract class ClientEventArgs : EventArgs
    {
        /// <summary>
        /// A client event for the given client.
        /// </summary>
        protected ClientEventArgs (IClient client)
        {
            Client = client;
        }

        /// <summary>
        /// The client.
        /// </summary>
        public IClient Client { get; }
    }

    /// <summary>
    /// Abstract base class for a client event.
    /// </summary>
    public abstract class ClientEventArgs<TStream> : EventArgs
        where TStream : IStream
    {
        /// <summary>
        /// A client event for the given client.
        /// </summary>
        protected ClientEventArgs (IClient<TStream> client)
        {
            Client = client;
        }

        /// <summary>
        /// The client.
        /// </summary>
        public IClient<TStream> Client { get; }
    }
}
