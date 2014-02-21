using UnityEngine;
using KRPC.Schema.Control;

namespace KRPCServices
{
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class PilotAddon : MonoBehaviour
    {
        global::Vessel controlledVessel;
        static ControlInputs controls;

        public void Awake ()
        {
            controls = ControlInputs.CreateBuilder ().Build ();
        }

        public static void SetControlInputs (ControlInputs controls)
        {
            PilotAddon.controls = controls;
        }

        public void FixedUpdate ()
        {
            // TODO: is this the best way to attach to OnFlyByWire of the active vessel?
            if (controlledVessel == null && FlightGlobals.ActiveVessel != null) {
                controlledVessel = FlightGlobals.ActiveVessel;
                controlledVessel.OnFlyByWire += new FlightInputCallback (Fly);
            } else if (controlledVessel != null && FlightGlobals.ActiveVessel == null) {
                controlledVessel.OnFlyByWire -= new FlightInputCallback (Fly);
                controlledVessel = null;
            }
        }

        public void OnDestroy ()
        {
            if (controlledVessel != null)
                controlledVessel.OnFlyByWire -= new FlightInputCallback (Fly);
        }

        static void Fly (FlightCtrlState state)
        {
            // TODO: need to clear these if all clients disconnect, or similar
            if (controls.HasPitch)
                state.pitch += controls.Pitch;
            if (controls.HasRoll)
                state.roll += controls.Roll;
            if (controls.HasYaw)
                state.yaw += controls.Yaw;
            if (controls.HasX)
                state.X += controls.X;
            if (controls.HasY)
                state.Y += controls.Y;
            if (controls.HasZ)
                state.Z += controls.Z;
        }
    }
}
