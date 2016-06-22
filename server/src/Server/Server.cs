using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using KRPC.Server;
using KRPC.Server.ProtocolBuffers;
using KRPC.Server.TCP;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server
{
    /// <summary>
    /// A kRPC server.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Correctness", "DeclareEventsExplicitlyRule")]
    sealed class Server : IServer
    {
        readonly TCPServer rpcTcpServer;
        readonly TCPServer streamTcpServer;

        internal IServer<Request,Response> RPCServer { get; private set; }

        internal IServer<NoMessage,StreamMessage> StreamServer { get; private set; }

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
        /// Event triggered when a client performs some activity
        /// </summary>
        public event EventHandler<ClientActivityEventArgs> OnClientActivity;

        /// <summary>
        /// Event triggered when a client has disconnected
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs> OnClientDisconnected;

        internal Server (IPAddress address, ushort rpcPort, ushort streamPort)
        {
            Core.Instance.Add (this);

            rpcTcpServer = new TCPServer ("RPCServer", address, rpcPort);
            streamTcpServer = new TCPServer ("StreamServer", address, streamPort);
            RPCServer = new RPCServer (rpcTcpServer);
            StreamServer = new StreamServer (streamTcpServer);

            // Tie events to underlying server
            RPCServer.OnStarted += (s, e) => EventHandlerExtensions.Invoke (OnStarted, this);
            RPCServer.OnStopped += (s, e) => EventHandlerExtensions.Invoke (OnStopped, this);
            RPCServer.OnClientRequestingConnection += (s, e) => EventHandlerExtensions.Invoke (OnClientRequestingConnection, s, e);
            RPCServer.OnClientConnected += (s, e) => EventHandlerExtensions.Invoke (OnClientConnected, s, new ClientConnectedEventArgs (e.Client));
            RPCServer.OnClientDisconnected += (s, e) => EventHandlerExtensions.Invoke (OnClientDisconnected, s, new ClientDisconnectedEventArgs (e.Client));

            // Add/remove clients from the scheduler
            RPCServer.OnClientConnected += (s, e) => Core.Instance.RPCClientConnected (e.Client);
            RPCServer.OnClientDisconnected += (s, e) => Core.Instance.RPCClientDisconnected (e.Client);

            // Add/remove clients from the list of stream requests
            StreamServer.OnClientConnected += (s, e) => Core.Instance.StreamClientConnected (e.Client);
            StreamServer.OnClientDisconnected += (s, e) => Core.Instance.StreamClientDisconnected (e.Client);

            // Validate stream client identifiers
            StreamServer.OnClientRequestingConnection += (s, e) => {
                if (RPCServer.Clients.Any (c => c.Guid == e.Client.Guid))
                    e.Request.Allow ();
                else
                    e.Request.Deny ();
            };
        }

        /// <summary>
        /// Start the server
        /// </summary>
        public void Start ()
        {
            RPCServer.Start ();
            StreamServer.Start ();
            ClearStats ();
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public void Stop ()
        {
            RPCServer.Stop ();
            StreamServer.Stop ();
            ObjectStore.Clear ();
        }

        /// <summary>
        /// Update the server.
        /// </summary>
        public void Update ()
        {
            RPCServer.Update ();
            StreamServer.Update ();
        }

        /// <summary>
        /// Get/set the servers listen address
        /// </summary>
        public IPAddress Address {
            get { return rpcTcpServer.Address; }
            set {
                rpcTcpServer.Address = value;
                streamTcpServer.Address = value;
            }
        }

        /// <summary>
        /// Get/set the RPC port
        /// </summary>
        public ushort RPCPort {
            get { return rpcTcpServer.Port; }
            set { rpcTcpServer.Port = value; }
        }

        /// <summary>
        /// Get/set the Stream port
        /// </summary>
        public ushort StreamPort {
            get { return streamTcpServer.Port; }
            set { streamTcpServer.Port = value; }
        }

        /// <summary>
        /// Returns true if the server is running
        /// </summary>
        public bool Running {
            get { return RPCServer.Running && StreamServer.Running; }
        }

        /// <summary>
        /// Returns a list of clients the server knows about. Note that they might
        /// not be connected to the server.
        /// </summary>
        public IEnumerable<IClient> Clients {
            get { return RPCServer.Clients.Cast <IClient> (); }
        }

        /// <summary>
        /// Get the total number of bytes read from the network.
        /// </summary>
        public ulong BytesRead {
            get { return RPCServer.BytesRead + StreamServer.BytesRead; }
        }

        /// <summary>
        /// Get the total number of bytes written to the network.
        /// </summary>
        public ulong BytesWritten {
            get { return RPCServer.BytesWritten + StreamServer.BytesWritten; }
        }

        /// <summary>
        /// Clear the server statistics.
        /// </summary>
        public void ClearStats ()
        {
            RPCServer.ClearStats ();
            StreamServer.ClearStats ();
        }
    }
}
