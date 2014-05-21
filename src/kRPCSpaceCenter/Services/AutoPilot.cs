using System;
using System.Collections.Generic;
using UnityEngine;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
{
    /// <remarks>
    /// Taken and adapted from MJ2/kOS/RT2/and forum discussion; credit goes to the authors of those plugins:
    /// r4m0n (MJ2), Nivekk (kOS), Cilph (RT2), and mic_e.
    /// https://github.com/MuMech/MechJeb2
    /// https://github.com/Nivekk/KOS
    /// https://github.com/Cilph/RemoteTech2
    /// http://forum.kerbalspaceprogram.com/threads/69313-WIP-kRPC-A-language-agnostic-Remote-Procedure-Call-server-for-KSP?p=1021721&viewfull=1#post1021721
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class AutoPilot : Equatable<AutoPilot>
    {
        const double DeltaInfluence = 1d;
        const double InertiaInfluence = 1d;
        const double StrengthPitch = 120d;
        const double StrengthYaw = 120d;
        const double StrengthRoll = 120d;
        global::Vessel vessel;
        static HashSet<AutoPilot> engaged = new HashSet<AutoPilot> ();
        ReferenceFrame referenceFrame;
        double pitch;
        double yaw;
        double roll;

        internal AutoPilot (global::Vessel vessel)
        {
            this.vessel = vessel;
        }

        public override bool Equals (AutoPilot other)
        {
            return vessel == other.vessel;
        }

        public override int GetHashCode ()
        {
            return vessel.GetHashCode ();
        }

        [KRPCMethod]
        public void SetRotation (double pitch, double yaw, double roll = Double.NaN, ReferenceFrame referenceFrame = ReferenceFrame.Orbital)
        {
            engaged.Add (this);
            this.referenceFrame = referenceFrame;
            this.pitch = pitch;
            this.yaw = yaw;
            this.roll = roll;
        }

        [KRPCMethod]
        public void SetDirection (KRPC.Schema.Geometry.Vector3 direction, double roll = Double.NaN, ReferenceFrame referenceFrame = ReferenceFrame.Orbital)
        {
            engaged.Add (this);
            this.referenceFrame = referenceFrame;
            var rotation = Quaternion.FromToRotation (Vector3d.forward, direction.ToVector ());
            this.pitch = Math.Abs (((rotation.eulerAngles.x + 270f) % 360f) - 180f) - 90f;
            this.yaw = 360f - rotation.eulerAngles.y;
            this.roll = roll;
        }

        [KRPCMethod]
        public void Disengage ()
        {
            engaged.Remove (this);
        }

        [KRPCProperty]
        public double Error {
            get {
                if (Double.IsNaN (roll))
                    return GetPYError ();
                else
                    return GetPYRError ();
            }
        }

        /// <summary>
        /// Returns the error of the current rotation against the set orientation,
        /// i.e. how many degrees the ship will need to rotate around the ideal axis to
        /// reach that orientation.
        /// </summary>
        float GetPYRError ()
        {
            Quaternion delta = GetErrorQuaternion ();
            Vector3 axis;
            float angle;
            delta.ToAngleAxis (out angle, out axis);
            return (angle > 180f) ? 360f - angle : angle;
        }

        /// <summary>
        /// Returns the error of the current rotation against the set orientation,
        /// ignoring the vessel's roll, i.e.
        /// the angle between the ship's orientation vector and the target direction.
        /// </summary>
        float GetPYError ()
        {
            Quaternion delta = GetErrorQuaternion ();
            Vector3 test = new Vector3 (0, 0, 1);
            return Vector3.Angle (test, delta * test);
        }

        Quaternion GetErrorQuaternion ()
        {
            Quaternion vesselR = vessel.transform.rotation;
            Quaternion target = ReferenceFrameTransform.GetRotation (referenceFrame, vessel);
            // TODO: don't force the roll to 0 if specific roll not requested
            var actualRoll = Double.IsNaN (roll) ? 0 : roll;
            target *= Quaternion.Euler (new Vector3d (pitch, -yaw, 180 - actualRoll));
            return Quaternion.Inverse (Quaternion.Euler (90, 0, 0) * Quaternion.Inverse (vesselR) * target);
        }

        public static void Fly (FlightCtrlState state)
        {
            foreach (var autoPilot in engaged) {
                autoPilot.DoAutoPiloting (state); //FIXME
            }
        }

        /// <summary>
        /// This is the main logic
        /// TODO: a controller that works with the delta quaternion as a whole, instead of the components.
        /// (nothing against kOS/MJ2, but this seems like a really cheap controller... not that I knew any better, though)
        /// </summary>
        void DoAutoPiloting (FlightCtrlState state)
        {
            Vector3d CoM = vessel.findWorldCenterOfMass ();
            Vector3d MoI = vessel.findLocalMOI (CoM);

            Quaternion delta = GetErrorQuaternion ();

            Vector3d deltaEuler = ((Vector3d)delta.eulerAngles).ReduceAngles ();
            deltaEuler.y *= -1d;

            Vector3d torque = GetTorque (state.mainThrottle);
            Vector3d inertia = GetEffectiveInertia (torque);

            Vector3d err = DeltaInfluence * deltaEuler * Math.PI / 180d;
            err += InertiaInfluence * new Vector3d (inertia.x, inertia.z, inertia.y);

            Vector3d act = new Vector3d (err.x * StrengthPitch, err.y * StrengthYaw, err.z * StrengthRoll);

            double precision = (torque.x * 20f / MoI.magnitude).Clamp (0.5f, 10f);
            double drive_limit = (err.magnitude * 380.0f / precision).Clamp (0d, 1d);

            act.x = act.x.Clamp (-drive_limit, drive_limit);
            act.y = act.y.Clamp (-drive_limit, drive_limit);
            act.z = act.z.Clamp (-drive_limit, drive_limit);

            state.pitch += (float)act.x;
            state.yaw += (float)act.y;
            //if (!Double.IsNaN (Roll))
            state.roll += (float)act.z;
        }

        /// <summary>
        /// Calculate the amount of torque that can be provided by all parts of the vessel
        /// </summary>
        Vector3d GetTorque (float thrust)
        {
            var CoM = vessel.findWorldCenterOfMass ();

            float pitchYaw = 0;
            float roll = 0;

            foreach (global::Part part in vessel.parts) {
                var relCoM = part.Rigidbody.worldCenterOfMass - CoM;

                if (part is CommandPod) {
                    pitchYaw += Math.Abs (((CommandPod)part).rotPower);
                    roll += Math.Abs (((CommandPod)part).rotPower);
                }

                if (part is RCSModule) {
                    //huh... shouldn't RCS provide roll, too?
                    float max = 0;
                    foreach (float power in ((RCSModule)part).thrusterPowers) {
                        max = Mathf.Max (max, power);
                    }

                    pitchYaw += max * relCoM.magnitude;
                }

                foreach (global::PartModule module in part.Modules) {
                    if (module is ModuleReactionWheel) {
                        pitchYaw += ((ModuleReactionWheel)module).PitchTorque;
                        roll += ((ModuleReactionWheel)module).RollTorque;
                    }
                }

                pitchYaw += (float)GetThrustTorque (part) * thrust;
            }

            return new Vector3d (pitchYaw, roll, pitchYaw);
        }

        /// <summary>
        // Calculate the amount of torque that can be provided by an engine
        /// </summary>
        double GetThrustTorque (global::Part p)
        {
            var CoM = vessel.CoM;
            if (p.State == PartStates.ACTIVE) {
                if (p is LiquidEngine) {
                    if (((LiquidEngine)p).thrustVectoringCapable) {
                        return Math.Sin (Math.Abs (((LiquidEngine)p).gimbalRange) * Math.PI / 180) * ((LiquidEngine)p).maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    }
                } else if (p is LiquidFuelEngine) {
                    if (((LiquidFuelEngine)p).thrustVectoringCapable) {
                        return Math.Sin (Math.Abs (((LiquidFuelEngine)p).gimbalRange) * Math.PI / 180) * ((LiquidFuelEngine)p).maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    }
                } else if (p is AtmosphericEngine) {
                    if (((AtmosphericEngine)p).thrustVectoringCapable) {
                        return Math.Sin (Math.Abs (((AtmosphericEngine)p).gimbalRange) * Math.PI / 180) * ((AtmosphericEngine)p).maximumEnginePower * ((AtmosphericEngine)p).totalEfficiency * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    }
                }
            }
            return 0;
        }

        /// <summary>
        // Calculate the inertia vector for the vessel
        /// </summary>
        Vector3d GetEffectiveInertia (Vector3d torque)
        {
            var CoM = vessel.findWorldCenterOfMass ();
            var MoI = vessel.findLocalMOI (CoM);
            var angularVelocity = Quaternion.Inverse (vessel.transform.rotation) * vessel.rigidbody.angularVelocity;
            var angularMomentum = new Vector3d (angularVelocity.x * MoI.x, angularVelocity.y * MoI.y, angularVelocity.z * MoI.z);
            var retVar = Vector3d.Scale (angularMomentum.Sign () * 2.0f, Vector3d.Scale (angularMomentum.Pow (2), Vector3d.Scale (torque, MoI).Inverse ()));
            retVar.y *= 10;
            return retVar;
        }
    }
}
