using System.Collections.Generic;
using System.Linq;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class StagingExtensions
    {
        /// <summary>
        /// Activation stage numbers for a set of parts, preferring the stock delta-v
        /// data for them and falling back to staging icons when it is unavailable.
        /// Shared by vessels in flight and the vessel in the editor, which have the same
        /// staging but reach their delta-v data by different routes.
        /// </summary>
        internal static IList<int> ActivationStageNumbers (VesselDeltaV deltaV, IList<Part> parts)
        {
            if (deltaV != null && deltaV.IsReady)
                return deltaV.OperatingStageInfo
                    .Select (stage => stage.stage)
                    .Distinct ().OrderBy (n => n).ToList ();
            return parts
                .Where (part => part.hasStagingIcon)
                .Select (part => part.inverseStage)
                .Distinct ().OrderBy (n => n).ToList ();
        }
    }
}
