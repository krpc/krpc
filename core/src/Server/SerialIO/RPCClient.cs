namespace KRPC.Server.SerialIO
{
    sealed class RPCClient : Message.RPCClient
    {
        public RPCClient (string name, uint protocolVersion, IClient<byte,byte> client, IServer<byte,byte> server) :
            base (name, protocolVersion, client, new RPCStream (client.Stream, server))
        {
        }
    }
}
