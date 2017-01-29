using System;
using KRPC.Service.Messages;

namespace KRPC.Server.Message
{
    abstract class StreamClient : IClient<NoMessage,StreamUpdate>
    {
        readonly IClient<byte,byte> client;

        protected StreamClient (Guid guid, IClient<byte,byte> innerClient, IStream<NoMessage,StreamUpdate> stream)
        {
            Guid = guid;
            client = innerClient;
            Stream = stream;
        }

        public string Name {
            get { return client.Name; }
        }

        public Guid Guid { get; private set; }

        public string Address {
            get { return client.Address; }
        }

        public IStream<NoMessage,StreamUpdate> Stream { get; private set; }

        public bool Connected {
            get { return client.Connected; }
        }

        public void Close ()
        {
            client.Close ();
        }

        public override bool Equals (object obj)
        {
            return obj != null && Equals (obj as StreamClient);
        }

        public bool Equals (IClient<NoMessage,StreamUpdate> other)
        {
            if (other == null)
                return false;
            var otherClient = other as StreamClient;
            if ((object)otherClient == null)
                return false;
            return client == otherClient.client;
        }

        public override int GetHashCode ()
        {
            return client.GetHashCode ();
        }

        public static bool operator == (StreamClient lhs, StreamClient rhs)
        {
            if (ReferenceEquals (lhs, rhs))
                return true;
            if ((object)lhs == null || (object)rhs == null)
                return false;
            return lhs.Equals (rhs);
        }

        public static bool operator != (StreamClient lhs, StreamClient rhs)
        {
            return !(lhs == rhs);
        }
    }
}
