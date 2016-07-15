using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class VesselSituationExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
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
    }
}
