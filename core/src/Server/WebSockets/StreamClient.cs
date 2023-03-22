using System;

namespace KRPC.Server.WebSockets
{
    sealed class StreamClient : Message.StreamClient
    {
        public StreamClient (Guid guid, IClient<byte,byte> client) :
            base (guid, client, new StreamStream (client.Stream))
        {
        }
    }
}
