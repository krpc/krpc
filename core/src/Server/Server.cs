using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server
{
    /// <summary>
    /// A kRPC server.
    /// </summary>
    public sealed class Server : IServer
    {
        /// <summary>
        /// Identifier of the server.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Protocol of the server.
        /// </summary>
        public Protocol Protocol { get; private set; }

        /// <summary>
        /// Name of the server.
        /// </summary>
        public string Name { get; set; }

        internal IServer<Request,Response> InnerServer { get; private set; }

        /// <summary>
        /// Event triggered when the server starts
        /// </summary>
        public event EventHandler OnStarted;

        /// <summary>
        /// Event triggered when the server stops
        /// </summary>
        public event EventHandler OnStopped;

        /// <summary>
        /// Event triggered when a client is requesting a connection
        /// </summary>
        public event EventHandler<ClientRequestingConnectionEventArgs> OnClientRequestingConnection;

        /// <summary>
        /// Event triggered when a client has connected
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> OnClientConnected;

        /// <summary>
        /// Event triggered when a client has disconnected
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs> OnClientDisconnected;

        /// <summary>
        /// Construct a server, from a message server instance.
        /// </summary>
        public Server (Guid id, Protocol protocol, string name, IServer<Request,Response> innerServer)
        {
            Id = id;
            Protocol = protocol;
            Name = name;
            InnerServer = innerServer;

            // Tie events to underlying server
            InnerServer.OnStarted += (s, e) => EventHandlerExtensions.Invoke (OnStarted, this);
            InnerServer.OnStopped += (s, e) => EventHandlerExtensions.Invoke (OnStopped, this);
            InnerServer.OnClientRequestingConnection += (s, e) => EventHandlerExtensions.Invoke (OnClientRequestingConnection, s, e);
            InnerServer.OnClientConnected += (s, e) => EventHandlerExtensions.Invoke (OnClientConnected, s, new ClientConnectedEventArgs (e.Client));
            InnerServer.OnClientDisconnected += (s, e) => EventHandlerExtensions.Invoke (OnClientDisconnected, s, new ClientDisconnectedEventArgs (e.Client));

            // Add/remove clients from the scheduler
            InnerServer.OnClientConnected += (s, e) => Core.Instance.ClientConnected (e.Client);
            InnerServer.OnClientDisconnected += (s, e) => Core.Instance.ClientDisconnected (e.Client);
        }

        /// <summary>
        /// Start the server
        /// </summary>
        public void Start ()
        {
            InnerServer.Start ();
            ClearStats ();
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public void Stop ()
        {
            InnerServer.Stop ();
        }

        /// <summary>
        /// Update the server.
        /// </summary>
        public void Update ()
        {
            InnerServer.Update ();
        }

        /// <summary>
        /// The servers address.
        /// </summary>
        public string Address {
            get { return InnerServer.Address; }
        }

        /// <summary>
        /// Information about the server.
        /// </summary>
        public string Info {
            get { return InnerServer.Info; }
        }

        /// <summary>
        /// Returns true if the server is running
        /// </summary>
        public bool Running {
            get { return InnerServer.Running; }
        }

        /// <summary>
        /// Returns a list of clients the server knows about. Note that they might
        /// not be connected to the server.
        /// </summary>
        public IEnumerable<IClient> Clients {
            get { return InnerServer.Clients.Cast <IClient> (); }
        }

        /// <summary>
        /// Get the total number of bytes read from the network.
        /// </summary>
        public ulong BytesRead {
            get { return InnerServer.BytesRead; }
        }

        /// <summary>
        /// Get the total number of bytes written to the network.
        /// </summary>
        public ulong BytesWritten {
            get { return InnerServer.BytesWritten; }
        }

        /// <summary>
        /// Clear the server statistics.
        /// </summary>
        public void ClearStats ()
        {
            InnerServer.ClearStats ();
        }
    }
}
