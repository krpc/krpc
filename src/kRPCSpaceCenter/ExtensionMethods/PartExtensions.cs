using System.Linq;

namespace KRPCSpaceCenter.ExtensionMethods
{
    public static class PartExtensions
    {
        /// <summary>
        /// Returns true if the part contains the given part module
        /// <summary>
        public static bool HasModule<T> (this Part part) where T : PartModule
        {
            return part.Modules.OfType<T> ().Any ();
        }

        /// <summary>
        /// Returns true if the part contributes to the physics simulation (e.g. it has mass)
        /// <summary>
        public static bool IsPhysicallySignificant (this Part part)
        {
            return (!part.HasModule<ModuleLandingGear> ()) &&
            (!part.HasModule<LaunchClamp> ()) &&
            (part.physicalSignificance != Part.PhysicalSignificance.NONE);
        }

        /// <summary>
        /// Returns the index of the stage in which the part will be decoupled,
        /// or 0 if it is never decoupled.
        /// <summary>
        public static int DecoupledAt (this Part part)
        {
            do {
                if (part.IsDecoupler ())
                    return part.inverseStage;
                part = part.parent;
            } while (part != null);
            return -1;
        }

        /// <summary>
        /// Returns true if the part is a decoupler.
        /// <summary>
        public static bool IsDecoupler (this Part part)
        {
            return part.HasModule<ModuleDecouple> () || part.HasModule<ModuleAnchoredDecoupler> ();
        }

        /// <summary>
        /// Returns the total mass of the part and any resources it contains, in kg.
        /// <summary>
        public static float TotalMass (this Part part)
        {
            return (part.mass + part.GetResourceMass ()) * 1000;
        }

        /// <summary>
        /// Returns the total mass of the part, excluding any resources it contains, in kg.
        /// <summary>
        public static float DryMass (this Part part)
        {
            return part.mass * 1000;
        }
    }
}
