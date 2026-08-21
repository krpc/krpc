using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace KRPC.Server.LocalSocket
{
    /// <summary>
    /// Putting a unix domain socket into a state where it accepts connections.
    ///
    /// Mono, which the game runs on, builds the address a bind needs out of a SocketAddress,
    /// and its Windows build is compiled without a unix domain case, so it answers a bind for
    /// one with "address family not supported". There the socket is bound by calling winsock
    /// directly, and told to listen the same way, as a socket refuses to listen unless it was
    /// bound through it. Mono asks nothing of a socket before accepting on it, so accepting
    /// connections, reading and writing go through the socket as usual.
    ///
    /// Every other runtime binds a unix domain socket itself, and is left to: a bind that goes
    /// straight to the handle does not tell the socket it is bound, and they refuse to accept
    /// on a socket they do not believe was bound.
    /// </summary>
    static class UnixSocket
    {
        /// <summary>
        /// What winsock returns from a call that failed.
        /// </summary>
        const int Failed = -1;

        [DllImport ("ws2_32.dll", EntryPoint = "bind")]
        static extern int WinsockBind (IntPtr socket, byte[] address, int addressLength);

        [DllImport ("ws2_32.dll", EntryPoint = "listen")]
        static extern int WinsockListen (IntPtr socket, int backlog);

        [DllImport ("ws2_32.dll", EntryPoint = "connect")]
        static extern int WinsockConnect (IntPtr socket, byte[] address, int addressLength);

        [DllImport ("ws2_32.dll", EntryPoint = "WSAGetLastError")]
        static extern int WinsockLastError ();

        /// <summary>
        /// Whether the socket has to be bound by calling winsock rather than through the
        /// runtime, which is so of the one runtime that cannot bind a unix domain socket.
        /// </summary>
        public static bool NeedsWinsock {
            get {
                return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                    Type.GetType ("Mono.Runtime") != null;
            }
        }

        /// <summary>
        /// Bind the socket to the given path and start it listening, allowing the given number
        /// of connections to be waiting to be accepted.
        /// </summary>
        public static void BindAndListen (Socket socket, string path, int backlog)
        {
            if (socket == null)
                throw new ArgumentNullException (nameof (socket));
            if (!NeedsWinsock) {
                socket.Bind (new UnixEndPoint (path));
                socket.Listen (backlog);
                return;
            }
            BindAndListenThroughWinsock (socket, path, backlog);
        }

        /// <summary>
        /// Bind and listen by calling winsock, whatever runtime this is. Only a runtime that
        /// cannot do it for itself goes this way, but any of them running on Windows can, which
        /// is what lets the calls below be tested wherever there is a winsock to make them.
        /// </summary>
        public static void BindAndListenThroughWinsock (Socket socket, string path, int backlog)
        {
            if (socket == null)
                throw new ArgumentNullException (nameof (socket));
            var address = Address (path);
            if (WinsockBind (socket.Handle, address, address.Length) == Failed)
                throw new SocketException (WinsockLastError ());
            if (WinsockListen (socket.Handle, backlog) == Failed)
                throw new SocketException (WinsockLastError ());
        }

        /// <summary>
        /// Connect the socket to the given path, whichever way this runtime allows.
        /// </summary>
        public static void Connect (Socket socket, string path)
        {
            if (socket == null)
                throw new ArgumentNullException (nameof (socket));
            if (!NeedsWinsock) {
                socket.Connect (new UnixEndPoint (path));
                return;
            }
            ConnectThroughWinsock (socket, path);
        }

        /// <summary>
        /// Connect by calling winsock, whatever runtime this is, as with the bind above.
        /// </summary>
        public static void ConnectThroughWinsock (Socket socket, string path)
        {
            if (socket == null)
                throw new ArgumentNullException (nameof (socket));
            var address = Address (path);
            if (WinsockConnect (socket.Handle, address, address.Length) == Failed)
                throw new SocketException (WinsockLastError ());
        }

        /// <summary>
        /// The path as the socket address winsock is handed. It is encoded once, here as
        /// everywhere, and copied into the whole of the field the system expects, with the room
        /// left over already zero.
        /// </summary>
        static byte[] Address (string path)
        {
            var endPoint = new UnixEndPoint (path).Serialize ();
            var address = new byte [UnixEndPoint.AddressLength];
            for (int i = 0; i < endPoint.Size; i++)
                address [i] = endPoint [i];
            return address;
        }
    }
}
