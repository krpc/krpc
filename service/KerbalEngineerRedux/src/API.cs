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
    static class API
    {
        const string assemblyName = "KerbalEngineer";
        const string readouts = "KerbalEngineer.Flight.Readouts.";
        // The longest wait for the first simulation result, in seconds of real time
        const float simulationTimeout = 5;

        static readonly List<string> missing = new List<string> ();

        public static bool IsAvailable { get; private set; }

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
            // A simulation of our own, so the editor windows of the mod leave its inputs alone
            public static object Instance;
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


        public static void Load ()
        {
            IsAvailable = false;
            var loaded = AssemblyLoader.loadedAssemblies.FirstOrDefault (a => a.assembly.GetName ().Name == assemblyName);
            if (loaded == null) {
                Logger.WriteLine ("Load API: " + assemblyName + " not found; skipping");
                return;
            }
            var assembly = loaded.assembly;
            missing.Clear ();

            var impact = assembly.GetType (readouts + "Surface.ImpactProcessor");
            ImpactProcessor = Processor.Bind (impact, missing);
            Impact.ShowDetails = Static<bool> (impact, "ShowDetails");
            Impact.Time = Static<double> (impact, "Time");
            Impact.Latitude = Static<double> (impact, "Latitude");
            Impact.Longitude = Static<double> (impact, "Longitude");
            Impact.Altitude = Static<double> (impact, "Altitude");
            Impact.Biome = Static<string> (impact, "Biome");
            Impact.SuicideDeltaV = Static<double> (impact, "SuicideDeltaV");
            Impact.SuicideDistance = Static<double> (impact, "SuicideDistance");
            Impact.SuicideAltitude = Static<double> (impact, "SuicideAltitude");
            Impact.SuicideLength = Static<double> (impact, "SuicideLength");
            Impact.SuicideCountdown = Static<double> (impact, "SuicideCountdown");

            var simulation = assembly.GetType (readouts + "Vessel.SimulationProcessor");
            SimulationProcessor = Processor.Bind (simulation, missing);
            Simulation.Stages = Static<object> (simulation, "Stages");
            var manager = assembly.GetType ("KerbalEngineer.VesselSimulator.SimManager");
            Simulation.FailMessage = Static<string> (manager, "failMessage");
            Simulation.ResultsReady = manager == null ? null : Accessor.Method<Func<bool>> (manager, "ResultsReady");
            if (Simulation.ResultsReady == null)
                missing.Add ("SimManager.ResultsReady");
            var locker = Static<object> (manager, "locker");
            Simulation.Lock = locker == null ? null : locker ();
            var manoeuvre = assembly.GetType (readouts + "Orbital.ManoeuvreNode.ManoeuvreProcessor");
            Simulation.BurnTime = manoeuvre == null ? null : Accessor.Method<BurnTimeFunction> (manoeuvre, "GetBurnTime");
            if (Simulation.BurnTime == null)
                missing.Add ("ManoeuvreProcessor.GetBurnTime");

            var core = assembly.GetType ("KerbalEngineer.Flight.FlightEngineerCore");
            Core.GamePaused = Static<bool> (core, "gamePaused");
            Core.SetGamePaused = core == null ? null : Accessor.StaticSetter<bool> (core, "gamePaused");
            if (Core.SetGamePaused == null)
                missing.Add ("FlightEngineerCore.gamePaused");

            var stage = assembly.GetType ("KerbalEngineer.VesselSimulator.Stage");
            Stage.Number = Field<int> (stage, "number");
            Stage.DeltaV = Field<double> (stage, "deltaV");
            Stage.TotalDeltaV = Field<double> (stage, "totalDeltaV");
            Stage.Isp = Field<double> (stage, "isp");
            Stage.Thrust = Field<double> (stage, "thrust");
            Stage.ActualThrust = Field<double> (stage, "actualThrust");
            Stage.ThrustToWeight = Field<double> (stage, "thrustToWeight");
            Stage.MaxThrustToWeight = Field<double> (stage, "maxThrustToWeight");
            Stage.ActualThrustToWeight = Field<double> (stage, "actualThrustToWeight");
            Stage.Mass = Field<double> (stage, "mass");
            Stage.TotalMass = Field<double> (stage, "totalMass");
            Stage.ResourceMass = Field<double> (stage, "resourceMass");
            Stage.Time = Field<double> (stage, "time");
            Stage.TotalTime = Field<double> (stage, "totalTime");
            Stage.MaxThrustTorque = Field<double> (stage, "maxThrustTorque");
            Stage.ThrustOffsetAngle = Field<double> (stage, "thrustOffsetAngle");
            Stage.RCSIsp = Field<double> (stage, "RCSIsp");
            Stage.RCSThrust = Field<double> (stage, "RCSThrust");
            Stage.RCSDeltaVStart = Field<double> (stage, "RCSdeltaVStart");
            Stage.RCSDeltaVEnd = Field<double> (stage, "RCSdeltaVEnd");
            Stage.RCSTWRStart = Field<double> (stage, "RCSTWRStart");
            Stage.RCSTWREnd = Field<double> (stage, "RCSTWREnd");
            Stage.RCSBurnTime = Field<double> (stage, "RCSBurnTime");
            Stage.Cost = Field<double> (stage, "cost");
            Stage.TotalCost = Field<double> (stage, "totalCost");
            Stage.PartCount = Field<int> (stage, "partCount");
            Stage.TotalPartCount = Field<int> (stage, "totalPartCount");

            var simulator = assembly.GetType ("KerbalEngineer.VesselSimulator.Simulation");
            EditorSimulation.Instance = simulator == null ? null : Activator.CreateInstance (simulator);
            EditorSimulation.Prepare = simulator == null ? null :
                Accessor.Method<Func<object, object, List<global::Part>, double, double, double, bool, bool, bool, bool>> (simulator, "PrepareSimulation");
            EditorSimulation.Run = simulator == null ? null : Accessor.Method<Func<object, object, object>> (simulator, "RunSimulation");
            EditorSimulation.UpdateModSettings = manager == null ? null : Accessor.Method<Action> (manager, "UpdateModSettings");
            EditorSimulation.VectoredThrust = Static<bool> (manager, "vectoredThrust");
            if (EditorSimulation.Instance == null || EditorSimulation.Prepare == null || EditorSimulation.Run == null || EditorSimulation.UpdateModSettings == null)
                missing.Add ("Simulation");

            var rendezvous = assembly.GetType (readouts + "Rendezvous.RendezvousProcessor");
            RendezvousProcessor = Processor.Bind (rendezvous, missing);
            Rendezvous.ShowDetails = Static<bool> (rendezvous, "ShowDetails");
            Rendezvous.LandedSamePlanet = Static<bool> (rendezvous, "landedSamePlanet");
            Rendezvous.SourceDisplay = Static<string> (rendezvous, "sourceDisplay");
            Rendezvous.OverrideANDN = Static<bool> (rendezvous, "overrideANDN");
            Rendezvous.OverrideANDNRev = Static<bool> (rendezvous, "overrideANDNRev");
            Rendezvous.PhaseAngle = Static<double> (rendezvous, "PhaseAngle");
            Rendezvous.InterceptAngle = Static<double> (rendezvous, "InterceptAngle");
            Rendezvous.TimeToTransferAngle = Static<double> (rendezvous, "TimeToTransferAngle");
            Rendezvous.AngleToAscendingNode = Static<double> (rendezvous, "AngleToAscendingNode");
            Rendezvous.AngleToDescendingNode = Static<double> (rendezvous, "AngleToDescendingNode");
            Rendezvous.TimeToAscendingNode = Static<double> (rendezvous, "TimeToAscendingNode");
            Rendezvous.TimeToDescendingNode = Static<double> (rendezvous, "TimeToDescendingNode");
            Rendezvous.AngleToPlane = Static<double[]> (rendezvous, "AngleToPlane");
            Rendezvous.TimeToPlane = Static<double[]> (rendezvous, "TimeToPlane");


            if (missing.Count > 0) {
                Logger.WriteLine ("Load API: " + assemblyName + " members not found: " +
                                  string.Join (", ", missing.ToArray ()) + "; skipping", Logger.Severity.Error);
                return;
            }
            IsAvailable = true;
            Logger.WriteLine ("Load API: Successfully loaded " + assemblyName + " version " + assembly.GetName ().Version);
        }

        static Func<T> Static<T> (Type type, string name)
        {
            var getter = type == null ? null : Accessor.StaticGetter<T> (type, name);
            if (getter == null)
                missing.Add ((type == null ? "?" : type.Name) + "." + name);
            return getter;
        }

        static Func<object, T> Field<T> (Type type, string name)
        {
            var getter = type == null ? null : Accessor.Getter<object, T> (type, name);
            if (getter == null)
                missing.Add ((type == null ? "?" : type.Name) + "." + name);
            return getter;
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
        readonly object instance;
        readonly Action<object> update;

        Processor (object instance, Action<object> update)
        {
            this.instance = instance;
            this.update = update;
        }

        public static Processor Bind (Type type, List<string> missing)
        {
            var instance = type == null ? null : Accessor.StaticGetter<object> (type, "Instance");
            var update = type == null ? null : Accessor.Method<Action<object>> (type, "Update");
            if (instance == null || update == null) {
                missing.Add (type == null ? "processor" : type.Name);
                return null;
            }
            return new Processor (instance (), update);
        }

        public void Update ()
        {
            update (instance);
        }
    }
}
