using System;
using System.Collections.Generic;
using KRPC.Continuations;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using UnityEngine;
using Tuple2 = KRPC.Utils.Tuple<double, double>;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Provides functionality to interact with Kerbal Space Program. This includes controlling
    /// the active vessel, managing its resources, planning maneuver nodes and auto-piloting.
    /// </summary>
    [KRPCService (GameScene = GameScene.Flight)]
    public static class SpaceCenter
    {
        /// <summary>
        /// The currently active vessel.
        /// </summary>
        [KRPCProperty]
        public static Vessel ActiveVessel {
            get { return new Vessel (FlightGlobals.ActiveVessel); }
            set {
                FlightGlobals.SetActiveVessel (value.InternalVessel);
                throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, 0));
            }
        }

        /// <summary>
        /// Wait until 10 frames after the active vessel is unpacked.
        /// </summary>
        static void WaitForVesselSwitch (int tick)
        {
            if (FlightGlobals.ActiveVessel.packed)
                throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, 0));
            if (tick < 10)
                throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, tick + 1));
        }

        /// <summary>
        /// A list of all the vessels in the game.
        /// </summary>
        [KRPCProperty]
        public static IList<Vessel> Vessels {
            get {
                var vessels = new List<Vessel> ();
                foreach (var vessel in FlightGlobals.Vessels) {
                    if (vessel.vesselType == global::VesselType.EVA ||
                        vessel.vesselType == global::VesselType.Flag ||
                        vessel.vesselType == global::VesselType.SpaceObject ||
                        vessel.vesselType == global::VesselType.Unknown)
                        continue;
                    vessels.Add (new Vessel (vessel));
                }
                return vessels;
            }
        }

        /// <summary>
        /// A dictionary of all celestial bodies (planets, moons, etc.) in the game,
        /// keyed by the name of the body.
        /// </summary>
        [KRPCProperty]
        public static IDictionary<string,CelestialBody> Bodies {
            get {
                var bodies = new Dictionary<string, CelestialBody> ();
                foreach (var body in FlightGlobals.Bodies)
                    bodies [body.name] = new CelestialBody (body);
                return bodies;
            }
        }

        /// <summary>
        /// The currently targeted celestial body.
        /// </summary>
        [KRPCProperty]
        public static CelestialBody TargetBody {
            get {
                var target = FlightGlobals.fetch.VesselTarget;
                return target is global::CelestialBody ? new CelestialBody (target as global::CelestialBody) : null;
            }
            set { FlightGlobals.fetch.SetVesselTarget (value == null ? null : value.InternalBody); }
        }

        /// <summary>
        /// The currently targeted vessel.
        /// </summary>
        [KRPCProperty]
        public static Vessel TargetVessel {
            get {
                var target = FlightGlobals.fetch.VesselTarget;
                return target is global::Vessel ? new Vessel (target as global::Vessel) : null;
            }
            set { FlightGlobals.fetch.SetVesselTarget (value == null ? null : value.InternalVessel); }
        }

        /// <summary>
        /// The currently targeted docking port.
        /// </summary>
        [KRPCProperty]
        public static Parts.DockingPort TargetDockingPort {
            get {
                var target = FlightGlobals.fetch.VesselTarget;
                var part = target is ModuleDockingNode ? new Parts.Part ((target as ModuleDockingNode).part) : null;
                return part != null ? new Parts.DockingPort (part) : null;
            }
            set { FlightGlobals.fetch.SetVesselTarget (value == null ? null : value.InternalPort); }
        }

        /// <summary>
        /// Clears the current target.
        /// </summary>
        [KRPCProcedure]
        public static void ClearTarget ()
        {
            FlightGlobals.fetch.SetVesselTarget (null);
        }

        /// <summary>
        /// Launch a new vessel from the VAB onto the launchpad.
        /// </summary>
        /// <param name="name">Name of the vessel's craft file.</param>
        [KRPCProcedure]
        public static void LaunchVesselFromVAB (string name)
        {
            var craft = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/Ships/VAB/" + name + ".craft";
            var crew = HighLogic.CurrentGame.CrewRoster.DefaultCrewForVessel (ConfigNode.Load (craft));
            FlightDriver.StartWithNewLaunch (craft, EditorLogic.FlagURL, "LaunchPad", crew);
            throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, 0));
        }

        /// <summary>
        /// Launch a new vessel from the SPH onto the runway.
        /// </summary>
        /// <param name="name">Name of the vessel's craft file.</param>
        [KRPCProcedure]
        public static void LaunchVesselFromSPH (string name)
        {
            var craft = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/Ships/SPH/" + name + ".craft";
            var crew = HighLogic.CurrentGame.CrewRoster.DefaultCrewForVessel (ConfigNode.Load (craft));
            FlightDriver.StartWithNewLaunch (craft, EditorLogic.FlagURL, "Runway", crew);
            throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, 0));
        }

        /// <summary>
        /// Save the game with a given name.
        /// This will create a save file called <c>name.sfs</c> in the folder of the current save game.
        /// </summary>
        [KRPCProcedure]
        public static void Save (string name)
        {
            GamePersistence.SaveGame (name, HighLogic.SaveFolder, SaveMode.OVERWRITE);
        }

        /// <summary>
        /// Load the game with the given name.
        /// This will create a load a save file called <c>name.sfs</c> from the folder of the current save game.
        /// </summary>
        [KRPCProcedure]
        public static void Load (string name)
        {
            var game = GamePersistence.LoadGame (name, HighLogic.SaveFolder, true, false);
            if (game == null || game.flightState == null || !game.compatible)
                throw new ArgumentException ("Failed to load " + name);
            FlightDriver.StartAndFocusVessel (game, game.flightState.activeVesselIdx);
            throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, 0));
        }

        /// <summary>
        /// Save a quicksave.
        /// </summary>
        /// <remarks>
        /// This is the same as calling <see cref="Save"/> with the name "quicksave".
        /// </remarks>
        [KRPCProcedure]
        public static void Quicksave ()
        {
            Save ("quicksave");
        }

        /// <summary>
        /// Load a quicksave.
        /// </summary>
        /// <remarks>
        /// This is the same as calling <see cref="Load"/> with the name "quicksave".
        /// </remarks>
        [KRPCProcedure]
        public static void Quickload ()
        {
            Load ("quicksave");
        }

        /// <summary>
        /// An object that can be used to control the camera.
        /// </summary>
        [KRPCProperty]
        public static Camera Camera {
            get { return new Camera (); }
        }

        /// <summary>
        /// The current universal time in seconds.
        /// </summary>
        [KRPCProperty]
        public static double UT {
            get { return Planetarium.GetUniversalTime (); }
        }

        /// <summary>
        /// The value of the <a href="https://en.wikipedia.org/wiki/Gravitational_constant">gravitational constant</a>
        /// G in <math>N(m/kg)^2</math>.
        /// </summary>
        [KRPCProperty]
        public static float G {
            get { return 6.673e-11f; }
        }

        /// <summary>
        /// The current time warp mode. Returns <see cref="WarpMode.None"/> if time
        /// warp is not active, <see cref="WarpMode.Rails"/> if regular "on-rails" time warp
        /// is active, or <see cref="WarpMode.Physics"/> if physical time warp is active.
        /// </summary>
        [KRPCProperty]
        public static WarpMode WarpMode {
            get {
                if (TimeWarp.CurrentRateIndex == 0)
                    return WarpMode.None;
                else if (TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
                    return WarpMode.Rails;
                else
                    return WarpMode.Physics;
            }
        }

        /// <summary>
        /// The current warp rate. This is the rate at which time is passing for
        /// either on-rails or physical time warp. For example, a value of 10 means
        /// time is passing 10x faster than normal. Returns 1 if time warp is not
        /// active.
        /// </summary>
        [KRPCProperty]
        public static float WarpRate {
            get { return TimeWarp.CurrentRate; }
        }

        /// <summary>
        /// The current warp factor. This is the index of the rate at which time
        /// is passing for either regular "on-rails" or physical time warp. Returns 0
        /// if time warp is not active. When in on-rails time warp, this is equal to
        /// <see cref="RailsWarpFactor"/>, and in physics time warp, this is equal to
        /// <see cref="PhysicsWarpFactor"/>.
        /// </summary>
        [KRPCProperty]
        public static float WarpFactor {
            get { return TimeWarp.CurrentRateIndex; }
        }

        /// <summary>
        /// The time warp rate, using regular "on-rails" time warp. A value between
        /// 0 and 7 inclusive. 0 means no time warp. Returns 0 if physical time warp
        /// is active.
        ///
        /// If requested time warp factor cannot be set, it will be set to the next
        /// lowest possible value. For example, if the vessel is too close to a
        /// planet. See <a href="http://wiki.kerbalspaceprogram.com/wiki/Time_warp">
        /// the KSP wiki</a> for details.
        /// </summary>
        [KRPCProperty]
        public static int RailsWarpFactor {
            get { return WarpMode == WarpMode.Rails ? TimeWarp.CurrentRateIndex : 0; }
            set { SetWarpFactor (TimeWarp.Modes.HIGH, value.Clamp (0, MaximumRailsWarpFactor)); }
        }

        /// <summary>
        /// The physical time warp rate. A value between 0 and 3 inclusive. 0 means
        /// no time warp. Returns 0 if regular "on-rails" time warp is active.
        /// </summary>
        [KRPCProperty]
        public static int PhysicsWarpFactor {
            get { return WarpMode == WarpMode.Physics ? TimeWarp.CurrentRateIndex : 0; }
            set { SetWarpFactor (TimeWarp.Modes.LOW, value.Clamp (0, 3)); }
        }

        /// <summary>
        /// Returns <c>true</c> if regular "on-rails" time warp can be used, at the specified warp
        /// <paramref name="factor"/>. The maximum time warp rate is limited by various things,
        /// including how close the active vessel is to a planet. See
        /// <a href="http://wiki.kerbalspaceprogram.com/wiki/Time_warp">the KSP wiki</a> for details.
        /// </summary>
        /// <param name="factor">The warp factor to check.</param>
        [KRPCProcedure]
        public static bool CanRailsWarpAt (int factor = 1)
        {
            if (factor == 0)
                return true;
            // Not a valid factor
            if (factor < 0 || factor >= TimeWarp.fetch.warpRates.Length)
                return false;
            // Landed
            if (ActiveVessel.InternalVessel.LandedOrSplashed)
                return true;
            // Below altitude limit
            var altitude = ActiveVessel.InternalVessel.mainBody.GetAltitude (ActiveVessel.InternalVessel.findWorldCenterOfMass ());
            var altitudeLimit = TimeWarp.fetch.GetAltitudeLimit (factor, ActiveVessel.InternalVessel.mainBody);
            if (altitude < altitudeLimit)
                return false;
            // Throttle is non-zero
            if (FlightInputHandler.state.mainThrottle > 0f)
                return false;
            return true;
        }

        /// <summary>
        /// The current maximum regular "on-rails" warp factor that can be set.
        /// A value between 0 and 7 inclusive.  See
        /// <a href="http://wiki.kerbalspaceprogram.com/wiki/Time_warp">the KSP wiki</a> for details.
        /// </summary>
        [KRPCProperty]
        public static int MaximumRailsWarpFactor {
            get {
                for (int i = TimeWarp.fetch.warpRates.Length - 1; i > 1; i--) {
                    if (CanRailsWarpAt (i))
                        return i;
                }
                return 0;
            }
        }

        /// <summary>
        /// Uses time acceleration to warp forward to a time in the future, specified
        /// by universal time <paramref name="ut"/>. This call blocks until the desired
        /// time is reached. Uses regular "on-rails" or physical time warp as appropriate.
        /// For example, physical time warp is used when the active vessel is traveling
        /// through an atmosphere. When using regular "on-rails" time warp, the warp
        /// rate is limited by <paramref name="maxRailsRate"/>, and when using physical
        /// time warp, the warp rate is limited by <paramref name="maxPhysicsRate"/>.
        /// </summary>
        /// <param name="ut">The universal time to warp to, in seconds.</param>
        /// <param name="maxRailsRate">The maximum warp rate in regular "on-rails" time warp.</param>
        /// <param name="maxPhysicsRate">The maximum warp rate in physical time warp.</param>
        /// <returns>When the time warp is complete.</returns>
        [KRPCProcedure]
        public static void WarpTo (double ut, float maxRailsRate = 100000, float maxPhysicsRate = 2)
        {
            float rate = Mathf.Clamp ((float)(ut - Planetarium.GetUniversalTime ()), 1f, maxRailsRate);

            if (CanRailsWarpAt ())
                RailsWarpAtRate (rate);
            else
                PhysicsWarpAtRate (Mathf.Min (rate, Math.Min (maxRailsRate, maxPhysicsRate)));

            if (Planetarium.GetUniversalTime () < ut)
                throw new YieldException (new ParameterizedContinuationVoid<double,float,float> (WarpTo, ut, maxRailsRate, maxPhysicsRate));
            else if (TimeWarp.CurrentRateIndex > 0) {
                SetWarpFactor (TimeWarp.Modes.HIGH, 0);
            }
        }

        static void SetWarpMode (TimeWarp.Modes mode)
        {
            if (TimeWarp.WarpMode != mode) {
                TimeWarp.fetch.Mode = mode;
                TimeWarp.SetRate (0, true);
            }
        }

        static void SetWarpFactor (TimeWarp.Modes mode, int factor)
        {
            SetWarpMode (mode);
            TimeWarp.SetRate (factor, false);
        }

        /// <summary>
        /// Warp using regular "on-rails" time warp at the given rate.
        /// </summary>
        static void RailsWarpAtRate (float rate)
        {
            SetWarpMode (TimeWarp.Modes.HIGH);
            if (rate < TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex])
                DecreaseRailsWarp ();
            else if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.warpRates.Length &&
                     rate >= TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex + 1])
                IncreaseRailsWarp ();
        }

        /// <summary>
        /// Decrease the regular "on-rails" time warp factor.
        /// </summary>
        static void DecreaseRailsWarp ()
        {
            if (TimeWarp.WarpMode != TimeWarp.Modes.HIGH)
                throw new InvalidOperationException ("Not in on-rails time warp");
            if (TimeWarp.CurrentRateIndex > 0)
                TimeWarp.SetRate (TimeWarp.CurrentRateIndex - 1, false);
        }

        /// <summary>
        /// Increase the regular "on-rails" time warp factor.
        /// </summary>
        static void IncreaseRailsWarp ()
        {
            if (TimeWarp.WarpMode != TimeWarp.Modes.HIGH)
                throw new InvalidOperationException ("Not in on-rails time warp");
            // Check if we're already warping at the maximum rate
            if (TimeWarp.CurrentRateIndex >= MaximumRailsWarpFactor)
                return;
            // Check that the previous rate update has taken effect
            float currentRate = TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex];
            if (Math.Abs (currentRate - TimeWarp.CurrentRate) > 0.01)
                return;
            // Increase the rate
            TimeWarp.SetRate (TimeWarp.CurrentRateIndex + 1, false);
        }

        /// <summary>
        /// Warp using physics time warp at the given rate.
        /// </summary>
        static void PhysicsWarpAtRate (float rate)
        {
            SetWarpMode (TimeWarp.Modes.LOW);
            if (rate < TimeWarp.fetch.physicsWarpRates [TimeWarp.CurrentRateIndex])
                DecreasePhysicsWarp ();
            else if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.physicsWarpRates.Length &&
                     rate >= TimeWarp.fetch.physicsWarpRates [TimeWarp.CurrentRateIndex + 1])
                IncreasePhysicsWarp ();
        }

        /// <summary>
        /// Decrease the physics time warp factor.
        /// </summary>
        static void DecreasePhysicsWarp ()
        {
            if (TimeWarp.WarpMode != TimeWarp.Modes.LOW)
                throw new InvalidOperationException ("Not in physical time warp");
            if (TimeWarp.CurrentRateIndex > 0)
                TimeWarp.SetRate (TimeWarp.CurrentRateIndex - 1, false);
        }

        /// <summary>
        /// Decrease the physics time warp factor.
        /// </summary>
        static void IncreasePhysicsWarp ()
        {
            if (TimeWarp.WarpMode != TimeWarp.Modes.LOW)
                throw new InvalidOperationException ("Not in physical time warp");
            // Check if we're already warping at the maximum rate
            if (TimeWarp.CurrentRateIndex + 1 >= TimeWarp.fetch.physicsWarpRates.Length)
                return;
            // Check that the previous rate update has taken effect
            var currentRate = TimeWarp.fetch.physicsWarpRates [TimeWarp.CurrentRateIndex];
            if (Math.Abs (currentRate - TimeWarp.CurrentRate) > 0.01)
                return;
            // Increase the rate
            TimeWarp.SetRate (TimeWarp.CurrentRateIndex + 1, false);
        }

        /// <summary>
        /// Converts a position vector from one reference frame to another.
        /// </summary>
        /// <param name="position">Position vector in reference frame <paramref name="from"/>.</param>
        /// <param name="from">The reference frame that the position vector is in.</param>
        /// <param name="to">The reference frame to covert the position vector to.</param>
        /// <returns>The corresponding position vector in reference frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple3 TransformPosition (Tuple3 position, ReferenceFrame from, ReferenceFrame to)
        {
            return to.PositionFromWorldSpace (from.PositionToWorldSpace (position.ToVector ())).ToTuple ();
        }

        /// <summary>
        /// Converts a direction vector from one reference frame to another.
        /// </summary>
        /// <param name="direction">Direction vector in reference frame <paramref name="from"/>.</param>
        /// <param name="from">The reference frame that the direction vector is in.</param>
        /// <param name="to">The reference frame to covert the direction vector to.</param>
        /// <returns>The corresponding direction vector in reference frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple3 TransformDirection (Tuple3 direction, ReferenceFrame from, ReferenceFrame to)
        {
            return to.DirectionFromWorldSpace (from.DirectionToWorldSpace (direction.ToVector ())).ToTuple ();
        }

        /// <summary>
        /// Converts a rotation from one reference frame to another.
        /// </summary>
        /// <param name="rotation">Rotation in reference frame <paramref name="from"/>.</param>
        /// <param name="from">The reference frame that the rotation is in.</param>
        /// <param name="to">The corresponding rotation in reference frame <paramref name="to"/>.</param>
        /// <returns>The corresponding rotation in reference frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple4 TransformRotation (Tuple4 rotation, ReferenceFrame from, ReferenceFrame to)
        {
            return to.RotationFromWorldSpace (from.RotationToWorldSpace (rotation.ToQuaternion ())).ToTuple ();
        }

        /// <summary>
        /// Converts a velocity vector (acting at the specified position vector) from one
        /// reference frame to another. The position vector is required to take the
        /// relative angular velocity of the reference frames into account.
        /// </summary>
        /// <param name="position">Position vector in reference frame <paramref name="from"/>.</param>
        /// <param name="velocity">Velocity vector in reference frame <paramref name="from"/>.</param>
        /// <param name="from">The reference frame that the position and velocity vectors are in.</param>
        /// <param name="to">The reference frame to covert the velocity vector to.</param>
        /// <returns>The corresponding velocity in reference frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple3 TransformVelocity (Tuple3 position, Tuple3 velocity, ReferenceFrame from, ReferenceFrame to)
        {
            var worldPosition = from.PositionToWorldSpace (position.ToVector ());
            var worldVelocity = from.VelocityToWorldSpace (position.ToVector (), velocity.ToVector ());
            return to.VelocityFromWorldSpace (worldPosition, worldVelocity).ToTuple ();
        }

        /// <summary>
        /// Whether <a href="http://forum.kerbalspaceprogram.com/index.php?/topic/19321-105-ferram-aerospace-research-v01557-johnson-21816/">Ferram Aerospace Research</a> is installed.
        /// </summary>
        [KRPCProperty]
        public static bool FARAvailable {
            get { return ExternalAPI.FAR.IsAvailable; }
        }
    }
}
