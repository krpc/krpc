using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Shared logic for the RPCs that split a vessel in two — decoupling and undocking — each of
    /// which returns the newly created vessel.
    /// </summary>
    static class PartSeparation
    {
        /// <summary>
        /// Number of frames to wait after triggering a separation before reading the resulting
        /// vessel. KSP does not finalise the split on the frame the event fires: for a few frames
        /// afterwards the new vessel may not yet be present in <c>FlightGlobals.Vessels</c>, and KSP
        /// can briefly change which vessel it treats as active. Ten frames is a conservative margin
        /// for that to settle.
        /// </summary>
        const int SettleFrames = 10;

        /// <summary>
        /// Yields until the separation has completed (<paramref name="separated"/> returns true) and
        /// the settle margin has elapsed, then returns the vessel that <c>FlightGlobals.Vessels</c>
        /// gained relative to <paramref name="preVesselIds"/> — the snapshot of vessel ids taken
        /// before the separation was triggered. A single separation produces exactly one new vessel.
        /// </summary>
        internal static Vessel NewVessel (IList<Guid> preVesselIds, Func<bool> separated, int wait = 0)
        {
            if (wait < SettleFrames || !separated ())
                throw new YieldException<Func<Vessel>> (
                    () => NewVessel (preVesselIds, separated, wait + 1));
            return new Vessel (
                FlightGlobals.Vessels.Select (v => v.id).Except (preVesselIds).Single ());
        }
    }
}
