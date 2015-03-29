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
        public static double G {
            get { return 6.673e-11; }
        }

        [KRPCProcedure]
        public static void WarpTo (double UT, double maxRate = 100000)
        {
            float rate = Mathf.Clamp ((float)(UT - Planetarium.GetUniversalTime ()), 1f, (float)maxRate);

            var vessel = ActiveVessel;
            var flight = vessel.Flight (ReferenceFrame.Orbital (vessel.InternalVessel));
            var altitudeLimit = TimeWarp.fetch.GetAltitudeLimit (1, vessel.Orbit.Body.InternalBody);

            if (vessel.Situation != VesselSituation.Landed && vessel.Situation != VesselSituation.Splashed && flight.MeanAltitude < altitudeLimit)
                WarpPhysicsAtRate (vessel, flight, Mathf.Min (rate, 2));
            else
                WarpRegularAtRate (vessel, flight, rate);

            if (rate > 1)
                throw new YieldException (new ParameterizedContinuationVoid<double,double> (WarpTo, UT, maxRate));
            else
                TimeWarp.SetRate (0, false);
        }

        static void WarpPhysicsAtRate (Vessel vessel, Flight flight, float rate)
        {
            throw new NotImplementedException ();
        }

        static void WarpRegularAtRate (Vessel vessel, Flight flight, float rate)
        {
            if (rate < TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex])
                DecreaseRegularWarp ();
            if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.warpRates.Length &&
                rate > TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex + 1])
                IncreaseRegularWarp (vessel, flight);
        }

        static void DecreaseRegularWarp ()
        {
            // Check we aren't warping the minimum amount
            if (TimeWarp.CurrentRateIndex == 0)
                return;
            TimeWarp.SetRate (TimeWarp.CurrentRateIndex - 1, false);
        }

        static double warpIncreaseAttemptTime = 0;

        static void IncreaseRegularWarp (Vessel vessel, Flight flight)
        {
            // Check if we're already warping at the maximum rate
            if (TimeWarp.CurrentRateIndex + 1 >= TimeWarp.fetch.warpRates.Length)
                return;
            // Check that the previous rate update has taken effect
            float currentRate = TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex];
            if (!(currentRate == TimeWarp.CurrentRate || currentRate <= 1))
                return;
            // Check we don't update the warp rate more than once every 2 seconds
            if (Math.Abs (Planetarium.GetUniversalTime () - warpIncreaseAttemptTime) < 2)
                return;
            // Check we don't increase the warp rate beyond the altitude limit
            if (flight.MeanAltitude < TimeWarp.fetch.GetAltitudeLimit (TimeWarp.CurrentRateIndex + 1, vessel.Orbit.Body.InternalBody))
                return;
            warpIncreaseAttemptTime = Planetarium.GetUniversalTime ();
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
