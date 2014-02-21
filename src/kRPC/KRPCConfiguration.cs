using System;
using System.Net;
using UnityEngine;
using KRPC.Utils;

namespace KRPC
{
    sealed class KRPCConfiguration : ConfigurationStorage
    {
        [Persistent] private string address = "127.0.0.1";
        [Persistent] private ushort port = 50000;
        [Persistent] private bool mainWindowVisible = true;
        [Persistent] private RectStorage mainWindowPosition = new RectStorage ();

        public IPAddress Address { get; set; }

        public ushort Port
        {
            get { return port; }
            set { port = value; }
        }

        public bool MainWindowVisible
        {
            get { return mainWindowVisible; }
            set { mainWindowVisible = value; }
        }

        public Rect MainWindowPosition
        {
            get { return mainWindowPosition.AsRect (); }
            set { mainWindowPosition = RectStorage.FromRect (value); }
        }

        public KRPCConfiguration (string filePath):
            base(filePath)
        {
            Address = IPAddress.Parse(address);
        }

        protected override void BeforeSave()
        {
            address = Address.ToString ();
        }

        protected override void AfterLoad()
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
