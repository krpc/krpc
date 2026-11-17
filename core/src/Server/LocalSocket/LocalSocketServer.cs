using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using KRPC.Utils;

namespace KRPC.Server.LocalSocket
{
    /// <summary>
    /// A raw server over a unix domain socket, for clients on the same machine.
    /// </summary>
    public sealed class LocalSocketServer : SocketServer
    {
        /// <summary>
        /// Create a server over a unix domain socket. After Start() is called, the
        /// server will listen for connections on the given socket path.
        /// </summary>
        public LocalSocketServer (string path) : base ("LocalSocketServer")
        {
            ListenPath = path;
        }

        /// <summary>
        /// Path of the socket the server listens on.
        /// </summary>
        public string ListenPath { get; set; }

        /// <summary>
        /// The longest a socket path may be, counted in the bytes it takes rather than the
        /// characters it is written with. The structure the path is copied into holds 108
        /// bytes on Linux and Windows and 104 on macOS, including the terminating zero, so
        /// this is the smallest of them with room to spare.
        /// </summary>
        public const int MaximumPathLength = 100;

        /// <summary>
        /// How much of a socket address the given path takes up.
        /// </summary>
        public static int PathLength (string path)
        {
            return Encoding.UTF8.GetByteCount (path);
        }

        /// <summary>
        /// The default socket path. It is krpc.sock in the runtime directory of the user
        /// running the game, which only that account can access. Windows has no runtime
        /// directory, so its per-user application data directory stands in.
        ///
        /// A client works the same path out for itself, so the two only meet if they agree
        /// on every step. The fallback therefore names the fixed directory /tmp, because
        /// TMPDIR can differ between the two processes. Windows always names a directory in
        /// LOCALAPPDATA and so never reaches the fallback.
        /// </summary>
        public static string DefaultPath ()
        {
            var windows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var directory = Environment.GetEnvironmentVariable (
                windows ? "LOCALAPPDATA" : "XDG_RUNTIME_DIR");
            if (!string.IsNullOrEmpty (directory))
                return Path.Combine (directory, "krpc.sock");
            var temporary = windows ? Path.GetTempPath () : "/tmp";
            return Path.Combine (temporary, "krpc-" + Environment.UserName + ".sock");
        }

        /// <summary>
        /// Open the listening socket.
        /// </summary>
        protected override Socket Listen ()
        {
            if (string.IsNullOrEmpty (ListenPath))
                throw new ServerException ("Socket path is empty");
            // Bind reports a path too long for the kernel's address structure as an invalid
            // argument, so check the length here and report what is wrong
            var pathLength = PathLength (ListenPath);
            if (pathLength > MaximumPathLength)
                throw new ServerException (
                    "Socket path takes " + pathLength + " bytes, which is longer " +
                    "than the " + MaximumPathLength + " a socket path may be");
            try {
                var directory = Path.GetDirectoryName (ListenPath);
                if (!string.IsNullOrEmpty (directory))
                    Directory.CreateDirectory (directory);
                ClearStaleSocketFile ();
                var listener = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                UnixSocket.BindAndListen (listener, ListenPath, 16);
                return listener;
            } catch (SocketException exn) {
                string socketError = "socket error '" + exn.SocketErrorCode + "': " + exn.Message;
                Logger.WriteLine ("LocalSocketServer: failed to start server; " + socketError, Logger.Severity.Error);
                throw new ServerException (socketError);
            } catch (IOException exn) {
                Logger.WriteLine ("LocalSocketServer: failed to start server; " + exn.Message, Logger.Severity.Error);
                throw new ServerException (exn.Message);
            }
        }

        // A unix socket has no remote address, so the path the client connected to is what
        // identifies it
        internal override SocketClient Accepted (Socket socket)
        {
            return new SocketClient (socket, ListenPath);
        }

        /// <summary>
        /// Remove the socket file, which outlives the socket.
        /// </summary>
        protected override void Stopped ()
        {
            try {
                File.Delete (ListenPath);
            } catch (IOException) {
            } catch (UnauthorizedAccessException) {
            }
        }

        /// <summary>
        /// Remove a socket file left behind by a run that was killed rather than stopped, which
        /// would otherwise make the bind fail for good. The file outlives the server that made
        /// it and says nothing about whether that server is still there, so the only way to tell
        /// one still in use from one merely left over is to see whether anything answers on it.
        /// </summary>
        void ClearStaleSocketFile ()
        {
            // A directory at the path is nothing this could remove, and the bind reports it
            if (!File.Exists (ListenPath))
                return;
            var error = ConnectError (ListenPath);
            if (!error.HasValue)
                throw new ServerException (
                    "Another server is already listening on " + ListenPath);
            // Only a refusal means nothing is listening. Any other error leaves that
            // unknown, so the path is left alone
            if (error.Value != SocketError.ConnectionRefused)
                throw new ServerException (
                    "Could not find out whether a server is listening on " + ListenPath +
                    "; connecting to it reported '" + error.Value + "'");
            // A socket carries no content, so a path with content belongs to something
            // else. A symbolic link holds the path it names, and is reported without
            // being followed
            if (new FileInfo (ListenPath).Length > 0)
                throw new ServerException (
                    ListenPath + " already exists and is not a socket");
            try {
                File.Delete (ListenPath);
            } catch (UnauthorizedAccessException exn) {
                throw new ServerException (
                    "Could not remove the socket file left at " + ListenPath + "; " + exn.Message);
            }
        }

        /// <summary>
        /// What connecting to the given path reports, or null if the connection was made and
        /// so a server is listening there.
        /// </summary>
        static SocketError? ConnectError (string path)
        {
            using (var socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified)) {
                try {
                    UnixSocket.Connect (socket, path);
                } catch (SocketException exn) {
                    return exn.SocketErrorCode;
                }
                return null;
            }
        }

        /// <summary>
        /// Address the server is listening on. Format depends on the server protocol.
        /// </summary>
        public override string Address {
            get { return ListenPath; }
        }

        /// <summary>
        /// Information about the server, displayed in the UI. A unix domain socket is
        /// reachable only from the machine it is on, so there is nothing to say about
        /// who can reach this one that the protocol does not already say.
        /// </summary>
        public override string Info {
            get { return string.Empty; }
        }
    }
}
