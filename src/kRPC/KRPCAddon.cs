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
        ApplicationLauncherButton applauncherButton;
        Texture textureOnline;
        Texture textureOffline;
        MainWindow mainWindow;
        ClientConnectingDialog clientConnectingDialog;
        ClientDisconnectDialog clientDisconnectDialog;

        public void Awake ()
        {
            config = new KRPCConfiguration ("settings.cfg");
            config.Load ();
            server = new KRPCServer (config.Address, config.RPCPort, config.StreamPort);
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
            mainWindow.OnStartServerPressed += (s, e) => StartServer ();
            mainWindow.OnStopServerPressed += (s, e) => {
                server.Stop ();
                clientConnectingDialog.Close ();
            };
            mainWindow.OnHide += (s, e) => {
                config.Load ();
                config.MainWindowVisible = false;
                config.Save ();
            };
            mainWindow.OnShow += (s, e) => {
                config.Load ();
                config.MainWindowVisible = true;
                config.Save ();
            };
            mainWindow.OnMoved += (s, e) => {
                config.Load ();
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

            // Add show/hide window button
            if (ToolbarManager.ToolbarAvailable) {
                // Add a button to the Toolbar plugin if it's available
                mainWindow.Closable = true;
                toolbarButton = ToolbarManager.Instance.add ("kRPC", "ToggleMainWindow");
                toolbarButton.TexturePath = "kRPC/icons/toolbar-offline";
                toolbarButton.ToolTip = "kRPC Server";
                toolbarButton.Visibility = new GameScenesVisibility (GameScenes.FLIGHT);
                toolbarButton.OnClick += e => mainWindow.Visible = !mainWindow.Visible;
                server.OnStarted += (s, e) => toolbarButton.TexturePath = "kRPC/icons/toolbar-online";
                server.OnStopped += (s, e) => toolbarButton.TexturePath = "kRPC/icons/toolbar-offline";
                applauncherButton = null;
            } else {
                // Otherwise add a button to the stock KSP applauncher
                mainWindow.Closable = true;
                textureOnline = GameDatabase.Instance.GetTexture ("kRPC/icons/applauncher-online", false);
                textureOffline = GameDatabase.Instance.GetTexture ("kRPC/icons/applauncher-offline", false);
                GameEvents.onGUIApplicationLauncherReady.Add (OnGUIApplicationLauncherReady);
                GameEvents.onGUIApplicationLauncherDestroyed.Add (OnGUIApplicationLauncherDestroyed);
                server.OnStarted += (s, e) => {
                    if (applauncherButton != null) {
                        applauncherButton.SetTexture (textureOnline);
                    }
                };
                server.OnStopped += (s, e) => {
                    if (applauncherButton != null) {
                        applauncherButton.SetTexture (textureOffline);
                    }
                };
                toolbarButton = null;
            }

            // Auto-start the server, if required
            if (config.AutoStartServer)
                StartServer ();
        }

        void OnGUIApplicationLauncherReady ()
        {
            applauncherButton = ApplicationLauncher.Instance.AddModApplication (
                () => mainWindow.Visible = !mainWindow.Visible,
                () => mainWindow.Visible = !mainWindow.Visible,
                null, null, null, null,
                ApplicationLauncher.AppScenes.FLIGHT,
                server.Running ? textureOnline : textureOffline);
        }

        void OnGUIApplicationLauncherDestroyed ()
        {
            ApplicationLauncher.Instance.RemoveModApplication (applauncherButton);
            applauncherButton = null;
        }

        void StartServer ()
        {
            config.Load ();
            server.RPCPort = config.RPCPort;
            server.StreamPort = config.StreamPort;
            server.Address = config.Address;
            try {
                server.Start ();
            } catch (ServerException exn) {
                mainWindow.Errors.Add (exn.Message);
            }
        }

        public void OnDestroy ()
        {
            if (server.Running)
                server.Stop ();
            if (toolbarButton != null)
                toolbarButton.Destroy ();
            if (applauncherButton != null) {
                OnGUIApplicationLauncherDestroyed ();
            }
            GameEvents.onGUIApplicationLauncherReady.Remove (OnGUIApplicationLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove (OnGUIApplicationLauncherDestroyed);
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
