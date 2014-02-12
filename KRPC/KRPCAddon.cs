using System;
using System.Reflection;
using System.Net;
using UnityEngine;
using KSP.IO;
using KRPC.Server;
using KRPC.Service;
using KRPC.Schema.RPC;
using KRPC.Utils;

namespace KRPC
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class KRPCAddon : MonoBehaviour
	{
		private static RPCServer server = null;
		private static TCPServer tcpServer = null;
		private const int defaultPort = 50000;
		private const string defaultEndPoint = "127.0.0.1";

		public static RPCServer Server {
			get { return server; }
		}

		public void Awake ()
		{
			if (server == null) {

				// Load configuration
				PluginConfiguration config = PluginConfiguration.CreateForType<KRPCAddon>();
				config.load ();
				int port = config.GetValue<int>("port", defaultPort);
				string endPointStr = config.GetValue<string> ("endpoint", defaultEndPoint);

				// Create the config file if it doesn't already exist
				//TODO: cleaner way to do this?
				config ["port"] = port;
				config ["endpoint"] = endPointStr;
				config.save ();

				// Parse the endpoint
				IPAddress endPoint;
				if (endPointStr == "*")
					endPoint = IPAddress.Any;
				else
					endPoint = IPAddress.Parse (endPointStr);

				// Start the server
				Debug.Log ("[kRPC] Starting RPC server on port " + port + "; accepting connections from " + endPoint);
				tcpServer = new TCPServer (endPoint, port);
				server = new RPCServer (tcpServer);
			}
		}

		public void OnDestroy ()
		{
			if (server.Running)
				server.Stop ();
		}

		public void Update ()
		{
			if (server.Running) {
				try {
					// Get request
					Tuple<int,Request> request = server.GetRequest ();
					Debug.Log("[kRPC] Received request from client " + request.Item1 + " (" + request.Item2.Service + "." + request.Item2.Method + ")");

					// Handle the request
					Response.Builder response;
					try {
						response = Services.HandleRequest (Assembly.GetExecutingAssembly (), "KRPC.Service", request.Item2);
					} catch (Exception e) {
						response = Response.CreateBuilder ()
							.SetError (true)
							.SetErrorMessage (e.ToString ());
						Debug.Log (e.ToString ());
					}
					// Send response
					response.SetTime (Planetarium.GetUniversalTime ());
					var builtResponse = response.BuildPartial();
					server.SendResponse (request.Item1, builtResponse);
					if (response.Error)
						Debug.Log("[kRPC] Sent error response to client " + request.Item1 + " (" + response.ErrorMessage + ")");
					else
						Debug.Log("[kRPC] Sent response to client " + request.Item1);
				} catch (NoRequestException) {
				}
			}
		}
	}
}
