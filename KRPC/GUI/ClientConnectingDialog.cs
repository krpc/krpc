using System;
using UnityEngine;
using KRPC.Server;

namespace KRPC.GUI
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class ClientConnectingDialog : MonoBehaviour
	{
		private static ClientConnectingDialog instance;
		public static ClientConnectingDialog Instance {
			get {
				if (instance == null)
					instance = (ClientConnectingDialog) FindObjectOfType (typeof(ClientConnectingDialog));
				return instance;
			}
		}

		private volatile bool showConnectionAttemptDialog = false;
		private System.Net.Sockets.Socket client;
		private ConnectionAttempt attempt;

		public void Awake() {
			RenderingManager.AddToPostDrawQueue(5, DrawGUI);
		}

		public static void ShowConnectionAttemptDialog (System.Net.Sockets.Socket client, Server.INetworkStream stream, ConnectionAttempt attempt)
		{
			//TODO: refactor this into a non-static method (note that it currently depends on the addon instantiation order)
			Instance.showConnectionAttemptDialog = true;
			Instance.client = client;
			Instance.attempt = attempt;
			//TODO: This spin lock is horrible. But it works...
			while (Instance.showConnectionAttemptDialog) {
				System.Threading.Thread.Sleep(50);
			}
		}

		public void CancelConnectionAttemptDialog () {
			showConnectionAttemptDialog = false;
		}

		private void DrawGUI() {
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
				string message = "A client is attempting to connect from " + client.RemoteEndPoint;
				var dialog = new MultiOptionDialog (message, "kRPC", UnityEngine.GUI.skin, options);
				dialog.DrawWindow ();
			}
		}
	}
}
