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
            ExpandedServers = new HashSet<Guid> ();
            HiddenServiceProblems = string.Empty;
            AutoStartServers = true;
            AutoAcceptConnections = true;
            ConfirmRemoveClient = true;
            PauseServerWithGame = false;
            VerboseErrors = true;
            AdaptiveRateControl = true;
            TargetFrameRate = 60;
            MaxTimePerFrame = 10000;
            MaxCallWait = 5000;
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
            /// The serial port settings a server takes when its settings leave them out.
            /// </summary>
            public static readonly IReadOnlyDictionary<string, string> SerialDefaults =
                new System.Collections.ObjectModel.ReadOnlyDictionary<string, string> (
                    new Dictionary<string, string> {
                        { "serialio_baud_rate", "9600" },
                        { "serialio_data_bits", "8" },
                        { "serialio_parity", "None" },
                        { "serialio_stop_bits", "One" },
                        { "serialio_flow_control", "None" }
                    });

            string SerialSetting (string key)
            {
                return Settings.GetValueOrDefault (key, SerialDefaults [key]);
            }

            /// <summary>
            /// Construct default settings for a TCP/IP server
            /// </summary>
            public Server() : this (Protocol.ProtocolBuffersOverTCP)
            {
            }

            /// <summary>
            /// Construct default settings for a TCP/IP, WebSockets or HTTP server, which listens
            /// on the loopback address and the protocol's default port
            /// </summary>
            public Server(Protocol protocol)
            {
                var prefix = SettingsPrefix (protocol);
                if (prefix == null)
                    throw new ArgumentException ("No default settings for " + protocol, nameof (protocol));
                Id = Guid.NewGuid();
                Name = protocol == Protocol.ProtocolBuffersOverWebsockets ? "WebSockets Server" :
                    protocol == Protocol.JSONOverHTTP ? "HTTP Server" : "TCP/IP Server";
                Protocol = protocol;
                Settings = new Dictionary<string, string>{
                    {prefix + "address", IPAddress.Loopback.ToString()},
                    {prefix + "port", DefaultPort (protocol).ToString (CultureInfo.InvariantCulture)}};
            }

            /// <summary>
            /// The origins a server of the protocol allows when its settings leave them out:
            /// none for HTTP, which allows the pages it serves itself.
            /// </summary>
            public static string DefaultAllowedOrigins (Protocol protocol)
            {
                return string.Empty;
            }

            /// <summary>
            /// The prefix of the address and port settings of a protocol that listens on a
            /// TCP/IP port, or null for any other protocol.
            /// </summary>
            public static string SettingsPrefix (Protocol protocol)
            {
                switch (protocol) {
                case Protocol.ProtocolBuffersOverTCP:
                    return "tcpip_";
                case Protocol.ProtocolBuffersOverWebsockets:
                    return "ws_";
                case Protocol.JSONOverHTTP:
                    return "http_";
                default:
                    return null;
                }
            }

            /// <summary>
            /// The origins whose web pages may connect to the server. An invalid setting allows
            /// none, as the server then fails safe.
            /// </summary>
            KRPC.Server.AllowedOrigins AllowedOrigins ()
            {
                var setting = Settings.GetValueOrDefault (
                    SettingsPrefix (Protocol) + "allowed_origins", DefaultAllowedOrigins (Protocol));
                try {
                    return new KRPC.Server.AllowedOrigins (setting);
                } catch (ArgumentException e) {
                    Logger.WriteLine ("Server '" + Name + "' has invalid allowed origins, allowing none: " + e.Message, Logger.Severity.Warning);
                    return new KRPC.Server.AllowedOrigins (null);
                }
            }

            /// <summary>
            /// Create a server instance from this configuration
            /// </summary>
            public KRPC.Server.Server Create ()
            {
                IMessageServer messageServer = null;

                var serverProtocol = Protocol;
                var prefix = SettingsPrefix (serverProtocol);
                if (prefix != null) {
                    var serverAddress = IPAddress.Loopback;
                    IPAddress.TryParse(Settings.GetValueOrDefault(prefix + "address", IPAddress.Loopback.ToString()), out serverAddress);
                    ushort port = 0;
                    ushort.TryParse(Settings.GetValueOrDefault(prefix + "port", DefaultPort (serverProtocol).ToString (CultureInfo.InvariantCulture)), out port);
                    var tcpServer = new TCPServer (serverAddress, port);
                    if (serverProtocol == Protocol.ProtocolBuffersOverTCP)
                        messageServer = new KRPC.Server.Delimited.Server (tcpServer);
                    else if (serverProtocol == Protocol.ProtocolBuffersOverWebsockets)
                        messageServer = new KRPC.Server.WebSockets.Server (tcpServer);
                    else
                        messageServer = new KRPC.Server.HTTP.Server (tcpServer, AllowedOrigins ());
                } else if (serverProtocol == Protocol.ProtocolBuffersOverLocalSocket) {
                    var socketServer = new KRPC.Server.LocalSocket.LocalSocketServer (
                        Settings.GetValueOrDefault ("localsocket_path", KRPC.Server.LocalSocket.LocalSocketServer.DefaultPath ()));
                    messageServer = new KRPC.Server.Delimited.Server (socketServer);
                } else {
                    uint baudRate = 0;
                    ushort dataBits = 0;
                    SerialPorts.Parity parity;
                    SerialPorts.StopBits stopBits;
                    SerialPorts.Handshake flowControl;
                    uint.TryParse (SerialSetting ("serialio_baud_rate"), out baudRate);
                    ushort.TryParse (SerialSetting ("serialio_data_bits"), out dataBits);
                    Enum.TryParse<SerialPorts.Parity> (SerialSetting ("serialio_parity"), out parity);
                    Enum.TryParse<SerialPorts.StopBits> (SerialSetting ("serialio_stop_bits"), out stopBits);
                    var flowControlName = SerialSetting ("serialio_flow_control");
                    if (flowControlName == "RequestToSend")
                        flowControl = SerialPorts.Handshake.RequestToSend;
                    else {
                        flowControl = SerialPorts.Handshake.None;
                        if (flowControlName != "None")
                            Logger.WriteLine ("Unsupported serialio_flow_control '" + flowControlName +
                                              "', using None", Logger.Severity.Warning);
                    }
                    var serialServer = new KRPC.Server.SerialIO.ByteServer(
                        Settings.GetValueOrDefault("serialio_port", string.Empty),
                        baudRate, dataBits, parity, stopBits, flowControl);
                    messageServer = new KRPC.Server.SerialIO.Server (serialServer);
                }
                return new KRPC.Server.Server (Id, serverProtocol, Name, messageServer);
            }
        }

        /// <summary>
        /// The port a server of the protocol listens on by default: 50000 for TCP/IP,
        /// 50001 for WebSockets and 50003 for HTTP. Zero for a protocol with no port.
        /// </summary>
        public static ushort DefaultPort (Protocol protocol)
        {
            switch (protocol) {
            case Protocol.ProtocolBuffersOverTCP:
                return 50000;
            case Protocol.ProtocolBuffersOverWebsockets:
                return 50001;
            case Protocol.JSONOverHTTP:
                return 50003;
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
        /// Default settings for a new server of the protocol, named "Server" and numbered
        /// when a server already has that name
        /// </summary>
        public Server NewServer (Protocol protocol)
        {
            var server = new Server (protocol) { Name = "Server" };
            for (var i = 2; servers.Exists (x => x.Name == server.Name); i++)
                server.Name = "Server " + i.ToString (CultureInfo.InvariantCulture);
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
                    ExpandedServers.Remove (id);
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
        /// The identifiers of the servers expanded in the main server window
        /// </summary>
        public ISet<Guid> ExpandedServers { get; private set; }

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
        /// Whether the server adapts its time per frame to hold the frame rate at
        /// <see cref="TargetFrameRate"/>
        /// </summary>
        public bool AdaptiveRateControl { get; set; }

        /// <summary>
        /// The lowest frame rate, in frames per second, the server's work pushes the game to,
        /// with adaptive rate control. From <see cref="TargetFrameRateMin"/> to
        /// <see cref="TargetFrameRateMax"/>
        /// </summary>
        public uint TargetFrameRate { get; set; }

        /// <summary>
        /// The lowest <see cref="TargetFrameRate"/>
        /// </summary>
        public const uint TargetFrameRateMin = 1;

        /// <summary>
        /// The highest <see cref="TargetFrameRate"/>
        /// </summary>
        public const uint TargetFrameRateMax = 1000;

        /// <summary>
        /// A preset of the settings that trade calls a second against the frame rate:
        /// adaptive rate control, the target frame rate, the max time per frame and the max
        /// call wait
        /// </summary>
        public sealed class Preset
        {
            internal Preset (string name, bool adaptiveRateControl, uint targetFrameRate,
                             uint maxTimePerFrame, uint maxCallWait)
            {
                Name = name;
                AdaptiveRateControl = adaptiveRateControl;
                TargetFrameRate = targetFrameRate;
                MaxTimePerFrame = maxTimePerFrame;
                MaxCallWait = maxCallWait;
            }

            /// <summary>
            /// The name of the preset
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Whether adaptive rate control is on
            /// </summary>
            public bool AdaptiveRateControl { get; private set; }

            /// <summary>
            /// The target frame rate, in frames per second, which applies only with adaptive
            /// rate control
            /// </summary>
            public uint TargetFrameRate { get; private set; }

            /// <summary>
            /// The max time per frame, in microseconds
            /// </summary>
            public uint MaxTimePerFrame { get; private set; }

            /// <summary>
            /// The max call wait, in microseconds
            /// </summary>
            public uint MaxCallWait { get; private set; }

            /// <summary>
            /// Set the configuration's settings to the preset's. Without adaptive rate
            /// control, the target frame rate is left as it is
            /// </summary>
            public void Apply (Configuration config)
            {
                config.AdaptiveRateControl = AdaptiveRateControl;
                if (AdaptiveRateControl)
                    config.TargetFrameRate = TargetFrameRate;
                config.MaxTimePerFrame = MaxTimePerFrame;
                config.MaxCallWait = MaxCallWait;
            }

            /// <summary>
            /// Whether the configuration's settings are the preset's
            /// </summary>
            public bool Matches (Configuration config)
            {
                return config.AdaptiveRateControl == AdaptiveRateControl &&
                    (!AdaptiveRateControl || config.TargetFrameRate == TargetFrameRate) &&
                    config.MaxTimePerFrame == MaxTimePerFrame &&
                    config.MaxCallWait == MaxCallWait;
            }
        }

        /// <summary>
        /// The presets, from the one that favors the frame rate to the one that favors calls
        /// </summary>
        public static readonly IList<Preset> Presets = new System.Collections.ObjectModel.ReadOnlyCollection<Preset> (
            new [] {
                new Preset ("Game first", false, 60, 1000, 1000),
                new Preset ("Balanced", true, 60, 10000, 5000),
                new Preset ("Calls first", false, 60, 20000, 20000)
            });

        /// <summary>
        /// The preset the settings match, or null if they match none
        /// </summary>
        public Preset CurrentPreset {
            get {
                foreach (var preset in Presets)
                    if (preset.Matches (this))
                        return preset;
                return null;
            }
        }

        /// <summary>
        /// The most time, in microseconds, the server spends per frame. The budget without
        /// adaptive rate control, and its ceiling with it. From <see cref="MaxTimePerFrameMin"/>
        /// to <see cref="MaxTimePerFrameMax"/>
        /// </summary>
        public uint MaxTimePerFrame { get; set; }

        /// <summary>
        /// The smallest <see cref="MaxTimePerFrame"/>, 100 microseconds
        /// </summary>
        public const uint MaxTimePerFrameMin = 100;

        /// <summary>
        /// The largest <see cref="MaxTimePerFrame"/>, one second
        /// </summary>
        public const uint MaxTimePerFrameMax = 1000000;

        /// <summary>
        /// The most frame time, in microseconds, the server spends waiting to gain a client's
        /// call this tick, and the longest it waits for one. Zero never waits
        /// </summary>
        public uint MaxCallWait { get; set; }

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
