using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
    static class AutoPilotModeExtensions
    {
        public static Services.SASMode ToSASMode (this VesselAutopilot.AutopilotMode mode)
        {
            switch (mode) {
            case VesselAutopilot.AutopilotMode.StabilityAssist:
                return Services.SASMode.StabilityAssist;
            case VesselAutopilot.AutopilotMode.Maneuver:
                return Services.SASMode.Maneuver;
            case VesselAutopilot.AutopilotMode.Prograde:
                return Services.SASMode.Prograde;
            case VesselAutopilot.AutopilotMode.Retrograde:
                return Services.SASMode.Retrograde;
            case VesselAutopilot.AutopilotMode.Normal:
                return Services.SASMode.Normal;
            case VesselAutopilot.AutopilotMode.Antinormal:
                return Services.SASMode.AntiNormal;
            case VesselAutopilot.AutopilotMode.RadialIn:
                return Services.SASMode.Radial;
            case VesselAutopilot.AutopilotMode.RadialOut:
                return Services.SASMode.AntiRadial;
            case VesselAutopilot.AutopilotMode.Target:
                return Services.SASMode.Target;
            case VesselAutopilot.AutopilotMode.AntiTarget:
                return Services.SASMode.AntiTarget;
            default:
                throw new ArgumentOutOfRangeException (nameof (mode));
            }
        }

        public static VesselAutopilot.AutopilotMode FromSASMode (this Services.SASMode mode)
        {
            switch (mode) {
            case Services.SASMode.StabilityAssist:
                return VesselAutopilot.AutopilotMode.StabilityAssist;
            case Services.SASMode.Maneuver:
                return VesselAutopilot.AutopilotMode.Maneuver;
            case Services.SASMode.Prograde:
                return VesselAutopilot.AutopilotMode.Prograde;
            case Services.SASMode.Retrograde:
                return VesselAutopilot.AutopilotMode.Retrograde;
            case Services.SASMode.Normal:
                return VesselAutopilot.AutopilotMode.Normal;
            case Services.SASMode.AntiNormal:
                return VesselAutopilot.AutopilotMode.Antinormal;
            case Services.SASMode.Radial:
                return VesselAutopilot.AutopilotMode.RadialIn;
            case Services.SASMode.AntiRadial:
                return VesselAutopilot.AutopilotMode.RadialOut;
            case Services.SASMode.Target:
                return VesselAutopilot.AutopilotMode.Target;
            case Services.SASMode.AntiTarget:
                return VesselAutopilot.AutopilotMode.AntiTarget;
            default:
                throw new ArgumentOutOfRangeException (nameof (mode));
            }
        }
    }
}
