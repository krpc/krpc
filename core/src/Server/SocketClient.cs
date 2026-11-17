using System;
using System.Net.Sockets;

namespace KRPC.Server
{
    /// <summary>
    /// A client connected over a socket: TCP/IP, or a unix domain socket.
    /// </summary>
    sealed class SocketClient : IClient<IByteStream>
    {
        readonly Socket socket;
        readonly SocketStream stream;
        readonly byte[] connectedTestBuffer = new byte[1];

        public SocketClient (Socket socket, string address)
        {
            if (socket == null)
                throw new ArgumentNullException (nameof (socket));
            Guid = Guid.NewGuid ();
            Address = address ?? string.Empty;
            this.socket = socket;
            stream = new SocketStream (new NetworkStream (socket), socket);
        }

        public string Name {
            get { return Guid.ToString (); }
        }

        public Guid Guid { get; private set; }

        public string Address { get; private set; }

        public IByteStream Stream {
            get { return stream; }
        }

        public bool Connected {
            get {
                try {
                    if (!(socket.Connected || stream.HalfClosed))
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
            return Equals (obj as SocketClient);
        }

        public bool Equals (IClient<IByteStream> other)
        {
            var client = other as SocketClient;
            return client != null && Guid == client.Guid;
        }

        public override int GetHashCode ()
        {
            return Guid.GetHashCode ();
        }
    }
}
