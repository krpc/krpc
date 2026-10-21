namespace KRPC.Server.ProtocolBuffers
{
    sealed class RPCClient : Message.RPCClient
    {
        public RPCClient (string name, uint protocolVersion, IClient<byte,byte> client) :
            base (name, protocolVersion, client, new RPCStream (client.Stream))
        {
        }
    }
}
