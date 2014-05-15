using System;
using KRPC.Service.Attributes;
using KRPC.Continuations;
using UnityEngine;
using System.Collections.Generic;

namespace KRPCSpaceCenter.Services
{
    [KRPCService]
    public static class SpaceCenter
    {
        [KRPCProperty]
        public static Vessel ActiveVessel {
            get { return new Vessel (FlightGlobals.ActiveVessel); }
        }

        static IDictionary<Guid, Vessel> vesselsCache = new Dictionary<Guid, Vessel> ();
        static IDictionary<string, CelestialBody> bodiesCache = new Dictionary<string, CelestialBody> ();

        [KRPCProperty]
        public static IList<Vessel> Vessels {
            get {
                var vessels = new List<Vessel> ();
                foreach (var vessel in FlightGlobals.Vessels) {
                    if (!vesselsCache.ContainsKey (vessel.id))
                        vesselsCache [vessel.id] = new Vessel (vessel);
                    vessels.Add (vesselsCache [vessel.id]);
                }
                return vessels;
            }
        }

        [KRPCProperty]
        public static IDictionary<string,CelestialBody> Bodies {
            get {
                // Note: Assumes body.name is a guid
                var bodies = new Dictionary<string, CelestialBody> ();
                foreach (var body in FlightGlobals.Bodies) {
                    if (!bodiesCache.ContainsKey (body.name))
                        bodiesCache [body.name] = new CelestialBody (body);
                    bodies [body.name] = bodiesCache [body.name];
                }
                return bodies;
            }
        }

        [KRPCProperty]
        public static double UT {
            get { return Planetarium.GetUniversalTime (); }
        }

        [KRPCProcedure]
        public static void WarpTo (double UT, double maxRate = 100000)
        {
            float rate = Mathf.Clamp ((float)(UT - Planetarium.GetUniversalTime ()), 1f, (float)maxRate);

            var vessel = ActiveVessel;
            var flight = vessel.Flight ();
            var altitudeLimit = TimeWarp.fetch.GetAltitudeLimit (1, vessel.Orbit.Body.Body);

            if (vessel.Situation != VesselSituation.Landed && vessel.Situation != VesselSituation.Splashed && flight.Altitude < altitudeLimit)
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
            if (flight.Altitude < TimeWarp.fetch.GetAltitudeLimit (TimeWarp.CurrentRateIndex + 1, vessel.Orbit.Body.Body))
                return;
            warpIncreaseAttemptTime = Planetarium.GetUniversalTime ();
            TimeWarp.SetRate (TimeWarp.CurrentRateIndex + 1, false);
        }
    }
}
