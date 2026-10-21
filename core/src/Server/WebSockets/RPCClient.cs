namespace KRPC.Server.WebSockets
{
    sealed class RPCClient : Message.RPCClient
    {
        public RPCClient (string name, uint protocolVersion, IClient<byte,byte> client, bool echo = false) :
            base (name, protocolVersion, client, new RPCStream (client.Stream, echo))
        {
        }
    }
}
