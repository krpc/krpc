using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Server;

namespace KRPC.Test.Server
{
    // TODO: This is only required due to mocking not performing equality testing. Is there a better way to do this?
    [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
    sealed class TestClient : IClient<byte,byte>
    {
        readonly Guid guid;

        public TestClient (TestStream stream)
        {
            guid = Guid.NewGuid ();
            Stream = stream;
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

        public IStream<byte,byte> Stream { get; private set; }

        public bool Connected {
            get { throw new NotSupportedException (); }
        }

        public void Close ()
        {
            throw new NotSupportedException ();
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
