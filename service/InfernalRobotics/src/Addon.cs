using UnityEngine;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// kRPC InfernalRobotics addon.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class Addon : MonoBehaviour
    {
        /// <summary>
        /// Load the InfernalRobotics API.
        /// </summary>
        public void Start ()
        {
            IRWrapper.InitWrapper ();
        }
    }
}
