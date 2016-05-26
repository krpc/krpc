using System;
using System.Net;
using System.Net.Sockets;

namespace KRPC.Server.TCP
{
    sealed class TCPClient : IClient<byte,byte>
    {
        readonly Guid guid;
        readonly TcpClient tcpClient;
        TCPStream stream;

        public TCPClient (TcpClient tcpClient)
        {
            guid = Guid.NewGuid ();
            this.tcpClient = tcpClient;
            try {
                var remoteEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
                Address = remoteEndPoint.Address.ToString ();
            } catch {
                Address = "";
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
                    stream = new TCPStream (tcpClient.GetStream ());
                return stream;
            }
        }

        byte[] connectedTestBuffer = new byte[1];

        public bool Connected {
            get {
                try {
                    if (!tcpClient.Client.Connected) {
                        return false;
                    }
                    if (tcpClient.Client.Poll (0, SelectMode.SelectRead))
                        return tcpClient.Client.Receive (connectedTestBuffer, SocketFlags.Peek) != 0;
                    return true;
                } catch {
                    return false;
                }
            }
        }

        public void Close ()
        {
            tcpClient.Close ();
        }

        public override bool Equals (Object obj)
        {
            return obj != null && Equals (obj as TCPClient);
        }

        public bool Equals (IClient<byte,byte> other)
        {
            if ((object)other == null)
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
            if (Object.ReferenceEquals (lhs, rhs))
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

