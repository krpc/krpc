using System;
using System.Net;
using System.Net.Sockets;

namespace KRPC.Server.Net
{
    sealed class TCPClient : IClient<byte,byte>
    {
        private int uuid;
        private TcpClient tcpClient;

        public TCPClient (int uuid, TcpClient tcpClient)
        {
            this.uuid = uuid;
            this.tcpClient = tcpClient;
        }

        public string Address {
            get { return ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString(); }
        }

        public IStream<byte,byte> Stream {
            get { return new TCPStream (tcpClient.GetStream ()); }
        }

        public bool Connected {
            get {
                try {
                    if (!tcpClient.Client.Connected) {
                        return false;
                    }
                    if (tcpClient.Client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] buffer = new byte[1];
                        return tcpClient.Client.Receive(buffer, SocketFlags.Peek) != 0;
                    }
                    return true;
                } catch {
                    return false;
                }
            }
        }

        public void Close () {
            tcpClient.Close ();
        }

        public override bool Equals (Object other) {
            if (other == null)
                return false;
            return Equals(other as TCPClient);
        }

        public bool Equals (IClient<byte,byte> other) {
            if ((object)other == null)
                return false;
            TCPClient otherClient = other as TCPClient;
            if ((object)otherClient == null)
                return false;
            return uuid == otherClient.uuid;
        }

        public override int GetHashCode () {
            return uuid;
        }

        public static bool operator == (TCPClient lhs, TCPClient rhs)
        {
            if (Object.ReferenceEquals (lhs, rhs))
                return true;
            if ((object)lhs == null || (object)rhs == null)
                return false;
            return lhs.Equals(rhs);
        }

        public static bool operator != (TCPClient lhs, TCPClient rhs)
        {
            return !(lhs == rhs);
        }
    }
}

