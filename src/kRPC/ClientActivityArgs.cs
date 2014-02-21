using System;

namespace KRPC.Server
{
    class ClientActivityArgs : EventArgs
    {
        public IClient Client { get; private set; }

        public ClientActivityArgs(IClient client)
        {
            Client = client;
        }
    }
}
