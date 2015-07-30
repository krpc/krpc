using System;
using System.Net;
using UnityEngine;
using KRPC.Service;
using KRPC.Utils;
using KRPC.Service.Scanner;

namespace KRPC
{
    sealed class KRPCConfiguration : ConfigurationStorage
    {
        [Persistent] string address = "127.0.0.1";
        [Persistent] ushort rpcPort = 50000;
        [Persistent] ushort streamPort = 50001;
        [Persistent] bool mainWindowVisible = true;
        [Persistent] RectStorage mainWindowPosition = new RectStorage ();
        [Persistent] bool infoWindowVisible = false;
        [Persistent] RectStorage infoWindowPosition = new RectStorage ();
        [Persistent] bool autoStartServer = false;
        [Persistent] bool autoAcceptConnections = false;
        [Persistent] string logLevel = Logger.Severity.Info.ToString ();
        [Persistent] bool verboseErrors = false;
        [Persistent] bool checkDocumented = false;
        [Persistent] bool oneRPCPerUpdate = false;
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

        public KRPCConfiguration (string filePath) :
            base (filePath)
        {
            Address = IPAddress.Parse (address);
            Logger.Level = (Logger.Severity)Enum.Parse (typeof(Logger.Severity), logLevel);
            RPCException.VerboseErrors = verboseErrors;
            ServicesChecker.CheckDocumented = checkDocumented;
        }

        protected override void BeforeSave ()
        {
            address = Address.ToString ();
            logLevel = Logger.Level.ToString ();
            verboseErrors = RPCException.VerboseErrors;
            checkDocumented = ServicesChecker.CheckDocumented;
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
            RPCException.VerboseErrors = verboseErrors;
            ServicesChecker.CheckDocumented = checkDocumented;
        }
    }
}
