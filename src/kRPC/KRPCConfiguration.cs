using System;
using System.Net;
using UnityEngine;
using KRPC.Utils;

namespace KRPC
{
    sealed class KRPCConfiguration : ConfigurationStorage
    {
        [Persistent] private int port = 50000;
        [Persistent] private string address = "127.0.0.1";
        [Persistent] private bool mainWindowVisible = true;
        [Persistent] private RectStorage mainWindowPosition = new RectStorage ();

        public IPAddress Address { get; set; }

        public int Port
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
            if (Address == IPAddress.Loopback)
                address = "any";
            else
                address = Address.ToString ();
        }

        protected override void AfterLoad()
        {
            if (address == "any")
                Address = IPAddress.Any;
            else {
                try {
                    Address = IPAddress.Parse (address);
                } catch (FormatException) {
                    //TODO: report error
                    Address = IPAddress.Loopback;
                }
            }
        }
    }
}
