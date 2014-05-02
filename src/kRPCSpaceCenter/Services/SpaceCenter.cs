using System;
using KRPC.Service.Attributes;
using KRPC.Continuations;
using UnityEngine;

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
        public static double UT {
            get { return Planetarium.GetUniversalTime (); }
        }

        [KRPCProcedure]
        public static void WarpTo (double UT, double maxRate = 100000)
        {
            float rate = Mathf.Clamp ((float)(UT - Planetarium.GetUniversalTime ()), 1f, (float)maxRate);

            var vessel = ActiveVessel;
            var flight = vessel.Flight ();
            var altitudeLimit = TimeWarp.fetch.GetAltitudeLimit (1, vessel.MainBody);

            if (vessel.Situation != VesselSituation.Landed && vessel.Situation != VesselSituation.Splashed && flight.Altitude < altitudeLimit)
                WarpPhysicsAtRate (Mathf.Min (rate, 2));
            else
                WarpRegularAtRate (rate);

            if (rate > 1)
                throw new YieldException (new ParameterizedContinuationVoid<double,double> (WarpTo, UT, maxRate));
            else
                TimeWarp.SetRate (0, false);
        }

        static void WarpPhysicsAtRate (float rate)
        {
            throw new NotImplementedException ();
        }

        static void WarpRegularAtRate (float rate)
        {
            if (rate < TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex])
                DecreaseRegularWarp ();
            if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.warpRates.Length &&
                rate > TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex + 1])
                IncreaseRegularWarp ();
        }

        static double warpIncreaseAttemptTime = 0;

        static void DecreaseRegularWarp ()
        {
            if (TimeWarp.CurrentRateIndex > 0)
                TimeWarp.SetRate (TimeWarp.CurrentRateIndex - 1, false);
        }

        static void IncreaseRegularWarp ()
        {
            float currentRate = TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex];
            if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.warpRates.Length &&
                (currentRate == TimeWarp.CurrentRate || currentRate <= 1) &&
                Math.Abs (Planetarium.GetUniversalTime () - warpIncreaseAttemptTime) > 2) {
                warpIncreaseAttemptTime = Planetarium.GetUniversalTime ();
                TimeWarp.SetRate (TimeWarp.CurrentRateIndex + 1, false);
            }
        }
    }
}
