using System;
using System.Collections.Generic;
using KRPC.Continuations;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPCSpaceCenter.ExtensionMethods;
using UnityEngine;
using Tuple2 = KRPC.Utils.Tuple<double, double>;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPCSpaceCenter.Services
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum WarpMode
    {
        Rails,
        Physics,
        None
    }

    [KRPCService (GameScene = GameScene.Flight)]
    public static class SpaceCenter
    {
        [KRPCProperty]
        public static Vessel ActiveVessel {
            get { return new Vessel (FlightGlobals.ActiveVessel); }
        }

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

        [KRPCProperty]
        public static IDictionary<string,CelestialBody> Bodies {
            get {
                var bodies = new Dictionary<string, CelestialBody> ();
                foreach (var body in FlightGlobals.Bodies)
                    bodies [body.name] = new CelestialBody (body);
                return bodies;
            }
        }

        [KRPCProperty]
        public static CelestialBody TargetBody {
            get {
                var target = FlightGlobals.fetch.VesselTarget;
                return target is global::CelestialBody ? new CelestialBody (target as global::CelestialBody) : null;
            }
            set { FlightGlobals.fetch.SetVesselTarget (value == null ? null : value.InternalBody); }
        }

        [KRPCProperty]
        public static Vessel TargetVessel {
            get {
                var target = FlightGlobals.fetch.VesselTarget;
                return target is global::Vessel ? new Vessel (target as global::Vessel) : null;
            }
            set { FlightGlobals.fetch.SetVesselTarget (value == null ? null : value.InternalVessel); }
        }

        [KRPCProperty]
        public static Parts.DockingPort TargetDockingPort {
            get {
                var target = FlightGlobals.fetch.VesselTarget;
                var part = target is ModuleDockingNode ? new Parts.Part ((target as ModuleDockingNode).part) : null;
                return part != null ? new Parts.DockingPort (part) : null;
            }
            set { FlightGlobals.fetch.SetVesselTarget (value == null ? null : value.InternalPort); }
        }

        [KRPCProcedure]
        public static void ClearTarget ()
        {
            FlightGlobals.fetch.SetVesselTarget (null);
        }

        [KRPCProperty]
        public static double UT {
            get { return Planetarium.GetUniversalTime (); }
        }

        [KRPCProperty]
        public static float G {
            get { return 6.673e-11f; }
        }

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

        [KRPCProperty]
        public static float WarpRate {
            get { return TimeWarp.CurrentRate; }
        }

        [KRPCProperty]
        public static float WarpFactor {
            get { return TimeWarp.CurrentRateIndex; }
        }

        [KRPCProperty]
        public static int RailsWarpFactor {
            get { return WarpMode == WarpMode.Rails ? TimeWarp.CurrentRateIndex : 0; }
            set { SetWarpFactor (TimeWarp.Modes.HIGH, value.Clamp (0, MaximumRailsWarpFactor)); }
        }

        [KRPCProperty]
        public static int PhysicsWarpFactor {
            get { return WarpMode == WarpMode.Physics ? TimeWarp.CurrentRateIndex : 0; }
            set { SetWarpFactor (TimeWarp.Modes.LOW, value.Clamp (0, 3)); }
        }

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
            var altitude = ActiveVessel.InternalVessel.mainBody.GetAltitude (ActiveVessel.InternalVessel.CoM);
            var altitudeLimit = TimeWarp.fetch.GetAltitudeLimit (factor, ActiveVessel.InternalVessel.mainBody);
            if (altitude < altitudeLimit)
                return false;
            // Throttle is non-zero
            if (FlightInputHandler.state.mainThrottle > 0f)
                return false;
            return true;
        }

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

        [KRPCProcedure]
        public static void WarpTo (double UT, float maxRate = 100000, float maxPhysicsRate = 2)
        {
            float rate = Mathf.Clamp ((float)(UT - Planetarium.GetUniversalTime ()), 1f, maxRate);

            if (CanRailsWarpAt ())
                RailsWarpAtRate (rate);
            else
                PhysicsWarpAtRate (Mathf.Min (rate, Math.Min (maxRate, maxPhysicsRate)));

            if (Planetarium.GetUniversalTime () < UT)
                throw new YieldException (new ParameterizedContinuationVoid<double,float,float> (WarpTo, UT, maxRate, maxPhysicsRate));
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
            else if (rate > TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex + 1])
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
            else if (rate > TimeWarp.fetch.physicsWarpRates [TimeWarp.CurrentRateIndex + 1])
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

        [KRPCProcedure]
        public static Tuple3 TransformPosition (Tuple3 position, ReferenceFrame from, ReferenceFrame to)
        {
            return to.PositionFromWorldSpace (from.PositionToWorldSpace (position.ToVector ())).ToTuple ();
        }

        [KRPCProcedure]
        public static Tuple3 TransformDirection (Tuple3 direction, ReferenceFrame from, ReferenceFrame to)
        {
            return to.DirectionFromWorldSpace (from.DirectionToWorldSpace (direction.ToVector ())).ToTuple ();
        }

        [KRPCProcedure]
        public static Tuple4 TransformRotation (Tuple4 rotation, ReferenceFrame from, ReferenceFrame to)
        {
            return to.RotationFromWorldSpace (from.RotationToWorldSpace (rotation.ToQuaternion ())).ToTuple ();
        }

        [KRPCProcedure]
        public static Tuple3 TransformVelocity (Tuple3 position, Tuple3 velocity, ReferenceFrame from, ReferenceFrame to)
        {
            var worldPosition = from.PositionToWorldSpace (position.ToVector ());
            var worldVelocity = from.VelocityToWorldSpace (position.ToVector (), velocity.ToVector ());
            return to.VelocityFromWorldSpace (worldPosition, worldVelocity).ToTuple ();
        }

        [KRPCProperty]
        public static bool FARAvailable {
            get { return ExternalAPI.FAR.IsAvailable; }
        }

        [KRPCProperty]
        public static bool RemoteTechAvailable {
            get { return ExternalAPI.RemoteTech.IsAvailable; }
        }

        [KRPCProcedure]
        public static void DrawDirection (Tuple3 direction, ReferenceFrame referenceFrame, Tuple3 color, float length = 10f)
        {
            DrawAddon.AddDirection (direction.ToVector (), referenceFrame, color, length);
        }

        [KRPCProcedure]
        public static void ClearDirections ()
        {
            DrawAddon.ClearDirections ();
        }
    }
}
