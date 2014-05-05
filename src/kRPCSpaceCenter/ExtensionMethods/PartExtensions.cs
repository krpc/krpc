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
            if (part.Modules.OfType<ModuleDecouple> ().Any () || part.Modules.OfType<ModuleAnchoredDecoupler> ().Any ())
                return part.inverseStage;
            else if (part.parent != null)
                return part.parent.DecoupledAt ();
            else
                return -1;
        }
    }
}
