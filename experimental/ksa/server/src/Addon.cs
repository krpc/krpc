using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Brutal.Logging;
using HarmonyLib;
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
        static readonly Queue<Action> afterFrame = new Queue<Action> ();

        readonly Harmony harmony = new Harmony ("kRPC.KSA");
        readonly string modDirectory;
        Core core;
        ConfigurationFile config;
        // Whether the server was updated before the solver step of the current frame
        bool updatedThisFrame;
        double unpausedSpeed = 1.0;

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
            config = ConfigurationFile.Load (Path.Combine (modDirectory, "settings.toml"));
            foreach (var server in config.Servers)
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

            if (config.AutoStartServers) {
                Logger.WriteLine ("Auto-starting server");
                try {
                    core.StartAll ();
                } catch (ServerException e) {
                    Logger.WriteLine ("Failed to auto-start servers: " + e, Logger.Severity.Error);
                }
            }
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
            while (afterFrame.Count > 0) {
                try {
                    afterFrame.Dequeue () ();
                } catch (Exception e) {
                    Logger.WriteLine ("After frame action failed: " + e, Logger.Severity.Error);
                }
            }
        }

        /// <summary>
        /// Run an action on the main thread at the end of the current frame. The game's menus
        /// load saves at that point, after the vehicle solver step has been queued.
        /// </summary>
        public static void RunAfterFrame (Action action)
        {
            afterFrame.Enqueue (action);
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

        static void BeforeVehicleSolvers ()
        {
            instance.updatedThisFrame = true;
            instance.Update ();
        }

        void Update ()
        {
            if (core == null || !core.AnyRunning)
                return;
            CallContext.GameScene =
                Universe.CurrentSystem != null && Program.ControlledVehicle != null
                ? GameScene.Flight : GameScene.None;
            try {
                core.Update ();
            } catch (Exception e) {
                // An exception escaping the patch would take the game's frame down with it
                Logger.WriteLine ("Server update failed: " + e, Logger.Severity.Error);
            }
        }

        void OnClientRequestingConnection (object sender, ClientRequestingConnectionEventArgs e)
        {
            if (config.AutoAcceptConnections) {
                Logger.WriteLine ("Accepting client connection (" + e.Client.Address + ")");
                e.Request.Allow ();
            } else {
                Logger.WriteLine ("Denying client connection (" + e.Client.Address + "), auto_accept_connections is off");
                e.Request.Deny ();
            }
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
