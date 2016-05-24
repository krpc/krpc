using System;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class VesselSituationExtensions
    {
        public static KRPC.SpaceCenter.Services.VesselSituation ToVesselSituation (this Vessel.Situations situation)
        {
            switch (situation) {
            case Vessel.Situations.DOCKED:
                return KRPC.SpaceCenter.Services.VesselSituation.Docked;
            case Vessel.Situations.ESCAPING:
                return KRPC.SpaceCenter.Services.VesselSituation.Escaping;
            case Vessel.Situations.FLYING:
                return KRPC.SpaceCenter.Services.VesselSituation.Flying;
            case Vessel.Situations.LANDED:
                return KRPC.SpaceCenter.Services.VesselSituation.Landed;
            case Vessel.Situations.ORBITING:
                return KRPC.SpaceCenter.Services.VesselSituation.Orbiting;
            case Vessel.Situations.PRELAUNCH:
                return KRPC.SpaceCenter.Services.VesselSituation.PreLaunch;
            case Vessel.Situations.SPLASHED:
                return KRPC.SpaceCenter.Services.VesselSituation.Splashed;
            case Vessel.Situations.SUB_ORBITAL:
                return KRPC.SpaceCenter.Services.VesselSituation.SubOrbital;
            default:
                throw new ArgumentOutOfRangeException ("situation");
            }
        }

        public static Vessel.Situations FromVesselSituation (this KRPC.SpaceCenter.Services.VesselSituation situation)
        {
            switch (situation) {
            case KRPC.SpaceCenter.Services.VesselSituation.Docked:
                return Vessel.Situations.DOCKED;
            case KRPC.SpaceCenter.Services.VesselSituation.Escaping:
                return Vessel.Situations.ESCAPING;
            case KRPC.SpaceCenter.Services.VesselSituation.Flying:
                return Vessel.Situations.FLYING;
            case KRPC.SpaceCenter.Services.VesselSituation.Landed:
                return Vessel.Situations.LANDED;
            case KRPC.SpaceCenter.Services.VesselSituation.Orbiting:
                return Vessel.Situations.ORBITING;
            case KRPC.SpaceCenter.Services.VesselSituation.PreLaunch:
                return Vessel.Situations.PRELAUNCH;
            case KRPC.SpaceCenter.Services.VesselSituation.Splashed:
                return Vessel.Situations.SPLASHED;
            case KRPC.SpaceCenter.Services.VesselSituation.SubOrbital:
                return Vessel.Situations.SUB_ORBITAL;
            default:
                throw new ArgumentOutOfRangeException ("situation");
            }
        }
    }
}
