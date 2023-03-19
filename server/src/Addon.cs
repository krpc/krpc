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
    [KSPAddon (KSPAddon.Startup.AllGameScenes, false)]
    [SuppressMessage ("Gendarme.Rules.Correctness", "DeclareEventsExplicitlyRule")]
    public sealed class Addon : MonoBehaviour
    {
        // TODO: clean this up
        internal static ConfigurationFile config;
        static Core core;
        static Texture textureOnline;
        static Texture textureOffline;

        ApplicationLauncherButton applauncherButton;
        MainWindow mainWindow;
        InfoWindow infoWindow;
        ClientConnectingDialog clientConnectingDialog;
        ClientDisconnectDialog clientDisconnectDialog;

        /// <summary>
        /// The instance of the addon
        /// </summary>
        public static Addon Instance { get; private set; }

        static void Init ()
        {
            if (core == null) {
                core = Core.Instance;
                config = ConfigurationFile.Instance;
                foreach (var server in config.Configuration.Servers)
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
            Instance = this;

            var gameScene = GameScenesExtensions.CurrentGameScene();
            Service.CallContext.GameScene = gameScene;
            Utils.Logger.WriteLine ("Game scene switched to " + gameScene);

            // If a game is not loaded, ensure the server is stopped and then exit
            if (gameScene == Service.GameScene.None) {
                core.StopAll ();
                return;
            }

            // Auto-start the server, if required
            if (config.Configuration.AutoStartServers) {
                Utils.Logger.WriteLine ("Auto-starting server");
                try {
                    core.StartAll ();
                } catch (ServerException e) {
                    Utils.Logger.WriteLine ("Failed to auto-start servers:" + e, Utils.Logger.Severity.Error);
                }
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
            infoWindow.Visible = config.Configuration.InfoWindowVisible;
            infoWindow.Position = config.Configuration.InfoWindowPosition.ToRect ();

            // Main window
            mainWindow = gameObject.AddComponent<MainWindow> ();
            mainWindow.Visible = config.Configuration.MainWindowVisible;
            mainWindow.Position = config.Configuration.MainWindowPosition.ToRect ();
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
                    Utils.Logger.WriteLine ("Server exception: " + exn.Message, Utils.Logger.Severity.Error);
                    mainWindow.Errors.Add (exn.Message);
                }
            };
            mainWindow.OnStopServerPressed += (s, e) => {
                e.Server.Stop ();
                clientConnectingDialog.Close ();
            };
            mainWindow.OnHide += (s, e) => {
                config.Load ();
                config.Configuration.MainWindowVisible = false;
                config.Save ();
            };
            mainWindow.OnShow += (s, e) => {
                config.Load ();
                config.Configuration.MainWindowVisible = true;
                config.Save ();
            };
            mainWindow.OnStartMoving += (s, e) => {
                config.Load();
            };
            mainWindow.OnMoved += (s, e) => {
                var window = s as MainWindow;
                config.Configuration.MainWindowPosition = window.Position.ToTuple ();
            };
            mainWindow.OnFinishMoving += (s, e) => {
                config.Save();
            };

            // Info window events
            infoWindow.OnHide += (s, e) => {
                config.Load ();
                config.Configuration.InfoWindowVisible = false;
                config.Save ();
            };
            infoWindow.OnShow += (s, e) => {
                config.Load ();
                config.Configuration.InfoWindowVisible = true;
                config.Save ();
            };
            infoWindow.OnStartMoving += (s, e) => {
                config.Load();
            };
            infoWindow.OnMoved += (s, e) => {
                var window = s as InfoWindow;
                config.Configuration.InfoWindowPosition = window.Position.ToTuple ();
            };
            infoWindow.OnFinishMoving += (s, e) => {
                config.Save();
            };

            // Server events
            core.OnClientRequestingConnection += (s, e) => {
                if (config.Configuration.AutoAcceptConnections) {
                    Utils.Logger.WriteLine ("Auto-accepting client connection (" + e.Client.Address + ")");
                    e.Request.Allow ();
                } else {
                    Utils.Logger.WriteLine ("Asking player to accept client connection (" + e.Client.Address + ")");
                    clientConnectingDialog.OnClientRequestingConnection (s, e);
                }
            };

            // KSP events
            IsPaused = false;
            GameEvents.onGamePause.Add (() => {
                IsPaused = true;
            });
            GameEvents.onGameUnpause.Add (() => {
                IsPaused = false;
            });
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
            Destroy (mainWindow);
            Destroy (clientConnectingDialog);
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

        /// <summary>
        /// Whether the game is paused
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Trigger server update, when the game is paused
        /// </summary>
        public void Update()
        {
            if (IsPaused && !config.Configuration.PauseServerWithGame)
                FixedUpdate();
        }
    }
}
