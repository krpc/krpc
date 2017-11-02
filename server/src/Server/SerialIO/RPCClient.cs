namespace KRPC.Server.SerialIO
{
    sealed class RPCClient : Message.RPCClient
    {
        public RPCClient (string name, IClient<byte,byte> client, IServer<byte,byte> server) :
            base (name, client, new RPCStream (client.Stream, server))
        {
        }
    }
}
