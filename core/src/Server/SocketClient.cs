using System;
using System.Net.Sockets;

namespace KRPC.Server
{
    /// <summary>
    /// A client connected over a socket: TCP/IP, or a unix domain socket.
    /// </summary>
    sealed class SocketClient : IClient<IByteStream>
    {
        readonly SocketStream stream;

        public SocketClient (Socket socket, string address)
        {
            if (socket == null)
                throw new ArgumentNullException (nameof (socket));
            Guid = Guid.NewGuid ();
            Address = address ?? string.Empty;
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

        /// <summary>
        /// Whether the client is connected: until the peer closes and the bytes it sent before
        /// are read, or until the client is closed.
        /// </summary>
        public bool Connected {
            get { return stream.Open; }
        }

        public void Close ()
        {
            stream.Close ();
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
