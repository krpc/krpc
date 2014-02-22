using System;

namespace KRPC.Server
{
    class ClientDisconnectedArgs : EventArgs, IClientEventArgs
    {
        public IClient Client { get; private set; }

        public ClientDisconnectedArgs (IClient client)
        {
            Client = client;
        }
    }

    class ClientDisconnectedArgs<In,Out> : EventArgs, IClientEventArgs<In,Out>
    {
        public IClient<In,Out> Client { get; private set; }

        public ClientDisconnectedArgs (IClient<In,Out> client)
        {
            Client = client;
        }

        public static implicit operator ClientDisconnectedArgs (ClientDisconnectedArgs<In,Out> args)
        {
            return new ClientDisconnectedArgs (args.Client);
        }
    }
}
