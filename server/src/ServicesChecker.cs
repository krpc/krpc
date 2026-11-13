using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Scanner;
using UnityEngine;

namespace KRPC
{
    /// <summary>
    /// Binds the mod integrations and checks the kRPC services, once. The loading screen runs
    /// the check as its last step, after each mod has set up and the parts have loaded.
    /// </summary>
    static class ServicesChecker
    {
        static bool? ok;

        /// <summary>
        /// Whether the services are valid. Runs the check if the loading screen has not.
        /// </summary>
        internal static bool OK {
            get {
                Run ();
                return ok.Value;
            }
        }

        /// <summary>
        /// Run the check, if it has not run.
        /// </summary>
        internal static void Run ()
        {
            if (ok.HasValue)
                return;
            Addon.InitLogger ();
            Utils.ModAPI.LoadAll (new Utils.GameModAssemblies ());
            var errors = new List<string>();
            Scanner.GetServices (errors);
            ok = !errors.Any ();
            if (errors.Any()) {
                Utils.Logger.WriteLine("Service errors encountered, plugin has been disabled. Errors were:", Utils.Logger.Severity.Error);
                foreach (var error in errors)
                    Utils.Logger.WriteLine(error, Utils.Logger.Severity.Error);
                PopupDialog.SpawnPopupDialog(
                    new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), "krpc-service-error", "kRPC Service Error",
                    "Service errors encountered, plugin has been disabled. See the log for more information.",
                    "OK", true, HighLogic.UISkin);
            }
        }
    }

    /// <summary>
    /// The loading screen step that runs <see cref="ServicesChecker"/>.
    /// </summary>
    sealed class ServicesCheckerLoadingStep : LoadingSystem
    {
        bool ready;

        public override bool IsReady ()
        {
            return ready;
        }

        public override string ProgressTitle ()
        {
            return "kRPC";
        }

        public override float ProgressFraction ()
        {
            return ready ? 1f : 0f;
        }

        public override void StartLoad ()
        {
            ServicesChecker.Run ();
            ready = true;
        }
    }

    /// <summary>
    /// Adds <see cref="ServicesCheckerLoadingStep"/> to the end of the loading screen's steps.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Instantly, true)]
    public sealed class ServicesCheckerAddon : MonoBehaviour
    {
        /// <summary>
        /// Add the step. Without a loading screen, the first read of
        /// <see cref="ServicesChecker.OK"/> runs the check.
        /// </summary>
        public void Awake ()
        {
            if (LoadingScreen.Instance != null)
                LoadingScreen.Instance.loaders.Add (gameObject.AddComponent<ServicesCheckerLoadingStep> ());
        }
    }
}
