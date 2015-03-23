using System;

namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client disconnected event
    /// </summary>
    public class ClientDisconnectedArgs : EventArgs, IClientEventArgs
    {
        /// <summary>
        /// The client
        /// </summary>
        public IClient Client { get; private set; }

        internal ClientDisconnectedArgs (IClient client)
        {
            Client = client;
        }
    }

    /// <summary>
    /// Arguments passed to a client disconnected event
    /// </summary>
    class ClientDisconnectedArgs<TIn,TOut> : EventArgs, IClientEventArgs<TIn,TOut>
    {
        /// <summary>
        /// The client
        /// </summary>
        public IClient<TIn,TOut> Client { get; private set; }

        internal ClientDisconnectedArgs (IClient<TIn,TOut> client)
        {
            Client = client;
        }

        public static implicit operator ClientDisconnectedArgs (ClientDisconnectedArgs<TIn,TOut> args)
        {
            return new ClientDisconnectedArgs (args.Client);
        }
    }
}
