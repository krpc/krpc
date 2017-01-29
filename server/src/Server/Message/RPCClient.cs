using System;
using KRPC.Service.Messages;

namespace KRPC.Server.Message
{
    abstract class RPCClient : IClient<Request,Response>
    {
        readonly IClient<byte,byte> client;

        protected RPCClient (string name, IClient<byte,byte> innerClient, IStream<Request,Response> stream)
        {
            Name = name;
            client = innerClient;
            Stream = stream;
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

        public override bool Equals (object obj)
        {
            return obj != null && Equals (obj as RPCClient);
        }

        public bool Equals (IClient<Request,Response> other)
        {
            if (other == null)
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
            if (ReferenceEquals (lhs, rhs))
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
