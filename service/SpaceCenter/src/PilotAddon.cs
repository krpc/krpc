using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Server;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.ExternalAPI;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon to update a vessels control inputs.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class PilotAddon : MonoBehaviour
    {
        internal class ControlInputs
        {
            readonly FlightCtrlState state;

            public ControlInputs ()
            {
                state = new FlightCtrlState ();
                ThrottleUpdated = false;
            }

            public ControlInputs (FlightCtrlState state)
            {
                this.state = state;
                ThrottleUpdated = false;
            }

            public float Throttle {
                get { return state.mainThrottle; }
                set {
                    state.mainThrottle = value.Clamp (0f, 1f);
                    ThrottleUpdated = true;
                }
            }

            public bool ThrottleUpdated { get; set; }

            public float Pitch {
                get { return state.pitch; }
                set { state.pitch = value.Clamp (-1f, 1f); }
            }

            public float Yaw {
                get { return state.yaw; }
                set { state.yaw = value.Clamp (-1f, 1f); }
            }

            public float Roll {
                get { return state.roll; }
                set { state.roll = value.Clamp (-1f, 1f); }
            }

            public float Forward {
                get { return -state.Z; }
                set { state.Z = -value.Clamp (-1f, 1f); }
            }

            public float Up {
                get { return state.Y; }
                set { state.Y = value.Clamp (-1f, 1f); }
            }

            public float Right {
                get { return -state.X; }
                set { state.X = -value.Clamp (-1f, 1f); }
            }

            public float WheelThrottle {
                get { return state.wheelThrottle; }
                set { state.wheelThrottle = value.Clamp (-1f, 1f); }
            }

            public float WheelSteer {
                get { return state.wheelSteer; }
                set { state.wheelSteer = value.Clamp (-1f, 1f); }
            }

            public void ClearExceptThrottle ()
            {
                state.roll = 0f;
                state.pitch = 0f;
                state.yaw = 0f;
                state.X = 0f;
                state.Y = 0f;
                state.Z = 0f;
                state.wheelThrottle = 0f;
                state.wheelSteer = 0f;
            }

            public void Add (ControlInputs other)
            {
                if (other.ThrottleUpdated)
                    state.mainThrottle = other.state.mainThrottle;
                ThrottleUpdated |= other.ThrottleUpdated;
                state.pitch += other.state.pitch;
                state.yaw += other.state.yaw;
                state.roll += other.state.roll;
                state.X += other.state.X;
                state.Y += other.state.Y;
                state.Z += other.state.Z;
                state.wheelThrottle += other.state.wheelThrottle;
                state.wheelSteer += other.state.wheelSteer;
            }

            public void CopyFrom (ControlInputs other)
            {
                state.CopyFrom (other.state);
                ThrottleUpdated = other.ThrottleUpdated;
            }
        };

        /// <summary>
        /// The current control inputs that the craft is using.
        /// </summary>
        static IDictionary<Vessel, ControlInputs> currentInputs;
        /// <summary>
        /// Control inputs that have been manually set.
        /// </summary>
        static IDictionary<Vessel, ControlInputs> manualInputs;
        /// <summary>
        /// Set of all clients that have set a manual control input.
        /// </summary>
        static HashSet<IClient> manualInputClients;
        /// <summary>
        /// Flag to determine if manual control inputs have been cleared
        /// as a result of all clients disconnecting.
        /// </summary>
        static bool clearedManualInputs;
        /// <summary>
        /// Control inputs that have been set by the auto-pilot.
        /// </summary>
        static IDictionary<Vessel, ControlInputs> autoPilotInputs;

        /// <summary>
        /// FlyByWire callbacks for vessels.
        /// </summary>
        static IDictionary<Vessel, Action<FlightCtrlState>> controlDelegates;
        /// <summary>
        /// Set of FlyByWire callbacks that have been registered with RemoteTech.
        /// </summary>
        static HashSet<Vessel> remoteTechSanctionedDelegates;

        /// <summary>
        /// Wake the addon
        /// </summary>
        public void Awake ()
        {
            currentInputs = new Dictionary<Vessel, ControlInputs> ();
            manualInputs = new Dictionary<Vessel, ControlInputs> ();
            manualInputClients = new HashSet<IClient> ();
            autoPilotInputs = new Dictionary<Vessel, ControlInputs> ();
            controlDelegates = new Dictionary<Vessel, Action<FlightCtrlState>> ();
            remoteTechSanctionedDelegates = new HashSet<Vessel> ();
        }

        /// <summary>
        /// Destroy the addon
        /// </summary>
        public void OnDestroy ()
        {
            currentInputs.Clear ();
            manualInputs.Clear ();
            manualInputClients.Clear ();
            autoPilotInputs.Clear ();
            controlDelegates.Clear ();
            remoteTechSanctionedDelegates.Clear ();
        }

        internal static ControlInputs Get (Vessel vessel)
        {
            if (!currentInputs.ContainsKey (vessel))
                currentInputs [vessel] = new ControlInputs ();
            return currentInputs [vessel];
        }

        internal static ControlInputs Set (Vessel vessel)
        {
            manualInputClients.Add (KRPC.KRPCCore.Context.RPCClient);
            if (!manualInputs.ContainsKey (vessel))
                manualInputs [vessel] = new ControlInputs ();
            return manualInputs [vessel];
        }

        /// <summary>
        /// Remove disconnect clients from the manualInputClients set
        /// and clear manual inputs if no clients are connected.
        /// </summary>
        static void CheckClients ()
        {
            foreach (var client in manualInputClients.ToList()) {
                if (!client.Connected)
                    manualInputClients.Remove (client);
            }
            if (manualInputClients.Any ())
                clearedManualInputs = false;
            else if (!manualInputClients.Any () && !clearedManualInputs) {
                foreach (var inputs in manualInputs.Values)
                    inputs.ClearExceptThrottle ();
                clearedManualInputs = true;
            }
        }

        /// <summary>
        /// Update the pilot addon
        /// </summary>
        public void FixedUpdate ()
        {
            CheckClients ();
            foreach (var vessel in FlightGlobals.Vessels) {
                // If the vessel is controllable, pilot it
                if (vessel.rootPart != null)
                    Fly (vessel);
            }
        }

        static void Fly (Vessel vessel)
        {
            if (!controlDelegates.ContainsKey (vessel)) {
                Action<FlightCtrlState> action = s => OnFlyByWire (vessel, s);
                controlDelegates [vessel] = action;
                vessel.OnFlyByWire += new FlightInputCallback (action);
            }
            if (RemoteTech.IsAvailable && !remoteTechSanctionedDelegates.Contains (vessel) && RemoteTech.HasFlightComputer (vessel.id)) {
                RemoteTech.AddSanctionedPilot (vessel.id, controlDelegates [vessel]);
                remoteTechSanctionedDelegates.Add (vessel);
            }
        }

        static void OnFlyByWire (Vessel vessel, FlightCtrlState state)
        {
            var inputs = new ControlInputs (state);

            // Manual inputs
            if (!manualInputs.ContainsKey (vessel))
                manualInputs [vessel] = new ControlInputs ();
            HandleThrottle (vessel, manualInputs [vessel]);
            inputs.Add (manualInputs [vessel]);

            // Auto-pilot inputs
            if (!autoPilotInputs.ContainsKey (vessel))
                autoPilotInputs [vessel] = new ControlInputs ();
            if (Services.AutoPilot.Fly (vessel, autoPilotInputs [vessel]))
                inputs.Add (autoPilotInputs [vessel]);

            // Update current inputs
            if (!currentInputs.ContainsKey (vessel))
                currentInputs [vessel] = new ControlInputs ();
            currentInputs [vessel].CopyFrom (inputs);
        }

        /// <summary>
        /// Handle throttle quirky operation...
        /// </summary>
        static void HandleThrottle (Vessel vessel, ControlInputs inputs)
        {
            if (!inputs.ThrottleUpdated)
                return;
            if (FlightGlobals.ActiveVessel != vessel)
                return;
            if (RemoteTech.IsAvailable && !(RemoteTech.HasLocalControl (vessel.id) || RemoteTech.HasAnyConnection (vessel.id)))
                return;
            FlightInputHandler.state.mainThrottle = inputs.Throttle;
            inputs.Throttle = 0f;
            inputs.ThrottleUpdated = false;
        }
    }
}
