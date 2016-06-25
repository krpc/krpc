using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class PartExtensions
    {
        /// <summary>
        /// Returns true if the part contains the given part module
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidMethodWithUnusedGenericTypeRule")]
        public static bool HasModule<T> (this Part part) where T : PartModule
        {
            return part.Modules.OfType<T> ().Any ();
        }

        /// <summary>
        /// Returns the first part module of the specified type, and null if none can be found
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidMethodWithUnusedGenericTypeRule")]
        public static T Module<T> (this Part part) where T : PartModule
        {
            return part.Modules.OfType<T> ().FirstOrDefault ();
        }

        /// <summary>
        /// Returns true if the part is massless
        /// </summary>
        public static bool IsMassless (this Part part)
        {
            return part.physicalSignificance == Part.PhysicalSignificance.NONE || part.HasModule<LaunchClamp> ();
        }

        /// <summary>
        /// The mass of the part, including resources, in kg.
        /// </summary>
        public static float WetMass (this Part part)
        {
            return !part.IsMassless () && part.rb != null ? part.rb.mass * 1000f : 0f;
        }

        /// <summary>
        /// The mass of the part, excluding resources.
        /// </summary>
        public static float DryMass (this Part part)
        {
            return !part.IsMassless () && part.rb != null ? Mathf.Max (0f, (part.rb.mass - part.resourceMass) * 1000f) : 0f;
        }

        /// <summary>
        /// Returns the index of the stage in which the part will be decoupled,
        /// or 0 if it is never decoupled.
        /// </summary>
        public static int DecoupledAt (this Part part)
        {
            do {
                if (part.HasModule<ModuleDecouple> () || part.HasModule<ModuleAnchoredDecoupler> () || part.HasModule<LaunchClamp> ())
                    return part.inverseStage;
                part = part.parent;
            } while (part != null);
            return -1;
        }

        /// <summary>
        /// Returns the position in world space of the center of mass of the part, or the parts transform position if it has no mass.
        /// </summary>
        public static Vector3d CenterOfMass (this Part part)
        {
            return part.rb != null ? part.rb.worldCenterOfMass : part.transform.position;
        }
    }
}
