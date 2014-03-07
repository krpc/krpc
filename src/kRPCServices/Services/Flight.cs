using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPCServices.Services
{
    [KRPCService]
    public static class Flight
    {
        internal static VesselData VesselData { set; get; }

        [KRPCProperty]
        public static double Altitude {
            get { return VesselData.Altitude; }
        }

        [KRPCProperty]
        public static double Pitch {
            get { return VesselData.Pitch; }
        }

        [KRPCProperty]
        public static double Heading {
            get { return VesselData.Heading; }
        }

        [KRPCProperty]
        public static double Roll {
            get { return VesselData.Roll; }
        }
    }
}
