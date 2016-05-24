using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon to load external APIs.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class Addon : MonoBehaviour
    {
        /// <summary>
        /// Load external APIs.
        /// </summary>
        public void Start ()
        {
            ExternalAPI.FAR.Load ();
            ExternalAPI.RemoteTech.Load ();
        }
    }
}
