using System;

namespace KRPC.Server
{
    class ClientConnectedArgs<In,Out> : EventArgs
    {
        public IClient<In,Out> Client { get; private set; }

        public ClientConnectedArgs(IClient<In,Out> client)
        {
            Client = client;
        }
    }
}
