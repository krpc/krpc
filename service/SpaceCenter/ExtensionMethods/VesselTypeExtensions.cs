using System;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class VesselTypeExtensions
    {
        public static KRPC.SpaceCenter.Services.VesselType ToVesselType (this global::VesselType type)
        {
            switch (type) {
            case global::VesselType.Ship:
                return KRPC.SpaceCenter.Services.VesselType.Ship;
            case global::VesselType.Station:
                return KRPC.SpaceCenter.Services.VesselType.Station;
            case global::VesselType.Lander:
                return KRPC.SpaceCenter.Services.VesselType.Lander;
            case global::VesselType.Probe:
                return KRPC.SpaceCenter.Services.VesselType.Probe;
            case global::VesselType.Rover:
                return KRPC.SpaceCenter.Services.VesselType.Rover;
            case global::VesselType.Base:
                return KRPC.SpaceCenter.Services.VesselType.Base;
            case global::VesselType.Debris:
                return KRPC.SpaceCenter.Services.VesselType.Debris;
            default:
                throw new ArgumentException ("Unsupported vessel type");
            }
        }

        public static global::VesselType FromVesselType (this KRPC.SpaceCenter.Services.VesselType type)
        {
            switch (type) {
            case KRPC.SpaceCenter.Services.VesselType.Ship:
                return global::VesselType.Ship;
            case KRPC.SpaceCenter.Services.VesselType.Station:
                return global::VesselType.Station;
            case KRPC.SpaceCenter.Services.VesselType.Lander:
                return global::VesselType.Lander;
            case KRPC.SpaceCenter.Services.VesselType.Probe:
                return global::VesselType.Probe;
            case KRPC.SpaceCenter.Services.VesselType.Rover:
                return global::VesselType.Rover;
            case KRPC.SpaceCenter.Services.VesselType.Base:
                return global::VesselType.Base;
            case KRPC.SpaceCenter.Services.VesselType.Debris:
                return global::VesselType.Debris;
            default:
                throw new ArgumentException ("Unsupported vessel type");
            }
        }
    }
}
