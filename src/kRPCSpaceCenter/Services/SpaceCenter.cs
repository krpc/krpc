using System;
using KRPC.Service.Attributes;
using KRPC.Continuations;
using UnityEngine;
using System.Collections.Generic;
using KRPCSpaceCenter.ExtensionMethods;
using Tuple3 = KRPC.Utils.Tuple<double,double,double>;
using Tuple2 = KRPC.Utils.Tuple<double,double>;

namespace KRPCSpaceCenter.Services
{
    [KRPCService]
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

        /// <summary>
        /// Given a position as a vector in reference frame `from`, convert it to a position in reference frame `to`.
        /// </summary>
        [KRPCProcedure]
        public static Tuple3 TransformPosition (Tuple3 position, ReferenceFrame from, ReferenceFrame to)
        {
            return to.PositionFromWorldSpace (from.PositionToWorldSpace (position.ToVector ())).ToTuple ();
        }

        /// <summary>
        /// Given a direction as a 3D unit vector in reference frame `from`, convert it to a unit vector in reference frame `to`.
        /// </summary>
        [KRPCProcedure]
        public static Tuple3 TransformDirection (Tuple3 direction, ReferenceFrame from, ReferenceFrame to)
        {
            return to.DirectionFromWorldSpace (from.DirectionToWorldSpace (direction.ToVector ())).ToTuple ();
        }

        /// <summary>
        /// Given a velocity as a 3D vector in reference frame `from`, convert it to a velocity in reference frame `to`.
        /// </summary>
        [KRPCProcedure]
        public static Tuple3 TransformVelocity (Tuple3 velocity, ReferenceFrame from, ReferenceFrame to)
        {
            return to.VelocityFromWorldSpace (from.VelocityToWorldSpace (velocity.ToVector ())).ToTuple ();
        }

        [KRPCProcedure]
        public static Tuple2 GetPitchHeading (Tuple3 direction)
        {
            // FIXME: QuarternionD.FromToRotation is not available at runtime !?
            QuaternionD rotation = Quaternion.FromToRotation (Vector3d.forward, direction.ToVector ());
            // FIXME: why doesn't rotation.PitchHeadingRoll work here?
            //return rotation.PitchHeadingRoll ().ToTuple ();
            var eulerAngles = ((Quaternion)rotation).eulerAngles;
            var pitch = -Math.Abs (((eulerAngles.x + 270f) % 360f) - 180f) + 90f;
            var yaw = eulerAngles.y;
            return new Tuple2 (pitch, yaw);
        }
    }
}
