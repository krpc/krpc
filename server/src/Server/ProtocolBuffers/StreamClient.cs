using System;

namespace KRPC.Server.ProtocolBuffers
{
    class StreamClient : Message.StreamClient
    {
        public StreamClient (IClient<byte,byte> client, Guid guid) :
            base (client, guid, new StreamStream (client.Stream))
        {
        }
    }
}
