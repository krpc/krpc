using KRPC.Service.Attributes;
using UnityEngine;
using KRPC.Schema.Geometry;

namespace KRPCServices.Services
{
    [KRPCService]
    public static class Flight
    {
        [KRPCProperty]
        public static Vessel ActiveVessel {
            get { return new Vessel (FlightGlobals.ActiveVessel); }
        }
    }
}
