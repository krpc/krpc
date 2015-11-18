using System;

namespace KRPCSpaceCenter.ExtensionMethods
{
    static class SpeedDisplayModesExtensions
    {
        public static Services.SpeedMode ToSpeedMode (this FlightUIController.SpeedDisplayModes mode)
        {
            switch (mode) {
            case FlightUIController.SpeedDisplayModes.Orbit:
                return Services.SpeedMode.Orbit;
            case FlightUIController.SpeedDisplayModes.Surface:
                return Services.SpeedMode.Surface;
            case FlightUIController.SpeedDisplayModes.Target:
                return Services.SpeedMode.Target;
            default:
                throw new ArgumentException ("Unsupported speed display mode");
            }
        }

        public static FlightUIController.SpeedDisplayModes FromSpeedMode (this Services.SpeedMode mode)
        {
            switch (mode) {
            case Services.SpeedMode.Orbit:
                return FlightUIController.SpeedDisplayModes.Orbit;
            case Services.SpeedMode.Surface:
                return FlightUIController.SpeedDisplayModes.Surface;
            case Services.SpeedMode.Target:
                return FlightUIController.SpeedDisplayModes.Target;
            default:
                throw new ArgumentException ("Unsupported speed mode");
            }
        }
    }
}

