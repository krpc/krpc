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
        }

        protected override void BeforeSave ()
        {
            address = Address.ToString ();
        }

        protected override void AfterLoad ()
        {
            try {
                Address = IPAddress.Parse (address);
            } catch (FormatException) {
                Debug.Log ("Error parsing IP address from configuration file. Got '" + address + "'. " +
                "Defaulting to loopback address " + IPAddress.Loopback);
                Address = IPAddress.Loopback;
            }
        }
    }
}
