using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Server;
using KRPC.Utils;
using Logger = KRPC.Utils.Logger;

namespace KRPC
{
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

        public sealed class KeyValuePair
        {
            [Persistent] public string key = string.Empty;
            [Persistent] public string value = string.Empty;
        }

        public sealed class Server
        {
            [Persistent] string id;
            [Persistent] string name;
            [Persistent] string protocol;
            [Persistent] List<KeyValuePair> settings;

            // A field missing from the file keeps the default TCP/IP server's value. The
            // settings start empty, as loading an empty settings node keeps the list as it is.
            // A missing setting takes its default when the server is created.
            public Server ()
            {
                BeforeSave (new Configuration.Server ());
                settings = new List<KeyValuePair> ();
            }

            public void BeforeSave (Configuration.Server server)
            {
                id = server.Id.ToString ();
                name = server.Name;
                protocol = server.Protocol.ToString ();
                settings = server.Settings.Select(x => new KeyValuePair { key = x.Key, value = x.Value}).ToList();
            }

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
                server.Settings = settings.ToDictionary(x => x.key, x => x.value);
            }
        }

        public Configuration Configuration { get; private set; }

        [Persistent] readonly List<Server> servers = new List<Server> ();
        [Persistent] bool mainWindowVisible;
        [Persistent] RectStorage mainWindowPosition = new RectStorage ();
        [Persistent] bool infoWindowVisible;
        [Persistent] RectStorage infoWindowPosition = new RectStorage ();
        [Persistent] string expandedServers = string.Empty;
        [Persistent] string hiddenServiceProblems = string.Empty;
        [Persistent] bool autoStartServers;
        [Persistent] bool autoAcceptConnections;
        [Persistent] bool confirmRemoveClient;
        [Persistent] bool pauseServerWithGame;
        [Persistent] string logLevel = Logger.Severity.Info.ToString ();
        [Persistent] bool verboseErrors;
        [Persistent] bool adaptiveRateControl;
        [Persistent] uint targetFrameRate;
        [Persistent] uint maxTimePerFrame;
        [Persistent] uint maxCallWait;
        [Persistent] uint tickHoldTimeout;

        public ConfigurationFile (string filePath, Configuration configuration) : base (filePath, "KRPCConfiguration")
        {
            Configuration = configuration;
            // Start each setting at its default, so a setting missing from the file, or with a
            // value that does not parse, keeps it
            BeforeSave ();
            // Without settings, as in a new install, start with the default servers
            if (!Load ()) {
                foreach (var server in Configuration.DefaultServers ())
                    Configuration.Servers.Add (server);
            }
            Save ();
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
            expandedServers = string.Join (",", Configuration.ExpandedServers.Select (x => x.ToString ()).ToArray ());
            hiddenServiceProblems = Configuration.HiddenServiceProblems;
            autoStartServers = Configuration.AutoStartServers;
            autoAcceptConnections = Configuration.AutoAcceptConnections;
            confirmRemoveClient = Configuration.ConfirmRemoveClient;
            pauseServerWithGame = Configuration.PauseServerWithGame;
            logLevel = Logger.Level.ToString ();
            verboseErrors = Configuration.VerboseErrors;
            adaptiveRateControl = Configuration.AdaptiveRateControl;
            targetFrameRate = Configuration.TargetFrameRate;
            maxTimePerFrame = Configuration.MaxTimePerFrame;
            maxCallWait = Configuration.MaxCallWait;
            tickHoldTimeout = Configuration.TickHoldTimeout;
        }

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
            Configuration.ExpandedServers.Clear ();
            // Ignore an identifier that does not parse, or names no server
            foreach (var id in (expandedServers ?? string.Empty).Split (',')) {
                Guid guid;
                if (Guid.TryParse (id.Trim (), out guid) && Configuration.Servers.Any (x => x.Id == guid))
                    Configuration.ExpandedServers.Add (guid);
            }
            Configuration.HiddenServiceProblems = hiddenServiceProblems ?? string.Empty;
            Configuration.AutoStartServers = autoStartServers;
            Configuration.AutoAcceptConnections = autoAcceptConnections;
            Configuration.ConfirmRemoveClient = confirmRemoveClient;
            Configuration.PauseServerWithGame = pauseServerWithGame;
            try {
                Logger.Level = (Logger.Severity)Enum.Parse (typeof(Logger.Severity), logLevel);
            } catch (ArgumentException) {
                Console.WriteLine (
                    "[kRPC] Error parsing log level from configuration file. Got '" + logLevel + "'. " +
                    "Defaulting to " + Logger.Severity.Info);
                Logger.Level = Logger.Severity.Info;
            }
            Configuration.VerboseErrors = verboseErrors;
            Configuration.AdaptiveRateControl = adaptiveRateControl;
            Configuration.TargetFrameRate = Math.Max (
                KRPC.Configuration.TargetFrameRateMin, Math.Min (KRPC.Configuration.TargetFrameRateMax, targetFrameRate));
            Configuration.MaxTimePerFrame = Math.Max (
                KRPC.Configuration.MaxTimePerFrameMin, Math.Min (KRPC.Configuration.MaxTimePerFrameMax, maxTimePerFrame));
            Configuration.MaxCallWait = maxCallWait;
            Configuration.TickHoldTimeout = tickHoldTimeout;
            Logger.WriteLine ("Loaded configuration", Logger.Severity.Debug);
        }
    }
}
