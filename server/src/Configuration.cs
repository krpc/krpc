using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using KRPC.Server;
using KRPC.Server.TCP;
using KRPC.Service;
using KRPC.Utils;
using UnityEngine;

namespace KRPC
{
    sealed class Configuration : ConfigurationStorage
    {
        static Configuration instance;

        public static Configuration Instance {
            get {
                if (instance == null)
                    instance = new Configuration ("PluginData/settings.cfg");
                return instance;
            }
        }

        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
        public sealed class Server
        {
            [Persistent] string id = Guid.NewGuid ().ToString ();
            [Persistent] string name = "Default Server";
            [Persistent] string protocol = Protocol.ProtocolBuffersOverTCP.ToString ();
            [Persistent] string address = "127.0.0.1";
            [Persistent] ushort rpcPort = 50000;
            [Persistent] ushort streamPort = 50001;

            public Guid Id {
                get { return new Guid (id); }
                set { id = value.ToString (); }
            }

            public string Name {
                get { return name; }
                set { name = value; }
            }

            public Protocol Protocol {
                get { return (Protocol)Enum.Parse (typeof(Protocol), protocol); }
                set { protocol = value.ToString (); }
            }

            public IPAddress Address {
                get {
                    IPAddress result;
                    IPAddress.TryParse (address, out result);
                    return result;
                }
                set { address = value.ToString (); }
            }

            public ushort RPCPort {
                get { return rpcPort; }
                set { rpcPort = value; }
            }

            public ushort StreamPort {
                get { return streamPort; }
                set { streamPort = value; }
            }

            [SuppressMessage ("Gendarme.Rules.BadPractice", "DisableDebuggingCodeRule")]
            public void AfterLoad ()
            {
                try {
                    Enum.Parse (typeof(Protocol), protocol);
                } catch (ArgumentException) {
                    Console.WriteLine (
                        "[kRPC] Error parsing server protocol from configuration file. Got '" + protocol + "'. " +
                        "Defaulting to " + Protocol.ProtocolBuffersOverTCP);
                    protocol = Protocol.ProtocolBuffersOverTCP.ToString ();
                }
                IPAddress ipAddress;
                bool validAddress = IPAddress.TryParse (address, out ipAddress);
                if (!validAddress) {
                    Console.WriteLine (
                        "[kRPC] Error parsing IP address from configuration file. Got '" + address + "'. " +
                        "Defaulting to loopback address " + IPAddress.Loopback);
                    address = IPAddress.Loopback.ToString ();
                }
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

        [Persistent] readonly List<Server> servers = new List<Server> ();
        [Persistent] bool mainWindowVisible = true;
        [Persistent] RectStorage mainWindowPosition = new RectStorage ();
        [Persistent] bool infoWindowVisible;
        [Persistent] RectStorage infoWindowPosition = new RectStorage ();
        [Persistent] bool autoStartServers;
        [Persistent] bool autoAcceptConnections;
        [Persistent] string logLevel = Logger.Severity.Info.ToString ();
        [Persistent] bool verboseErrors;
        [Persistent] bool checkDocumented;
        [Persistent] bool oneRPCPerUpdate;
        [Persistent] uint maxTimePerUpdate = 5000;
        [Persistent] bool adaptiveRateControl = true;
        [Persistent] bool blockingRecv = true;
        [Persistent] uint recvTimeout = 1000;

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

        public bool MainWindowVisible {
            get { return mainWindowVisible; }
            set { mainWindowVisible = value; }
        }

        public Rect MainWindowPosition {
            get { return mainWindowPosition.AsRect (); }
            set { mainWindowPosition = RectStorage.FromRect (value); }
        }

        public bool InfoWindowVisible {
            get { return infoWindowVisible; }
            set { infoWindowVisible = value; }
        }

        public Rect InfoWindowPosition {
            get { return infoWindowPosition.AsRect (); }
            set { infoWindowPosition = RectStorage.FromRect (value); }
        }

        public bool AutoStartServers {
            get { return autoStartServers; }
            set { autoStartServers = value; }
        }

        public bool AutoAcceptConnections {
            get { return autoAcceptConnections; }
            set { autoAcceptConnections = value; }
        }

        public bool OneRPCPerUpdate {
            get { return oneRPCPerUpdate; }
            set { oneRPCPerUpdate = value; }
        }

        public uint MaxTimePerUpdate {
            get { return maxTimePerUpdate; }
            set { maxTimePerUpdate = value; }
        }

        public bool AdaptiveRateControl {
            get { return adaptiveRateControl; }
            set { adaptiveRateControl = value; }
        }

        public bool BlockingRecv {
            get { return blockingRecv; }
            set { blockingRecv = value; }
        }

        public uint RecvTimeout {
            get { return recvTimeout; }
            set { recvTimeout = value; }
        }

        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public bool DebugLogging {
            get { return Logger.Level == Logger.Severity.Debug; }
            set { Logger.Level = value ? Logger.Severity.Debug : Logger.Severity.Info; }
        }

        public Configuration (string filePath) :
            base (filePath, "KRPCConfiguration")
        {
            Load ();
        }

        protected override void BeforeSave ()
        {
            Logger.WriteLine ("Saving configuration", Logger.Severity.Debug);
            logLevel = Logger.Level.ToString ();
            verboseErrors = RPCException.VerboseErrors;
            checkDocumented = ServicesChecker.CheckDocumented;
        }

        [SuppressMessage ("Gendarme.Rules.BadPractice", "DisableDebuggingCodeRule")]
        protected override void AfterLoad ()
        {
            foreach (var server in servers)
                server.AfterLoad ();
            try {
                Logger.Level = (Logger.Severity)Enum.Parse (typeof(Logger.Severity), logLevel);
            } catch (ArgumentException) {
                Console.WriteLine (
                    "[kRPC] Error parsing log level from configuration file. Got '" + logLevel + "'. " +
                    "Defaulting to " + Logger.Severity.Info);
                Logger.Level = Logger.Severity.Info;
            }
            RPCException.VerboseErrors = verboseErrors;
            ServicesChecker.CheckDocumented = checkDocumented;
            Logger.WriteLine ("Loaded configuration", Logger.Severity.Debug);
        }
    }
}
