using System;
using System.Collections.Generic;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon that holds the physics ranges requested through
    /// <see cref="Services.Vessel.PhysicsRange"/> and keeps them applied.
    /// </summary>
    /// <remarks>
    /// KSP assigns <c>Vessel.vesselRanges</c> only in <c>Vessel.Awake</c>, copying
    /// <c>PhysicsGlobals.VesselRangesDefault</c>, and never saves it. A vessel rebuilt from its
    /// protovessel — after a scene change, or when it comes back into loading range — therefore
    /// starts again at the stock ranges. The requested range is held here, keyed by vessel id so
    /// that it outlives the vessel object, and re-applied so it survives those rebuilds.
    ///
    /// Overrides are deliberately not owned by the client that set them and are not released on
    /// disconnect: dropping a distant vessel back to the stock bubble because a script exited
    /// would pack it onto rails mid-flight, which is a worse outcome than an override outliving
    /// its script. A client releases an override by setting the range to zero; all of them are
    /// discarded when the game exits.
    /// </remarks>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class VesselPhysicsRangeAddon : MonoBehaviour
    {
        sealed class Override
        {
            public float Range;
            public VesselRanges Original;
        }

        /// <summary>
        /// The fraction of the requested range at which a vessel is loaded and unpacked. The
        /// gap between this and the range itself is the hysteresis that stops a vessel sitting
        /// on the threshold from flapping between states; the ratio matches the stock
        /// load-to-unload ratio of 2250/2500.
        /// </summary>
        const float hysteresis = 0.9f;

        static readonly IDictionary<Guid, Override> overrides = new Dictionary<Guid, Override> ();

        /// <summary>
        /// The distance beyond which a vessel stops running physics, for its current situation.
        /// </summary>
        /// <remarks>
        /// A vessel is put on rails when it is further away than <c>pack</c>, and also when it is
        /// further away than <c>unload</c>, because unloading forces it on rails. The smaller of
        /// the two is therefore the distance out to which it keeps simulating. This reads the
        /// vessel's live ranges rather than any requested value, so it reports what the game will
        /// actually do.
        /// </remarks>
        internal static float Get (global::Vessel vessel)
        {
            var ranges = vessel.vesselRanges.GetSituationRanges (vessel.situation);
            return Math.Min (ranges.pack, ranges.unload);
        }

        /// <summary>
        /// Request a physics range for a vessel, applying it immediately. A range of zero or
        /// less drops the request and restores the ranges the vessel had when it was made.
        /// </summary>
        internal static void Set (global::Vessel vessel, float range)
        {
            Override entry;
            var exists = overrides.TryGetValue (vessel.id, out entry);
            if (range <= 0) {
                if (exists) {
                    Restore (vessel, entry.Original);
                    overrides.Remove (vessel.id);
                }
                return;
            }
            if (!exists) {
                entry = new Override { Original = new VesselRanges (vessel.vesselRanges) };
                overrides [vessel.id] = entry;
            }
            entry.Range = range;
            Apply (vessel, range);
        }

        /// <summary>
        /// Re-apply the requested ranges, so that they survive a vessel being rebuilt from its
        /// protovessel — on entering the scene, or on coming back into loading range.
        /// </summary>
        public void FixedUpdate ()
        {
            ApplyAll ();
        }

        static void ApplyAll ()
        {
            if (overrides.Count == 0 || !FlightGlobals.ready)
                return;
            foreach (var vessel in FlightGlobals.Vessels) {
                Override entry;
                if (overrides.TryGetValue (vessel.id, out entry))
                    Apply (vessel, entry.Range);
            }
        }

        static void Apply (global::Vessel vessel, float range)
        {
            var ranges = vessel.vesselRanges;
            Apply (ranges.prelaunch, range);
            Apply (ranges.landed, range);
            Apply (ranges.splashed, range);
            Apply (ranges.flying, range);
            Apply (ranges.subOrbital, range);
            Apply (ranges.orbit, range);
            Apply (ranges.escaping, range);
        }

        static void Apply (VesselRanges.Situation situation, float range)
        {
            situation.unload = range;
            situation.pack = range;
            situation.load = range * hysteresis;
            situation.unpack = range * hysteresis;
        }

        static void Restore (global::Vessel vessel, VesselRanges original)
        {
            var ranges = vessel.vesselRanges;
            Restore (ranges.prelaunch, original.prelaunch);
            Restore (ranges.landed, original.landed);
            Restore (ranges.splashed, original.splashed);
            Restore (ranges.flying, original.flying);
            Restore (ranges.subOrbital, original.subOrbital);
            Restore (ranges.orbit, original.orbit);
            Restore (ranges.escaping, original.escaping);
        }

        static void Restore (VesselRanges.Situation situation, VesselRanges.Situation original)
        {
            situation.load = original.load;
            situation.unload = original.unload;
            situation.pack = original.pack;
            situation.unpack = original.unpack;
        }
    }
}
