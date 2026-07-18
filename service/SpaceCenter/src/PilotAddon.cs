using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Server;
using KRPC.Service;
using KRPC.SpaceCenter.AutoPilot;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.ExternalAPI;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon to update a vessels control inputs.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class PilotAddon : MonoBehaviour
    {
        internal sealed class ControlInputs
        {
            readonly Vessel vessel;
            readonly FlightCtrlState state;
            readonly bool[] customAxisUpdated = new bool[4];
            AxisGroupsModule axisModule;

            public ControlInputs (Vessel vessel)
            {
                this.vessel = vessel;
                state = new FlightCtrlState ();
                InputMode = Services.ControlInputMode.Additive;
                ThrottleUpdated = false;
                WheelThrottleUpdated = false;
                WheelSteerUpdated = false;
            }

            public ControlInputs (Vessel vessel, FlightCtrlState ctrlState)
            {
                this.vessel = vessel;
                state = ctrlState;
                InputMode = Services.ControlInputMode.Additive;
                ThrottleUpdated = false;
                WheelThrottleUpdated = false;
                WheelSteerUpdated = false;
            }

            /// <summary>
            /// The axis groups module of the vessel these inputs are for. Resolved
            /// lazily so that the custom axes are applied to that vessel, rather than
            /// to whichever vessel happens to be active.
            /// </summary>
            AxisGroupsModule AxisModule {
                get {
                    if (axisModule == null && vessel != null)
                        axisModule = vessel.FindVesselModuleImplementing<AxisGroupsModule> ();
                    return axisModule;
                }
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
                    state.custom_axes[0] = value.Clamp (-1f, 1f);
                    customAxisUpdated[0] = true;
                }
            }

            public float CustomAxis02 {
                get { return state.custom_axes[1]; }
                set {
                    state.custom_axes[1] = value.Clamp (-1f, 1f);
                    customAxisUpdated[1] = true;
                }
            }

            public float CustomAxis03 {
                get { return state.custom_axes[2]; }
                set {
                    state.custom_axes[2] = value.Clamp (-1f, 1f);
                    customAxisUpdated[2] = true;
                }
            }

            public float CustomAxis04 {
                get { return state.custom_axes[3]; }
                set {
                    state.custom_axes[3] = value.Clamp (-1f, 1f);
                    customAxisUpdated[3] = true;
                }
            }

            /// <summary>
            /// Apply the custom axes to the vessel's axis groups. Only the axes that
            /// have been set are applied, so vessels that never use custom axes do
            /// not trigger an axis groups module lookup.
            /// </summary>
            public void ApplyCustomAxes ()
            {
                if (!customAxisUpdated[0] && !customAxisUpdated[1] &&
                    !customAxisUpdated[2] && !customAxisUpdated[3])
                    return;
                var module = AxisModule;
                if (module == null)
                    return;
                if (customAxisUpdated[0])
                    module.SetAxisGroup (KSPAxisGroup.Custom01, state.custom_axes[0]);
                if (customAxisUpdated[1])
                    module.SetAxisGroup (KSPAxisGroup.Custom02, state.custom_axes[1]);
                if (customAxisUpdated[2])
                    module.SetAxisGroup (KSPAxisGroup.Custom03, state.custom_axes[2]);
                if (customAxisUpdated[3])
                    module.SetAxisGroup (KSPAxisGroup.Custom04, state.custom_axes[3]);
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

            public void Add (ControlInputs other)
            {
                if (other.ThrottleUpdated)
                    state.mainThrottle = other.state.mainThrottle;
                ThrottleUpdated |= other.ThrottleUpdated;
                // Custom axes are absolute axis positions, so they override (like the
                // throttle) rather than accumulate. The flight control state is not
                // reset each frame for non-active vessels, so accumulating them across
                // frames would run away.
                for (int i = 0; i < 4; i++) {
                    if (other.customAxisUpdated[i])
                        state.custom_axes[i] = other.state.custom_axes[i];
                    customAxisUpdated[i] |= other.customAxisUpdated[i];
                }
                if (other.InputMode == Services.ControlInputMode.Additive) {
                    state.pitch += other.state.pitch;
                    state.yaw += other.state.yaw;
                    state.roll += other.state.roll;
                    state.X += other.state.X;
                    state.Y += other.state.Y;
                    state.Z += other.state.Z;
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
                for (int i = 0; i < 4; i++)
                    customAxisUpdated[i] = other.customAxisUpdated[i];
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
        /// The attitude controller for each vessel. Owned here rather than by the AutoPilot
        /// service objects — those are transient API surface objects, so controller state (the
        /// autotuner, the oscillation detector's persistent structural level, ...) must not be
        /// tied to their lifetime. Held per vessel for the duration of the flight session,
        /// however many AutoPilot objects come and go.
        /// </summary>
        static IDictionary<Guid, AttitudeController> attitudeControllers = new Dictionary<Guid, AttitudeController> ();

        /// <summary>
        /// Wake the addon
        /// </summary>
        public void Awake ()
        {
            Clear ();
            GameEvents.onVesselDestroy.Add (OnVesselDestroy);
        }

        /// <summary>
        /// Destroy the addon
        /// </summary>
        public void OnDestroy ()
        {
            GameEvents.onVesselDestroy.Remove (OnVesselDestroy);
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
            attitudeControllers.Clear ();
        }

        /// <summary>
        /// Tear down all control state for a vessel as soon as it is destroyed rather than only
        /// at scene teardown via <see cref="Clear"/>.
        /// </summary>
        void OnVesselDestroy (Vessel vessel)
        {
            Remove (vessel);
        }

        static void Remove (Vessel vessel)
        {
            Action<FlightCtrlState> action;
            if (controlDelegates.TryGetValue (vessel, out action)) {
                vessel.OnFlyByWire -= new FlightInputCallback (action);
                if (RemoteTech.IsAvailable && remoteTechSanctionedDelegates.Contains (vessel) &&
                    RemoteTech.RemoveSanctionedPilot != null)
                    RemoteTech.RemoveSanctionedPilot (vessel.id, action);
                controlDelegates.Remove (vessel);
            }
            remoteTechSanctionedDelegates.Remove (vessel);
            currentInputs.Remove (vessel);
            manualInputs.Remove (vessel);
            autoPilotInputs.Remove (vessel);
            attitudeControllers.Remove (vessel.id);
        }

        /// <summary>
        /// The vessel's attitude controller, created on first use.
        /// </summary>
        internal static AttitudeController GetAttitudeController (Vessel vessel)
        {
            AttitudeController controller;
            if (!attitudeControllers.TryGetValue (vessel.id, out controller)) {
                controller = new AttitudeController (vessel);
                attitudeControllers [vessel.id] = controller;
            }
            return controller;
        }

        /// <summary>
        /// The vessel's attitude controller, or null if none has been created. A lookup that
        /// never creates, for the per-tick fly-by-wire path (which runs for every vessel) and
        /// the info window.
        /// </summary>
        internal static AttitudeController FindAttitudeController (Guid vesselId)
        {
            AttitudeController controller;
            return attitudeControllers.TryGetValue (vesselId, out controller) ? controller : null;
        }

        internal static ControlInputs Get (Vessel vessel)
        {
            if (!currentInputs.ContainsKey (vessel))
                currentInputs [vessel] = new ControlInputs (vessel);
            return currentInputs [vessel];
        }

        internal static ControlInputs Set (Vessel vessel)
        {
            manualInputClients.Add (CallContext.Client);
            if (!manualInputs.ContainsKey (vessel))
                manualInputs [vessel] = new ControlInputs (vessel);
            return manualInputs [vessel];
        }

        /// <summary>
        /// Remove disconnect clients from the manualInputClients set
        /// and clear manual inputs if no clients are connected.
        /// </summary>
        static void CheckClients ()
        {
            foreach (var client in manualInputClients.ToList()) {
                if (ClientConnections.Disconnected (client))
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
            var inputs = new ControlInputs (vessel, state);

            // Manual inputs
            if (!manualInputs.ContainsKey (vessel))
                manualInputs [vessel] = new ControlInputs (vessel);
            HandleThrottle (vessel, manualInputs [vessel]);
            inputs.Add (manualInputs [vessel]);

            // Auto-pilot inputs
            if (!autoPilotInputs.ContainsKey (vessel))
                autoPilotInputs [vessel] = new ControlInputs (vessel);
            var attitudeController = FindAttitudeController (vessel.id);
            if (attitudeController != null && attitudeController.Fly (autoPilotInputs [vessel]))
                inputs.Add (autoPilotInputs [vessel]);

            // Apply the merged custom axes to the vessel's axis groups
            inputs.ApplyCustomAxes ();

            // Update current inputs
            if (!currentInputs.ContainsKey (vessel))
                currentInputs [vessel] = new ControlInputs (vessel);
            currentInputs [vessel].CopyFrom (inputs);
        }

        /// <summary>
        /// Whether kRPC is permitted to send control inputs to the vessel.
        /// When RemoteTech is installed this requires the vessel to be controllable via
        /// RemoteTech: either local (crewed) control, or a connection to a command station
        /// (a ground station or a crewed command station).
        /// </summary>
        static bool HasControlConnection (Vessel vessel)
        {
            if (!RemoteTech.IsAvailable)
                return true;
            return RemoteTech.HasLocalControl (vessel.id) || RemoteTech.HasAnyConnection (vessel.id);
        }

        /// <summary>
        /// Apply the manually set throttle to the vessel.
        ///
        /// Throttle is handled differently to the other control axes. Rather than being
        /// an additive per-frame input on the FlightCtrlState, the active vessel's throttle
        /// is a persistent value latched onto FlightInputHandler, so that it survives across
        /// frames and stays in sync with the throttle gauge. The input is consumed once
        /// handled so that it is not also re-applied via the per-frame FlightCtrlState in
        /// ControlInputs.Add.
        /// </summary>
        static void HandleThrottle (Vessel vessel, ControlInputs inputs)
        {
            if (!inputs.ThrottleUpdated)
                return;
            if (FlightGlobals.ActiveVessel != vessel)
                return;
            // If RemoteTech is controlling the vessel and there is no control connection,
            // drop the input rather than applying it. Note this must consume the input
            // (rather than just returning) so that it is not subsequently re-applied via
            // the per-frame FlightCtrlState in ControlInputs.Add.
            if (!HasControlConnection (vessel)) {
                inputs.Throttle = 0f;
                inputs.ThrottleUpdated = false;
                return;
            }
            FlightInputHandler.state.mainThrottle = inputs.Throttle;
            inputs.Throttle = 0f;
            inputs.ThrottleUpdated = false;
        }
    }
}
