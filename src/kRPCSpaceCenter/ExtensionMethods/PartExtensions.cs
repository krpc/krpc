using System;
using System.Linq;

namespace KRPCSpaceCenter.ExtensionMethods
{
    public static class PartExtensions
    {
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

        public static bool IsDecoupler (this Part part)
        {
            return part.Modules.OfType<ModuleDecouple> ().Any () || part.Modules.OfType<ModuleAnchoredDecoupler> ().Any ();
        }
    }
}
