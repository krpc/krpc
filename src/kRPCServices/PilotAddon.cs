using UnityEngine;

namespace KRPCServices
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
            state.pitch += Pitch;
            state.roll += Roll;
            state.yaw += Yaw;
            state.X += X;
            state.Y += Y;
            state.Z += Z;
        }
    }
}
