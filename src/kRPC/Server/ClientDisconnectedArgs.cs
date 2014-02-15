using System;

namespace KRPC.Server
{
    class ClientDisconnectedArgs<In,Out> : EventArgs
    {
        public IClient<In,Out> Client { get; private set; }

        public ClientDisconnectedArgs(IClient<In,Out> client)
        {
            Client = client;
        }
    }
}
