using System.Diagnostics.CodeAnalysis;
using KRPC.Server;
using KRPC.UI;
using KRPC.Utils;
using KSP.UI.Screens;
using UnityEngine;

namespace KRPC
{
    /// <summary>
    /// Main KRPC addon. Contains the kRPC core, config and UI.
    /// </summary>
    [KSPAddonImproved (KSPAddonImproved.Startup.All, false)]
    [SuppressMessage ("Gendarme.Rules.Correctness", "DeclareEventsExplicitlyRule")]
    sealed public class Addon : MonoBehaviour
    {
        static Configuration config;
        static Core core;
        static Texture textureOnline;
        static Texture textureOffline;

        ApplicationLauncherButton applauncherButton;
        MainWindow mainWindow;
        InfoWindow infoWindow;
        ClientConnectingDialog clientConnectingDialog;
        ClientDisconnectDialog clientDisconnectDialog;

        static void Init ()
        {
            if (core == null) {
                core = Core.Instance;
                config = Configuration.Instance;
                foreach (var server in config.Servers)
                    core.Add (server.Create ());
            }
        }

        /// <summary>
        /// Called whenever a scene change occurs. Ensures the server has been initialized,
        /// (re)creates the UI, and shuts down the server in the main menu.
        /// </summary>
        public void Awake ()
        {
            if (!ServicesChecker.OK)
                return;

            Init ();

            Service.CallContext.SetGameScene (KSPAddonImproved.CurrentGameScene.ToGameScene ());
            Logger.WriteLine ("Game scene switched to " + Service.CallContext.GameScene);

            // If a game is not loaded, ensure the server is stopped and then exit
            if (KSPAddonImproved.CurrentGameScene != GameScenes.EDITOR &&
                KSPAddonImproved.CurrentGameScene != GameScenes.FLIGHT &&
                KSPAddonImproved.CurrentGameScene != GameScenes.SPACECENTER &&
                KSPAddonImproved.CurrentGameScene != GameScenes.TRACKSTATION) {
                core.StopAll ();
                return;
            }

            // Auto-start the servers, if required
            if (config.AutoStartServers) {
                Logger.WriteLine ("Auto-starting servers");
                core.StartAll ();
            }

            // (Re)create the UI
            InitUI ();
        }

        [SuppressMessage ("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
        void InitUI ()
        {
            // Layout extensions
            GUILayoutExtensions.Init (gameObject);

            // Disconnect client dialog
            clientDisconnectDialog = gameObject.AddComponent<ClientDisconnectDialog> ();

            // Info window
            infoWindow = gameObject.AddComponent<InfoWindow> ();
            infoWindow.Closable = true;
            infoWindow.Visible = config.InfoWindowVisible;
            infoWindow.Position = config.InfoWindowPosition;

            // Main window
            mainWindow = gameObject.AddComponent<MainWindow> ();
            mainWindow.Visible = config.MainWindowVisible;
            mainWindow.Position = config.MainWindowPosition;
            mainWindow.ClientDisconnectDialog = clientDisconnectDialog;
            mainWindow.InfoWindow = infoWindow;

            // New connection dialog
            clientConnectingDialog = gameObject.AddComponent<ClientConnectingDialog> ();

            // Set up events
            InitEvents ();

            // Add button to the applauncher
            mainWindow.Closable = true;
            textureOnline = GameDatabase.Instance.GetTexture ("kRPC/icons/applauncher-online", false);
            textureOffline = GameDatabase.Instance.GetTexture ("kRPC/icons/applauncher-offline", false);
            GameEvents.onGUIApplicationLauncherReady.Add (OnGUIApplicationLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add (OnGUIApplicationLauncherDestroyed);
            core.OnServerStarted += (s, e) => {
                if (applauncherButton != null)
                    applauncherButton.SetTexture (core.AnyRunning ? textureOnline : textureOffline);
            };
            core.OnServerStopped += (s, e) => {
                if (applauncherButton != null)
                    applauncherButton.SetTexture (core.AnyRunning ? textureOnline : textureOffline);
            };
        }

        void InitEvents ()
        {
            // Main window events
            mainWindow.OnStartServerPressed += (s, e) => {
                try {
                    e.Server.Start ();
                } catch (ServerException exn) {
                    Logger.WriteLine ("Server exception: " + exn.Message, Logger.Severity.Error);
                    mainWindow.Errors.Add (exn.Message);
                }
            };
            mainWindow.OnStopServerPressed += (s, e) => {
                e.Server.Stop ();
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

            // Info window events
            infoWindow.OnHide += (s, e) => {
                config.Load ();
                config.InfoWindowVisible = false;
                config.Save ();
            };
            infoWindow.OnShow += (s, e) => {
                config.Load ();
                config.InfoWindowVisible = true;
                config.Save ();
            };
            infoWindow.OnMoved += (s, e) => {
                config.Load ();
                var window = s as InfoWindow;
                config.InfoWindowPosition = window.Position;
                config.Save ();
            };

            // Server events
            core.OnClientRequestingConnection += (s, e) => {
                if (config.AutoAcceptConnections) {
                    Logger.WriteLine ("Auto-accepting client connection (" + e.Client.Address + ")");
                    e.Request.Allow ();
                } else {
                    Logger.WriteLine ("Asking player to accept client connection (" + e.Client.Address + ")");
                    clientConnectingDialog.OnClientRequestingConnection (s, e);
                }
            };
        }

        void OnGUIApplicationLauncherReady ()
        {
            applauncherButton = ApplicationLauncher.Instance.AddModApplication (
                () => mainWindow.Visible = !mainWindow.Visible,
                () => mainWindow.Visible = !mainWindow.Visible,
                null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS,
                core.AnyRunning ? textureOnline : textureOffline);
        }

        void OnGUIApplicationLauncherDestroyed ()
        {
            ApplicationLauncher.Instance.RemoveModApplication (applauncherButton);
            applauncherButton = null;
        }

        /// <summary>
        /// Destroy the UI.
        /// </summary>
        public void OnDestroy ()
        {
            if (!ServicesChecker.OK)
                return;

            // Destroy the UI
            if (applauncherButton != null)
                OnGUIApplicationLauncherDestroyed ();
            GameEvents.onGUIApplicationLauncherReady.Remove (OnGUIApplicationLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove (OnGUIApplicationLauncherDestroyed);
            Object.Destroy (mainWindow);
            Object.Destroy (clientConnectingDialog);
            GUILayoutExtensions.Destroy ();
        }

        /// <summary>
        /// Stop the server if running
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void OnApplicationQuit ()
        {
            core.StopAll ();
        }

        /// <summary>
        /// GUI update
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void OnGUI ()
        {
            GUILayoutExtensions.OnGUI ();
        }

        /// <summary>
        /// Trigger server update
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void FixedUpdate ()
        {
            if (!ServicesChecker.OK)
                return;
            if (core != null && core.AnyRunning)
                core.Update ();
        }
    }
}
