using System;

namespace KRPC.Server
{
    class ClientConnectedArgs : EventArgs, IClientEventArgs
    {
        public IClient Client { get; private set; }

        public ClientConnectedArgs(IClient client)
        {
            Client = client;
        }
    }

    class ClientConnectedArgs<In,Out> : EventArgs, IClientEventArgs<In,Out>
    {
        public IClient<In,Out> Client { get; private set; }

        public ClientConnectedArgs(IClient<In,Out> client)
        {
            Client = client;
        }

        public static implicit operator ClientConnectedArgs (ClientConnectedArgs<In,Out> args)
        {
            return new ClientConnectedArgs (args.Client);
        }
    }
}
