using System;
using System.Net;
using UnityEngine;
using KSP;
using KRPC.Server;

namespace KRPC.GUI
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class MainWindow : MonoBehaviour
	{
		private int windowId = UnityEngine.Random.Range(1000, 2000000);
		private Rect windowPosition = new Rect();
		private bool hasInitStyles = false;
		private GUIStyle windowStyle, labelStyle;

		public void Awake() {
			RenderingManager.AddToPostDrawQueue(5, DrawGUI);
		}

		private void InitStyles() {
			if (!hasInitStyles) {
				windowStyle = new GUIStyle(UnityEngine.GUI.skin.window);
				windowStyle.fixedWidth = 250f;
				labelStyle = new GUIStyle(UnityEngine.GUI.skin.label);
				labelStyle.stretchWidth = true;
				hasInitStyles = true;
			}
		}

		private void DrawGUI() {
			windowPosition = GUILayout.Window (windowId, windowPosition, DrawWindow, "kRPC Server", windowStyle);
		}

		private void DrawWindow(int windowID) {
			InitStyles ();
			GUILayout.BeginVertical();
			GUILayout.Label ("Server status: " + (KRPCAddon.Server.Running ? "Online" : "Offline"), labelStyle);
			if (KRPCAddon.Server.Running) {
				if (GUILayout.Button ("Stop server"))
					StopServerPressed();
				TCPServer tcpServer = (TCPServer)KRPCAddon.Server.Server;
				GUILayout.Label ("Port: " + tcpServer.Port, labelStyle);
				GUILayout.Label ("Allowed client(s): " + EndPointToString(tcpServer.EndPoint), labelStyle);
				GUILayout.Label (tcpServer.GetConnectedClientIds().Count + " clients connected", labelStyle);
			} else {
				if (GUILayout.Button ("Start server"))
					StartServerPressed ();
			}
			GUILayout.EndVertical ();
			UnityEngine.GUI.DragWindow ();
		}

		private void StartServerPressed () {
			KRPCAddon.Server.Start ();
		}

		private void StopServerPressed () {
			KRPCAddon.Server.Stop ();
			ClientConnectingDialog.Instance.CancelConnectionAttemptDialog();
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

