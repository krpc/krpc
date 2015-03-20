using System;
using System.Net;
using UnityEngine;
using KRPC.Utils;

namespace KRPC
{
    sealed class KRPCConfiguration : ConfigurationStorage
    {
        [Persistent] string address = "127.0.0.1";
        [Persistent] ushort rpcPort = 50000;
        [Persistent] ushort streamPort = 50001;
        [Persistent] bool mainWindowVisible = true;
        [Persistent] RectStorage mainWindowPosition = new RectStorage ();
        [Persistent] bool autoStartServer = false;
        [Persistent] bool autoAcceptConnections = false;
        [Persistent] string logLevel = Logger.Severity.Info.ToString ();

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

        public bool AutoStartServer {
            get { return autoStartServer; }
            set { autoStartServer = value; }
        }

        public bool AutoAcceptConnections {
            get { return autoAcceptConnections; }
            set { autoAcceptConnections = value; }
        }

        public KRPCConfiguration (string filePath) :
            base (filePath)
        {
            Address = IPAddress.Parse (address);
            Logger.Level = (Logger.Severity)Enum.Parse (typeof(Logger.Severity), logLevel);
        }

        protected override void BeforeSave ()
        {
            address = Address.ToString ();
            logLevel = Logger.Level.ToString ();
        }

        protected override void AfterLoad ()
        {
            try {
                Address = IPAddress.Parse (address);
            } catch (FormatException) {
                Console.WriteLine (
                    "[kRPC] Error parsing IP address from configuration file. Got '" + address + "'. " +
                    "Defaulting to loopback address " + IPAddress.Loopback);
                Address = IPAddress.Loopback;
            }
            try {
                Logger.Level = (Logger.Severity)Enum.Parse (typeof(Logger.Severity), logLevel);
            } catch (ArgumentException) {
                Console.WriteLine (
                    "[kRPC] Error parsing log level from configuration file. Got '" + logLevel + "'. " +
                    "Defaulting to " + Logger.Severity.Info);
                Logger.Level = Logger.Severity.Info;
            }
        }
    }
}
