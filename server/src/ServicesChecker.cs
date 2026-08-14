using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Scanner;
using UnityEngine;

namespace KRPC
{
    /// <summary>
    /// Check kRPC services.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Instantly, true)]
    public sealed class ServicesChecker : MonoBehaviour
    {
        internal static bool OK { get; private set; }

        /// <summary>
        /// Start the services checker addon
        /// </summary>
        public void Start ()
        {
            Addon.InitLogger ();
            OK = true;
            var errors = new List<string>();
            Scanner.GetServices (errors);
            if (errors.Any()) {
                OK = false;
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
}
