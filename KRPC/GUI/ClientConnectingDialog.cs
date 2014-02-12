using System;
using UnityEngine;
using KRPC.Server;

namespace KRPC.GUI
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class ClientConnectingDialog : MonoBehaviour
	{
		private volatile bool showConnectionAttemptDialog = false;
		private System.Net.Sockets.Socket client;
		private ConnectionAttempt attempt;

		public void Awake() {
			RenderingManager.AddToPostDrawQueue(5, DrawGUI);
			KRPCAddon.Server.Server.OnClientRequestingConnection += ShowConnectionAttemptDialog;
		}

		private void ShowConnectionAttemptDialog (System.Net.Sockets.Socket client, ConnectionAttempt attempt)
		{
			showConnectionAttemptDialog = true;
			this.client = client;
			this.attempt = attempt;
			//TODO: This spin lock is horrible. But it works...
			while (showConnectionAttemptDialog) {
				System.Threading.Thread.Sleep(50);
			}
		}

		private void CancelConnectionAttemptDialog () {
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
				var dialog = new MultiOptionDialog (message, "kRPC", HighLogic.Skin, options);
				dialog.DrawWindow ();
			}
		}
	}
}
