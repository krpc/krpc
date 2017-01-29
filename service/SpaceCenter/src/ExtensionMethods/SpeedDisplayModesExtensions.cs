using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
    static class SpeedDisplayModesExtensions
    {
        public static Services.SpeedMode ToSpeedMode (this FlightGlobals.SpeedDisplayModes mode)
        {
            switch (mode) {
            case FlightGlobals.SpeedDisplayModes.Orbit:
                return Services.SpeedMode.Orbit;
            case FlightGlobals.SpeedDisplayModes.Surface:
                return Services.SpeedMode.Surface;
            case FlightGlobals.SpeedDisplayModes.Target:
                return Services.SpeedMode.Target;
            default:
                throw new ArgumentOutOfRangeException (nameof (mode));
            }
        }

        public static FlightGlobals.SpeedDisplayModes FromSpeedMode (this Services.SpeedMode mode)
        {
            switch (mode) {
            case Services.SpeedMode.Orbit:
                return FlightGlobals.SpeedDisplayModes.Orbit;
            case Services.SpeedMode.Surface:
                return FlightGlobals.SpeedDisplayModes.Surface;
            case Services.SpeedMode.Target:
                return FlightGlobals.SpeedDisplayModes.Target;
            default:
                throw new ArgumentOutOfRangeException (nameof (mode));
            }
        }
    }
}
