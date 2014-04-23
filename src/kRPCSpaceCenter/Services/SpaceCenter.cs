using System;
using KRPC.Service.Attributes;

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
        public static void WarpTo (double UT)
        {
            throw new NotImplementedException ();
        }
    }
}
