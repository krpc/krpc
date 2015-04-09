using System;

namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client activity event
    /// </summary>
    public class ClientActivityArgs : EventArgs, IClientEventArgs
    {
        /// <summary>
        /// The client
        /// </summary>
        public IClient Client { get; private set; }

        internal ClientActivityArgs (IClient client)
        {
            Client = client;
        }
    }

    /// <summary>
    /// Arguments passed to a client activity event
    /// </summary>
    class ClientActivityArgs<TIn,TOut> : EventArgs, IClientEventArgs<TIn,TOut>
    {
        /// <summary>
        /// The client
        /// </summary>
        public IClient<TIn,TOut> Client { get; private set; }

        internal ClientActivityArgs (IClient<TIn,TOut> client)
        {
            Client = client;
        }
    }
}
