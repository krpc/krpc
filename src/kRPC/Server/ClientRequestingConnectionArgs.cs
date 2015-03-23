using System;

namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client requesting connection event
    /// </summary>
    public class ClientRequestingConnectionArgs : EventArgs, IClientEventArgs
    {
        /// <summary>
        /// The client
        /// </summary>
        public IClient Client { get; private set; }

        /// <summary>
        /// The request
        /// </summary>
        public ClientConnectionRequest Request { get; private set; }

        internal ClientRequestingConnectionArgs (IClient client)
        {
            Client = client;
            Request = new ClientConnectionRequest ();
        }

        internal ClientRequestingConnectionArgs (IClient client, ClientConnectionRequest request)
        {
            Client = client;
            Request = request;
        }
    }

    /// <summary>
    /// Arguments passed to a client requesting connection event
    /// </summary>
    class ClientRequestingConnectionArgs<TIn,TOut> : EventArgs, IClientEventArgs<TIn,TOut>
    {
        /// <summary>
        /// The client
        /// </summary>
        public IClient<TIn,TOut> Client { get; private set; }

        /// <summary>
        /// The request
        /// </summary>
        public ClientConnectionRequest Request { get; private set; }

        internal ClientRequestingConnectionArgs (IClient<TIn,TOut> client)
        {
            Client = client;
            Request = new ClientConnectionRequest ();
        }

        internal ClientRequestingConnectionArgs (IClient<TIn,TOut> client, ClientConnectionRequest request)
        {
            Client = client;
            Request = request;
        }

        public static implicit operator ClientRequestingConnectionArgs (ClientRequestingConnectionArgs<TIn,TOut> args)
        {
            return new ClientRequestingConnectionArgs (args.Client, args.Request);
        }
    }
}

