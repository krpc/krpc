using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class VesselExtensions
    {
        /// <summary>
        /// The angular velocity of the vessel in world space, in radians per second.
        /// The vessel behavior is attached to the root part's game object, so the
        /// vessel's rigidbody is the root part's, available from its cached rb field
        /// without a GetComponent lookup.
        /// </summary>
        internal static Vector3 WorldAngularVelocity (this global::Vessel vessel)
        {
            return vessel.rootPart.rb.angularVelocity;
        }

        /// <summary>
        /// The total mass of the vessel's parts, including resources, in kg.
        /// </summary>
        internal static float WetMass (this global::Vessel vessel)
        {
            return vessel.parts.Sum (part => part.WetMass ());
        }

        /// <summary>
        /// Activation stage numbers for the vessel, preferring stock
        /// delta-v data and falling back to staging icons when unavailable.
        /// </summary>
        internal static IList<int> ActivationStageNumbers (this global::Vessel vessel)
        {
            return StagingExtensions.ActivationStageNumbers (vessel.VesselDeltaV, vessel.Parts);
        }
    }
}
