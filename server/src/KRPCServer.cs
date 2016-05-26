using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using KRPC.Server;
using KRPC.Server.ProtocolBuffers;
using KRPC.Server.TCP;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC
{
    /// <summary>
    /// A kRPC server.
    /// </summary>
    public class KRPCServer : IServer
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
        public event EventHandler<ClientRequestingConnectionArgs> OnClientRequestingConnection;

        /// <summary>
        /// Event triggered when a client has connected
        /// </summary>
        public event EventHandler<ClientConnectedArgs> OnClientConnected;

        /// <summary>
        /// Event triggered when a client performs some activity
        /// </summary>
        public event EventHandler<ClientActivityArgs> OnClientActivity;

        /// <summary>
        /// Event triggered when a client has disconnected
        /// </summary>
        public event EventHandler<ClientDisconnectedArgs> OnClientDisconnected;

        internal KRPCServer (IPAddress address, ushort rpcPort, ushort streamPort)
        {
            KRPCCore.Instance.AddServer (this);

            rpcTcpServer = new TCPServer ("RPCServer", address, rpcPort);
            streamTcpServer = new TCPServer ("StreamServer", address, streamPort);
            RPCServer = new RPCServer (rpcTcpServer);
            StreamServer = new StreamServer (streamTcpServer);

            // Tie events to underlying server
            RPCServer.OnStarted += (s, e) => {
                if (OnStarted != null)
                    OnStarted (this, EventArgs.Empty);
            };
            RPCServer.OnStopped += (s, e) => {
                if (OnStopped != null)
                    OnStopped (this, EventArgs.Empty);
            };
            RPCServer.OnClientRequestingConnection += (s, e) => {
                if (OnClientRequestingConnection != null)
                    OnClientRequestingConnection (s, e);
            };
            RPCServer.OnClientConnected += (s, e) => {
                if (OnClientConnected != null)
                    OnClientConnected (s, e);
            };
            RPCServer.OnClientDisconnected += (s, e) => {
                if (OnClientDisconnected != null)
                    OnClientDisconnected (s, e);
            };

            // Add/remove clients from the scheduler
            RPCServer.OnClientConnected += (s, e) => KRPCCore.Instance.RPCClientConnected (e.Client);
            RPCServer.OnClientDisconnected += (s, e) => KRPCCore.Instance.RPCClientDisconnected (e.Client);

            // Add/remove clients from the list of stream requests
            StreamServer.OnClientConnected += (s, e) => KRPCCore.Instance.StreamClientConnected (e.Client);
            StreamServer.OnClientDisconnected += (s, e) => KRPCCore.Instance.StreamClientDisconnected (e.Client);

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

        ExponentialMovingAverage bytesReadRate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage bytesWrittenRate = new ExponentialMovingAverage (0.25);

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
        /// Get the total number of bytes read from the network.
        /// </summary>
        public float BytesReadRate {
            get { return bytesReadRate.Value; }
            set { bytesReadRate.Update (value); }
        }

        /// <summary>
        /// Get the total number of bytes written to the network.
        /// </summary>
        public float BytesWrittenRate {
            get { return bytesWrittenRate.Value; }
            set { bytesWrittenRate.Update (value); }
        }

        ExponentialMovingAverage rpcRate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage timePerRPCUpdate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage pollTimePerRPCUpdate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage execTimePerRPCUpdate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage streamRPCRate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage timePerStreamUpdate = new ExponentialMovingAverage (0.25);

        /// <summary>
        /// Total number of RPCs executed.
        /// </summary>
        public ulong RPCsExecuted { get; private set; }

        /// <summary>
        /// Number of RPCs processed per second.
        /// </summary>
        public float RPCRate {
            get { return rpcRate.Value; }
            set { rpcRate.Update (value); }
        }

        /// <summary>
        /// Time taken by the update loop per update, in seconds.
        /// </summary>
        public float TimePerRPCUpdate {
            get { return timePerRPCUpdate.Value; }
            set { timePerRPCUpdate.Update (value); }
        }

        /// <summary>
        /// Time taken polling for new RPCs per update, in seconds.
        /// </summary>
        public float PollTimePerRPCUpdate {
            get { return pollTimePerRPCUpdate.Value; }
            set { pollTimePerRPCUpdate.Update (value); }
        }

        /// <summary>
        /// Time taken polling executing RPCs per update, in seconds.
        /// </summary>
        public float ExecTimePerRPCUpdate {
            get { return execTimePerRPCUpdate.Value; }
            set { execTimePerRPCUpdate.Update (value); }
        }

        /// <summary>
        /// Number of currently active streaming RPCs.
        /// </summary>
        public uint StreamRPCs { get; private set; }

        /// <summary>
        /// Total number of streaming RPCs executed.
        /// </summary>
        public ulong StreamRPCsExecuted { get; private set; }

        /// <summary>
        /// Number of streaming RPCs processed per second.
        /// </summary>
        public float StreamRPCRate {
            get { return streamRPCRate.Value; }
            set { streamRPCRate.Update (value); }
        }

        /// <summary>
        /// Time taken by the stream update loop, in seconds.
        /// </summary>
        public float TimePerStreamUpdate {
            get { return timePerStreamUpdate.Value; }
            set { timePerStreamUpdate.Update (value); }
        }

        /// <summary>
        /// Clear the server statistics.
        /// </summary>
        public void ClearStats ()
        {
            RPCServer.ClearStats ();
            StreamServer.ClearStats ();
            RPCsExecuted = 0;
            RPCRate = 0;
            TimePerRPCUpdate = 0;
            ExecTimePerRPCUpdate = 0;
            PollTimePerRPCUpdate = 0;
            StreamRPCs = 0;
            StreamRPCsExecuted = 0;
            TimePerStreamUpdate = 0;
        }
    }
}
