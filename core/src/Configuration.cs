using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using KRPC.Server;
using KRPC.Server.TCP;
using KRPC.Utils;
using Logger = KRPC.Utils.Logger;

namespace KRPC
{
    /// <summary>
    /// kRPC configuration options
    /// </summary>
    public sealed class Configuration
    {
        static Configuration instance;

        /// <summary>
        /// Returns the static configuration instance
        /// </summary>
        public static Configuration Instance {
            get {
                if (instance == null)
                    instance = new Configuration ();
                return instance;
            }
        }

        internal Configuration ()
        {
            MainWindowVisible = true;
            MainWindowPosition = new Tuple<float,float,float,float> (0, 0, 0, 0);
            InfoWindowVisible = false;
            InfoWindowPosition = new Tuple<float,float,float,float> (0, 0, 0, 0);
            AutoStartServers = false;
            AutoAcceptConnections = false;
            ConfirmRemoveClient = true;
            PauseServerWithGame = false;
            VerboseErrors = true;
            OneRPCPerUpdate = false;
            MaxTimePerUpdate = 5000;
            AdaptiveRateControl = true;
            BlockingRecv = true;
            RecvTimeout = 1000;
        }

        /// <summary>
        /// Per-server configuration options
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
        public sealed class Server
        {
            /// <summary>
            /// Identifier of the server
            /// </summary>
            public Guid Id { get; set; }

            /// <summary>
            /// Name of the server
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Protocol the server uses
            /// </summary>
            public Protocol Protocol { get; set; }

            /// <summary>
            /// Additional configration settings, specific to the protocol.
            /// </summary>
            public IDictionary<string, string> Settings { get; set; }

            /// <summary>
            /// Construct default settings for a server
            /// </summary>
            public Server()
            {
                Id = Guid.NewGuid();
                Name = "Default Server";
                Protocol = Protocol.ProtocolBuffersOverTCP;
                Settings = new Dictionary<string, string>{
                    {"address", IPAddress.Loopback.ToString()},
                    {"rpc_port", "50000"},
                    {"stream_port", "50001"}};
            }

            /// <summary>
            /// Create a server instance from this configuration
            /// </summary>
            public KRPC.Server.Server Create ()
            {
                KRPC.Server.Message.RPCServer rpcServer = null;
                KRPC.Server.Message.StreamServer streamServer = null;

                var serverProtocol = Protocol;
                if (serverProtocol == Protocol.ProtocolBuffersOverTCP ||
                    serverProtocol == Protocol.ProtocolBuffersOverWebsockets) {
                    var serverAddress = IPAddress.Loopback;
                    IPAddress.TryParse(Settings.GetValueOrDefault("address", IPAddress.Loopback.ToString()), out serverAddress);
                    ushort rpcPort = 0;
                    ushort streamPort = 0;
                    ushort.TryParse(Settings.GetValueOrDefault("rpc_port", "0"), out rpcPort);
                    ushort.TryParse(Settings.GetValueOrDefault("stream_port", "0"), out streamPort);
                    var rpcTcpServer = new TCPServer (serverAddress, rpcPort);
                    var streamTcpServer = new TCPServer (serverAddress, streamPort);
                    if (serverProtocol == Protocol.ProtocolBuffersOverTCP) {
                        rpcServer = new KRPC.Server.ProtocolBuffers.RPCServer (rpcTcpServer);
                        streamServer = new KRPC.Server.ProtocolBuffers.StreamServer (streamTcpServer);
                    } else {
                        rpcServer = new KRPC.Server.WebSockets.RPCServer (rpcTcpServer);
                        streamServer = new KRPC.Server.WebSockets.StreamServer (streamTcpServer);
                    }
                } else {
                    uint baudRate = 0;
                    ushort dataBits = 0;
                    KRPC.IO.Ports.Parity parity;
                    KRPC.IO.Ports.StopBits stopBits;
                    uint.TryParse(Settings.GetValueOrDefault("baud_rate", "9600"), out baudRate);
                    ushort.TryParse(Settings.GetValueOrDefault("data_bits", "8"), out dataBits);
                    Enum.TryParse<KRPC.IO.Ports.Parity>(Settings.GetValueOrDefault("parity", "None"), out parity);
                    Enum.TryParse<KRPC.IO.Ports.StopBits>(Settings.GetValueOrDefault("stop_bits", "One"), out stopBits);
                    var serialServer = new KRPC.Server.SerialIO.ByteServer(
                        Settings.GetValueOrDefault("port", string.Empty),
                        baudRate, dataBits, parity, stopBits);
                    rpcServer = new KRPC.Server.SerialIO.RPCServer(serialServer);
                    streamServer = new KRPC.Server.SerialIO.StreamServer();
                }
                return new KRPC.Server.Server (Id, serverProtocol, Name, rpcServer, streamServer);
            }
        }

        private readonly List<Server> servers = new List<Server> ();

        /// <summary>
        /// Configuration for all of the servers
        /// </summary>
        public IList<Server> Servers {
            get { return servers; }
        }

        /// <summary>
        /// Get the server configuration with the given identifier
        /// </summary>
        public Server GetServer (Guid id)
        {
            foreach (var server in servers)
                if (server.Id == id)
                    return server;
            throw new KeyNotFoundException ();
        }

        /// <summary>
        /// Replace the server configuration with the given identifier
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        public void ReplaceServer (Server newServer)
        {
            for (var i = 0; i < servers.Count; i++) {
                if (servers [i].Id == newServer.Id) {
                    servers [i] = newServer;
                    return;
                }
            }
            throw new KeyNotFoundException ();
        }

        /// <summary>
        /// Remove the server configuration with the given identifier
        /// </summary>
        public void RemoveServer (Guid id)
        {
            for (var i = 0; i < servers.Count; i++) {
                if (servers [i].Id == id) {
                    servers.RemoveAt (i);
                    return;
                }
            }
            throw new KeyNotFoundException ();
        }

        /// <summary>
        /// Whether the main server window should be shown
        /// </summary>
        public bool MainWindowVisible { get; set; }

        /// <summary>
        /// Screen position of the main server window
        /// </summary>
        public Tuple<float,float,float,float> MainWindowPosition { get; set; }

        /// <summary>
        /// Whether the info window should be shown
        /// </summary>
        public bool InfoWindowVisible { get; set; }

        /// <summary>
        /// Screen position of the info window
        /// </summary>
        public Tuple<float,float,float,float> InfoWindowPosition { get; set; }

        /// <summary>
        /// Whether servers should be started when the game loads
        /// </summary>
        public bool AutoStartServers { get; set; }

        /// <summary>
        /// Whether new client connections should be allowed without user confirmation
        /// </summary>
        public bool AutoAcceptConnections { get; set; }

        /// <summary>
        /// Whether to confirm with the user before removing client connections
        /// </summary>
        public bool ConfirmRemoveClient { get; set; }

        /// <summary>
        /// Whether the server update loop should be paused along with the game
        /// </summary>
        public bool PauseServerWithGame { get; set; }

        /// <summary>
        /// Whether to write verbose error messages to the log
        /// </summary>
        public bool VerboseErrors { get; set; }

        /// <summary>
        /// Whether to execute a single RPCs per frame, or multiple up to the time limit
        /// </summary>
        public bool OneRPCPerUpdate { get; set; }

        /// <summary>
        /// Maximum amount of time to spend executing RPCs per frame
        /// </summary>
        public uint MaxTimePerUpdate { get; set; }

        /// <summary>
        /// Whether adaptive rate control is enabled
        /// </summary>
        public bool AdaptiveRateControl { get; set; }

        /// <summary>
        /// Whether blocking receives are enabled
        /// </summary>
        public bool BlockingRecv { get; set; }

        /// <summary>
        /// Timeout when waiting for data from a client
        /// </summary>
        public uint RecvTimeout { get; set; }

        /// <summary>
        /// Whether debug logging is enable
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public bool DebugLogging {
            get { return Logger.Level == Logger.Severity.Debug; }
            set { Logger.Level = value ? Logger.Severity.Debug : Logger.Severity.Info; }
        }
    }
}
