using UnityEngine;

namespace KRPC.DockingCamera
{
    /// <summary>
    /// kRPC Camera addon.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class Addon : MonoBehaviour
    {
        /// <summary>
        /// Load the Camera API.
        /// </summary>
        public void Start()
        {
            API.Load();
        }
    }
}
