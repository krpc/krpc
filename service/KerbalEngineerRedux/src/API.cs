using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Utils;
using UnityEngine;
using Logger = KRPC.Utils.Logger;

namespace KRPC.KerbalEngineerRedux
{
    // Reads the readout processors of Kerbal Engineer Redux by reflection. The mod has no public
    // API, so we bind to the members of its 1.1.9.5 release. Each processor is a singleton whose
    // Update computes static properties for the active vessel.
    sealed class API : ModAPI
    {
        public API () : base (ServiceInfo.Mods.KerbalEngineerRedux.Name, ServiceInfo.Mods.KerbalEngineerRedux.Assembly)
        {
        }

        const string readouts = "KerbalEngineer.Flight.Readouts.";
        // The longest wait for the first simulation result, in seconds of real time
        const float simulationTimeout = 5;

        public static Processor ImpactProcessor { get; private set; }
        public static Processor SimulationProcessor { get; private set; }
        public static Processor RendezvousProcessor { get; private set; }

        public static class Impact
        {
            public static Func<bool> ShowDetails;
            public static Func<double> Time;
            public static Func<double> Latitude;
            public static Func<double> Longitude;
            public static Func<double> Altitude;
            public static Func<string> Biome;
            public static Func<double> SuicideDeltaV;
            public static Func<double> SuicideDistance;
            public static Func<double> SuicideAltitude;
            public static Func<double> SuicideLength;
            public static Func<double> SuicideCountdown;
        }

        public static class Simulation
        {
            public static Func<object> Stages;
            public static Func<string> FailMessage;
            public static BurnTimeFunction BurnTime;
            // False while a run is in flight on KER's worker thread
            public static Func<bool> ResultsReady;
            // Held by the worker while it publishes a result
            public static object Lock;
        }

        public static class Core
        {
            public static Func<bool> GamePaused;
            public static Action<bool> SetGamePaused;
        }

        public static class EditorSimulation
        {
            // A simulation of our own, so the editor windows of the mod leave its inputs alone.
            // We create it on first use, so a missing constructor fails only the calls that need it
            public static Func<object> Create;
            static object instance;

            public static object Instance {
                get { return instance ?? (instance = Create ()); }
            }

            public static Func<object, object, List<global::Part>, double, double, double, bool, bool, bool, bool> Prepare;
            public static Func<object, object, object> Run;
            public static Action UpdateModSettings;
            public static Func<bool> VectoredThrust;
        }

        public delegate bool BurnTimeFunction (double deltaV, ref double burnTime, ref double midPointTime);

        public static class Stage
        {
            public static Func<object, int> Number;
            public static Func<object, double> DeltaV;
            public static Func<object, double> TotalDeltaV;
            public static Func<object, double> Isp;
            public static Func<object, double> Thrust;
            public static Func<object, double> ActualThrust;
            public static Func<object, double> ThrustToWeight;
            public static Func<object, double> MaxThrustToWeight;
            public static Func<object, double> ActualThrustToWeight;
            public static Func<object, double> Mass;
            public static Func<object, double> TotalMass;
            public static Func<object, double> ResourceMass;
            public static Func<object, double> Time;
            public static Func<object, double> TotalTime;
            public static Func<object, double> MaxThrustTorque;
            public static Func<object, double> ThrustOffsetAngle;
            public static Func<object, double> RCSIsp;
            public static Func<object, double> RCSThrust;
            public static Func<object, double> RCSDeltaVStart;
            public static Func<object, double> RCSDeltaVEnd;
            public static Func<object, double> RCSTWRStart;
            public static Func<object, double> RCSTWREnd;
            public static Func<object, double> RCSBurnTime;
            public static Func<object, double> Cost;
            public static Func<object, double> TotalCost;
            public static Func<object, int> PartCount;
            public static Func<object, int> TotalPartCount;
        }

        public static class Rendezvous
        {
            public static Func<bool> ShowDetails;
            public static Func<bool> LandedSamePlanet;
            public static Func<string> SourceDisplay;
            public static Func<bool> OverrideANDN;
            public static Func<bool> OverrideANDNRev;
            public static Func<double> PhaseAngle;
            public static Func<double> InterceptAngle;
            public static Func<double> TimeToTransferAngle;
            public static Func<double> AngleToAscendingNode;
            public static Func<double> AngleToDescendingNode;
            public static Func<double> TimeToAscendingNode;
            public static Func<double> TimeToDescendingNode;
            public static Func<double[]> AngleToPlane;
            public static Func<double[]> TimeToPlane;
        }


        protected override void Bind (ModBinder mod)
        {
            var impact = mod.Type (readouts + "Surface.ImpactProcessor");
            ImpactProcessor = new Processor (impact);
            Impact.ShowDetails = impact.StaticGetter<bool> ("ShowDetails");
            Impact.Time = impact.StaticGetter<double> ("Time");
            Impact.Latitude = impact.StaticGetter<double> ("Latitude");
            Impact.Longitude = impact.StaticGetter<double> ("Longitude");
            Impact.Altitude = impact.StaticGetter<double> ("Altitude");
            Impact.Biome = impact.StaticGetter<string> ("Biome");
            Impact.SuicideDeltaV = impact.StaticGetter<double> ("SuicideDeltaV");
            Impact.SuicideDistance = impact.StaticGetter<double> ("SuicideDistance");
            Impact.SuicideAltitude = impact.StaticGetter<double> ("SuicideAltitude");
            Impact.SuicideLength = impact.StaticGetter<double> ("SuicideLength");
            Impact.SuicideCountdown = impact.StaticGetter<double> ("SuicideCountdown");

            var simulation = mod.Type (readouts + "Vessel.SimulationProcessor");
            SimulationProcessor = new Processor (simulation);
            Simulation.Stages = simulation.StaticGetter<object> ("Stages");
            var manager = mod.Type ("KerbalEngineer.VesselSimulator.SimManager");
            Simulation.FailMessage = manager.StaticGetter<string> ("failMessage");
            Simulation.ResultsReady = manager.Method<Func<bool>> ("ResultsReady");
            var locker = manager.StaticGetter<object> ("locker");
            Simulation.Lock = Accessor.IsMissing (locker) ? new object () : locker ();
            var manoeuvre = mod.Type (readouts + "Orbital.ManoeuvreNode.ManoeuvreProcessor");
            Simulation.BurnTime = manoeuvre.Method<BurnTimeFunction> ("GetBurnTime");

            var core = mod.Type ("KerbalEngineer.Flight.FlightEngineerCore");
            Core.GamePaused = core.StaticGetter<bool> ("gamePaused");
            Core.SetGamePaused = core.StaticSetter<bool> ("gamePaused");

            var stage = mod.Type ("KerbalEngineer.VesselSimulator.Stage");
            Stage.Number = stage.Getter<object, int> ("number");
            Stage.DeltaV = stage.Getter<object, double> ("deltaV");
            Stage.TotalDeltaV = stage.Getter<object, double> ("totalDeltaV");
            Stage.Isp = stage.Getter<object, double> ("isp");
            Stage.Thrust = stage.Getter<object, double> ("thrust");
            Stage.ActualThrust = stage.Getter<object, double> ("actualThrust");
            Stage.ThrustToWeight = stage.Getter<object, double> ("thrustToWeight");
            Stage.MaxThrustToWeight = stage.Getter<object, double> ("maxThrustToWeight");
            Stage.ActualThrustToWeight = stage.Getter<object, double> ("actualThrustToWeight");
            Stage.Mass = stage.Getter<object, double> ("mass");
            Stage.TotalMass = stage.Getter<object, double> ("totalMass");
            Stage.ResourceMass = stage.Getter<object, double> ("resourceMass");
            Stage.Time = stage.Getter<object, double> ("time");
            Stage.TotalTime = stage.Getter<object, double> ("totalTime");
            Stage.MaxThrustTorque = stage.Getter<object, double> ("maxThrustTorque");
            Stage.ThrustOffsetAngle = stage.Getter<object, double> ("thrustOffsetAngle");
            Stage.RCSIsp = stage.Getter<object, double> ("RCSIsp");
            Stage.RCSThrust = stage.Getter<object, double> ("RCSThrust");
            Stage.RCSDeltaVStart = stage.Getter<object, double> ("RCSdeltaVStart");
            Stage.RCSDeltaVEnd = stage.Getter<object, double> ("RCSdeltaVEnd");
            Stage.RCSTWRStart = stage.Getter<object, double> ("RCSTWRStart");
            Stage.RCSTWREnd = stage.Getter<object, double> ("RCSTWREnd");
            Stage.RCSBurnTime = stage.Getter<object, double> ("RCSBurnTime");
            Stage.Cost = stage.Getter<object, double> ("cost");
            Stage.TotalCost = stage.Getter<object, double> ("totalCost");
            Stage.PartCount = stage.Getter<object, int> ("partCount");
            Stage.TotalPartCount = stage.Getter<object, int> ("totalPartCount");

            var simulator = mod.Type ("KerbalEngineer.VesselSimulator.Simulation");
            EditorSimulation.Create = simulator.Constructor<Func<object>> ();
            EditorSimulation.Prepare =
                simulator.Method<Func<object, object, List<global::Part>, double, double, double, bool, bool, bool, bool>> ("PrepareSimulation");
            EditorSimulation.Run = simulator.Method<Func<object, object, object>> ("RunSimulation");
            EditorSimulation.UpdateModSettings = manager.Method<Action> ("UpdateModSettings");
            EditorSimulation.VectoredThrust = manager.StaticGetter<bool> ("vectoredThrust");

            var rendezvous = mod.Type (readouts + "Rendezvous.RendezvousProcessor");
            RendezvousProcessor = new Processor (rendezvous);
            Rendezvous.ShowDetails = rendezvous.StaticGetter<bool> ("ShowDetails");
            Rendezvous.LandedSamePlanet = rendezvous.StaticGetter<bool> ("landedSamePlanet");
            Rendezvous.SourceDisplay = rendezvous.StaticGetter<string> ("sourceDisplay");
            Rendezvous.OverrideANDN = rendezvous.StaticGetter<bool> ("overrideANDN");
            Rendezvous.OverrideANDNRev = rendezvous.StaticGetter<bool> ("overrideANDNRev");
            Rendezvous.PhaseAngle = rendezvous.StaticGetter<double> ("PhaseAngle");
            Rendezvous.InterceptAngle = rendezvous.StaticGetter<double> ("InterceptAngle");
            Rendezvous.TimeToTransferAngle = rendezvous.StaticGetter<double> ("TimeToTransferAngle");
            Rendezvous.AngleToAscendingNode = rendezvous.StaticGetter<double> ("AngleToAscendingNode");
            Rendezvous.AngleToDescendingNode = rendezvous.StaticGetter<double> ("AngleToDescendingNode");
            Rendezvous.TimeToAscendingNode = rendezvous.StaticGetter<double> ("TimeToAscendingNode");
            Rendezvous.TimeToDescendingNode = rendezvous.StaticGetter<double> ("TimeToDescendingNode");
            Rendezvous.AngleToPlane = rendezvous.StaticGetter<double[]> ("AngleToPlane");
            Rendezvous.TimeToPlane = rendezvous.StaticGetter<double[]> ("TimeToPlane");
        }

        /// <summary>
        /// The stages of a simulation of the parts, run now, or null if the simulation could not
        /// be set up. The gravity is in <unit>m/s^2</unit> and the atmospheric pressure in
        /// atmospheres.
        /// </summary>
        public static Array Simulate (List<global::Part> parts, double gravity, double atmosphere, double mach)
        {
            EditorSimulation.UpdateModSettings ();
            // In the editor, part.mass holds the crew's mass for part of each frame. The
            // simulation reads it, so we reset it to the mass without the crew.
            for (int i = 0; i < parts.Count; i++)
                parts [i].UpdateMass ();
            var instance = EditorSimulation.Instance;
            if (!EditorSimulation.Prepare (instance, null, parts, gravity, atmosphere, mach, false, EditorSimulation.VectoredThrust (), false))
                return null;
            return EditorSimulation.Run (instance, null) as Array;
        }

        // The active vessel, its stage and its part count when the simulation results were
        // last checked
        static global::Vessel simulatedVessel;
        static int simulatedStage;
        static int simulatedPartCount;
        // The results held then, and the number of new results to wait for
        static object staleStages;
        static int staleResults;
        static float waitStart = -1;

        /// <summary>
        /// The stages of the latest simulation of the active vessel, or null if none has started
        /// since the vessel last changed. A change is a switch, staging or a change in its
        /// parts. Requests a new simulation. Throws an exception if no simulation has finished
        /// for some time.
        /// </summary>
        /// <remarks>
        /// Call while holding <see cref="Simulation.Lock"/>, so the results stay in place.
        /// </remarks>
        public static Array SimulationStages ()
        {
            var vessel = FlightGlobals.ActiveVessel;
            if (vessel != simulatedVessel || vessel.currentStage != simulatedStage || vessel.parts.Count != simulatedPartCount) {
                simulatedVessel = vessel;
                simulatedStage = vessel.currentStage;
                simulatedPartCount = vessel.parts.Count;
                staleStages = Simulation.Stages ();
                // A run in flight read the parts before the change, so its result is stale too
                staleResults = Simulation.ResultsReady () ? 1 : 2;
            }
            SimulationProcessor.Update ();
            var stages = Simulation.Stages () as Array;
            // Note: two results between calls count as one, which only waits longer
            if (staleResults > 0 && !ReferenceEquals (stages, staleStages)) {
                staleResults--;
                staleStages = stages;
            }
            if (staleResults == 0 && stages != null && stages.Length > 0) {
                waitStart = -1;
                return stages;
            }
            var now = UnityEngine.Time.realtimeSinceStartup;
            if (waitStart < 0)
                waitStart = now;
            else if (now - waitStart > simulationTimeout) {
                waitStart = -1;
                var message = Simulation.FailMessage ();
                throw new InvalidOperationException (
                    "Kerbal Engineer Redux did not simulate the vessel" +
                    (string.IsNullOrEmpty (message) ? string.Empty : ": " + message));
            }
            return null;
        }

        /// <summary>
        /// Update the impact processor. It skips updates while the game is paused, so we clear
        /// its flag for the call.
        /// </summary>
        public static void UpdateImpact ()
        {
            var paused = Core.GamePaused ();
            Core.SetGamePaused (false);
            try {
                ImpactProcessor.Update ();
            } finally {
                Core.SetGamePaused (paused);
            }
        }
    }

    // A readout processor. Each read updates it, as other RPCs in the same tick can change
    // its inputs, such as the target
    sealed class Processor
    {
        readonly Func<object> getInstance;
        readonly Action<object> update;

        public Processor (MemberBinder type)
        {
            getInstance = type.StaticGetter<object> ("Instance");
            update = type.Method<Action<object>> ("Update");
        }

        public void Update ()
        {
            update (getInstance ());
        }
    }
}
