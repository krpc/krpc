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
    sealed class Configuration
    {
        static Configuration instance;

        public static Configuration Instance {
            get {
                if (instance == null)
                    instance = new Configuration ();
                return instance;
            }
        }

        public Configuration ()
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

        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
        public sealed class Server
        {
            public Guid Id { get; set; }

            public string Name { get; set; }

            public Protocol Protocol { get; set; }

            public IDictionary<string, string> Settings { get; set; }

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

        readonly List<Server> servers = new List<Server> ();

        public IList<Server> Servers {
            get { return servers; }
        }

        public Server GetServer (Guid id)
        {
            foreach (var server in servers)
                if (server.Id == id)
                    return server;
            throw new KeyNotFoundException ();
        }

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

        public bool MainWindowVisible { get; set; }

        public Tuple<float,float,float,float> MainWindowPosition { get; set; }

        public bool InfoWindowVisible { get; set; }

        public Tuple<float,float,float,float> InfoWindowPosition { get; set; }

        public bool AutoStartServers { get; set; }

        public bool AutoAcceptConnections { get; set; }

        public bool ConfirmRemoveClient { get; set; }

        public bool PauseServerWithGame { get; set; }

        public bool VerboseErrors { get; set; }

        public bool OneRPCPerUpdate { get; set; }

        public uint MaxTimePerUpdate { get; set; }

        public bool AdaptiveRateControl { get; set; }

        public bool BlockingRecv { get; set; }

        public uint RecvTimeout { get; set; }

        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public bool DebugLogging {
            get { return Logger.Level == Logger.Severity.Debug; }
            set { Logger.Level = value ? Logger.Severity.Debug : Logger.Severity.Info; }
        }
    }
}
