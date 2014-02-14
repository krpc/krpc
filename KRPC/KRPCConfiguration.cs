using System;
using System.Net;
using KSP.IO;

namespace KRPC
{
	public class KRPCConfiguration
	{
		private PluginConfiguration config;
		private const int defaultPort = 50000;
		private const string defaultLocalAddress = "127.0.0.1";

		public IPAddress LocalAddress {
			get {
				config.load ();
				string localAddress = config.GetValue<string> ("localaddress");
				if (localAddress == "any")
					return IPAddress.Any;
				try {
					return IPAddress.Parse (localAddress);
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
			string localAddress = config.GetValue<string> ("localaddress", defaultLocalAddress);

			// Create the config file if it doesn't already exist
			//TODO: cleaner way to do this?
			config ["port"] = port;
			config ["localaddress"] = localAddress;
			config.save ();
		}
	}
}

