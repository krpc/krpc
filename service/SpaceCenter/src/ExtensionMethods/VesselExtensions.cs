using System.Collections.Generic;
using System.Linq;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class VesselExtensions
    {
        /// <summary>
        /// Activation stage numbers for the vessel, preferring stock
        /// delta-v data and falling back to staging icons when unavailable.
        /// </summary>
        internal static IList<int> ActivationStageNumbers (this global::Vessel vessel)
        {
            var dv = vessel.VesselDeltaV;
            if (dv != null && dv.IsReady)
                return dv.OperatingStageInfo
                    .Select (stage => stage.stage)
                    .Distinct ().OrderBy (n => n).ToList ();
            return vessel.Parts
                .Where (part => part.hasStagingIcon)
                .Select (part => part.inverseStage)
                .Distinct ().OrderBy (n => n).ToList ();
        }
    }
}
