using UnityEngine;

namespace KRPCSpaceCenter
{
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class PilotAddon : MonoBehaviour
    {
        global::Vessel controlledVessel;

        public static float Pitch { get; set; }

        public static float Roll { get; set; }

        public static float Yaw { get; set; }

        public static float X { get; set; }

        public static float Y { get; set; }

        public static float Z { get; set; }

        public void Awake ()
        {
            Pitch = 0;
            Roll = 0;
            Yaw = 0;
            X = 0;
            Y = 0;
            Z = 0;
        }

        public void FixedUpdate ()
        {
            if (controlledVessel == null && FlightGlobals.ActiveVessel != null) {
                controlledVessel = FlightGlobals.ActiveVessel;
                controlledVessel.OnFlyByWire += Fly;
            } else if (controlledVessel != null && FlightGlobals.ActiveVessel == null) {
                controlledVessel.OnFlyByWire -= Fly;
                controlledVessel = null;
            } else if (controlledVessel != FlightGlobals.ActiveVessel) {
                controlledVessel.OnFlyByWire -= Fly;
                controlledVessel = FlightGlobals.ActiveVessel;
                controlledVessel.OnFlyByWire += Fly;
            }
        }

        public void OnDestroy ()
        {
            if (controlledVessel != null)
                controlledVessel.OnFlyByWire -= Fly;
        }

        static void Fly (FlightCtrlState state)
        {
            if (FlightGlobals.ActiveVessel == null)
                return;

            // TODO: need to clear these if all clients disconnect, or similar
            state.pitch += Pitch;
            state.roll += Roll;
            state.yaw += Yaw;
            state.X += X;
            state.Y += Y;
            state.Z += Z;
            Services.AutoPilot.Fly (state);
        }
    }
}
