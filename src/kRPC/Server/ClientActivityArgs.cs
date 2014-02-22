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

    class ClientActivityArgs<In,Out> : EventArgs, IClientEventArgs<In,Out>
    {
        public IClient<In,Out> Client { get; private set; }

        public ClientActivityArgs (IClient<In,Out> client)
        {
            Client = client;
        }
    }
}
