using System;
using System.Net;
using System.Net.Sockets;

namespace KRPC.Server.TCP
{
    sealed class TCPClient : IClient<byte,byte>
    {
        readonly Guid guid;
        readonly TcpClient client;
        TCPStream stream;

        public TCPClient (TcpClient innerClient)
        {
            if (innerClient == null)
                throw new ArgumentNullException (nameof (innerClient));
            guid = Guid.NewGuid ();
            client = innerClient;
            Address = string.Empty;
            try {
                var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                if (remoteEndPoint != null)
                    Address = remoteEndPoint.Address.ToString ();
            } catch (SocketException) {
            } catch (ObjectDisposedException) {
            }
        }

        public string Name {
            get { return Guid.ToString (); }
        }

        public Guid Guid {
            get { return guid; }
        }

        public string Address { get; private set; }

        public IStream<byte,byte> Stream {
            get {
                if (stream == null)
                    stream = new TCPStream (client.GetStream (), client.Client);
                return stream;
            }
        }

        byte[] connectedTestBuffer = new byte[1];

        public bool Connected {
            get {
                try {
                    // A closed client has no socket
                    var socket = client.Client;
                    if (socket == null || !(socket.Connected || (stream != null && stream.HalfClosed)))
                        return false;
                    if (socket.Poll (0, SelectMode.SelectRead))
                        return socket.Receive (connectedTestBuffer, SocketFlags.Peek) != 0;
                    return true;
                } catch (SocketException) {
                    return false;
                } catch (ObjectDisposedException) {
                    return false;
                }
            }
        }

        public void Close ()
        {
            client.Close ();
        }

        public override bool Equals (object obj)
        {
            return obj != null && Equals (obj as TCPClient);
        }

        public bool Equals (IClient<byte,byte> other)
        {
            if (other == null)
                return false;
            var otherClient = other as TCPClient;
            if ((object)otherClient == null)
                return false;
            return guid == otherClient.guid;
        }

        public override int GetHashCode ()
        {
            return guid.GetHashCode ();
        }

        public static bool operator == (TCPClient lhs, TCPClient rhs)
        {
            if (ReferenceEquals (lhs, rhs))
                return true;
            if ((object)lhs == null || (object)rhs == null)
                return false;
            return lhs.Equals (rhs);
        }

        public static bool operator != (TCPClient lhs, TCPClient rhs)
        {
            return !(lhs == rhs);
        }
    }
}
