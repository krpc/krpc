using System;
using KRPCSpaceCenter;

namespace KRPCSpaceCenter.ExtensionMethods
{
    public static class VesselSItuationExtensions
    {
        public static KRPCSpaceCenter.Services.VesselSituation ToVesselSituation (this global::Vessel.Situations situation)
        {
            switch (situation) {
            case global::Vessel.Situations.DOCKED:
                return KRPCSpaceCenter.Services.VesselSituation.Docked;
            case global::Vessel.Situations.ESCAPING:
                return KRPCSpaceCenter.Services.VesselSituation.Escaping;
            case global::Vessel.Situations.FLYING:
                return KRPCSpaceCenter.Services.VesselSituation.Flying;
            case global::Vessel.Situations.LANDED:
                return KRPCSpaceCenter.Services.VesselSituation.Landed;
            case global::Vessel.Situations.ORBITING:
                return KRPCSpaceCenter.Services.VesselSituation.Orbiting;
            case global::Vessel.Situations.PRELAUNCH:
                return KRPCSpaceCenter.Services.VesselSituation.PreLaunch;
            case global::Vessel.Situations.SPLASHED:
                return KRPCSpaceCenter.Services.VesselSituation.Splashed;
            case global::Vessel.Situations.SUB_ORBITAL:
                return KRPCSpaceCenter.Services.VesselSituation.SubOrbital;
            default:
                throw new ArgumentException ("Unsupported vessel situation");
            }
        }

        public static global::Vessel.Situations FromVesselSituation (this KRPCSpaceCenter.Services.VesselSituation situation)
        {
            switch (situation) {
            case KRPCSpaceCenter.Services.VesselSituation.Docked:
                return global::Vessel.Situations.DOCKED;
            case KRPCSpaceCenter.Services.VesselSituation.Escaping:
                return global::Vessel.Situations.ESCAPING;
            case KRPCSpaceCenter.Services.VesselSituation.Flying:
                return global::Vessel.Situations.FLYING;
            case KRPCSpaceCenter.Services.VesselSituation.Landed:
                return global::Vessel.Situations.LANDED;
            case KRPCSpaceCenter.Services.VesselSituation.Orbiting:
                return global::Vessel.Situations.ORBITING;
            case KRPCSpaceCenter.Services.VesselSituation.PreLaunch:
                return global::Vessel.Situations.PRELAUNCH;
            case KRPCSpaceCenter.Services.VesselSituation.Splashed:
                return global::Vessel.Situations.SPLASHED;
            case KRPCSpaceCenter.Services.VesselSituation.SubOrbital:
                return global::Vessel.Situations.SUB_ORBITAL;
            default:
                throw new ArgumentException ("Unsupported vessel situation");
            }
        }
    }
}
