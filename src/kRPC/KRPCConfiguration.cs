using System;
using System.Net;
using KRPC.Utils;

namespace KRPC
{
    sealed class KRPCConfiguration : ConfigurationStorage
    {
        [Persistent]
        private int port = 50000;
        [Persistent]
        private string address = "127.0.0.1";

        public IPAddress Address { get; private set; }

        public int Port {
            get { return port; }
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
