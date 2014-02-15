using System;
using System.Net;
using KSP.IO;

namespace KRPC
{
    sealed class KRPCConfiguration
    {
        private PluginConfiguration config;
        private const int defaultPort = 50000;
        private const string defaultAddress = "127.0.0.1";

        public IPAddress Address {
            get {
                config.load ();
                string address = config.GetValue<string> ("address");
                if (address == "any")
                    return IPAddress.Any;
                try {
                    return IPAddress.Parse (address);
                } catch (FormatException) {
                    //TODO: report error in GUI
                    return IPAddress.Loopback;
                }
            }
        }

        public int Port {
            get {
                config.load ();
                try {
                    return config.GetValue<int> ("port");
                } catch (FormatException) {
                    //TODO: report error in GUI
                    return defaultPort;
                }
            }
        }

        public KRPCConfiguration ()
        {
            config = PluginConfiguration.CreateForType<KRPCAddon>();
            config.load ();
            int port = config.GetValue<int>("port", defaultPort);
            string address = config.GetValue<string> ("address", defaultAddress);

            // Create the config file if it doesn't already exist
            //TODO: cleaner way to do this?
            config ["port"] = port;
            config ["address"] = address;
            config.save ();
        }
    }
}

