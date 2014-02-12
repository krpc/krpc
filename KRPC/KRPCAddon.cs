using System;
using System.Reflection;
using System.Net;
using UnityEngine;
using KRPC.Server;
using KRPC.Service;
using KRPC.Schema.RPC;
using KRPC.Utils;

namespace KRPC
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class KRPCAddon : MonoBehaviour
	{
		private static bool hasInitServer = false;
		private static RPCServer server;

		public void Awake ()
		{
			Debug.Log ("[kRPC] Awake");
			if (!hasInitServer) {
				Debug.Log ("[kRPC] Starting server on port 50000; accepting connections from 127.0.0.1");
				server = new RPCServer (new TCPServer (IPAddress.Parse("127.0.0.1"), 50000));
				server.Start ();
				hasInitServer = true;
			}
		}

		/*
		public override void OnDestroy ()
		{
			if (hasInitServer)
				server.Stop ();
		}
		*/

		public void Update ()
		{
			if (hasInitServer) {
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
			} else {
				Debug.Log ("[kRPC] ERROR: Server has not started!");
			}
		}
	}
}
