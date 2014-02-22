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

    class ClientDisconnectedArgs<TIn,TOut> : EventArgs, IClientEventArgs<TIn,TOut>
    {
        public IClient<TIn,TOut> Client { get; private set; }

        public ClientDisconnectedArgs (IClient<TIn,TOut> client)
        {
            Client = client;
        }

        public static implicit operator ClientDisconnectedArgs (ClientDisconnectedArgs<TIn,TOut> args)
        {
            return new ClientDisconnectedArgs (args.Client);
        }
    }
}
