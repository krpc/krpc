using UnityEngine;

namespace KRPC.RemoteTech
{
    /// <summary>
    /// kRPC RemoteTech addon.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class Addon : MonoBehaviour
    {
        /// <summary>
        /// Load the RemoteTech API.
        /// </summary>
        public void Start ()
        {
            API.Load ();
        }
    }
}
