using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Continuations;
using KRPC.Server;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPCSpaceCenter.Services
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum SASMode
    {
        StabilityAssist,
        Maneuver,
        Prograde,
        Retrograde,
        Normal,
        AntiNormal,
        Radial,
        AntiRadial,
        Target,
        AntiTarget
    }

    [KRPCEnum (Service = "SpaceCenter")]
    public enum SpeedMode
    {
        Orbit,
        Surface,
        Target
    }

    /// <remarks>
    /// Taken and adapted from MechJeb2/KOS/RemoteTech2/forum discussion; credit goes to the authors of those plugins/posts:
    /// https://github.com/MuMech/MechJeb2
    /// https://github.com/KSP-KOS/KOS
    /// https://github.com/RemoteTechnologiesGroup/RemoteTech
    /// http://forum.kerbalspaceprogram.com/threads/69313-WIP-kRPC-A-language-agnostic-Remote-Procedure-Call-server-for-KSP?p=1021721&amp;viewfull=1#post1021721
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class AutoPilot : Equatable<AutoPilot>
    {
        readonly global::Vessel vessel;
        static IDictionary<global::Vessel, AutoPilot> engaged = new Dictionary<global::Vessel, AutoPilot> ();
        IClient requestingClient;
        ReferenceFrame referenceFrame;
        float pitch;
        float heading;
        float roll;

        internal AutoPilot (global::Vessel vessel)
        {
            this.vessel = vessel;
            if (!engaged.ContainsKey (vessel))
                engaged [vessel] = null;
        }

        public override bool Equals (AutoPilot obj)
        {
            return vessel == obj.vessel;
        }

        public override int GetHashCode ()
        {
            return vessel.GetHashCode ();
        }

        [KRPCProperty]
        public bool SAS {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.SAS)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.SAS, value); }
        }

        [KRPCProperty]
        public SASMode SASMode {
            get { return vessel.Autopilot.Mode.ToSASMode (); }
            set {
                var mode = value.FromSASMode ();
                if (!vessel.Autopilot.CanSetMode (mode))
                    throw new InvalidOperationException ("Cannot set SAS mode of vessel");
                vessel.Autopilot.SetMode (mode);
                // Update the UI buttons
                var modeIndex = (int)vessel.Autopilot.Mode;
                var modeButtons = UnityEngine.Object.FindObjectOfType<VesselAutopilotUI> ().modeButtons;
                modeButtons.ElementAt<RUIToggleButton> (modeIndex).SetTrue (true, true);
            }
        }

        [KRPCProperty]
        public SpeedMode SpeedMode {
            get { return FlightUIController.speedDisplayMode.ToSpeedMode (); }
            set {
                var startMode = FlightUIController.speedDisplayMode;
                var mode = value.FromSpeedMode ();
                while (FlightUIController.speedDisplayMode != mode) {
                    FlightUIController.fetch.cycleSpdModes ();
                    if (FlightUIController.speedDisplayMode == startMode)
                        break;
                }
            }
        }

        [KRPCMethod]
        public void SetRotation (float pitch, float heading, float roll = float.NaN, ReferenceFrame referenceFrame = null, bool wait = false)
        {
            if (referenceFrame == null)
                referenceFrame = ReferenceFrame.Surface (vessel);
            this.referenceFrame = referenceFrame;
            this.pitch = pitch;
            this.heading = heading;
            this.roll = roll;
            Engage ();
            if (wait)
                throw new YieldException (new ParameterizedContinuationVoid (Wait));
        }

        [KRPCMethod]
        public void SetDirection (Tuple3 direction, float roll = float.NaN, ReferenceFrame referenceFrame = null, bool wait = false)
        {
            if (referenceFrame == null)
                referenceFrame = ReferenceFrame.Surface (vessel);
            this.referenceFrame = referenceFrame;
            QuaternionD rotation = Quaternion.FromToRotation (Vector3d.up, direction.ToVector ());
            var phr = rotation.PitchHeadingRoll ();
            pitch = (float) phr [0];
            heading = (float) phr [1];
            this.roll = (float) roll;
            Engage ();
            if (wait)
                throw new YieldException (new ParameterizedContinuationVoid (Wait));
        }

        void Wait ()
        {
            if (Error > 0.5f)
                throw new YieldException (new ParameterizedContinuationVoid (Wait));
            if (vessel.angularVelocity.magnitude > 0.05f)
                throw new YieldException (new ParameterizedContinuationVoid (Wait));
        }

        void Engage ()
        {
            requestingClient = KRPC.KRPCServer.Context.RPCClient;
            engaged [vessel] = this;
        }

        [KRPCMethod]
        public void Disengage ()
        {
            requestingClient = null;
            engaged [vessel] = null;
        }

        [KRPCProperty]
        public float Error {
            get {
                if (engaged [vessel] == this)
                    return Vector3.Angle (vessel.ReferenceTransform.up, TargetDirection ());
                else if (SAS && SASMode != SASMode.StabilityAssist)
                    return Vector3.Angle (vessel.ReferenceTransform.up, SASTargetDirection ());
                else
                    return 0f;
            }
        }

        [KRPCProperty]
        public float RollError {
            get {
                if (engaged [vessel] != this || Double.IsNaN (roll))
                    return 0f;
                var currentRoll = referenceFrame.RotationFromWorldSpace (vessel.ReferenceTransform.rotation).PitchHeadingRoll ().z;
                return (float) Math.Abs (roll - currentRoll);
            }
        }

        /// <summary>
        /// Gets the direction vector that the SAS autopilot is trying to hold in world space
        /// </summary>
        Vector3d SASTargetDirection ()
        {
            // Stability assist
            if (SASMode == SASMode.StabilityAssist)
                throw new InvalidOperationException ("No target direction in stability assist mode");

            // Maneuver node
            if (SASMode == SASMode.Maneuver) {
                var node = vessel.patchedConicSolver.maneuverNodes.OrderBy (x => x.UT).FirstOrDefault ();
                if (node == null)
                    throw new InvalidOperationException ("No maneuver node");
                return new Node (node).WorldBurnVector;
            }

            // Orbital directions, in different speed modes
            if (SASMode == SASMode.Prograde || SASMode == SASMode.Retrograde ||
                SASMode == SASMode.Normal || SASMode == SASMode.AntiNormal ||
                SASMode == SASMode.Radial || SASMode == SASMode.AntiRadial) {

                if (SpeedMode == SpeedMode.Orbit) {
                    switch (SASMode) {
                    case SASMode.Prograde:
                        return ReferenceFrame.Orbital (vessel).DirectionToWorldSpace (Vector3d.up);
                    case SASMode.Retrograde:
                        return ReferenceFrame.Orbital (vessel).DirectionToWorldSpace (Vector3d.down);
                    case SASMode.Normal:
                        return ReferenceFrame.Orbital (vessel).DirectionToWorldSpace (Vector3d.forward);
                    case SASMode.AntiNormal:
                        return ReferenceFrame.Orbital (vessel).DirectionToWorldSpace (Vector3d.back);
                    case SASMode.Radial:
                        return ReferenceFrame.Orbital (vessel).DirectionToWorldSpace (Vector3d.left);
                    case SASMode.AntiRadial:
                        return ReferenceFrame.Orbital (vessel).DirectionToWorldSpace (Vector3d.right);
                    }
                } else if (SpeedMode == SpeedMode.Surface) {
                    switch (SASMode) {
                    case SASMode.Prograde:
                        return ReferenceFrame.SurfaceVelocity (vessel).DirectionToWorldSpace (Vector3d.up);
                    case SASMode.Retrograde:
                        return ReferenceFrame.SurfaceVelocity (vessel).DirectionToWorldSpace (Vector3d.down);
                    case SASMode.Normal:
                        return ReferenceFrame.Object (vessel.orbit.referenceBody).DirectionToWorldSpace (Vector3d.up);
                    case SASMode.AntiNormal:
                        return ReferenceFrame.Object (vessel.orbit.referenceBody).DirectionToWorldSpace (Vector3d.down);
                    case SASMode.Radial:
                        return ReferenceFrame.Surface (vessel).DirectionToWorldSpace (Vector3d.right);
                    case SASMode.AntiRadial:
                        return ReferenceFrame.Surface (vessel).DirectionToWorldSpace (Vector3d.left);
                    }
                } else if (SpeedMode == SpeedMode.Target) {
                    switch (SASMode) {
                    case SASMode.Prograde:
                        return vessel.GetWorldVelocity () - FlightGlobals.fetch.VesselTarget.GetWorldVelocity ();
                    case SASMode.Retrograde:
                        return FlightGlobals.fetch.VesselTarget.GetWorldVelocity () - vessel.GetWorldVelocity ();
                    case SASMode.Normal:
                        return ReferenceFrame.Object (vessel.orbit.referenceBody).DirectionToWorldSpace (Vector3d.up);
                    case SASMode.AntiNormal:
                        return ReferenceFrame.Object (vessel.orbit.referenceBody).DirectionToWorldSpace (Vector3d.down);
                    case SASMode.Radial:
                        return ReferenceFrame.Surface (vessel).DirectionToWorldSpace (Vector3d.right);
                    case SASMode.AntiRadial:
                        return ReferenceFrame.Surface (vessel).DirectionToWorldSpace (Vector3d.left);
                    }
                }
                throw new InvalidOperationException ("Unknown speed mode for orbital direction");
            }

            // Target and anti-target
            if (SASMode == SASMode.Target || SASMode == SASMode.AntiTarget) {
                var target = FlightGlobals.fetch.VesselTarget;
                if (target == null)
                    throw new InvalidOperationException ("No target");
                var direction = target.GetWorldPosition () - vessel.GetWorldPos3D ();
                if (SASMode == SASMode.AntiTarget)
                    direction *= -1;
                return direction;
            }

            throw new InvalidOperationException ("Unknown SAS mode");
        }

        internal static void Fly (global::Vessel vessel, FlightCtrlState state)
        {
            // Get the auto-pilot object. Do nothing if there is no auto-pilot engaged for this vessel.
            if (!engaged.ContainsKey (vessel))
                return;
            var autoPilot = engaged [vessel];
            if (autoPilot == null)
                return;
            // If the client that engaged the auto-pilot has disconnected, disengage the auto-pilot
            if (autoPilot.requestingClient != null && !autoPilot.requestingClient.Connected) {
                autoPilot.Disengage ();
                return;
            }
            // Run the auto-pilot
            autoPilot.DoAutoPiloting (state);
        }

        void DoAutoPiloting (FlightCtrlState state)
        {
            SAS = false;
            SteerShipToward (TargetRotation (), state, vessel);
        }

        Quaternion TargetRotation ()
        {
            // Compute world space target rotation from pitch, heading, roll
            Quaternion rotation = GeometryExtensions.QuaternionFromPitchHeadingRoll (new Vector3d (pitch, heading, float.IsNaN (roll) ? 0.0f : roll));
            var target = referenceFrame.RotationToWorldSpace (rotation);

            // If roll is not specified, re-compute rotation between direction vectors
            if (float.IsNaN (roll)) {
                var from = Vector3.up;
                var to = target * Vector3.up;
                target = Quaternion.FromToRotation (from, to);
            }

            return target;
        }

        Vector3d TargetDirection ()
        {
            return TargetRotation () * Vector3.up;
        }

        double ComputeError (Quaternion target)
        {
            return Math.Abs (Quaternion.Angle (vessel.ReferenceTransform.rotation, target));
        }

        static void SteerShipToward (Quaternion target, FlightCtrlState c, global::Vessel vessel)
        {
            target = target * Quaternion.Inverse (Quaternion.Euler (90, 0, 0));

            var centerOfMass = vessel.findWorldCenterOfMass ();
            var momentOfInertia = vessel.findLocalMOI (centerOfMass);

            var vesselRotation = vessel.ReferenceTransform.rotation;

            Quaternion delta = Quaternion.Inverse (Quaternion.Euler (90, 0, 0) * Quaternion.Inverse (vesselRotation) * target);

            Vector3d deltaEuler = ReduceAngles (delta.eulerAngles);
            deltaEuler.y *= -1;

            Vector3d torque = GetTorque (vessel, c.mainThrottle);
            Vector3d inertia = GetEffectiveInertia (vessel, torque);

            Vector3d err = deltaEuler * Math.PI / 180.0F;
            err += new Vector3d (inertia.x, inertia.z, inertia.y);

            Vector3d act = 120.0f * err;

            float precision = Mathf.Clamp ((float)torque.x * 20f / momentOfInertia.magnitude, 0.5f, 10f);
            float driveLimit = Mathf.Clamp01 ((float)(err.magnitude * 380.0f / precision));

            act.x = Mathf.Clamp ((float)act.x, -driveLimit, driveLimit);
            act.y = Mathf.Clamp ((float)act.y, -driveLimit, driveLimit);
            act.z = Mathf.Clamp ((float)act.z, -driveLimit, driveLimit);

            c.roll = Mathf.Clamp ((float)(c.roll + act.z), -driveLimit, driveLimit);
            c.pitch = Mathf.Clamp ((float)(c.pitch + act.x), -driveLimit, driveLimit);
            c.yaw = Mathf.Clamp ((float)(c.yaw + act.y), -driveLimit, driveLimit);

        }

        static Vector3d GetEffectiveInertia (global::Vessel vessel, Vector3d torque)
        {
            var centerOfMass = vessel.findWorldCenterOfMass ();
            var momentOfInertia = vessel.findLocalMOI (centerOfMass);
            var angularVelocity = Quaternion.Inverse (vessel.ReferenceTransform.rotation) * vessel.rigidbody.angularVelocity;
            var angularMomentum = new Vector3d (angularVelocity.x * momentOfInertia.x, angularVelocity.y * momentOfInertia.y, angularVelocity.z * momentOfInertia.z);

            var retVar = Vector3d.Scale
                (
                             Sign (angularMomentum) * 2.0f,
                             Vector3d.Scale (Pow (angularMomentum, 2), Inverse (Vector3d.Scale (torque, momentOfInertia)))
                         );

            retVar.y *= 10;

            return retVar;
        }

        static Vector3d GetTorque (global::Vessel vessel, float thrust)
        {
            var centerOfMass = vessel.findWorldCenterOfMass ();
            var rollaxis = vessel.ReferenceTransform.up;
            rollaxis.Normalize ();
            var pitchaxis = vessel.GetFwdVector ();
            pitchaxis.Normalize ();

            float pitch = 0.0f;
            float yaw = 0.0f;
            float roll = 0.0f;

            foreach (var part in vessel.parts) {
                var relCoM = part.Rigidbody.worldCenterOfMass - centerOfMass;

                foreach (global::PartModule module in part.Modules) {
                    var wheel = module as ModuleReactionWheel;
                    if (wheel == null)
                        continue;

                    pitch += wheel.PitchTorque;
                    yaw += wheel.YawTorque;
                    roll += wheel.RollTorque;
                }
                if (vessel.ActionGroups [KSPActionGroup.RCS]) {
                    foreach (global::PartModule module in part.Modules) {
                        var rcs = module as ModuleRCS;
                        if (rcs == null || !rcs.rcsEnabled)
                            continue;

                        bool enoughfuel = rcs.propellants.All (p => (int)(p.totalResourceAvailable) != 0);
                        if (!enoughfuel)
                            continue;
                        foreach (Transform thrustdir in rcs.thrusterTransforms) {
                            float rcsthrust = rcs.thrusterPower;
                            //just counting positive contributions in one direction. This is incorrect for asymmetric thruster placements.
                            roll += Mathf.Max (rcsthrust * Vector3.Dot (Vector3.Cross (relCoM, thrustdir.up), rollaxis), 0.0f);
                            pitch += Mathf.Max (rcsthrust * Vector3.Dot (Vector3.Cross (Vector3.Cross (relCoM, thrustdir.up), rollaxis), pitchaxis), 0.0f);
                            yaw += Mathf.Max (rcsthrust * Vector3.Dot (Vector3.Cross (Vector3.Cross (relCoM, thrustdir.up), rollaxis), Vector3.Cross (rollaxis, pitchaxis)), 0.0f);
                        }
                    }
                }
                pitch += (float)GetThrustTorque (part, vessel) * thrust;
                yaw += (float)GetThrustTorque (part, vessel) * thrust;
            }

            return new Vector3d (pitch, roll, yaw);
        }

        static double GetThrustTorque (global::Part p, global::Vessel vessel)
        {
            //TODO: implement gimbalthrust Torque calculation
            return 0;
        }

        static Vector3d Pow (Vector3d vector, float exponent)
        {
            return new Vector3d (Math.Pow (vector.x, exponent), Math.Pow (vector.y, exponent), Math.Pow (vector.z, exponent));
        }

        static Vector3d ReduceAngles (Vector3d input)
        {
            return new Vector3d (
                (input.x > 180f) ? (input.x - 360f) : input.x,
                (input.y > 180f) ? (input.y - 360f) : input.y,
                (input.z > 180f) ? (input.z - 360f) : input.z
            );
        }

        static Vector3d Inverse (Vector3d input)
        {
            return new Vector3d (1 / input.x, 1 / input.y, 1 / input.z);
        }

        static Vector3d Sign (Vector3d vector)
        {
            return new Vector3d (Math.Sign (vector.x), Math.Sign (vector.y), Math.Sign (vector.z));
        }
    }
}
