using System;
using KRPC.Service;
using KRPC.Schema.Control;
using KSP;

namespace KRPCServices
{
    [KRPCService]
    public class Control
    {
        [KRPCProcedure]
        public static void SetControlInputs(ControlInputs controls) {
            if (controls.HasThrottle)
                FlightInputHandler.state.mainThrottle = controls.Throttle;
            if (controls.HasPitch)
                FlightInputHandler.state.pitch = controls.Pitch;
            if (controls.HasRoll)
                FlightInputHandler.state.roll = controls.Roll;
            if (controls.HasYaw)
                FlightInputHandler.state.yaw = controls.Yaw;
            if (controls.HasX)
                FlightInputHandler.state.X = controls.X;
            if (controls.HasY)
                FlightInputHandler.state.Y = controls.Y;
            if (controls.HasZ)
                FlightInputHandler.state.Z = controls.Z;
            if (controls.HasSas)
                FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.SAS, controls.Sas);
            if (controls.HasRcs)
                FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.RCS, controls.Rcs);
        }

        [KRPCProcedure]
        public static ControlInputs GetControlInputs() {
            // TODO: setting the control inputs only has an effect for a single frame
            return ControlInputs.CreateBuilder ()
                .SetThrottle (FlightInputHandler.state.mainThrottle)
                .SetPitch (FlightInputHandler.state.pitch)
                .SetRoll  (FlightInputHandler.state.roll)
                .SetYaw   (FlightInputHandler.state.yaw)
                .SetX   (FlightInputHandler.state.X)
                .SetY   (FlightInputHandler.state.Y)
                .SetZ   (FlightInputHandler.state.Z)
                .SetSas (FlightInputHandler.state.killRot)
                .SetRcs (FlightInputHandler.RCSLock) // TODO: this is wrong
                .Build ();
        }

        [KRPCProcedure]
        public static void ActivateNextStage() {
            Staging.ActivateNextStage();
        }
    }
}

