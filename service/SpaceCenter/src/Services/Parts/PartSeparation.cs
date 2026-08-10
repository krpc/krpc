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
        /// before the separation was triggered.
        ///
        /// A separation usually produces one new vessel, but an omni-decoupler detaches at every
        /// node at once: it separates the vessel beyond it and is itself left on a vessel of its
        /// own, so two appear. The one to return is the vessel that was separated, which is the one
        /// <paramref name="part"/> — the decoupler or docking port that was fired — did not end up
        /// on. It is looked up once the wait is over, and by identifier, so that it resolves to
        /// the right object however the separation rearranged the parts in the meantime.
        /// </summary>
        internal static Vessel NewVessel (Part part, IList<Guid> preVesselIds, Func<bool> separated, int wait = 0)
        {
            if (wait < SettleFrames || !separated ())
                throw new YieldException<Func<Vessel>> (
                    () => NewVessel (part, preVesselIds, separated, wait + 1));
            var newVessels = FlightGlobals.Vessels
                .Where (vessel => !preVesselIds.Contains (vessel.id)).ToList ();
            if (newVessels.Count == 1)
                return new Vessel (newVessels [0].id);
            var partVessel = part.InternalPart.vessel;
            var separatedVessels = newVessels.Where (vessel => vessel != partVessel).ToList ();
            if (separatedVessels.Count != 1)
                throw new InvalidOperationException (
                    "The separation produced " + newVessels.Count + " new vessels, and which of " +
                    "them was separated is ambiguous");
            return new Vessel (separatedVessels [0].id);
        }
    }
}
