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
            MainWindowPosition = new KRPC.Utils.Tuple<float,float,float,float> (0, 0, 0, 0);
            InfoWindowVisible = false;
            InfoWindowPosition = new KRPC.Utils.Tuple<float,float,float,float> (0, 0, 0, 0);
            AutoStartServers = false;
            AutoAcceptConnections = false;
            ConfirmRemoveClient = true;
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

            public IPAddress Address { get; set; }

            public ushort RPCPort { get; set; }

            public ushort StreamPort { get; set; }

            public Server ()
            {
                Id = Guid.NewGuid ();
                Name = "Default Server";
                Protocol = Protocol.ProtocolBuffersOverTCP;
                Address = IPAddress.Loopback;
                RPCPort = 50000;
                StreamPort = 50001;
            }

            /// <summary>
            /// Create a server instance from this configuration
            /// </summary>
            public KRPC.Server.Server Create ()
            {
                var serverAddress = Address;
                var serverProtocol = Protocol;
                var rpcTcpServer = new TCPServer (serverAddress, RPCPort);
                var streamTcpServer = new TCPServer (serverAddress, StreamPort);
                KRPC.Server.Message.RPCServer rpcServer;
                KRPC.Server.Message.StreamServer streamServer;
                if (serverProtocol == Protocol.ProtocolBuffersOverTCP) {
                    rpcServer = new KRPC.Server.ProtocolBuffers.RPCServer (rpcTcpServer);
                    streamServer = new KRPC.Server.ProtocolBuffers.StreamServer (streamTcpServer);
                } else {
                    rpcServer = new KRPC.Server.WebSockets.RPCServer (rpcTcpServer);
                    streamServer = new KRPC.Server.WebSockets.StreamServer (streamTcpServer);
                }
                return new KRPC.Server.Server (Id, serverProtocol, Name, rpcServer, streamServer);
            }
        }

        readonly List<Server> servers = new List<Server> ();

        public IList<Server> Servers {
            get { return servers; }
        }

        public Configuration.Server GetServer (Guid id)
        {
            foreach (var server in servers)
                if (server.Id == id)
                    return server;
            throw new KeyNotFoundException ();
        }

        [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        public void ReplaceServer (Configuration.Server newServer)
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

        public KRPC.Utils.Tuple<float,float,float,float> MainWindowPosition { get; set; }

        public bool InfoWindowVisible { get; set; }

        public KRPC.Utils.Tuple<float,float,float,float> InfoWindowPosition { get; set; }

        public bool AutoStartServers { get; set; }

        public bool AutoAcceptConnections { get; set; }

        public bool ConfirmRemoveClient { get; set; }

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
