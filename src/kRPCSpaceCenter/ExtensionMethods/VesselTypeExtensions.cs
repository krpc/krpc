using System;
using KRPCSpaceCenter;

namespace KRPCSpaceCenter.ExtensionMethods
{
    public static class VesselTypeExtensions
    {
        public static KRPCSpaceCenter.Services.VesselType ToVesselType (this global::VesselType type)
        {
            switch (type) {
            case global::VesselType.Ship:
                return KRPCSpaceCenter.Services.VesselType.Ship;
            case global::VesselType.Station:
                return KRPCSpaceCenter.Services.VesselType.Station;
            case global::VesselType.Lander:
                return KRPCSpaceCenter.Services.VesselType.Lander;
            case global::VesselType.Probe:
                return KRPCSpaceCenter.Services.VesselType.Probe;
            case global::VesselType.Rover:
                return KRPCSpaceCenter.Services.VesselType.Rover;
            case global::VesselType.Base:
                return KRPCSpaceCenter.Services.VesselType.Base;
            case global::VesselType.Debris:
                return KRPCSpaceCenter.Services.VesselType.Debris;
            default:
                throw new ArgumentException ("Unsupported vessel type");
            }
        }

        public static global::VesselType FromVesselType (this KRPCSpaceCenter.Services.VesselType type)
        {
            switch (type) {
            case KRPCSpaceCenter.Services.VesselType.Ship:
                return global::VesselType.Ship;
            case KRPCSpaceCenter.Services.VesselType.Station:
                return global::VesselType.Station;
            case KRPCSpaceCenter.Services.VesselType.Lander:
                return global::VesselType.Lander;
            case KRPCSpaceCenter.Services.VesselType.Probe:
                return global::VesselType.Probe;
            case KRPCSpaceCenter.Services.VesselType.Rover:
                return global::VesselType.Rover;
            case KRPCSpaceCenter.Services.VesselType.Base:
                return global::VesselType.Base;
            case KRPCSpaceCenter.Services.VesselType.Debris:
                return global::VesselType.Debris;
            default:
                throw new ArgumentException ("Unsupported vessel type");
            }
        }
    }
}
