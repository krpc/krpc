using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace KRPC.RemoteTech
{
    /// <summary>
    /// kRPC RemoteTech addon.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
    public sealed class Addon : MonoBehaviour
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
