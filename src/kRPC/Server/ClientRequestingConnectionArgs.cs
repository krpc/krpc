using System;

namespace KRPC.Server
{
    class ClientRequestingConnectionArgs : EventArgs, IClientEventArgs
    {
        public IClient Client { get; private set; }

        public ClientConnectionRequest Request { get; private set; }

        public ClientRequestingConnectionArgs (IClient client)
        {
            Client = client;
            Request = new ClientConnectionRequest ();
        }

        public ClientRequestingConnectionArgs (IClient client, ClientConnectionRequest request)
        {
            Client = client;
            Request = request;
        }
    }

    class ClientRequestingConnectionArgs<TIn,TOut> : EventArgs, IClientEventArgs<TIn,TOut>
    {
        public IClient<TIn,TOut> Client { get; private set; }

        public ClientConnectionRequest Request { get; private set; }

        public ClientRequestingConnectionArgs (IClient<TIn,TOut> client)
        {
            Client = client;
            Request = new ClientConnectionRequest ();
        }

        public ClientRequestingConnectionArgs (IClient<TIn,TOut> client, ClientConnectionRequest request)
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

