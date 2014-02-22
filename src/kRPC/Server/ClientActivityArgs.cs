using System;

namespace KRPC.Server
{
    class ClientActivityArgs : EventArgs, IClientEventArgs
    {
        public IClient Client { get; private set; }

        public ClientActivityArgs (IClient client)
        {
            Client = client;
        }
    }

    class ClientActivityArgs<TIn,TOut> : EventArgs, IClientEventArgs<TIn,TOut>
    {
        public IClient<TIn,TOut> Client { get; private set; }

        public ClientActivityArgs (IClient<TIn,TOut> client)
        {
            Client = client;
        }
    }
}
