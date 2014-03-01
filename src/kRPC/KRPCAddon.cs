using UnityEngine;
using KRPC.Server;
using KRPC.UI;

namespace KRPC
{
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    sealed public class KRPCAddon : MonoBehaviour
    {
        KRPCServer server;
        KRPCConfiguration config;
        IButton toolbarButton;
        MainWindow mainWindow;
        ClientConnectingDialog clientConnectingDialog;
        ClientDisconnectDialog clientDisconnectDialog;

        public void Awake ()
        {
            config = new KRPCConfiguration ("settings.cfg");
            config.Load ();
            server = new KRPCServer (config.Address, config.Port);
            server.GetUniversalTime = Planetarium.GetUniversalTime;

            // Disconnect client dialog
            clientDisconnectDialog = gameObject.AddComponent<ClientDisconnectDialog> ();

            // Create main window
            mainWindow = gameObject.AddComponent<MainWindow> ();
            mainWindow.Config = config;
            mainWindow.Server = server;
            mainWindow.Visible = config.MainWindowVisible;
            mainWindow.Position = config.MainWindowPosition;
            mainWindow.ClientDisconnectDialog = clientDisconnectDialog;

            // Create new connection dialog
            clientConnectingDialog = gameObject.AddComponent<ClientConnectingDialog> ();

            // Main window events
            mainWindow.OnStartServerPressed += (s, e) => {
                server.Port = config.Port;
                server.Address = config.Address;
                try {
                    server.Start ();
                } catch (ServerException exn) {
                    mainWindow.Errors.Add (exn.Message);
                }
            };
            mainWindow.OnStopServerPressed += (s, e) => {
                server.Stop ();
                clientConnectingDialog.Close ();
            };
            mainWindow.OnHide += (s, e) => {
                config.MainWindowVisible = false;
                config.Save ();
            };
            mainWindow.OnShow += (s, e) => {
                config.MainWindowVisible = true;
                config.Save ();
            };
            mainWindow.OnMoved += (s, e) => {
                var window = s as MainWindow;
                config.MainWindowPosition = window.Position;
                config.Save ();
            };

            // Server events
            server.OnClientRequestingConnection += (s, e) => {
                if (config.AutoAcceptConnections)
                    e.Request.Allow ();
                else
                    clientConnectingDialog.OnClientRequestingConnection (s, e);
            };

            // Toolbar API
            if (ToolbarManager.ToolbarAvailable) {
                toolbarButton = ToolbarManager.Instance.add ("kRPC", "ToggleMainWindow");
                toolbarButton.TexturePath = "kRPC/icons/toolbar-offline";
                toolbarButton.ToolTip = "kRPC Server";
                toolbarButton.Visibility = new GameScenesVisibility (GameScenes.FLIGHT);
                toolbarButton.OnClick += (e) => mainWindow.Visible = !mainWindow.Visible;
                server.OnStarted += (s, e) => toolbarButton.TexturePath = "kRPC/icons/toolbar-online";
                server.OnStopped += (s, e) => toolbarButton.TexturePath = "kRPC/icons/toolbar-offline";
            } else {
                // If there is no toolbar button a hidden window can't be shown, so force it to be displayed
                mainWindow.Visible = true;
            }
        }

        public void OnDestroy ()
        {
            if (server.Running)
                server.Stop ();
            toolbarButton.Destroy ();
            UnityEngine.Object.Destroy (mainWindow);
            UnityEngine.Object.Destroy (clientConnectingDialog);
        }

        public void FixedUpdate ()
        {
            if (server.Running)
                server.Update ();
        }
    }
}
