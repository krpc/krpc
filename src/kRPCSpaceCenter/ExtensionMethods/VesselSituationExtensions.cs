using System;

namespace KRPCSpaceCenter.ExtensionMethods
{
    static class VesselSituationExtensions
    {
        public static KRPCSpaceCenter.Services.VesselSituation ToVesselSituation (this Vessel.Situations situation)
        {
            switch (situation) {
            case Vessel.Situations.DOCKED:
                return KRPCSpaceCenter.Services.VesselSituation.Docked;
            case Vessel.Situations.ESCAPING:
                return KRPCSpaceCenter.Services.VesselSituation.Escaping;
            case Vessel.Situations.FLYING:
                return KRPCSpaceCenter.Services.VesselSituation.Flying;
            case Vessel.Situations.LANDED:
                return KRPCSpaceCenter.Services.VesselSituation.Landed;
            case Vessel.Situations.ORBITING:
                return KRPCSpaceCenter.Services.VesselSituation.Orbiting;
            case Vessel.Situations.PRELAUNCH:
                return KRPCSpaceCenter.Services.VesselSituation.PreLaunch;
            case Vessel.Situations.SPLASHED:
                return KRPCSpaceCenter.Services.VesselSituation.Splashed;
            case Vessel.Situations.SUB_ORBITAL:
                return KRPCSpaceCenter.Services.VesselSituation.SubOrbital;
            default:
                throw new ArgumentException ("Unsupported vessel situation");
            }
        }

        public static Vessel.Situations FromVesselSituation (this KRPCSpaceCenter.Services.VesselSituation situation)
        {
            switch (situation) {
            case KRPCSpaceCenter.Services.VesselSituation.Docked:
                return Vessel.Situations.DOCKED;
            case KRPCSpaceCenter.Services.VesselSituation.Escaping:
                return Vessel.Situations.ESCAPING;
            case KRPCSpaceCenter.Services.VesselSituation.Flying:
                return Vessel.Situations.FLYING;
            case KRPCSpaceCenter.Services.VesselSituation.Landed:
                return Vessel.Situations.LANDED;
            case KRPCSpaceCenter.Services.VesselSituation.Orbiting:
                return Vessel.Situations.ORBITING;
            case KRPCSpaceCenter.Services.VesselSituation.PreLaunch:
                return Vessel.Situations.PRELAUNCH;
            case KRPCSpaceCenter.Services.VesselSituation.Splashed:
                return Vessel.Situations.SPLASHED;
            case KRPCSpaceCenter.Services.VesselSituation.SubOrbital:
                return Vessel.Situations.SUB_ORBITAL;
            default:
                throw new ArgumentException ("Unsupported vessel situation");
            }
        }
    }
}
