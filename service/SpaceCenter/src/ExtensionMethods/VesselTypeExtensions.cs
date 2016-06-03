using System;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class VesselTypeExtensions
    {
        public static KRPC.SpaceCenter.Services.VesselType ToVesselType (this VesselType type)
        {
            switch (type) {
            case VesselType.Ship:
                return KRPC.SpaceCenter.Services.VesselType.Ship;
            case VesselType.Station:
                return KRPC.SpaceCenter.Services.VesselType.Station;
            case VesselType.Lander:
                return KRPC.SpaceCenter.Services.VesselType.Lander;
            case VesselType.Probe:
                return KRPC.SpaceCenter.Services.VesselType.Probe;
            case VesselType.Rover:
                return KRPC.SpaceCenter.Services.VesselType.Rover;
            case VesselType.Base:
                return KRPC.SpaceCenter.Services.VesselType.Base;
            case VesselType.Debris:
                return KRPC.SpaceCenter.Services.VesselType.Debris;
            default:
                throw new ArgumentOutOfRangeException ("type");
            }
        }

        public static VesselType FromVesselType (this KRPC.SpaceCenter.Services.VesselType type)
        {
            switch (type) {
            case KRPC.SpaceCenter.Services.VesselType.Ship:
                return VesselType.Ship;
            case KRPC.SpaceCenter.Services.VesselType.Station:
                return VesselType.Station;
            case KRPC.SpaceCenter.Services.VesselType.Lander:
                return VesselType.Lander;
            case KRPC.SpaceCenter.Services.VesselType.Probe:
                return VesselType.Probe;
            case KRPC.SpaceCenter.Services.VesselType.Rover:
                return VesselType.Rover;
            case KRPC.SpaceCenter.Services.VesselType.Base:
                return VesselType.Base;
            case KRPC.SpaceCenter.Services.VesselType.Debris:
                return VesselType.Debris;
            default:
                throw new ArgumentOutOfRangeException ("type");
            }
        }
    }
}
