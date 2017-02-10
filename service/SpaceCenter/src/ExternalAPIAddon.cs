using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void Start ()
        {
            ExternalAPI.AGX.Load ();
            ExternalAPI.FAR.Load ();
            ExternalAPI.RemoteTech.Load ();
        }
    }
}
