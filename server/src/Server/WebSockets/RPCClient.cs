namespace KRPC.Server.WebSockets
{
    sealed class RPCClient : Message.RPCClient
    {
        public RPCClient (string name, IClient<byte,byte> client, bool echo = false) :
            base (name, client, new RPCStream (client.Stream, echo))
        {
        }
    }
}
