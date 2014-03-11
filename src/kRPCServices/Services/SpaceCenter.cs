using KRPC.Service.Attributes;
using UnityEngine;
using KRPC.Schema.Geometry;

namespace KRPCServices.Services
{
    [KRPCService]
    public static class SpaceCenter
    {
        [KRPCProperty]
        public static Vessel ActiveVessel {
            get { return new Vessel (FlightGlobals.ActiveVessel); }
            // TODO: implement set to switch active vessel
        }
    }
}
