using UnityEngine;

namespace KRPC.LiDAR
{
    /// <summary>
    /// kRPC LiDAR addon.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class Addon : MonoBehaviour
    {
        /// <summary>
        /// Load the LiDAR API.
        /// </summary>
        public void Start()
        {
            API.Load();
        }
    }
}