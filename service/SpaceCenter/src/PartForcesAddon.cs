using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.Services.Parts;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon to apply forces to parts.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class PartForcesAddon : MonoBehaviour
    {
        /// <summary>
        /// The forces currently begin applied.
        /// </summary>
        static readonly IList<Force> forces = new List<Force> ();
        static readonly IList<Force> instantaneousForces = new List<Force> ();

        /// <summary>
        /// Destroy the addon
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void OnDestroy ()
        {
            forces.Clear ();
        }

        /// <summary>
        /// Add a force
        /// </summary>
        static internal void Add (Force force)
        {
            forces.Add (force);
        }

        /// <summary>
        /// Add an instantaneous force
        /// </summary>
        static internal void AddInstantaneous (Force force)
        {
            instantaneousForces.Add (force);
        }

        /// <summary>
        /// Add a force
        /// </summary>
        static internal void Remove (Force force)
        {
            forces.Remove (force);
        }

        /// <summary>
        /// Apply the forces to the parts
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void FixedUpdate ()
        {
            foreach (var force in instantaneousForces)
                force.Update ();
            instantaneousForces.Clear ();
            foreach (var force in forces)
                force.Update ();
        }
    }
}
