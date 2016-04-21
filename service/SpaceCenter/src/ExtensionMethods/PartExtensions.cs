using System.Linq;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class PartExtensions
    {
        /// <summary>
        /// Returns true if the part contains the given part module
        /// </summary>
        public static bool HasModule<T> (this Part part) where T : PartModule
        {
            return part.Modules.OfType<T> ().Any ();
        }

        /// <summary>
        /// Returns the first part module of the specified type, and null if none can be found
        /// </summary>
        public static T Module<T> (this Part part) where T : PartModule
        {
            return part.Modules.OfType<T> ().FirstOrDefault ();
        }

        /// <summary>
        /// Returns true if the part contributes to the physics simulation (e.g. it has mass)
        /// </summary>
        public static bool IsPhysicallySignificant (this Part part)
        {
            return !part.HasModule<LaunchClamp> () && part.physicalSignificance != Part.PhysicalSignificance.NONE;
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

        /// <summary>
        /// Returns the total mass of the part and any resources it contains, in kg.
        /// </summary>
        public static float TotalMass (this Part part)
        {
            return (part.mass + part.GetResourceMass ()) * 1000;
        }

        /// <summary>
        /// Returns the total mass of the part, excluding any resources it contains, in kg.
        /// </summary>
        public static float DryMass (this Part part)
        {
            return part.mass * 1000;
        }
    }
}
