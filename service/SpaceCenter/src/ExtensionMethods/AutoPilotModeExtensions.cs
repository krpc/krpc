using System;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class AutoPilotModeExtensions
    {
        public static KRPC.SpaceCenter.Services.SASMode ToSASMode (this VesselAutopilot.AutopilotMode mode)
        {
            switch (mode) {
            case VesselAutopilot.AutopilotMode.StabilityAssist:
                return KRPC.SpaceCenter.Services.SASMode.StabilityAssist;
            case VesselAutopilot.AutopilotMode.Maneuver:
                return KRPC.SpaceCenter.Services.SASMode.Maneuver;
            case VesselAutopilot.AutopilotMode.Prograde:
                return KRPC.SpaceCenter.Services.SASMode.Prograde;
            case VesselAutopilot.AutopilotMode.Retrograde:
                return KRPC.SpaceCenter.Services.SASMode.Retrograde;
            case VesselAutopilot.AutopilotMode.Normal:
                return KRPC.SpaceCenter.Services.SASMode.Normal;
            case VesselAutopilot.AutopilotMode.Antinormal:
                return KRPC.SpaceCenter.Services.SASMode.AntiNormal;
            case VesselAutopilot.AutopilotMode.RadialIn:
                return KRPC.SpaceCenter.Services.SASMode.Radial;
            case VesselAutopilot.AutopilotMode.RadialOut:
                return KRPC.SpaceCenter.Services.SASMode.AntiRadial;
            case VesselAutopilot.AutopilotMode.Target:
                return KRPC.SpaceCenter.Services.SASMode.Target;
            case VesselAutopilot.AutopilotMode.AntiTarget:
                return KRPC.SpaceCenter.Services.SASMode.AntiTarget;
            default:
                throw new ArgumentOutOfRangeException ("mode");
            }
        }

        public static VesselAutopilot.AutopilotMode FromSASMode (this KRPC.SpaceCenter.Services.SASMode mode)
        {
            switch (mode) {
            case KRPC.SpaceCenter.Services.SASMode.StabilityAssist:
                return VesselAutopilot.AutopilotMode.StabilityAssist;
            case KRPC.SpaceCenter.Services.SASMode.Maneuver:
                return VesselAutopilot.AutopilotMode.Maneuver;
            case KRPC.SpaceCenter.Services.SASMode.Prograde:
                return VesselAutopilot.AutopilotMode.Prograde;
            case KRPC.SpaceCenter.Services.SASMode.Retrograde:
                return VesselAutopilot.AutopilotMode.Retrograde;
            case KRPC.SpaceCenter.Services.SASMode.Normal:
                return VesselAutopilot.AutopilotMode.Normal;
            case KRPC.SpaceCenter.Services.SASMode.AntiNormal:
                return VesselAutopilot.AutopilotMode.Antinormal;
            case KRPC.SpaceCenter.Services.SASMode.Radial:
                return VesselAutopilot.AutopilotMode.RadialIn;
            case KRPC.SpaceCenter.Services.SASMode.AntiRadial:
                return VesselAutopilot.AutopilotMode.RadialOut;
            case KRPC.SpaceCenter.Services.SASMode.Target:
                return VesselAutopilot.AutopilotMode.Target;
            case KRPC.SpaceCenter.Services.SASMode.AntiTarget:
                return VesselAutopilot.AutopilotMode.AntiTarget;
            default:
                throw new ArgumentOutOfRangeException ("mode");
            }
        }
    }
}
