using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Server;
using KRPCSpaceCenter.ExtensionMethods;
using UnityEngine;

namespace KRPCSpaceCenter
{
    /// <summary>
    /// Addon to update a vessels control inputs
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class PilotAddon : MonoBehaviour
    {
        public class ControlInputs
        {
            float pitch;
            float yaw;
            float roll;
            float forward;
            float up;
            float right;
            float wheelThrottle;
            float wheelSteer;

            public float Pitch {
                get { return pitch; }
                set { pitch = value.Clamp (-1f, 1f); }
            }

            public float Yaw {
                get { return yaw; }
                set { yaw = value.Clamp (-1f, 1f); }
            }

            public float Roll {
                get { return roll; }
                set { roll = value.Clamp (-1f, 1f); }
            }

            public float Forward {
                get { return forward; }
                set { forward = value.Clamp (-1f, 1f); }
            }

            public float Up {
                get { return up; }
                set { up = value.Clamp (-1f, 1f); }
            }

            public float Right {
                get { return right; }
                set { right = value.Clamp (-1f, 1f); }
            }

            public float WheelThrottle {
                get { return wheelThrottle; }
                set { wheelThrottle = value.Clamp (-1f, 1f); }
            }

            public float WheelSteer {
                get { return wheelSteer; }
                set { wheelSteer = value.Clamp (-1f, 1f); }
            }
        };

        static IDictionary<Vessel, IDictionary<IClient, ControlInputs>> controlInputs;

        /// <summary>
        /// Wake the addon
        /// </summary>
        public void Awake ()
        {
            controlInputs = new Dictionary<Vessel, IDictionary<IClient, ControlInputs>> ();
        }

        /// <summary>
        /// Destroy the addon
        /// </summary>
        public void OnDestroy ()
        {
            controlInputs.Clear ();
        }

        internal static ControlInputs Get (Vessel vessel)
        {
            var client = KRPC.KRPCServer.Context.RPCClient;
            if (!controlInputs.ContainsKey (vessel))
                controlInputs [vessel] = new Dictionary<IClient, ControlInputs> ();
            if (!controlInputs [vessel].ContainsKey (client))
                controlInputs [vessel] [client] = new ControlInputs ();
            return controlInputs [vessel] [client];
        }

        /// <summary>
        /// Remove entries from the controlInputs dictionary for which the client has disconnected
        /// </summary>
        static void CheckClients ()
        {
            foreach (var entry in controlInputs) {
                foreach (var client in entry.Value.Keys.ToList()) {
                    if (!client.Connected)
                        entry.Value.Remove (client);
                }
            }
        }

        /// <summary>
        /// Update the pilot addon
        /// </summary>
        public void FixedUpdate ()
        {
            CheckClients ();
            foreach (var vessel in FlightGlobals.Vessels) {
                if (vessel.rootPart != null) { // If the vessel is controllable
                    Fly (vessel, vessel.ctrlState);
                }
            }
        }

        static void Fly (Vessel vessel, FlightCtrlState state)
        {
            if (controlInputs.ContainsKey (vessel)) {
                var inputs = controlInputs [vessel].Values;
                state.pitch += inputs.Sum (x => x.Pitch).Clamp (-1f, 1f);
                state.yaw += inputs.Sum (x => x.Yaw).Clamp (-1f, 1f);
                state.roll += inputs.Sum (x => x.Roll).Clamp (-1f, 1f);
                state.Z += inputs.Sum (x => x.Forward).Clamp (-1f, 1f);
                state.Y += inputs.Sum (x => x.Up).Clamp (-1f, 1f);
                state.X += inputs.Sum (x => x.Right).Clamp (-1f, 1f);
                state.wheelThrottle += inputs.Sum (x => x.WheelThrottle).Clamp (-1f, 1f);
                state.wheelSteer += inputs.Sum (x => x.WheelSteer).Clamp (-1f, 1f);
            }
            Services.AutoPilot.Fly (vessel, state);
        }
    }
}
