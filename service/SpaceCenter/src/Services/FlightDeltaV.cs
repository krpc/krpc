using System;
using System.Linq;
using KRPC.Service;
using KRPC.SpaceCenter.ExtensionMethods;
using UnityEngine;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The stock delta-v simulation for a vessel in flight, and the means to ask the game
    /// to run it.
    /// </summary>
    /// <remarks>
    /// The game runs the simulation for the active vessel alone, and only on a tick that
    /// finds the vessel's figures marked out of date. It clears that mark before it
    /// decides it has no engines to work from, and marks them again only when the vessel
    /// changes.
    /// A first run that lands before the vessel's staging is built is therefore dropped,
    /// and the vessel is left without figures for as long as it is flown. Asking for
    /// another run is what recovers it.
    /// </remarks>
    static class FlightDeltaV
    {
        /// <summary>
        /// Ticks to wait for a run before giving up. The game holds a run asked for by a
        /// vessel event back by <c>DELTAV_VESSEL_EVENT_DELAY_SECS</c>, one second in the
        /// stock settings, and then spends a rendered frame per stage on it.
        /// </summary>
        const int WaitTicks = 500;

        /// <summary>
        /// Ticks to leave the game to run the simulation by itself before asking for one.
        /// It holds a run back until the vessel has settled after an event, and figures
        /// taken from a vessel still settling differ from the ones it comes to rest with.
        /// Asking straight away would report those instead of the game's own.
        /// </summary>
        const int GraceTicks = 150;

        /// <summary>
        /// The vessel a run was last asked for, and the fixed time it was asked at. The
        /// game does not act on the request, or drop its own ready flag, until the next
        /// fixed update, so until the clock has moved past this the flag still describes
        /// the figures from before the request. One record covers every vessel, as only
        /// the active vessel is ever run.
        /// </summary>
        static Guid requestedFor;
        static double requestedAt = double.NegativeInfinity;

        /// <summary>
        /// Whether the vessel carries an engine the simulation counts. The game leaves the
        /// ready flag down for a vessel without one, however many runs it is given, and
        /// the zeros it computed are all there is to report.
        /// </summary>
        static bool HasEngines (global::Vessel vessel)
        {
            return vessel.parts.Any (
                part => part.Modules.OfType<ModuleEngines> ()
                    .Any (engine => engine.includeinDVCalcs && !engine.nonThrustMotor));
        }

        /// <summary>
        /// Ask the game to run the simulation, and record when, so that the figures are
        /// reported as out of date until it has.
        /// </summary>
        internal static void Recalculate (Guid id)
        {
            RequireSimulation (FlightGlobalsExtensions.GetVesselById (id)).SetCalcsDirty (true);
            requestedFor = id;
            requestedAt = Time.fixedTime;
        }

        /// <summary>
        /// Whether the figures are current: the game is not mid-run, any run asked for
        /// here has been picked up, and the game either says the figures are ready or the
        /// vessel has no engines for it to work from.
        /// </summary>
        internal static bool Ready (Guid id)
        {
            return Ready (FlightGlobalsExtensions.GetVesselById (id));
        }

        /// <summary>
        /// Whether the vessel is far enough through a scene load to be worth calculating.
        /// A run against one the game is still unpacking latches figures taken from a part
        /// of it.
        /// </summary>
        static bool Settled (global::Vessel vessel)
        {
            return FlightGlobals.ready && vessel.loaded && !vessel.packed;
        }

        static bool Ready (global::Vessel vessel)
        {
            var simulation = vessel.VesselDeltaV;
            if (simulation == null || simulation.SimulationRunning)
                return false;
            if (vessel.id == requestedFor && Time.fixedTime <= requestedAt)
                return false;
            // A vessel between scenes holds no parts yet, and reads as engine-less until
            // the game has rebuilt it.
            return simulation.IsReady || (vessel.parts.Count > 0 && !HasEngines (vessel));
        }

        /// <summary>
        /// The simulation for the vessel, or an error naming the reason the game will
        /// never produce figures for it.
        /// </summary>
        static VesselDeltaV RequireSimulation (global::Vessel vessel)
        {
            var simulation = vessel.VesselDeltaV;
            if (simulation == null)
                throw new InvalidOperationException (
                    "The game does not calculate delta-v for this kind of vessel.");
            if (!simulation.DoStockSimulation)
                throw new InvalidOperationException (
                    "The game's delta-v calculations are turned off.");
            var active = FlightGlobals.ActiveVessel;
            if (ReferenceEquals (active, null) || active.id != vessel.id)
                throw new InvalidOperationException (
                    "The game only calculates delta-v for the active vessel.");
            return simulation;
        }

        /// <summary>
        /// Read a figure off the simulation, asking the game for a run when the figures
        /// are out of date and yielding until it has finished. Callers pass no tick; the
        /// continuations count it up.
        /// </summary>
        internal static T Read<T> (Guid id, Func<VesselDeltaV, T> figure, int tick = 0)
        {
            var vessel = FlightGlobalsExtensions.GetVesselById (id);
            if (Ready (vessel))
                return figure (vessel.VesselDeltaV);
            if (tick > WaitTicks)
                throw TimedOut ();
            Request (vessel, tick);
            throw new YieldException<Func<T>> (() => Read (id, figure, tick + 1));
        }

        /// <summary>
        /// Yield until the figures are current.
        /// </summary>
        internal static void Wait (Guid id, int tick = 0)
        {
            var vessel = FlightGlobalsExtensions.GetVesselById (id);
            if (Ready (vessel))
                return;
            if (tick > WaitTicks)
                throw TimedOut ();
            Request (vessel, tick);
            throw new YieldException<Action> (() => Wait (id, tick + 1));
        }

        /// <summary>
        /// Ask for a run on a tick spent waiting for one, once the grace period the game
        /// has to run it itself is up. The game drops a run that found nothing to work
        /// from and never repeats it, so a wait that only sat out the first run would wait
        /// out the whole budget. Asking only while the game is idle leaves a run it has
        /// started to finish.
        /// </summary>
        static void Request (global::Vessel vessel, int tick)
        {
            var simulation = RequireSimulation (vessel);
            if (tick < GraceTicks || !Settled (vessel) || simulation.SimulationRunning)
                return;
            Recalculate (vessel.id);
        }

        static TimeoutException TimedOut ()
        {
            return new TimeoutException (
                "Timed out waiting for the game to calculate delta-v for this vessel.");
        }
    }
}
