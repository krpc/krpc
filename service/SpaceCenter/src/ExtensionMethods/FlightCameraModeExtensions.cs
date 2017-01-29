using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.Services;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class CameraModeExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static CameraMode ToCameraMode (this FlightCamera.Modes mode)
        {
            switch (mode) {
            case FlightCamera.Modes.AUTO:
                return CameraMode.Automatic;
            case FlightCamera.Modes.FREE:
                return CameraMode.Free;
            case FlightCamera.Modes.CHASE:
                return CameraMode.Chase;
            case FlightCamera.Modes.LOCKED:
                return CameraMode.Locked;
            case FlightCamera.Modes.ORBITAL:
                return CameraMode.Orbital;
            default:
                throw new ArgumentOutOfRangeException (nameof (mode));
            }
        }
    }
}
