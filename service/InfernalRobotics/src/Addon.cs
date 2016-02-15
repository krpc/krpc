using UnityEngine;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// kRPC InfernalRobotics wrapper.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class Addon : MonoBehaviour
    {
        /// <summary>
        /// Start the addon.
        /// </summary>
        public void Start ()
        {
            IRWrapper.InitWrapper ();
        }
    }
}
