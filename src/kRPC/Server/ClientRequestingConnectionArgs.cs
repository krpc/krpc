using System;
using System.Net.Sockets;

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

    class ClientRequestingConnectionArgs<In,Out> : EventArgs, IClientEventArgs<In,Out>
    {
        public IClient<In,Out> Client { get; private set; }

        public ClientConnectionRequest Request { get; private set; }

        public ClientRequestingConnectionArgs (IClient<In,Out> client)
        {
            Client = client;
            Request = new ClientConnectionRequest ();
        }

        public ClientRequestingConnectionArgs (IClient<In,Out> client, ClientConnectionRequest request)
        {
            Client = client;
            Request = request;
        }

        public static implicit operator ClientRequestingConnectionArgs (ClientRequestingConnectionArgs<In,Out> args)
        {
            return new ClientRequestingConnectionArgs (args.Client, args.Request);
        }
    }
}

