using System;

namespace KRPC.Server.ProtocolBuffers
{
    class StreamClient : Message.StreamClient
    {
        public StreamClient (Guid guid, IClient<byte,byte> client) :
            base (guid, client, new StreamStream (client.Stream))
        {
        }
    }
}
