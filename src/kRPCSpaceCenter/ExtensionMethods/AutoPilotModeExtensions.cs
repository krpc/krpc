using System;

namespace KRPCSpaceCenter.ExtensionMethods
{
    public static class AutoPilotModeExtensions
    {
        public static KRPCSpaceCenter.Services.SASMode ToSASMode (this VesselAutopilot.AutopilotMode mode)
        {
            switch (mode) {
            case VesselAutopilot.AutopilotMode.StabilityAssist:
                return KRPCSpaceCenter.Services.SASMode.StabilityAssist;
            case VesselAutopilot.AutopilotMode.Maneuver:
                return KRPCSpaceCenter.Services.SASMode.Maneuver;
            case VesselAutopilot.AutopilotMode.Prograde:
                return KRPCSpaceCenter.Services.SASMode.Prograde;
            case VesselAutopilot.AutopilotMode.Retrograde:
                return KRPCSpaceCenter.Services.SASMode.Retrograde;
            case VesselAutopilot.AutopilotMode.Normal:
                return KRPCSpaceCenter.Services.SASMode.Normal;
            case VesselAutopilot.AutopilotMode.Antinormal:
                return KRPCSpaceCenter.Services.SASMode.AntiNormal;
            case VesselAutopilot.AutopilotMode.RadialIn:
                return KRPCSpaceCenter.Services.SASMode.Radial;
            case VesselAutopilot.AutopilotMode.RadialOut:
                return KRPCSpaceCenter.Services.SASMode.AntiRadial;
            case VesselAutopilot.AutopilotMode.Target:
                return KRPCSpaceCenter.Services.SASMode.Target;
            case VesselAutopilot.AutopilotMode.AntiTarget:
                return KRPCSpaceCenter.Services.SASMode.AntiTarget;
            default:
                throw new ArgumentException ("Unsupported auto-pilot mode");
            }
        }

        public static VesselAutopilot.AutopilotMode FromSASMode (this KRPCSpaceCenter.Services.SASMode mode)
        {
            switch (mode) {
            case KRPCSpaceCenter.Services.SASMode.StabilityAssist:
                return VesselAutopilot.AutopilotMode.StabilityAssist;
            case KRPCSpaceCenter.Services.SASMode.Maneuver:
                return VesselAutopilot.AutopilotMode.Maneuver;
            case KRPCSpaceCenter.Services.SASMode.Prograde:
                return VesselAutopilot.AutopilotMode.Prograde;
            case KRPCSpaceCenter.Services.SASMode.Retrograde:
                return VesselAutopilot.AutopilotMode.Retrograde;
            case KRPCSpaceCenter.Services.SASMode.Normal:
                return VesselAutopilot.AutopilotMode.Normal;
            case KRPCSpaceCenter.Services.SASMode.AntiNormal:
                return VesselAutopilot.AutopilotMode.Antinormal;
            case KRPCSpaceCenter.Services.SASMode.Radial:
                return VesselAutopilot.AutopilotMode.RadialIn;
            case KRPCSpaceCenter.Services.SASMode.AntiRadial:
                return VesselAutopilot.AutopilotMode.RadialOut;
            case KRPCSpaceCenter.Services.SASMode.Target:
                return VesselAutopilot.AutopilotMode.Target;
            case KRPCSpaceCenter.Services.SASMode.AntiTarget:
                return VesselAutopilot.AutopilotMode.AntiTarget;
            default:
                throw new ArgumentException ("Unsupported SAS mode");
            }
        }
    }
}
