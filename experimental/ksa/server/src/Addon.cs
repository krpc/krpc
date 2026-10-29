using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Brutal.ImGuiApi;
using Brutal.Logging;
using HarmonyLib;
using KRPC.KSA.UI;
using KRPC.Server;
using KRPC.Service;
using KSA;
using StarMap.API;
using Logger = KRPC.Utils.Logger;

namespace KRPC.KSA
{
    /// <summary>
    /// StarMap entry point for kRPC for KSA. Starts the server and runs it once per frame.
    /// All of the StarMap and game lifecycle code is in this file.
    /// </summary>
    [StarMapMod]
    public sealed class Addon
    {
        static readonly LogCategory log = LogCategory.Make ("kRPC");

        static Addon instance;

        readonly Harmony harmony = new Harmony ("kRPC");
        readonly string modDirectory;
        Core core;
        ConfigurationFile config;
        ClientRequestingConnectionEventArgs connectionRequest;
        // Whether the server was updated before the solver step of the current frame
        bool updatedThisFrame;
        double unpausedSpeed = 1.0;
        // The RPCs broken by the running game, when it is not the version the mod was built against
        IList<string> breakages;
        bool showCompatibilityWarning;

        /// <summary>
        /// Construct the addon. StarMap creates one instance.
        /// </summary>
        public Addon ()
        {
            instance = this;
            modDirectory = Path.GetDirectoryName (typeof (Addon).Assembly.Location);
        }

        /// <summary>
        /// Start the server, once the game has loaded every mod.
        /// </summary>
        [StarMapAllModsLoaded]
        public void OnAllModsLoaded ()
        {
            InitLogger ();
            LoadServiceAssemblies ();

            core = Core.Instance;
            var version = typeof (Addon).Assembly.GetName ().Version;
            core.Version = version.Major + "." + version.Minor + "." + version.Build;
            config = new ConfigurationFile (Path.Combine (modDirectory, "settings.toml"));
            config.Load ();
            foreach (var server in config.Configuration.Servers)
                core.Add (server.Create ());
            core.OnClientRequestingConnection += OnClientRequestingConnection;

            CallContext.GameScene = GameScene.None;
            CallContext.IsPaused = Universe.IsPaused;
            CallContext.Pause = Pause;
            CallContext.Unpause = Unpause;

            // The server runs on the main thread between the frame's vehicle solver results
            // being applied and the next solver step being queued. Vehicle state is settled
            // there, and control inputs written there feed the next step
            var tick = typeof (Universe).GetMethod (
                nameof (Universe.ExecuteNextVehicleSolvers), BindingFlags.Public | BindingFlags.Static);
            if (tick == null)
                Logger.WriteLine ("Universe.ExecuteNextVehicleSolvers not found, updating after each frame instead", Logger.Severity.Error);
            else
                harmony.Patch (tick, prefix: new HarmonyMethod (
                    typeof (Addon).GetMethod (nameof (BeforeVehicleSolvers), BindingFlags.NonPublic | BindingFlags.Static)));

            // On .NET, closing a TcpClient sets its socket to null, so KRPC.Core's connected check
            // throws for a client that disconnected during a call that yields. We treat it as
            // disconnected, as the check does for a disposed socket on Mono
            var connected = AccessTools.PropertyGetter (AccessTools.TypeByName ("KRPC.Server.TCP.TCPClient"), "Connected");
            if (connected == null)
                Logger.WriteLine ("TCPClient.Connected not found, a client that disconnects during a call may stop the server", Logger.Severity.Error);
            else
                harmony.Patch (connected, finalizer: new HarmonyMethod (
                    typeof (Addon).GetMethod (nameof (TCPClientConnectedFinalizer), BindingFlags.NonPublic | BindingFlags.Static)));

            // A save load replaces every vehicle, which the core counts as a new game state
            var deserialize = typeof (Universe).GetMethod (
                nameof (Universe.DeserializeSave), BindingFlags.Public | BindingFlags.Static);
            if (deserialize == null)
                Logger.WriteLine ("Universe.DeserializeSave not found, save loads are not counted", Logger.Severity.Error);
            else
                harmony.Patch (deserialize, postfix: new HarmonyMethod (
                    typeof (Addon).GetMethod (nameof (AfterSaveLoaded), BindingFlags.NonPublic | BindingFlags.Static)));

            MenuBar.Patch (harmony, this);
            ConsoleWindow.Patch (harmony);

            if (config.Configuration.AutoStartServers) {
                Logger.WriteLine ("Auto-starting server");
                try {
                    core.StartAll ();
                } catch (ServerException e) {
                    Logger.WriteLine ("Failed to auto-start servers: " + e, Logger.Severity.Error);
                }
            }

            CheckGameVersion ();
        }

        /// <summary>
        /// Find the RPCs broken by the running game, when it is not the version the mod was
        /// built against.
        /// </summary>
        void CheckGameVersion ()
        {
            var running = GameCompatibility.Running;
            if (running == GameCompatibility.BuiltFor)
                return;
            Logger.WriteLine ("Built for KSA " + GameCompatibility.BuiltFor + ", running on KSA " + running, Logger.Severity.Warning);
            try {
                breakages = GameCompatibility.FindBreakages ();
                Logger.WriteLine (breakages.Count + " RPCs use game members missing from KSA " + running, Logger.Severity.Warning);
            } catch (Exception e) {
                Logger.WriteLine ("Failed to check the RPCs against the game: " + e, Logger.Severity.Error);
                breakages = new List<string> ();
            }
            showCompatibilityWarning = config.IgnoredGameVersion != running.ToString ();
        }

        // In a method of its own, so that a popup broken by the running game fails only here
        [MethodImpl (MethodImplOptions.NoInlining)]
        void ShowCompatibilityPopup ()
        {
            new CompatibilityPopup (typeof (Addon).Assembly.GetName ().Version, breakages, () => {
                config.IgnoredGameVersion = GameCompatibility.Running.ToString ();
                config.Save ();
            });
        }

        /// <summary>
        /// The server.
        /// </summary>
        internal Core Core {
            get { return core; }
        }

        /// <summary>
        /// The kRPC window, created on the first UI frame.
        /// </summary>
        internal MainWindow MainWindow { get; private set; }

        /// <summary>
        /// The server statistics window, created on the first UI frame.
        /// </summary>
        internal InfoWindow InfoWindow { get; private set; }

        PromptPopup connectingPrompt;

        /// <summary>
        /// Create the windows, which need the game's UI to be up, and save their visibility,
        /// position and size when they change.
        /// </summary>
        [StarMapBeforeGui]
        public void OnBeforeGui (double dt)
        {
            if (core == null)
                return;
            var c = config.Configuration;
            if (MainWindow == null) {
                InfoWindow = new InfoWindow (core, c.InfoWindowVisible, c.InfoWindowPosition);
                MainWindow = new MainWindow (this, core, config);
                if (showCompatibilityWarning) {
                    try {
                        ShowCompatibilityPopup ();
                    } catch (Exception e) {
                        Logger.WriteLine ("Failed to show the game version warning: " + e, Logger.Severity.Error);
                    }
                }
            }
            // Window rectangles are saved once a move or resize has finished
            var moving = ImGui.IsMouseDown (ImGuiMouseButton.Left);
            if (MainWindow.IsShown != c.MainWindowVisible || InfoWindow.IsShown != c.InfoWindowVisible ||
                (!moving && (!MainWindow.Rect.Equals (c.MainWindowPosition) || !InfoWindow.Rect.Equals (c.InfoWindowPosition)))) {
                c.MainWindowVisible = MainWindow.IsShown;
                c.InfoWindowVisible = InfoWindow.IsShown;
                c.MainWindowPosition = MainWindow.Rect;
                c.InfoWindowPosition = InfoWindow.Rect;
                config.Save ();
            }
        }

        /// <summary>
        /// Start a server, reporting a failure in the kRPC window.
        /// </summary>
        internal void StartServer (Server.Server server)
        {
            try {
                server.Start ();
            } catch (ServerException e) {
                Logger.WriteLine ("Server exception: " + e.Message, Logger.Severity.Error);
                MainWindow?.Errors.Add (e.Message);
            }
        }

        /// <summary>
        /// Stop a server.
        /// </summary>
        internal void StopServer (Server.Server server)
        {
            server.Stop ();
            if (!core.AnyRunning)
                CloseConnectionRequest ();
        }

        /// <summary>
        /// Start every server that is not running.
        /// </summary>
        internal void StartAllServers ()
        {
            MainWindow?.Errors.Clear ();
            foreach (var server in core.Servers) {
                if (!server.Running)
                    StartServer (server);
            }
        }

        /// <summary>
        /// Stop every server.
        /// </summary>
        internal void StopAllServers ()
        {
            foreach (var server in core.Servers) {
                if (server.Running)
                    StopServer (server);
            }
        }

        /// <summary>
        /// Ask the player to confirm disconnecting a client.
        /// </summary>
        internal void ConfirmDisconnect (IClient client)
        {
            var name = client.Name;
            new PromptPopup (
                "Are you sure you want to disconnect " + (name.Length == 0 ? "the client" : "'" + name + "'") + "?",
                new [] {
                    new KeyValuePair<string, Action> ("Yes", client.Close),
                    new KeyValuePair<string, Action> ("Yes (don't ask again)", () => {
                        config.Configuration.ConfirmRemoveClient = false;
                        config.Save ();
                        client.Close ();
                    }),
                    new KeyValuePair<string, Action> ("No", () => { })
                });
        }

        /// <summary>
        /// Update the server after a frame that did not reach the solver step.
        /// </summary>
        [StarMapAfterOnFrame]
        public void OnAfterFrame (double currentPlayerTime, double dtPlayer)
        {
            if (!updatedThisFrame)
                Update ();
            updatedThisFrame = false;
            Utils.AfterFrame.RunAll ();
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        [StarMapUnload]
        public void OnUnload ()
        {
            harmony.UnpatchAll (harmony.Id);
            if (core != null)
                core.StopAll ();
        }

        static Exception TCPClientConnectedFinalizer (Exception __exception, ref bool __result)
        {
            if (__exception is not NullReferenceException)
                return __exception;
            __result = false;
            return null;
        }

        static void AfterSaveLoaded ()
        {
            GameState.Changed ();
        }

        static void BeforeVehicleSolvers ()
        {
            instance.updatedThisFrame = true;
            instance.Update ();
        }

        void Update ()
        {
            if (core == null || !core.AnyRunning)
                return;
            if (config.Configuration.PauseServerWithGame && Universe.IsPaused ())
                return;
            CallContext.GameScene =
                Universe.CurrentSystem != null && Program.ControlledVehicle != null
                ? GameScene.Flight : GameScene.None;
            try {
                Utils.ObjectSweep.Update ();
                core.Update ();
            } catch (Exception e) {
                // An exception escaping the patch would take the game's frame down with it
                Logger.WriteLine ("Server update failed: " + e, Logger.Severity.Error);
            }
        }

        void OnClientRequestingConnection (object sender, ClientRequestingConnectionEventArgs e)
        {
            if (config.Configuration.AutoAcceptConnections) {
                Logger.WriteLine ("Accepting client connection (" + e.Client.Address + ")");
                e.Request.Allow ();
                return;
            }
            // The core raises the event on every update until the request is answered. The
            // player answers the stored request, and the answer is passed on to the next event
            // Another client waits for the stored request, unless that request was answered
            // for a client that has since gone
            if (connectionRequest != null && connectionRequest.Client != e.Client) {
                if (connectionRequest.Request.StillPending)
                    return;
                connectionRequest = null;
            }
            if (connectionRequest == null) {
                Logger.WriteLine ("Asking the player to allow or deny a client connection (" + e.Client.Address + ")");
                connectionRequest = e;
                var request = e.Request;
                var name = e.Client.Name;
                connectingPrompt = new PromptPopup (
                    (name.Length == 0 ? "A client" : "'" + name + "'") + " is attempting to connect from " + e.Client.Address,
                    new [] {
                        new KeyValuePair<string, Action> ("Allow", request.Allow),
                        new KeyValuePair<string, Action> ("Allow (don't ask again)", () => {
                            config.Configuration.AutoAcceptConnections = true;
                            config.Save ();
                            request.Allow ();
                        }),
                        new KeyValuePair<string, Action> ("Deny", request.Deny)
                    });
            } else if (!connectionRequest.Request.StillPending) {
                if (connectionRequest.Request.ShouldAllow)
                    e.Request.Allow ();
                else
                    e.Request.Deny ();
                CloseConnectionRequest ();
            }
        }

        void CloseConnectionRequest ()
        {
            connectionRequest = null;
            if (connectingPrompt != null)
                connectingPrompt.Active = false;
            connectingPrompt = null;
        }

        void Pause ()
        {
            if (!Universe.IsPaused ())
                unpausedSpeed = Universe.SimulationSpeed;
            Universe.SetSimulationSpeed (0.0, alert: false);
        }

        void Unpause ()
        {
            Universe.SetSimulationSpeed (unpausedSpeed);
        }

        static void InitLogger ()
        {
            Logger.Writer = (message, severity) => {
                switch (severity) {
                case Logger.Severity.Error:
                    log.Error ($"{message}");
                    break;
                case Logger.Severity.Warning:
                    log.Warning ($"{message}");
                    break;
                case Logger.Severity.Debug:
                    log.Debug ($"{message}");
                    break;
                default:
                    log.Info ($"{message}");
                    break;
                }
            };
        }

        /// <summary>
        /// Load the service assemblies in the mod folder, so that the service scan finds them.
        /// </summary>
        /// <remarks>
        /// StarMap loads only the entry assembly. The services are loaded into its load
        /// context, which also resolves their dependencies from the mod folder.
        /// </remarks>
        void LoadServiceAssemblies ()
        {
            var context = AssemblyLoadContext.GetLoadContext (typeof (Addon).Assembly);
            context.Resolving += (ctx, name) => {
                var path = Path.Combine (modDirectory, name.Name + ".dll");
                return File.Exists (path) ? ctx.LoadFromAssemblyPath (path) : null;
            };
            var self = typeof (Addon).Assembly.GetName ().Name;
            foreach (var path in Directory.GetFiles (modDirectory, "KRPC.KSA.*.dll")) {
                var name = Path.GetFileNameWithoutExtension (path);
                if (name == self)
                    continue;
                var assembly = context.LoadFromAssemblyPath (path);
                Logger.WriteLine ("Loaded " + assembly.GetName ().Name);
            }
        }
    }
}
