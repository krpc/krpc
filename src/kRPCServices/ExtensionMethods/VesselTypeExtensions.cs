using System;
using KRPCServices;

namespace KRPCServices.ExtensionMethods
{
    public static class VesselTypeExtensions
    {
        public static KRPCServices.Services.VesselType ToVesselType (this global::VesselType type)
        {
            switch (type) {
            case global::VesselType.Ship:
                return KRPCServices.Services.VesselType.Ship;
            case global::VesselType.Station:
                return KRPCServices.Services.VesselType.Station;
            case global::VesselType.Lander:
                return KRPCServices.Services.VesselType.Lander;
            case global::VesselType.Probe:
                return KRPCServices.Services.VesselType.Probe;
            case global::VesselType.Rover:
                return KRPCServices.Services.VesselType.Rover;
            case global::VesselType.Base:
                return KRPCServices.Services.VesselType.Base;
            case global::VesselType.Debris:
                return KRPCServices.Services.VesselType.Debris;
            default:
                throw new ArgumentException ("Unsupported vessel type");
            }
        }

        public static global::VesselType FromVesselType (this KRPCServices.Services.VesselType type)
        {
            switch (type) {
            case KRPCServices.Services.VesselType.Ship:
                return global::VesselType.Ship;
            case KRPCServices.Services.VesselType.Station:
                return global::VesselType.Station;
            case KRPCServices.Services.VesselType.Lander:
                return global::VesselType.Lander;
            case KRPCServices.Services.VesselType.Probe:
                return global::VesselType.Probe;
            case KRPCServices.Services.VesselType.Rover:
                return global::VesselType.Rover;
            case KRPCServices.Services.VesselType.Base:
                return global::VesselType.Base;
            case KRPCServices.Services.VesselType.Debris:
                return global::VesselType.Debris;
            default:
                throw new ArgumentException ("Unsupported vessel type");
            }
        }
    }
}
