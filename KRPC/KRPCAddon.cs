using System;
using System.Reflection;
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
			if (!hasInitServer) {
				Debug.Log ("KRPC starting server");
				server = new RPCServer (new TCPServer (8888));
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
					server.SendResponse (request.Item1, response.BuildPartial ());
				} catch (NoRequestException) {
				}
			} else {
				Debug.Log ("KRPC server not started");
			}
		}
	}
}
