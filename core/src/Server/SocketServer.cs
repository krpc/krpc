using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using KRPC.Utils;

namespace KRPC.Server
{
    /// <summary>
    /// A server that accepts clients on a listening socket. A thread accepts each connection,
    /// and the server's update adds it to the clients. Subclasses open the socket.
    /// </summary>
    public abstract class SocketServer : IServer<IByteStream>
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
        /// Event handler for when a new client has connected.
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs<IByteStream>> OnClientConnected;

        /// <summary>
        /// Event handler when a client disconnects.
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs<IByteStream>> OnClientDisconnected;

        readonly string logName;
        Socket listener;
        Thread listenerThread;
        AutoResetEvent startedEvent;
        volatile bool running;
        volatile bool stopping;
        readonly List<SocketClient> clients = new List<SocketClient> ();

        // Clients the listener thread has accepted, for the next update to add
        readonly object acceptedLock = new object ();
        List<SocketClient> accepted = new List<SocketClient> ();
        List<SocketClient> adding = new List<SocketClient> ();

        ulong closedClientsBytesRead;
        ulong closedClientsBytesWritten;

        /// <summary>
        /// A server whose log lines start with the given name.
        /// </summary>
        protected SocketServer (string name)
        {
            logName = name;
        }

        /// <summary>
        /// Open the listening socket. Throws a <see cref="ServerException"/> if it cannot be
        /// opened.
        /// </summary>
        protected abstract Socket Listen ();

        /// <summary>
        /// The client for an accepted connection.
        /// </summary>
        internal abstract SocketClient Accepted (Socket socket);

        /// <summary>
        /// Called once the server has stopped and closed its clients.
        /// </summary>
        protected virtual void Stopped ()
        {
        }

        /// <summary>
        /// Address the server is listening on. Format depends on the server protocol.
        /// </summary>
        public abstract string Address { get; }

        /// <summary>
        /// Information about the server, displayed in the UI.
        /// </summary>
        public abstract string Info { get; }

        /// <summary>
        /// Start the server.
        /// </summary>
        public void Start ()
        {
            if (running) {
                Logger.WriteLine (logName + ": start requested, but server is already running", Logger.Severity.Warning);
                return;
            }
            Logger.WriteLine (logName + ": starting", Logger.Severity.Debug);
            stopping = false;
            listener = Listen ();
            startedEvent = new AutoResetEvent (false);
            listenerThread = new Thread (ListenerThread);
            listenerThread.Start ();
            startedEvent.WaitOne (500);
            if (!running) {
                Logger.WriteLine (logName + ": failed to start server, timed out waiting for the listener to start", Logger.Severity.Error);
                // Closing the socket ends the listener thread's blocking accept
                stopping = true;
                listener.Close ();
                listenerThread.Join ();
                listener = null;
                throw new ServerException ("Failed to start server, timed out waiting for the listener to start");
            }
            EventHandlerExtensions.Invoke (OnStarted, this);
            Logger.WriteLine (logName + ": started successfully", Logger.Severity.Debug);
            Logger.WriteLine (logName + ": listening on " + Address, Logger.Severity.Debug);
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop ()
        {
            if (!running)
                return;
            Logger.WriteLine (logName + ": stop requested", Logger.Severity.Debug);
            stopping = true;
            listener.Close ();
            if (!listenerThread.Join (3000))
                throw new ServerException ("Failed to stop the listener thread (timed out after 3 seconds)");
            lock (acceptedLock) {
                foreach (var client in accepted)
                    DisconnectClient (client, true);
                accepted.Clear ();
            }
            foreach (var client in clients) {
                Logger.WriteLine (logName + ": closing connection to client (" + client.Address + ")", Logger.Severity.Debug);
                DisconnectClient (client);
            }
            clients.Clear ();
            Stopped ();
            running = false;
            Logger.WriteLine (logName + ": stopped successfully", Logger.Severity.Debug);
            EventHandlerExtensions.Invoke (OnStopped, this);
        }

        /// <summary>
        /// Update the server: remove the clients that have disconnected, and add the ones that
        /// have connected.
        /// </summary>
        public void Update ()
        {
            for (int i = clients.Count - 1; i >= 0; i--) {
                var client = clients [i];
                if (!client.Connected) {
                    clients.RemoveAt (i);
                    DisconnectClient (client);
                }
            }
            lock (acceptedLock) {
                var swap = adding;
                adding = accepted;
                accepted = swap;
            }
            foreach (var client in adding) {
                Logger.WriteLine (logName + ": connection from " + client.Label () + " accepted", Logger.Severity.Debug);
                clients.Add (client);
                EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs<IByteStream> (client));
            }
            adding.Clear ();
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
        public IEnumerable<IClient<IByteStream>> Clients {
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
                running = true;
                startedEvent.Set ();
                while (true) {
                    // Block until a client connects to the server
                    var socket = listener.Accept ();
                    var client = Accepted (socket);
                    Logger.WriteLine (logName + ".Listener: client connected (" + client.Address + ")", Logger.Severity.Debug);
                    lock (acceptedLock)
                        accepted.Add (client);
                    // Ends the server's wait for calls, so that its update adds the client
                    Arrivals.Set ();
                }
            } catch (Exception e) {
                // Stop closes the socket to end the blocking accept, which then throws
                if (stopping) {
                    Logger.WriteLine (logName + ".Listener: stopped", Logger.Severity.Debug);
                } else {
                    Logger.WriteLine (logName + ".Listener: stopped after an exception: " + e, Logger.Severity.Error);
                    listener.Close ();
                }
            }
        }

        void DisconnectClient (SocketClient client, bool noEvent = false)
        {
            var label = client.Label ();
            closedClientsBytesRead += client.Stream.BytesRead;
            closedClientsBytesWritten += client.Stream.BytesWritten;
            client.Close ();
            if (!noEvent)
                EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs<IByteStream> (client));
            Logger.WriteLine (logName + ": connection from " + label + " closed", Logger.Severity.Debug);
        }
    }
}
