namespace KRPC.Server.ProtocolBuffers
{
    sealed class RPCClient : Message.RPCClient
    {
        public RPCClient (string name, IClient<byte,byte> client) :
            base (name, client, new RPCStream (client.Stream))
        {
        }
    }
}
