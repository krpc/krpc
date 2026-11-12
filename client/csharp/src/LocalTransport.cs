using System.Net.Sockets;

namespace KRPC.Client
{
    /// <summary>
    /// A transport to a server on the same machine, over a unix domain socket.
    /// </summary>
    sealed class LocalTransport : Transport
    {
        /// <summary>
        /// Connect to the socket at the given path. Throws a SocketException if the connection
        /// fails.
        /// </summary>
        public LocalTransport (string path)
            : base (Open (path))
        {
        }

        static Socket Open (string path)
        {
            var socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try {
                socket.Connect (new UnixEndPoint (path));
            } catch (SocketException) {
                socket.Close ();
                throw;
            }
            return socket;
        }
    }
}
