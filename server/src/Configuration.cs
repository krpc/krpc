using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using KRPC.Service;
using KRPC.Utils;
using UnityEngine;

namespace KRPC
{
    sealed class Configuration : ConfigurationStorage
    {
        [Persistent] string address = "127.0.0.1";
        [Persistent] ushort rpcPort = 50000;
        [Persistent] ushort streamPort = 50001;
        [Persistent] bool mainWindowVisible = true;
        [Persistent] RectStorage mainWindowPosition = new RectStorage ();
        [Persistent] bool infoWindowVisible;
        [Persistent] RectStorage infoWindowPosition = new RectStorage ();
        [Persistent] bool autoStartServer;
        [Persistent] bool autoAcceptConnections;
        [Persistent] bool confirmRemoveClient = true;
        [Persistent] string logLevel = Utils.Logger.Severity.Info.ToString ();
        [Persistent] bool verboseErrors;
        [Persistent] bool checkDocumented;
        [Persistent] bool oneRPCPerUpdate;
        [Persistent] uint maxTimePerUpdate = 5000;
        [Persistent] bool adaptiveRateControl = true;
        [Persistent] bool blockingRecv = true;
        [Persistent] uint recvTimeout = 1000;

        public IPAddress Address { get; set; }

        public ushort RPCPort {
            get { return rpcPort; }
            set { rpcPort = value; }
        }

        public ushort StreamPort {
            get { return streamPort; }
            set { streamPort = value; }
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

        public bool AutoStartServer {
            get { return autoStartServer; }
            set { autoStartServer = value; }
        }

        public bool AutoAcceptConnections {
            get { return autoAcceptConnections; }
            set { autoAcceptConnections = value; }
        }

        public bool ConfirmRemoveClient {
            get { return confirmRemoveClient; }
            set { confirmRemoveClient = value; }
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

        public Configuration (string filePath) :
            base (filePath, "KRPCConfiguration")
        {
            AfterLoad ();
        }

        protected override void BeforeSave ()
        {
            address = Address.ToString ();
            logLevel = Utils.Logger.Level.ToString ();
            verboseErrors = RPCException.VerboseErrors;
            checkDocumented = ServicesChecker.CheckDocumented;
        }

        [SuppressMessage ("Gendarme.Rules.BadPractice", "DisableDebuggingCodeRule")]
        protected override void AfterLoad ()
        {
            IPAddress ipAddress;
            bool validAddress = IPAddress.TryParse (address, out ipAddress);
            if (!validAddress) {
                Console.WriteLine (
                    "[kRPC] Error parsing IP address from configuration file. Got '" + address + "'. " +
                    "Defaulting to loopback address " + IPAddress.Loopback);
                Address = IPAddress.Loopback;
            } else {
                Address = ipAddress;
            }
            try {
                Utils.Logger.Level = (Utils.Logger.Severity)Enum.Parse (typeof(Utils.Logger.Severity), logLevel);
            } catch (ArgumentException) {
                Console.WriteLine (
                    "[kRPC] Error parsing log level from configuration file. Got '" + logLevel + "'. " +
                    "Defaulting to " + Utils.Logger.Severity.Info);
                Utils.Logger.Level = Utils.Logger.Severity.Info;
            }
            RPCException.VerboseErrors = verboseErrors;
            ServicesChecker.CheckDocumented = checkDocumented;
        }
    }
}
