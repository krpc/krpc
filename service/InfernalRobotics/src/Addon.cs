using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// kRPC InfernalRobotics addon.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class Addon : MonoBehaviour
    {
        /// <summary>
        /// Load the InfernalRobotics API.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void Start ()
        {
            IRWrapper.InitWrapper ();
        }
    }
}
