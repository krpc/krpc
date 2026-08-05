using System;

namespace KRPC.Server.SerialIO
{
    sealed class ByteClient : IClient<byte,byte>
    {
        readonly Guid guid;
        BufferedPort port;
        ByteStream stream;

        public ByteClient (BufferedPort bufferedPort, byte[] buffer = null)
        {
            if (bufferedPort == null)
                throw new ArgumentNullException (nameof (bufferedPort));
            guid = Guid.NewGuid ();
            port = bufferedPort;
            Address = port.PortName;
            stream = new ByteStream (port, buffer);
        }

        public string Name {
            get { return Guid.ToString (); }
        }

        public Guid Guid {
            get { return guid; }
        }

        public string Address { get; private set; }

        public IStream<byte,byte> Stream {
            get { return stream; }
        }

        public bool Connected {
            get { return port != null && port.IsOpen; }
        }

        public void Close ()
        {
            stream.Close ();
            port = null;
        }

        public override bool Equals (object obj)
        {
            return obj != null && Equals (obj as ByteClient);
        }

        public bool Equals (IClient<byte,byte> other)
        {
            if (other == null)
                return false;
            var otherClient = other as ByteClient;
            if ((object)otherClient == null)
                return false;
            return guid == otherClient.guid;
        }

        public override int GetHashCode ()
        {
            return guid.GetHashCode ();
        }

        public static bool operator == (ByteClient lhs, ByteClient rhs)
        {
            if (ReferenceEquals (lhs, rhs))
                return true;
            if ((object)lhs == null || (object)rhs == null)
                return false;
            return lhs.Equals (rhs);
        }

        public static bool operator != (ByteClient lhs, ByteClient rhs)
        {
            return !(lhs == rhs);
        }
    }
}
