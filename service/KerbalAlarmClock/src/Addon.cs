using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace KRPC.KerbalAlarmClock
{
    /// <summary>
    /// kRPC KerbalAlarmClock addon.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class Addon : MonoBehaviour
    {
        /// <summary>
        /// Load the KerbalAlarmClock API.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void Start ()
        {
            KACWrapper.InitKACWrapper ();
        }
    }
}
