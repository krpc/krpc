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
    public abstract class ClientEventArgs<TIn,TOut> : EventArgs
    {
        /// <summary>
        /// A client event for the given client.
        /// </summary>
        protected ClientEventArgs (IClient<TIn,TOut> client)
        {
            Client = client;
        }

        /// <summary>
        /// The client.
        /// </summary>
        public IClient<TIn,TOut> Client { get; }
    }
}
