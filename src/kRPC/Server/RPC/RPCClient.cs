using System;
using System.Net.Sockets;
using KRPC.Schema.KRPC;

namespace KRPC.Server.RPC
{
    sealed class RPCClient : IClient<Request,Response>
    {
        private IClient<byte,byte> client;

        public RPCClient (string name, IClient<byte,byte> client)
        {
            Name = name;
            this.client = client;
            Stream = new RPCStream (client.Stream);
        }

        public string Name { get; private set; }

        public string Address {
            get { return client.Address; }
        }

        public IStream<Request,Response> Stream { get; private set; }

        public bool Connected {
            get { return client.Connected; }
        }

        public void Close () {
            client.Close ();
        }

        public override bool Equals(Object other) {
            if (other == null)
                return false;
            return Equals(other as RPCClient);
        }

        public bool Equals (IClient<Request,Response> other) {
            if ((object)other == null)
                return false;
            RPCClient otherClient = other as RPCClient;
            if ((object)otherClient == null)
                return false;
            return client == otherClient.client;
        }

        public override int GetHashCode () {
            return client.GetHashCode ();
        }

        public static bool operator == (RPCClient lhs, RPCClient rhs)
        {
            if (Object.ReferenceEquals (lhs, rhs))
                return true;
            if ((object)lhs == null || (object)rhs == null)
                return false;
            return lhs.Equals (rhs);
        }

        public static bool operator != (RPCClient lhs, RPCClient rhs)
        {
            return ! (lhs == rhs);
        }
    }
}

