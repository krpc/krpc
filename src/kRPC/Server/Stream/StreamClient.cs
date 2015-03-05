using System;
using KRPC.Schema.KRPC;

namespace KRPC.Server.Stream
{
    sealed class StreamClient : IClient<byte,StreamMessage>
    {
        readonly IClient<byte,byte> client;

        public StreamClient (IClient<byte,byte> client)
        {
            this.client = client;
            Stream = new StreamStream (client.Stream);
        }

        public string Name {
            get { return client.Name; }
        }

        public Guid Guid {
            get { return client.Guid; }
        }

        public string Address {
            get { return client.Address; }
        }

        public IStream<byte,StreamMessage> Stream { get; private set; }

        public bool Connected {
            get { return client.Connected; }
        }

        public void Close ()
        {
            client.Close ();
        }

        public override bool Equals (Object obj)
        {
            return obj != null && Equals (obj as StreamClient);
        }

        public bool Equals (IClient<byte,StreamMessage> other)
        {
            if ((object)other == null)
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
            if (Object.ReferenceEquals (lhs, rhs))
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

