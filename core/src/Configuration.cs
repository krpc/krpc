using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using KRPC.Server;
using KRPC.Server.TCP;
using KRPC.Utils;
using Logger = KRPC.Utils.Logger;
#if NET
using SerialPorts = System.IO.Ports;
#else
using SerialPorts = KRPC.IO.Ports;
#endif

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
            HiddenServiceProblems = string.Empty;
            AutoStartServers = true;
            AutoAcceptConnections = true;
            ConfirmRemoveClient = true;
            PauseServerWithGame = false;
            VerboseErrors = true;
            MaxTimePerUpdate = 5000;
            AdaptiveRateControl = true;
            BlockingRecv = true;
            RecvTimeout = 1000;
            TickHoldTimeout = 1000000;
        }

        /// <summary>
        /// Per-server configuration options
        /// </summary>
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
            /// Construct default settings for a TCP/IP server
            /// </summary>
            public Server() : this (Protocol.ProtocolBuffersOverTCP)
            {
            }

            /// <summary>
            /// Construct default settings for a TCP/IP or WebSockets server, which listens on
            /// the loopback address and the protocol's default port
            /// </summary>
            public Server(Protocol protocol)
            {
                if (protocol != Protocol.ProtocolBuffersOverTCP && protocol != Protocol.ProtocolBuffersOverWebsockets)
                    throw new ArgumentException ("No default settings for " + protocol, nameof (protocol));
                var websockets = protocol == Protocol.ProtocolBuffersOverWebsockets;
                var prefix = websockets ? "ws_" : "tcpip_";
                Id = Guid.NewGuid();
                Name = websockets ? "WebSockets Server" : "TCP/IP Server";
                Protocol = protocol;
                Settings = new Dictionary<string, string>{
                    {prefix + "address", IPAddress.Loopback.ToString()},
                    {prefix + "port", DefaultPort (protocol).ToString (CultureInfo.InvariantCulture)}};
            }

            /// <summary>
            /// Create a server instance from this configuration
            /// </summary>
            public KRPC.Server.Server Create ()
            {
                KRPC.Server.Message.Server messageServer = null;

                var serverProtocol = Protocol;
                if (serverProtocol == Protocol.ProtocolBuffersOverTCP ||
                    serverProtocol == Protocol.ProtocolBuffersOverWebsockets) {
                    var prefix = serverProtocol == Protocol.ProtocolBuffersOverTCP ? "tcpip_" : "ws_";
                    var serverAddress = IPAddress.Loopback;
                    IPAddress.TryParse(Settings.GetValueOrDefault(prefix + "address", IPAddress.Loopback.ToString()), out serverAddress);
                    ushort port = 0;
                    ushort.TryParse(Settings.GetValueOrDefault(prefix + "port", DefaultPort (serverProtocol).ToString (CultureInfo.InvariantCulture)), out port);
                    var tcpServer = new TCPServer (serverAddress, port);
                    if (serverProtocol == Protocol.ProtocolBuffersOverTCP)
                        messageServer = new KRPC.Server.Delimited.Server (tcpServer);
                    else
                        messageServer = new KRPC.Server.WebSockets.Server (tcpServer);
                } else if (serverProtocol == Protocol.ProtocolBuffersOverLocalSocket) {
                    var socketServer = new KRPC.Server.LocalSocket.LocalSocketServer (
                        Settings.GetValueOrDefault ("localsocket_path", KRPC.Server.LocalSocket.LocalSocketServer.DefaultPath ()));
                    messageServer = new KRPC.Server.Delimited.Server (socketServer);
                } else {
                    uint baudRate = 0;
                    ushort dataBits = 0;
                    SerialPorts.Parity parity;
                    SerialPorts.StopBits stopBits;
                    uint.TryParse(Settings.GetValueOrDefault("serialio_baud_rate", "9600"), out baudRate);
                    ushort.TryParse(Settings.GetValueOrDefault("serialio_data_bits", "8"), out dataBits);
                    Enum.TryParse<SerialPorts.Parity>(Settings.GetValueOrDefault("serialio_parity", "None"), out parity);
                    Enum.TryParse<SerialPorts.StopBits>(Settings.GetValueOrDefault("serialio_stop_bits", "One"), out stopBits);
                    var serialServer = new KRPC.Server.SerialIO.ByteServer(
                        Settings.GetValueOrDefault("serialio_port", string.Empty),
                        baudRate, dataBits, parity, stopBits);
                    messageServer = new KRPC.Server.SerialIO.Server (serialServer);
                }
                return new KRPC.Server.Server (Id, serverProtocol, Name, messageServer);
            }
        }

        /// <summary>
        /// The port a server of the protocol listens on by default: 50000 for TCP/IP and
        /// 50001 for WebSockets. Zero for a protocol with no port.
        /// </summary>
        public static ushort DefaultPort (Protocol protocol)
        {
            switch (protocol) {
            case Protocol.ProtocolBuffersOverTCP:
                return 50000;
            case Protocol.ProtocolBuffersOverWebsockets:
                return 50001;
            default:
                return 0;
            }
        }

        /// <summary>
        /// The servers of a new configuration: a TCP/IP server and a WebSockets server, each
        /// on the loopback address and its protocol's default port
        /// </summary>
        public static IList<Server> DefaultServers ()
        {
            return new List<Server> {
                new Server (Protocol.ProtocolBuffersOverTCP),
                new Server (Protocol.ProtocolBuffersOverWebsockets)
            };
        }

        private readonly List<Server> servers = new List<Server> ();

        /// <summary>
        /// Configuration for all of the servers
        /// </summary>
        public IList<Server> Servers {
            get { return servers; }
        }

        /// <summary>
        /// Default settings for a new server of the protocol, named for the protocol and
        /// numbered when a server already has that name
        /// </summary>
        public Server NewServer (Protocol protocol)
        {
            var server = new Server (protocol);
            var name = server.Name;
            for (var i = 2; servers.Exists (x => x.Name == server.Name); i++)
                server.Name = name + " " + i.ToString (CultureInfo.InvariantCulture);
            return server;
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
        /// The digest of the service problems the player chose not to be shown again, or an
        /// empty string
        /// </summary>
        public string HiddenServiceProblems { get; set; }

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
        /// How long a client may hold the game on one physics tick before the server takes
        /// the tick back
        /// </summary>
        public uint TickHoldTimeout { get; set; }

        /// <summary>
        /// Whether debug logging is enable
        /// </summary>
        public bool DebugLogging {
            get { return Logger.Level == Logger.Severity.Debug; }
            set { Logger.Level = value ? Logger.Severity.Debug : Logger.Severity.Info; }
        }
    }
}
