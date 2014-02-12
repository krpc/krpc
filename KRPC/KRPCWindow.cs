using System;
using System.Net;
using UnityEngine;
using KSP;
using KRPC.Server;

namespace KRPC
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class KRPCWindow : MonoBehaviour
	{
		private int windowId = UnityEngine.Random.Range(1000, 2000000);
		private Rect windowPosition = new Rect();
		private GUIStyle windowStyle, labelStyle;

		private volatile bool showConnectionAttemptDialog = false;
		private System.Net.Sockets.Socket client;
		private ConnectionAttempt attempt;

		public void Awake() {
			windowStyle = new GUIStyle(HighLogic.Skin.window);
			windowStyle.fixedWidth = 250f;
			labelStyle = new GUIStyle(HighLogic.Skin.label);
			labelStyle.stretchWidth = true;
			RenderingManager.AddToPostDrawQueue(5, DrawGUI);
			KRPCAddon.Server.Server.OnClientRequestingConnection += HandleClientConnectionRequest;
		}

		private void HandleClientConnectionRequest (System.Net.Sockets.Socket client, ConnectionAttempt attempt)
		{
			showConnectionAttemptDialog = true;
			this.client = client;
			this.attempt = attempt;
			//TODO: This spin lock is horrible. But it works...
			while (showConnectionAttemptDialog) {
				System.Threading.Thread.Sleep(50);
			}
		}

		private void OnGUI() {
			if (showConnectionAttemptDialog) {
				DialogOption[] options = {
					new DialogOption ("Allow", () => {
						attempt.Allow ();
						showConnectionAttemptDialog = false;
					}),
					new DialogOption ("Deny", () => {
						attempt.Deny ();
						showConnectionAttemptDialog = false;
					})
				};
				var dialog = new MultiOptionDialog ("A client is attempting to connect from " + client.RemoteEndPoint, "kRPC", HighLogic.Skin, options);
				dialog.DrawWindow ();
			}
		}

		private void DrawGUI() {
			windowPosition = GUILayout.Window (windowId, windowPosition, DrawWindow, "kRPC Server", windowStyle);
		}

		private void DrawWindow(int windowID) {
			GUILayout.BeginVertical();
			GUILayout.Label ("Server status: " + (KRPCAddon.Server.Running ? "Online" : "Offline"), labelStyle);
			if (KRPCAddon.Server.Running) {
				if (GUILayout.Button ("Stop server"))
					KRPCAddon.Server.Stop ();
				TCPServer tcpServer = (TCPServer)KRPCAddon.Server.Server;
				GUILayout.Label ("Port: " + tcpServer.Port, labelStyle);
				GUILayout.Label ("Allowed client(s): " + EndPointToString(tcpServer.EndPoint), labelStyle);
				GUILayout.Label (tcpServer.GetConnectedClientIds().Count + " clients connected", labelStyle);
			} else {
				if (GUILayout.Button ("Start server"))
					KRPCAddon.Server.Start ();
			}
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}

		private string EndPointToString(IPAddress endPoint) {
			if (endPoint.ToString() == "0.0.0.0")
				return "any";
			else if (endPoint.ToString() == "127.0.0.1")
				return "local";
			return endPoint.ToString ();
		}
	}
}

