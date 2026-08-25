using System;
using System.Net.Sockets;
using KRPC.Server.TCP;

namespace KRPC.Server.LocalSocket
{
    sealed class LocalSocketClient : IClient<byte,byte>
    {
        readonly Guid guid;
        readonly Socket socket;
        readonly string address;
        TCPStream stream;

        public LocalSocketClient (Socket innerSocket, string socketPath)
        {
            if (innerSocket == null)
                throw new ArgumentNullException (nameof (innerSocket));
            guid = Guid.NewGuid ();
            socket = innerSocket;
            // A unix socket has no remote address, so the path the client connected to is
            // what identifies it
            address = socketPath ?? string.Empty;
        }

        public string Name {
            get { return Guid.ToString (); }
        }

        public Guid Guid {
            get { return guid; }
        }

        public string Address {
            get { return address; }
        }

        public IStream<byte,byte> Stream {
            get {
                if (stream == null)
                    stream = new TCPStream (new NetworkStream (socket));
                return stream;
            }
        }

        byte[] connectedTestBuffer = new byte[1];

        public bool Connected {
            get {
                try {
                    if (!socket.Connected)
                        return false;
                    // A closed peer polls readable and then reads nothing, which tells a
                    // disconnect apart from an idle connection
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
            socket.Close ();
        }

        public override bool Equals (object obj)
        {
            return obj != null && Equals (obj as LocalSocketClient);
        }

        public bool Equals (IClient<byte,byte> other)
        {
            if (other == null)
                return false;
            var otherClient = other as LocalSocketClient;
            if ((object)otherClient == null)
                return false;
            return guid == otherClient.guid;
        }

        public override int GetHashCode ()
        {
            return guid.GetHashCode ();
        }

        public static bool operator == (LocalSocketClient lhs, LocalSocketClient rhs)
        {
            if (ReferenceEquals (lhs, rhs))
                return true;
            if ((object)lhs == null || (object)rhs == null)
                return false;
            return lhs.Equals (rhs);
        }

        public static bool operator != (LocalSocketClient lhs, LocalSocketClient rhs)
        {
            return !(lhs == rhs);
        }
    }
}
