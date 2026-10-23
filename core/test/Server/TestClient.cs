using System;
using KRPC.Server;

namespace KRPC.Test.Server
{
    // A concrete client stand-in: framework-generated mocks do not implement the
    // equality comparisons the server code under test relies on, so tests that
    // compare clients use this class instead.
    sealed class TestClient : IClient<byte,byte>
    {
        readonly Guid guid;

        readonly TestStream stream;

        public TestClient (TestStream innerStream)
        {
            guid = Guid.NewGuid ();
            stream = innerStream;
        }

        public string Name {
            get { throw new NotSupportedException (); }
        }

        public Guid Guid {
            get { return guid; }
        }

        public string Address {
            get { return "TestClientAddress"; }
        }

        public IStream<byte,byte> Stream {
            get { return stream; }
        }

        /// <summary>
        /// Whether the other end of the connection has closed it.
        /// </summary>
        public bool PeerClosed { get; set; }

        // As TCPClient, which sees a closed peer only once its unread bytes are read
        public bool Connected {
            get { return !stream.Closed && !(PeerClosed && !stream.DataAvailable); }
        }

        public void Close ()
        {
            stream.Closed = true;
        }

        public override bool Equals (object obj)
        {
            return obj != null && Equals (obj as TestClient);
        }

        public bool Equals (IClient<byte,byte> other)
        {
            if (other == null)
                return false;
            var otherClient = other as TestClient;
            if ((object)otherClient == null)
                return false;
            return guid == otherClient.guid;
        }

        public override int GetHashCode ()
        {
            return guid.GetHashCode ();
        }

        public static bool operator == (TestClient lhs, TestClient rhs)
        {
            if (ReferenceEquals (lhs, rhs))
                return true;
            if ((object)lhs == null || (object)rhs == null)
                return false;
            return lhs.Equals (rhs);
        }

        public static bool operator != (TestClient lhs, TestClient rhs)
        {
            return !(lhs == rhs);
        }
    }
}
