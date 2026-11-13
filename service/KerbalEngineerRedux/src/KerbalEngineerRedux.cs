using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ImpactAPI = KRPC.KerbalEngineerRedux.API.Impact;
using RendezvousAPI = KRPC.KerbalEngineerRedux.API.Rendezvous;
using StageAPI = KRPC.KerbalEngineerRedux.API.Stage;

namespace KRPC.KerbalEngineerRedux
{
    /// <summary>
    /// This service provides the flight readouts of
    /// <a href="https://forum.kerbalspaceprogram.com/topic/17833-kerbal-engineer-redux/">Kerbal Engineer Redux</a>.
    /// </summary>
    /// <remarks>
    /// The flight readouts are for the active vessel only. A procedure that needs the vessel
    /// simulation waits for a result that started after the vessel last changed: a switch, a
    /// stage or a change in its parts. Other changes, such as shutting down an engine, show in
    /// the next result.
    /// </remarks>
    [KRPCService (Id = ServiceInfo.Id, GameScene = GameScene.All)]
    [RequiresMod (typeof (API))]
    public static class KerbalEngineerRedux
    {
        const double degreesToRadians = Math.PI / 180;
        // The mod's masses are in tonnes, and its forces in kilonewtons
        const double kilo = 1000;
        // The mod's surface gravity of a body is in multiples of 9.81 m/s^2
        const double standardGravity = 9.81;
        const double kilopascalsPerAtmosphere = 101.325;

        static void CheckActiveVessel (SpaceCenter.Services.Vessel vessel)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            if (vessel.InternalVessel != FlightGlobals.ActiveVessel)
                throw new ArgumentException ("The vessel is not the active vessel", nameof (vessel));
        }

        // Reads the latest simulation, or yields to the next tick until there is one. We hold the
        // lock KER's worker takes to publish a result, so the results stay in place for the read.
        static T ReadSimulation<T> (Func<T> retry, Func<Array, T> read)
        {
            lock (API.Simulation.Lock) {
                var stages = API.SimulationStages ();
                if (stages == null)
                    throw new YieldException<Func<T>> (retry);
                return read (stages);
            }
        }

        /// <summary>
        /// Whether Kerbal Engineer Redux is available.
        /// </summary>
        [KRPCProperty]
        public static bool Available {
            get { return ModAPI.Get<API> ().IsAvailable; }
        }

        /// <summary>
        /// The impact of the vessel with the terrain, or <c>null</c> if its orbit does not meet
        /// the terrain. Throws an exception if the vessel is not the active vessel.
        /// </summary>
        /// <param name="vessel">The active vessel.</param>
        [KRPCProcedure (Nullable = true, GameScene = GameScene.Flight)]
        public static Impact? Impact (SpaceCenter.Services.Vessel vessel)
        {
            CheckActiveVessel (vessel);
            // The impact processor reads the last stage of the simulation
            return ReadSimulation (() => Impact (vessel), stages => {
                API.UpdateImpact ();
                if (!ImpactAPI.ShowDetails ())
                    return (Impact?)null;
                var timeTo = ImpactAPI.Time ();
                return new Impact {
                    UT = Planetarium.GetUniversalTime () + timeTo,
                    TimeTo = timeTo,
                    Latitude = ImpactAPI.Latitude (),
                    Longitude = ImpactAPI.Longitude (),
                    Elevation = ImpactAPI.Altitude (),
                    Biome = ImpactAPI.Biome ()
                };
            });
        }

        /// <summary>
        /// The suicide burn that stops the vessel at the terrain, or <c>null</c> if its orbit
        /// does not meet the terrain or its thrust cannot overcome gravity. Throws an exception
        /// if the vessel is not the active vessel.
        /// </summary>
        /// <param name="vessel">The active vessel.</param>
        [KRPCProcedure (Nullable = true, GameScene = GameScene.Flight)]
        public static SuicideBurn? SuicideBurn (SpaceCenter.Services.Vessel vessel)
        {
            CheckActiveVessel (vessel);
            return ReadSimulation (() => SuicideBurn (vessel), stages => {
                API.UpdateImpact ();
                var deltaV = ImpactAPI.SuicideDeltaV ();
                // The processor leaves the burn at zero when there is no impact or too little thrust
                if (!ImpactAPI.ShowDetails () || deltaV <= 0)
                    return (SuicideBurn?)null;
                var timeTo = ImpactAPI.SuicideCountdown ();
                return new SuicideBurn {
                    UT = Planetarium.GetUniversalTime () + timeTo,
                    TimeTo = timeTo,
                    Altitude = ImpactAPI.SuicideAltitude (),
                    DeltaV = deltaV,
                    Distance = ImpactAPI.SuicideDistance (),
                    Duration = ImpactAPI.SuicideLength ()
                };
            });
        }

        /// <summary>
        /// The stages of the vessel, lowest stage number first. Throws an exception if the
        /// vessel is not the active vessel.
        /// </summary>
        /// <remarks>
        /// The last is the current stage. The thrust and thrust-to-weight ratios are for the
        /// pressure and gravity at the vessel's altitude.
        /// </remarks>
        /// <param name="vessel">The active vessel.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static IList<Stage> Stages (SpaceCenter.Services.Vessel vessel)
        {
            CheckActiveVessel (vessel);
            return ReadSimulation (() => Stages (vessel), stages => (IList<Stage>)stages.Cast<object> ().Select (ToStage).ToList ());
        }

        /// <summary>
        /// The stages of the vessel in the editor, lowest stage number first.
        /// </summary>
        /// <remarks>
        /// The last is the first stage to activate, and the list is empty when the editor has no
        /// vessel. The thrust-to-weight ratios are for the gravity at the body's surface. The
        /// masses leave out the crew.
        /// </remarks>
        /// <param name="body">The body to launch from.</param>
        /// <param name="altitude">The altitude for the atmospheric pressure, in meters.</param>
        /// <param name="mach">The Mach number, for engines whose thrust depends on it.</param>
        [KRPCProcedure (GameScene = GameScene.Editor)]
        public static IList<Stage> EditorStages (SpaceCenter.Services.CelestialBody body, double altitude = 0, double mach = 0)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            if (altitude < 0)
                throw new ArgumentOutOfRangeException (nameof (altitude));
            if (mach < 0)
                throw new ArgumentOutOfRangeException (nameof (mach));
            var editor = EditorLogic.fetch;
            var ship = editor == null ? null : editor.ship;
            if (ship == null || ship.parts.Count == 0)
                return new List<Stage> ();
            var internalBody = body.InternalBody;
            var gravity = standardGravity * internalBody.GeeASL;
            var atmosphere = internalBody.atmosphere ? internalBody.GetPressure (altitude) / kilopascalsPerAtmosphere : 0;
            var stages = API.Simulate (ship.parts, gravity, atmosphere, mach);
            if (stages == null)
                return new List<Stage> ();
            return stages.Cast<object> ().Select (ToStage).ToList ();
        }

        static Stage ToStage (object stage)
        {
            return new Stage {
                Number = StageAPI.Number (stage),
                DeltaV = StageAPI.DeltaV (stage),
                TotalDeltaV = StageAPI.TotalDeltaV (stage),
                SpecificImpulse = StageAPI.Isp (stage),
                Thrust = StageAPI.ActualThrust (stage) * kilo,
                AvailableThrust = StageAPI.Thrust (stage) * kilo,
                TWR = StageAPI.ActualThrustToWeight (stage),
                AvailableTWR = StageAPI.ThrustToWeight (stage),
                MaxTWR = StageAPI.MaxThrustToWeight (stage),
                StartMass = StageAPI.TotalMass (stage) * kilo,
                MassLost = StageAPI.Mass (stage) * kilo,
                ResourceMass = StageAPI.ResourceMass (stage) * kilo,
                BurnTime = StageAPI.Time (stage),
                TotalBurnTime = StageAPI.TotalTime (stage),
                AvailableThrustTorque = StageAPI.MaxThrustTorque (stage) * kilo,
                ThrustOffsetAngle = StageAPI.ThrustOffsetAngle (stage),
                RCSSpecificImpulse = StageAPI.RCSIsp (stage),
                RCSThrust = StageAPI.RCSThrust (stage) * kilo,
                RCSStartDeltaV = StageAPI.RCSDeltaVStart (stage),
                RCSEndDeltaV = StageAPI.RCSDeltaVEnd (stage),
                RCSStartTWR = StageAPI.RCSTWRStart (stage),
                RCSEndTWR = StageAPI.RCSTWREnd (stage),
                RCSBurnTime = StageAPI.RCSBurnTime (stage),
                StartCost = StageAPI.TotalCost (stage),
                CostLost = StageAPI.Cost (stage),
                StartPartCount = StageAPI.TotalPartCount (stage),
                DroppedPartCount = StageAPI.PartCount (stage)
            };
        }

        /// <summary>
        /// The time to burn a delta-v, or <c>null</c> if the vessel has too little delta-v.
        /// Throws an exception if the vessel is not the active vessel.
        /// </summary>
        /// <param name="vessel">The active vessel.</param>
        /// <param name="deltaV">The delta-v, in <unit>m/s</unit>.</param>
        [KRPCProcedure (Nullable = true, GameScene = GameScene.Flight)]
        public static BurnTime? BurnTime (SpaceCenter.Services.Vessel vessel, double deltaV)
        {
            CheckActiveVessel (vessel);
            return ReadSimulation (() => BurnTime (vessel, deltaV), stages => {
                double duration = 0;
                double halfDuration = 0;
                if (!API.Simulation.BurnTime (deltaV, ref duration, ref halfDuration))
                    return (BurnTime?)null;
                return new BurnTime { Duration = duration, HalfDuration = halfDuration };
            });
        }

        /// <summary>
        /// The transfer to the vessel's target, or <c>null</c> if it has no target, or the
        /// target is the body it orbits. Throws an exception if the vessel is not the active
        /// vessel.
        /// </summary>
        /// <param name="vessel">The active vessel.</param>
        [KRPCProcedure (Nullable = true, GameScene = GameScene.Flight)]
        public static Rendezvous? Rendezvous (SpaceCenter.Services.Vessel vessel)
        {
            CheckActiveVessel (vessel);
            API.RendezvousProcessor.Update ();
            // The processor has no source orbit for the body the vessel orbits, and computes
            // nothing for a target landed on the same body
            if (!RendezvousAPI.ShowDetails () || RendezvousAPI.SourceDisplay () == "N/A" || RendezvousAPI.LandedSamePlanet ())
                return null;
            var result = new Rendezvous {
                PhaseAngle = RendezvousAPI.PhaseAngle () * degreesToRadians,
                InterceptAngle = RendezvousAPI.InterceptAngle () * degreesToRadians,
                TimeToInterceptAngle = RendezvousAPI.TimeToTransferAngle (),
                AngleToAscendingNode = RendezvousAPI.AngleToAscendingNode () * degreesToRadians,
                AngleToDescendingNode = RendezvousAPI.AngleToDescendingNode () * degreesToRadians,
                TimeToAscendingNode = RendezvousAPI.TimeToAscendingNode (),
                TimeToDescendingNode = RendezvousAPI.TimeToDescendingNode ()
            };
            // With the vessel or target landed, the nodes are the launch site's plane crossings
            if (RendezvousAPI.OverrideANDN () || RendezvousAPI.OverrideANDNRev ()) {
                var angles = RendezvousAPI.AngleToPlane ();
                var times = RendezvousAPI.TimeToPlane ();
                result.AngleToAscendingNode = angles [0] * degreesToRadians;
                result.AngleToDescendingNode = angles [1] * degreesToRadians;
                result.TimeToAscendingNode = times [0];
                result.TimeToDescendingNode = times [1];
            }
            return result;
        }
    }
}
