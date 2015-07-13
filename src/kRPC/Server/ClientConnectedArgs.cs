using System;

namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client connected event
    /// </summary>
    public class ClientConnectedArgs : EventArgs, IClientEventArgs
    {
        /// <summary>
        /// The client
        /// </summary>
        public IClient Client { get; private set; }

        internal ClientConnectedArgs (IClient client)
        {
            Client = client;
        }
    }

    /// <summary>
    /// Arguments passed to a client connected event
    /// </summary>
    class ClientConnectedArgs<TIn,TOut> : EventArgs, IClientEventArgs<TIn,TOut>
    {
        /// <summary>
        /// The client
        /// </summary>
        public IClient<TIn,TOut> Client { get; private set; }

        internal ClientConnectedArgs (IClient<TIn,TOut> client)
        {
            Client = client;
        }

        public static implicit operator ClientConnectedArgs (ClientConnectedArgs<TIn,TOut> args)
        {
            return new ClientConnectedArgs (args.Client);
        }
    }
}
