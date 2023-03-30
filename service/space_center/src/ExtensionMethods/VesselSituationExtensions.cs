using System;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class VesselSituationExtensions
    {
        public static Services.VesselSituation ToVesselSituation (this Vessel.Situations situation)
        {
            switch (situation) {
            case Vessel.Situations.DOCKED:
                return Services.VesselSituation.Docked;
            case Vessel.Situations.ESCAPING:
                return Services.VesselSituation.Escaping;
            case Vessel.Situations.FLYING:
                return Services.VesselSituation.Flying;
            case Vessel.Situations.LANDED:
                return Services.VesselSituation.Landed;
            case Vessel.Situations.ORBITING:
                return Services.VesselSituation.Orbiting;
            case Vessel.Situations.PRELAUNCH:
                return Services.VesselSituation.PreLaunch;
            case Vessel.Situations.SPLASHED:
                return Services.VesselSituation.Splashed;
            case Vessel.Situations.SUB_ORBITAL:
                return Services.VesselSituation.SubOrbital;
            default:
                throw new ArgumentOutOfRangeException (nameof (situation));
            }
        }
    }
}
