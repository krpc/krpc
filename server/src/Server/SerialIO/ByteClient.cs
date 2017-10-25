using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.IO.Ports;

namespace KRPC.Server.SerialIO
{
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    sealed class ByteClient : IClient<byte,byte>
    {
        readonly Guid guid;
        SerialPort port;
        ByteStream stream;

        public ByteClient (SerialPort serialPort, byte[] buffer = null)
        {
            if (serialPort == null)
                throw new ArgumentNullException (nameof (serialPort));
            guid = Guid.NewGuid ();
            port = serialPort;
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
