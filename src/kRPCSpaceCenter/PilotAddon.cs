using System.Collections.Generic;
using System.Linq;
using KRPC.Server;
using KRPCSpaceCenter.ExtensionMethods;
using KRPCSpaceCenter.ExternalAPI;
using UnityEngine;

namespace KRPCSpaceCenter
{
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class PilotAddon : MonoBehaviour
    {
        class ControlInputs
        {
            public float pitch;
            public float yaw;
            public float roll;
            public float forward;
            public float up;
            public float right;
            public float wheelThrottle;
            public float wheelSteer;
        };

        Vessel controlledVessel;
        static IDictionary<IClient, ControlInputs> controlInputs;

        static void AddClient (IClient client)
        {
            if (!controlInputs.ContainsKey (client))
                controlInputs [client] = new ControlInputs ();
        }

        static void CheckClients ()
        {
            foreach (var client in controlInputs.Keys.ToList ())
                if (!client.Connected)
                    controlInputs.Remove (client);
        }

        static void Clear ()
        {
            foreach (var client in controlInputs.Keys) {
                controlInputs [client].pitch = 0f;
                controlInputs [client].yaw = 0f;
                controlInputs [client].roll = 0f;
                controlInputs [client].forward = 0f;
                controlInputs [client].up = 0f;
                controlInputs [client].right = 0f;
                controlInputs [client].wheelThrottle = 0f;
                controlInputs [client].wheelSteer = 0f;
            }
            CheckClients ();
        }

        public static float Pitch {
            get {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                return controlInputs [KRPC.KRPCServer.Context.RPCClient].pitch;
            }
            set {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                controlInputs [KRPC.KRPCServer.Context.RPCClient].pitch = value.Clamp (-1f, 1f);
            }
        }

        public static float Yaw {
            get {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                return controlInputs [KRPC.KRPCServer.Context.RPCClient].yaw;
            }
            set {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                controlInputs [KRPC.KRPCServer.Context.RPCClient].yaw = value.Clamp (-1f, 1f);
            }
        }

        public static float Roll {
            get {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                return controlInputs [KRPC.KRPCServer.Context.RPCClient].roll;
            }
            set {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                controlInputs [KRPC.KRPCServer.Context.RPCClient].roll = value.Clamp (-1f, 1f);
            }
        }

        public static float Forward {
            get {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                return controlInputs [KRPC.KRPCServer.Context.RPCClient].forward;
            }
            set {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                controlInputs [KRPC.KRPCServer.Context.RPCClient].forward = value.Clamp (-1f, 1f);
            }
        }

        public static float Up {
            get {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                return controlInputs [KRPC.KRPCServer.Context.RPCClient].up;
            }
            set {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                controlInputs [KRPC.KRPCServer.Context.RPCClient].up = value.Clamp (-1f, 1f);
            }
        }

        public static float Right {
            get {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                return controlInputs [KRPC.KRPCServer.Context.RPCClient].right;
            }
            set {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                controlInputs [KRPC.KRPCServer.Context.RPCClient].right = value.Clamp (-1f, 1f);
            }
        }

        public static float WheelThrottle {
            get {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                return controlInputs [KRPC.KRPCServer.Context.RPCClient].wheelThrottle;
            }
            set {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                controlInputs [KRPC.KRPCServer.Context.RPCClient].wheelThrottle = value.Clamp (-1f, 1f);
            }
        }

        public static float WheelSteer {
            get {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                return controlInputs [KRPC.KRPCServer.Context.RPCClient].wheelSteer;
            }
            set {
                AddClient (KRPC.KRPCServer.Context.RPCClient);
                controlInputs [KRPC.KRPCServer.Context.RPCClient].wheelSteer = value.Clamp (-1f, 1f);
            }
        }

        public void Awake ()
        {
            controlInputs = new Dictionary<IClient, ControlInputs> ();
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
        }

        void AddPilot ()
        {
            if (RemoteTech.IsAvailable && RemoteTech.HasFlightComputer (controlledVessel.id))
                RemoteTech.AddSanctionedPilot (controlledVessel.id, Fly);
            else
                controlledVessel.OnFlyByWire += Fly;
            Clear ();
        }

        void RemovePilot ()
        {
            if (RemoteTech.IsAvailable && RemoteTech.HasFlightComputer (controlledVessel.id))
                RemoteTech.RemoveSanctionedPilot (controlledVessel.id, Fly);
            else
                controlledVessel.OnFlyByWire -= Fly;
            Clear ();
        }

        static void Fly (FlightCtrlState state)
        {
            //TODO: cannot control vessels other than the active vessel
            if (FlightGlobals.ActiveVessel == null)
                return;

            CheckClients ();

            state.pitch += controlInputs.Sum (x => x.Value.pitch).Clamp (-1f, 1f);
            state.yaw += controlInputs.Sum (x => x.Value.yaw).Clamp (-1f, 1f);
            state.roll += controlInputs.Sum (x => x.Value.roll).Clamp (-1f, 1f);
            state.Z += -controlInputs.Sum (x => x.Value.forward).Clamp (-1f, 1f);
            state.Y += controlInputs.Sum (x => x.Value.up).Clamp (-1f, 1f);
            state.X += -controlInputs.Sum (x => x.Value.right).Clamp (-1f, 1f);
            state.wheelThrottle += controlInputs.Sum (x => x.Value.wheelThrottle).Clamp (-1f, 1f);
            state.wheelSteer += controlInputs.Sum (x => x.Value.wheelSteer).Clamp (-1f, 1f);

            Services.AutoPilot.Fly (FlightGlobals.ActiveVessel, state);
        }
    }
}
