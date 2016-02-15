using UnityEngine;

namespace KRPC.KerbalAlarmClock
{
    /// <summary>
    /// kRPC KAC wrapper.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class Addon : MonoBehaviour
    {
        /// <summary>
        /// Start the addon.
        /// </summary>
        public void Start ()
        {
            KACWrapper.InitKACWrapper ();
        }
    }
}
