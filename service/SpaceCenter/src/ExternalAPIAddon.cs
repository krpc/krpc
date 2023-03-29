using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon to load external APIs.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class ExternalAPIAddon : MonoBehaviour
    {
        /// <summary>
        /// Load external APIs.
        /// </summary>
        public void Start ()
        {
            ExternalAPI.AGX.Load ();
            ExternalAPI.FAR.Load ();
            ExternalAPI.RemoteTech.Load ();
        }
    }
}
