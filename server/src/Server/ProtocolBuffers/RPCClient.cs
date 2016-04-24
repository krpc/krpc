using System;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class RPCClient : IClient<Request,Response>
    {
        readonly IClient<byte,byte> client;

        public RPCClient (string name, IClient<byte,byte> client)
        {
            Name = name;
            this.client = client;
            Stream = new RPCStream (client.Stream);
        }

        public string Name { get; private set; }

        public Guid Guid {
            get { return client.Guid; }
        }

        public string Address {
            get { return client.Address; }
        }

        public IStream<Request,Response> Stream { get; private set; }

        public bool Connected {
            get { return client.Connected; }
        }

        public void Close ()
        {
            client.Close ();
        }

        public override bool Equals (Object obj)
        {
            return obj != null && Equals (obj as RPCClient);
        }

        public bool Equals (IClient<Request,Response> other)
        {
            if ((object)other == null)
                return false;
            var otherClient = other as RPCClient;
            if ((object)otherClient == null)
                return false;
            return client == otherClient.client;
        }

        public override int GetHashCode ()
        {
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
            return !(lhs == rhs);
        }
    }
}

