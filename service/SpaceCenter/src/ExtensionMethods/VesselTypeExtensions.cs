using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
    static class VesselTypeExtensions
    {
        public static Services.VesselType ToVesselType (this VesselType type)
        {
            switch (type) {
            case VesselType.Base:
                return Services.VesselType.Base;
            case VesselType.Debris:
                return Services.VesselType.Debris;
            case VesselType.Lander:
                return Services.VesselType.Lander;
            case VesselType.Plane:
                return Services.VesselType.Plane;
            case VesselType.Probe:
                return Services.VesselType.Probe;
            case VesselType.Relay:
                return Services.VesselType.Relay;
            case VesselType.Rover:
                return Services.VesselType.Rover;
            case VesselType.Ship:
                return Services.VesselType.Ship;
            case VesselType.Station:
                return Services.VesselType.Station;
            default:
                throw new ArgumentOutOfRangeException (nameof (type));
            }
        }

        public static VesselType FromVesselType (this Services.VesselType type)
        {
            switch (type) {
            case Services.VesselType.Base:
                return VesselType.Base;
            case Services.VesselType.Debris:
                return VesselType.Debris;
            case Services.VesselType.Lander:
                return VesselType.Lander;
            case Services.VesselType.Plane:
                return VesselType.Plane;
            case Services.VesselType.Probe:
                return VesselType.Probe;
            case Services.VesselType.Relay:
                return VesselType.Relay;
            case Services.VesselType.Rover:
                return VesselType.Rover;
            case Services.VesselType.Ship:
                return VesselType.Ship;
            case Services.VesselType.Station:
                return VesselType.Station;
            default:
                throw new ArgumentOutOfRangeException (nameof (type));
            }
        }
    }
}
