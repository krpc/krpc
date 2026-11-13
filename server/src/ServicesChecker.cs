using KRPC.Service;
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
        /// Whether kRPC can run. A service with errors leaves out only the members with them.
        /// Runs the check if the loading screen has not.
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
            ok = ServiceProblems.Load ();
        }
    }

    /// <summary>
    /// Shows the problems window at the first main menu of each launch, unless the player
    /// hid these problems.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.MainMenu, true)]
    public sealed class ServicesProblemsAddon : MonoBehaviour
    {
        /// <summary>
        /// Show the window, if there are problems the player has not hidden.
        /// </summary>
        public void Start ()
        {
            ServicesChecker.Run ();
            var digest = ServiceProblems.Digest;
            if (digest == null || digest == ConfigurationFile.Instance.Configuration.HiddenServiceProblems)
                return;
            var window = gameObject.AddComponent<UI.ProblemsWindow> ();
            window.Closable = true;
            window.Visible = true;
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
