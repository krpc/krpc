using UnityEngine;
using KRPC;
using KRPCSpaceCenter.ExternalAPI;

namespace KRPCSpaceCenter
{
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class PilotAddon : MonoBehaviour
    {
        Vessel controlledVessel;

        public static float Pitch { get; set; }

        public static float Roll { get; set; }

        public static float Yaw { get; set; }

        public static float X { get; set; }

        public static float Y { get; set; }

        public static float Z { get; set; }

        public static float WheelThrottle { get; set; }

        public static float WheelSteer { get; set; }

        public void Awake ()
        {
            Clear ();
        }

        static void Clear ()
        {
            Pitch = 0;
            Roll = 0;
            Yaw = 0;
            X = 0;
            Y = 0;
            Z = 0;
            WheelThrottle = 0;
            WheelSteer = 0;
        }

        public void FixedUpdate ()
        {
            if (controlledVessel == null && FlightGlobals.ActiveVessel != null) {
                controlledVessel = FlightGlobals.ActiveVessel;
                AddPilot ();
            } else if (controlledVessel != null && FlightGlobals.ActiveVessel == null) {
                RemovePilot ();
                controlledVessel = null;
            } else if (controlledVessel != FlightGlobals.ActiveVessel) {
                RemovePilot ();
                controlledVessel = FlightGlobals.ActiveVessel;
                AddPilot ();
            }
        }

        public void OnDestroy ()
        {
            if (controlledVessel != null)
                RemovePilot ();
            Services.AutoPilot.Clear ();
        }

        void AddPilot ()
        {
            if (RemoteTech.IsAvailable && RemoteTech.HasFlightComputer (controlledVessel.id))
                RemoteTech.AddSanctionedPilot (controlledVessel.id, Fly);
            else
                controlledVessel.OnFlyByWire += Fly;
        }

        void RemovePilot ()
        {
            if (RemoteTech.IsAvailable && RemoteTech.HasFlightComputer (controlledVessel.id))
                RemoteTech.RemoveSanctionedPilot (controlledVessel.id, Fly);
            else
                controlledVessel.OnFlyByWire -= Fly;
        }

        static void Fly (FlightCtrlState state)
        {
            if (FlightGlobals.ActiveVessel == null)
                return;

            var krpc = Object.FindObjectOfType<KRPCAddon> ();
            if (krpc != null && krpc.NumberOfClients == 0) {
                Clear ();
            }

            // TODO: need to clear these if all clients disconnect, or similar
            state.pitch += Pitch;
            state.roll += Roll;
            state.yaw += Yaw;
            state.X += X;
            state.Y += Y;
            state.Z += Z;
            state.wheelThrottle += WheelThrottle;
            state.wheelSteer += WheelSteer;

            //FIXME: send appropriate state for each vessel
            Services.AutoPilot.Fly (state);
        }
    }
}
