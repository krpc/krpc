using System;

namespace KRPC.Server
{
    class ClientConnectedArgs : EventArgs, IClientEventArgs
    {
        public IClient Client { get; private set; }

        public ClientConnectedArgs (IClient client)
        {
            Client = client;
        }
    }

    class ClientConnectedArgs<TIn,TOut> : EventArgs, IClientEventArgs<TIn,TOut>
    {
        public IClient<TIn,TOut> Client { get; private set; }

        public ClientConnectedArgs (IClient<TIn,TOut> client)
        {
            Client = client;
        }

        public static implicit operator ClientConnectedArgs (ClientConnectedArgs<TIn,TOut> args)
        {
            return new ClientConnectedArgs (args.Client);
        }
    }
}
