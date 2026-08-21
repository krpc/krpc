using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using KRPC.Utils;

namespace KRPC.Server.LocalSocket
{
    /// <summary>
    /// A raw server over a unix domain socket, for clients on the same machine.
    /// </summary>
    public sealed class LocalSocketServer : IServer<byte,byte>
    {
        /// <summary>
        /// Event handler for when the server starts.
        /// </summary>
        public event EventHandler OnStarted;

        /// <summary>
        /// Event handler for when the server stops.
        /// </summary>
        public event EventHandler OnStopped;

        /// <summary>
        /// Event handler for when a new client requests a connection.
        /// </summary>
        public event EventHandler<ClientRequestingConnectionEventArgs<byte,byte>> OnClientRequestingConnection;

        /// <summary>
        /// Event handler for when a new client has connected.
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs<byte,byte>> OnClientConnected;

        /// <summary>
        /// Event handler when client activity occurs.
        /// </summary>
        #pragma warning disable 0067
        public event EventHandler<ClientActivityEventArgs<byte,byte>> OnClientActivity;

        /// <summary>
        /// Event handler when a client disconnects.
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs<byte,byte>> OnClientDisconnected;

        /// <summary>
        /// Thread used to poll for new connections.
        /// </summary>
        Thread listenerThread;

        Socket listener;

        /// <summary>
        /// Event used to wait for the listener to start
        /// </summary>
        volatile AutoResetEvent startedEvent;

        /// <summary>
        /// True if the listenerThread is running.
        /// </summary>
        volatile bool running;

        /// <summary>
        /// Connected clients.
        /// </summary>
        List<LocalSocketClient> clients = new List<LocalSocketClient> ();

        /// <summary>
        /// Clients requesting a connection. Must be locked before accessing.
        /// </summary>
        List<LocalSocketClient> pendingClients = new List<LocalSocketClient> ();

        object pendingClientsLock = new object ();
        ulong closedClientsBytesRead;
        ulong closedClientsBytesWritten;

        /// <summary>
        /// Create a server over a unix domain socket. After Start() is called, the
        /// server will listen for connections on the given socket path.
        /// </summary>
        public LocalSocketServer (string path)
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
        /// A default path for a socket of the given name, in a directory belonging to
        /// the user running the game, so that sockets are neither shared between
        /// accounts nor left in a directory anyone can write to. Windows has no runtime
        /// directory for this, so its per-user application data directory stands in.
        /// </summary>
        public static string DefaultPath (string name)
        {
            var windows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var directory = Environment.GetEnvironmentVariable (
                windows ? "LOCALAPPDATA" : "XDG_RUNTIME_DIR");
            if (string.IsNullOrEmpty (directory))
                directory = Path.Combine (Path.GetTempPath (), "krpc-" + Environment.UserName);
            else
                directory = Path.Combine (directory, "krpc");
            return Path.Combine (directory, name);
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public void Start ()
        {
            if (OnClientRequestingConnection == null)
                throw new ServerException ("Client request handler not set");
            if (running) {
                Logger.WriteLine ("LocalSocketServer: start requested, but server is already running", Logger.Severity.Warning);
                return;
            }
            Logger.WriteLine ("LocalSocketServer: starting", Logger.Severity.Debug);
            if (string.IsNullOrEmpty (ListenPath))
                throw new ServerException ("Socket path is empty");
            // Bind reports a path that does not fit in the kernel's address structure as
            // nothing more specific than an invalid argument, so check it here and say
            // what is actually wrong
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
                listener = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                UnixSocket.BindAndListen (listener, ListenPath, 16);
            } catch (SocketException exn) {
                string socketError = "socket error '" + exn.SocketErrorCode + "': " + exn.Message;
                Logger.WriteLine ("LocalSocketServer: failed to start server; " + socketError, Logger.Severity.Error);
                throw new ServerException (socketError);
            } catch (IOException exn) {
                Logger.WriteLine ("LocalSocketServer: failed to start server; " + exn.Message, Logger.Severity.Error);
                throw new ServerException (exn.Message);
            }
            startedEvent = new AutoResetEvent (false);
            listenerThread = new Thread (ListenerThread);
            listenerThread.Start ();
            startedEvent.WaitOne (500);
            if (!running) {
                Logger.WriteLine ("LocalSocketServer: failed to start server, timed out waiting for the listener to start", Logger.Severity.Error);
                // Closing the socket causes the listener thread's blocking accept to
                // throw and exit (Thread.Abort is unsupported on modern .NET)
                listener.Close ();
                listenerThread.Join ();
                listener = null;
                throw new ServerException ("Failed to start server, timed out waiting for the listener to start");
            }
            EventHandlerExtensions.Invoke (OnStarted, this);
            Logger.WriteLine ("LocalSocketServer: started successfully", Logger.Severity.Debug);
            Logger.WriteLine ("LocalSocketServer: listening on " + ListenPath, Logger.Severity.Debug);
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
            // Only a refusal means nothing is there to answer. Anything else leaves the
            // question open, and a path that cannot be asked is not one to delete.
            if (error.Value != SocketError.ConnectionRefused)
                throw new ServerException (
                    "Could not find out whether a server is listening on " + ListenPath +
                    "; connecting to it reported '" + error.Value + "'");
            // A socket carries no content, so a path holding some is not one and belongs to
            // whoever put it there. A symbolic link counts as holding the path it names, so
            // one is reported rather than followed to whatever is on the end of it.
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
        /// Stop the server.
        /// </summary>
        public void Stop ()
        {
            if (!running)
                return;

            Logger.WriteLine ("LocalSocketServer: stop requested", Logger.Severity.Debug);
            listener.Close ();
            if (!listenerThread.Join (3000))
                throw new ServerException ("Failed to stop local socket listener thread (timed out after 3 seconds)");

            // Close all client connections
            foreach (var client in pendingClients) {
                Logger.WriteLine ("LocalSocketServer: cancelling pending connection to client (" + client.Address + ")", Logger.Severity.Debug);
                DisconnectClient (client, true);
            }
            foreach (var client in clients) {
                Logger.WriteLine ("LocalSocketServer: closing connection to client (" + client.Address + ")", Logger.Severity.Debug);
                DisconnectClient (client);
            }
            pendingClients.Clear ();
            clients.Clear ();

            // The socket file outlives the socket, so it has to be removed by hand
            try {
                File.Delete (ListenPath);
            } catch (IOException) {
            } catch (UnauthorizedAccessException) {
            }

            // Exited cleanly
            running = false;
            Logger.WriteLine ("LocalSocketServer: stopped successfully", Logger.Severity.Debug);

            EventHandlerExtensions.Invoke (OnStopped, this);
        }

        /// <summary>
        /// Update the server.
        /// </summary>
        public void Update ()
        {
            // Remove disconnected clients
            for (int i = clients.Count - 1; i >= 0; i--) {
                var client = clients [i];
                if (!client.Connected) {
                    clients.RemoveAt (i);
                    DisconnectClient (client);
                }
            }

            // Process pending clients
            lock (pendingClientsLock) {
                if (pendingClients.Count > 0) {
                    var stillPendingClients = new List<LocalSocketClient> ();
                    foreach (var client in pendingClients) {
                        // Trigger OnClientRequestingConnection events to verify the connection
                        var args = new ClientRequestingConnectionEventArgs<byte,byte> (client);
                        EventHandlerExtensions.Invoke (OnClientRequestingConnection, this, args);

                        // Deny the connection
                        if (args.Request.ShouldDeny) {
                            Logger.WriteLine ("LocalSocketServer: client connection denied (" + client.Address + ")", Logger.Severity.Debug);
                            DisconnectClient (client, true);
                        }

                        // Allow the connection
                        else if (args.Request.ShouldAllow) {
                            Logger.WriteLine ("LocalSocketServer: client connection accepted (" + client.Address + ")", Logger.Severity.Debug);
                            clients.Add (client);
                            EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs<byte,byte> (client));
                        }

                        // Still pending, will either be denied or allowed on a subsequent call to Update
                        else {
                            Logger.WriteLine ("LocalSocketServer: client connection still pending (" + client.Address + ")", Logger.Severity.Debug);
                            stillPendingClients.Add (client);
                        }
                    }
                    pendingClients = stillPendingClients;
                }
            }
        }

        /// <summary>
        /// Address the server is listening on. Format depends on the server protocol.
        /// </summary>
        public string Address {
            get { return ListenPath; }
        }

        /// <summary>
        /// Information about the server, displayed in the UI. A unix domain socket is
        /// reachable only from the machine it is on, so there is nothing to say about
        /// who can reach this one that the protocol does not already say.
        /// </summary>
        public string Info {
            get { return string.Empty; }
        }

        /// <summary>
        /// Whether the server is running.
        /// </summary>
        public bool Running {
            get { return running; }
        }

        /// <summary>
        /// Clients connected to the server.
        /// </summary>
        public IEnumerable<IClient<byte,byte>> Clients {
            get {
                foreach (var client in clients)
                    yield return client;
            }
        }

        /// <summary>
        /// Number of bytes received from clients.
        /// </summary>
        public ulong BytesRead {
            get {
                ulong read = closedClientsBytesRead;
                for (int i = 0; i < clients.Count; i++)
                    read += clients [i].Stream.BytesRead;
                return read;
            }
        }

        /// <summary>
        /// Number of bytes sent to clients.
        /// </summary>
        public ulong BytesWritten {
            get {
                ulong written = closedClientsBytesWritten;
                for (int i = 0; i < clients.Count; i++)
                    written += clients [i].Stream.BytesWritten;
                return written;
            }
        }

        /// <summary>
        /// Clear statistics.
        /// </summary>
        public void ClearStats ()
        {
            closedClientsBytesRead = 0;
            closedClientsBytesWritten = 0;
            foreach (var client in clients)
                client.Stream.ClearStats ();
        }

        void ListenerThread ()
        {
            try {
                try {
                    running = true;
                    startedEvent.Set ();
                    while (true) {
                        // Block until a client connects to the server
                        var client = listener.Accept ();
                        Logger.WriteLine ("LocalSocketServer.Listener: client requesting connection", Logger.Severity.Debug);
                        // Add to pending clients
                        lock (pendingClientsLock) {
                            pendingClients.Add (new LocalSocketClient (client, ListenPath));
                        }
                    }
                } catch (SocketException e) {
                    if (e.SocketErrorCode == SocketError.Interrupted)
                        Logger.WriteLine ("LocalSocketServer.Listener: stopped", Logger.Severity.Debug);
                    else
                        throw;
                } catch (ObjectDisposedException) {
                    // Stop() closes the socket to interrupt the blocking accept above
                    Logger.WriteLine ("LocalSocketServer.Listener: stopped", Logger.Severity.Debug);
                }
            } catch (Exception e) {
                Logger.WriteLine ("LocalSocketServer.Listener: caught exception, stopped", Logger.Severity.Error);
                Logger.WriteLine (e.GetType ().Name);
                Logger.WriteLine (e.Message);
                Logger.WriteLine (e.StackTrace);
                listener.Close ();
            }
        }

        void DisconnectClient (IClient<byte,byte> client, bool noEvent = false)
        {
            var clientAddress = client.Address;
            try {
                var stream = client.Stream;
                closedClientsBytesRead += stream.BytesRead;
                closedClientsBytesWritten += stream.BytesWritten;
            } catch (ClientDisconnectedException) {
            }
            client.Close ();
            if (!noEvent)
                EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs<byte,byte> (client));
            Logger.WriteLine ("LocalSocketServer: client disconnected (" + clientAddress + ")");
        }
    }
}
