using UnityEngine;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The stock delta-v simulation for the vessel under construction, and whether its
    /// figures are current.
    /// </summary>
    static class EditorDeltaV
    {
        /// <summary>
        /// The fixed time at which the simulation was last asked to re-run. The game
        /// does not act on the request, or drop its own ready flag, until the next fixed
        /// update, so until the clock has moved past this the flag still describes the
        /// figures from before the request.
        /// </summary>
        static double requestedAt = double.NegativeInfinity;

        /// <summary>
        /// The simulation for the vessel in the editor, or null when there is none.
        /// </summary>
        internal static VesselDeltaV Simulation {
            get {
                var ship = EditorExtensions.Ship;
                return ReferenceEquals (ship, null) ? null : ship.vesselDeltaV;
            }
        }

        /// <summary>
        /// Ask the game to re-run the simulation, and record when, so that the figures
        /// are reported as out of date until it has.
        /// </summary>
        internal static void Recalculate ()
        {
            var simulation = Simulation;
            if (simulation == null)
                return;
            simulation.SetCalcsDirty (true);
            requestedAt = Time.fixedTime;
        }

        /// <summary>
        /// Whether the figures are current: the game says they are ready, it is not
        /// re-running the simulation, and any re-run asked for here has been picked up.
        /// </summary>
        internal static bool Ready {
            get {
                var simulation = Simulation;
                return simulation != null && simulation.IsReady &&
                       !simulation.SimulationRunning && Time.fixedTime > requestedAt;
            }
        }
    }
}
