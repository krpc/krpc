using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using KRPC.Server;
using KRPC.Server.TCP;
using KRPC.Service;
using KRPC.Utils;
using Logger = KRPC.Utils.Logger;

namespace KRPC
{
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
    sealed class ConfigurationFile : ConfigurationStorage
    {
        static ConfigurationFile instance;

        public static ConfigurationFile Instance {
            get {
                if (instance == null)
                    instance = new ConfigurationFile ("PluginData/settings.cfg", Configuration.Instance);
                return instance;
            }
        }

        public sealed class Server
        {
            [Persistent] string id = Guid.NewGuid ().ToString ();
            [Persistent] string name = "Default Server";
            [Persistent] string protocol = Protocol.ProtocolBuffersOverTCP.ToString ();
            [Persistent] string address = "127.0.0.1";
            [Persistent] ushort rpcPort = 50000;
            [Persistent] ushort streamPort = 50001;

            public void BeforeSave (Configuration.Server server)
            {
                id = server.Id.ToString ();
                name = server.Name;
                protocol = server.Protocol.ToString ();
                address = server.Address.ToString ();
                rpcPort = server.RPCPort;
                streamPort = server.StreamPort;
            }

            [SuppressMessage ("Gendarme.Rules.BadPractice", "DisableDebuggingCodeRule")]
            public void AfterLoad (Configuration.Server server)
            {
                server.Id = new Guid (id);
                server.Name = name;
                try {
                    server.Protocol = (Protocol)Enum.Parse (typeof(Protocol), protocol);
                } catch (ArgumentException) {
                    Console.WriteLine (
                        "[kRPC] Error parsing server protocol from configuration file. Got '" + protocol + "'. " +
                        "Defaulting to " + Protocol.ProtocolBuffersOverTCP);
                    server.Protocol = Protocol.ProtocolBuffersOverTCP;
                    protocol = server.Protocol.ToString ();
                }
                IPAddress ipAddress;
                bool validAddress = IPAddress.TryParse (address, out ipAddress);
                if (validAddress) {
                    server.Address = ipAddress;
                } else {
                    Console.WriteLine (
                        "[kRPC] Error parsing IP address from configuration file. Got '" + address + "'. " +
                        "Defaulting to loopback address " + IPAddress.Loopback);
                    server.Address = IPAddress.Loopback;
                    address = server.Address.ToString ();
                }
                server.RPCPort = rpcPort;
                server.StreamPort = streamPort;
            }
        }

        public Configuration Configuration { get; private set; }

        [Persistent] readonly List<Server> servers = new List<Server> ();
        [Persistent] bool mainWindowVisible;
        [Persistent] RectStorage mainWindowPosition = new RectStorage ();
        [Persistent] bool infoWindowVisible;
        [Persistent] RectStorage infoWindowPosition = new RectStorage ();
        [Persistent] bool autoStartServers;
        [Persistent] bool autoAcceptConnections;
        [Persistent] bool confirmRemoveClient;
        [Persistent] string logLevel = Logger.Severity.Info.ToString ();
        [Persistent] bool verboseErrors;
        [Persistent] bool checkDocumented;
        [Persistent] bool oneRPCPerUpdate;
        [Persistent] uint maxTimePerUpdate;
        [Persistent] bool adaptiveRateControl;
        [Persistent] bool blockingRecv;
        [Persistent] uint recvTimeout;

        public ConfigurationFile (string filePath, Configuration configuration) :
            base (filePath, "KRPCConfiguration")
        {
            Configuration = configuration;
            Load ();
        }

        protected override void BeforeSave ()
        {
            Logger.WriteLine ("Saving configuration", Logger.Severity.Debug);
            servers.Clear ();
            foreach (var server in Configuration.Servers) {
                var newServer = new Server ();
                newServer.BeforeSave (server);
                servers.Add (newServer);
            }
            mainWindowVisible = Configuration.MainWindowVisible;
            mainWindowPosition = RectStorage.FromRect (Configuration.MainWindowPosition.ToRect ());
            infoWindowVisible = Configuration.InfoWindowVisible;
            infoWindowPosition = RectStorage.FromRect (Configuration.InfoWindowPosition.ToRect ());
            autoStartServers = Configuration.AutoStartServers;
            autoAcceptConnections = Configuration.AutoAcceptConnections;
            confirmRemoveClient = Configuration.ConfirmRemoveClient;
            logLevel = Logger.Level.ToString ();
            verboseErrors = Configuration.VerboseErrors;
            checkDocumented = ServicesChecker.CheckDocumented;
            oneRPCPerUpdate = Configuration.OneRPCPerUpdate;
            maxTimePerUpdate = Configuration.MaxTimePerUpdate;
            adaptiveRateControl = Configuration.AdaptiveRateControl;
            blockingRecv = Configuration.BlockingRecv;
            recvTimeout = Configuration.RecvTimeout;

        }

        [SuppressMessage ("Gendarme.Rules.BadPractice", "DisableDebuggingCodeRule")]
        protected override void AfterLoad ()
        {
            Configuration.Servers.Clear ();
            foreach (var server in servers) {
                var newServer = new Configuration.Server ();
                server.AfterLoad (newServer);
                Configuration.Servers.Add (newServer);
            }
            Configuration.MainWindowVisible = mainWindowVisible;
            Configuration.MainWindowPosition = mainWindowPosition.AsRect ().ToTuple ();
            Configuration.InfoWindowVisible = infoWindowVisible;
            Configuration.InfoWindowPosition = infoWindowPosition.AsRect ().ToTuple ();
            Configuration.AutoStartServers = autoStartServers;
            Configuration.AutoAcceptConnections = autoAcceptConnections;
            Configuration.ConfirmRemoveClient = confirmRemoveClient;
            try {
                Logger.Level = (Logger.Severity)Enum.Parse (typeof(Logger.Severity), logLevel);
            } catch (ArgumentException) {
                Console.WriteLine (
                    "[kRPC] Error parsing log level from configuration file. Got '" + logLevel + "'. " +
                    "Defaulting to " + Logger.Severity.Info);
                Logger.Level = Logger.Severity.Info;
            }
            Configuration.VerboseErrors = verboseErrors;
            ServicesChecker.CheckDocumented = checkDocumented;
            Configuration.OneRPCPerUpdate = oneRPCPerUpdate;
            Configuration.MaxTimePerUpdate = maxTimePerUpdate;
            Configuration.AdaptiveRateControl = adaptiveRateControl;
            Configuration.BlockingRecv = blockingRecv;
            Configuration.RecvTimeout = recvTimeout;
            Logger.WriteLine ("Loaded configuration", Logger.Severity.Debug);
        }
    }
}
