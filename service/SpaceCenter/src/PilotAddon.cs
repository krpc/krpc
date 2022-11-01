using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Server;
using KRPC.Service;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.ExternalAPI;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon to update a vessels control inputs.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
    public sealed class PilotAddon : MonoBehaviour
    {
        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
        internal sealed class ControlInputs
        {
            readonly FlightCtrlState state;
            AxisGroupsModule axisModule;

            public ControlInputs ()
            {
                state = new FlightCtrlState ();
                InputMode = Services.ControlInputMode.Additive;
                ThrottleUpdated = false;
                WheelThrottleUpdated = false;
                WheelSteerUpdated = false;
                axisModule = FlightGlobals.ActiveVessel.FindVesselModuleImplementing<AxisGroupsModule>();
            }

            public ControlInputs (FlightCtrlState ctrlState)
            {
                state = ctrlState;
                InputMode = Services.ControlInputMode.Additive;
                ThrottleUpdated = false;
                WheelThrottleUpdated = false;
                WheelSteerUpdated = false;
            }

            public Services.ControlInputMode InputMode { get; set; }

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
                set {
                    state.wheelThrottle = value.Clamp (-1f, 1f);
                    WheelThrottleUpdated = true;
                }
            }

            public bool WheelThrottleUpdated { get; set; }

            public float WheelSteer {
                get { return state.wheelSteer; }
                set {
                    state.wheelSteer = value.Clamp (-1f, 1f);
                    WheelSteerUpdated = true;
                }
            }

            public bool WheelSteerUpdated { get; set; }

            public float CustomAxis01 {
                get { return state.custom_axes[0]; }
                set {
                    axisModule.SetAxisGroup(KSPAxisGroup.Custom01, value.Clamp (-1f, 1f));
                }
            }

            public float CustomAxis02 {
                get { return state.custom_axes[1]; }
                set {
                    axisModule.SetAxisGroup(KSPAxisGroup.Custom02, value.Clamp (-1f, 1f));
                }
            }

            public float CustomAxis03 {
                get { return state.custom_axes[2]; }
                set {
                    axisModule.SetAxisGroup(KSPAxisGroup.Custom03, value.Clamp (-1f, 1f));
                }
            }

            public float CustomAxis04 {
                get { return state.custom_axes[3]; }
                set {
                    axisModule.SetAxisGroup(KSPAxisGroup.Custom04, value.Clamp (-1f, 1f));
                }
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
                state.custom_axes[0] = 0f;
                state.custom_axes[1] = 0f;
                state.custom_axes[2] = 0f;
                state.custom_axes[3] = 0f;
            }

            [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
            public void Add (ControlInputs other)
            {
                if (other.ThrottleUpdated)
                    state.mainThrottle = other.state.mainThrottle;
                ThrottleUpdated |= other.ThrottleUpdated;
                if (other.InputMode == Services.ControlInputMode.Additive) {
                    state.pitch += other.state.pitch;
                    state.yaw += other.state.yaw;
                    state.roll += other.state.roll;
                    state.X += other.state.X;
                    state.Y += other.state.Y;
                    state.Z += other.state.Z;
                    axisModule.SetAxisGroup(KSPAxisGroup.Custom01, state.custom_axes[0] + other.state.custom_axes[0]);
                    axisModule.SetAxisGroup(KSPAxisGroup.Custom02, state.custom_axes[1] + other.state.custom_axes[1]);
                    axisModule.SetAxisGroup(KSPAxisGroup.Custom03, state.custom_axes[2] + other.state.custom_axes[2]);
                    axisModule.SetAxisGroup(KSPAxisGroup.Custom04, state.custom_axes[3] + other.state.custom_axes[3]);
                } else {
                    if (Math.Abs(other.state.pitch) > 0.001)
                        state.pitch = other.state.pitch;
                    if (Math.Abs(other.state.yaw) > 0.001)
                        state.yaw = other.state.yaw;
                    if (Math.Abs(other.state.roll) > 0.001)
                        state.roll = other.state.roll;
                    if (Math.Abs(other.state.X) > 0.001)
                        state.X = other.state.X;
                    if (Math.Abs(other.state.Y) > 0.001)
                        state.Y = other.state.Y;
                    if (Math.Abs(other.state.Z) > 0.001)
                        state.Z = other.state.Z;
                    if (Math.Abs(other.state.custom_axes[0]) > 0.001)
                        axisModule.SetAxisGroup(KSPAxisGroup.Custom01, other.state.custom_axes[0]);
                    if (Math.Abs(other.state.custom_axes[1]) > 0.001)
                        axisModule.SetAxisGroup(KSPAxisGroup.Custom02, other.state.custom_axes[1]);
                    if (Math.Abs(other.state.custom_axes[2]) > 0.001)
                        axisModule.SetAxisGroup(KSPAxisGroup.Custom03, other.state.custom_axes[2]);
                    if (Math.Abs(other.state.custom_axes[3]) > 0.001)
                        axisModule.SetAxisGroup(KSPAxisGroup.Custom04, other.state.custom_axes[3]);
                }
                if (other.WheelThrottleUpdated)
                    state.wheelThrottle = other.state.wheelThrottle;
                if (other.WheelSteerUpdated)
                    state.wheelSteer = other.state.wheelSteer;
            }

            public void CopyFrom (ControlInputs other)
            {
                state.CopyFrom (other.state);
                ThrottleUpdated = other.ThrottleUpdated;
                WheelThrottleUpdated = other.WheelThrottleUpdated;
                WheelSteerUpdated = other.WheelSteerUpdated;
            }
        }

        /// <summary>
        /// The current control inputs that the craft is using.
        /// </summary>
        static IDictionary<Vessel, ControlInputs> currentInputs = new Dictionary<Vessel, ControlInputs> ();
        /// <summary>
        /// Control inputs that have been manually set.
        /// </summary>
        static IDictionary<Vessel, ControlInputs> manualInputs = new Dictionary<Vessel, ControlInputs> ();
        /// <summary>
        /// Set of all clients that have set a manual control input.
        /// </summary>
        static HashSet<IClient> manualInputClients = new HashSet<IClient> ();
        /// <summary>
        /// Flag to determine if manual control inputs have been cleared
        /// as a result of all clients disconnecting.
        /// </summary>
        static bool clearedManualInputs;
        /// <summary>
        /// Control inputs that have been set by the auto-pilot.
        /// </summary>
        static IDictionary<Vessel, ControlInputs> autoPilotInputs = new Dictionary<Vessel, ControlInputs> ();
        /// <summary>
        /// FlyByWire callbacks for vessels.
        /// </summary>
        static IDictionary<Vessel, Action<FlightCtrlState>> controlDelegates = new Dictionary<Vessel, Action<FlightCtrlState>> ();
        /// <summary>
        /// Set of FlyByWire callbacks that have been registered with RemoteTech.
        /// </summary>
        static HashSet<Vessel> remoteTechSanctionedDelegates = new HashSet<Vessel> ();

        /// <summary>
        /// Wake the addon
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void Awake ()
        {
            Clear ();
        }

        /// <summary>
        /// Destroy the addon
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void OnDestroy ()
        {
            Clear ();
        }

        static void Clear ()
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
            manualInputClients.Add (CallContext.Client);
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
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
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
