using System;
using System.Reflection;
using System.Net;
using UnityEngine;
using KSP.IO;
using KRPC.Server;
using KRPC.Service;
using KRPC.Schema.RPC;
using KRPC.Utils;
using KRPC.GUI;

namespace KRPC
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class KRPCAddon : MonoBehaviour
	{
		private static RPCServer server = null;
		private static TCPServer tcpServer = null;
		private MainWindow mainWindow;
		private KRPCConfiguration config;

		public void Awake ()
		{

			config = new KRPCConfiguration ();
			tcpServer = new TCPServer (config.LocalAddress, config.Port);
			server = new RPCServer (tcpServer);
			mainWindow = gameObject.AddComponent<MainWindow>();
			mainWindow.Init(server);
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
