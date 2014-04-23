using KRPC.Service.Attributes;
using System;
using UnityEngine;
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
    public class AutoPilot
    {
        static bool Engaged = false;
        static ReferenceFrame ReferenceFrame = ReferenceFrame.Surface;
        static double Pitch = 0d;
        static double Yaw = 0d;
        static double Roll = 0d;
        const double DeltaInfluence = 1d;
        const double InertiaInfluence = 1d;
        const double StrengthPitch = 120d;
        const double StrengthYaw = 120d;
        const double StrengthRoll = 120d;

        public AutoPilot (global::Vessel vessel)
        {
        }

        [KRPCMethod]
        public void SetRotation (ReferenceFrame referenceFrame, double pitch, double yaw, double roll = Double.NaN)
        {
            Engaged = true;
            ReferenceFrame = referenceFrame;
            Pitch = pitch;
            Yaw = yaw;
            Roll = roll;
        }

        [KRPCMethod]
        public void SetDirection (ReferenceFrame referenceFrame, KRPC.Schema.Geometry.Vector3 direction, double roll = Double.NaN)
        {
            var rotation = Quaternion.FromToRotation (Vector3d.forward, direction.ToVector ());
            Engaged = true;
            ReferenceFrame = referenceFrame;
            Pitch = Math.Abs (((rotation.eulerAngles.x + 270f) % 360f) - 180f) - 90f;
            Yaw = 360f - rotation.eulerAngles.y;
            Roll = roll;
        }

        [KRPCMethod]
        public void Disengage ()
        {
            AutoPilot.Engaged = false;
        }

        [KRPCProperty]
        public double Error {
            get {
                if (Double.IsNaN (Roll))
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
            Quaternion delta = getErrorQuaternion ();
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
            Quaternion delta = getErrorQuaternion ();
            Vector3 test = new Vector3 (0, 0, 1);
            return Vector3.Angle (test, delta * test);
        }

        static Quaternion getErrorQuaternion ()
        {
            //if (FlightGlobals.ActiveVessel != vessel)
            //    throw new InvalidOperationException ();
            Quaternion vesselR = FlightGlobals.ActiveVessel.transform.rotation;
            Quaternion target = ReferenceFrameTransform.GetRotation (ReferenceFrame, FlightGlobals.ActiveVessel);
            // TODO: don't force the roll to 0 if specific roll not requested
            var roll = Double.IsNaN (AutoPilot.Roll) ? 0 : AutoPilot.Roll;
            target *= Quaternion.Euler (new Vector3d (Pitch, -Yaw, 180 - roll));
            return Quaternion.Inverse (Quaternion.Euler (90, 0, 0) * Quaternion.Inverse (vesselR) * target);
        }

        public static void Fly (FlightCtrlState state)
        {
            if (Engaged)
                DoAutoPiloting (state);
        }

        /// <summary>
        /// This is the main logic
        /// TODO: a controller that works with the delta quaternion as a whole, instead of the components.
        /// (nothing against kOS/MJ2, but this seems like a really cheap controller... not that I knew any better, though)
        /// </summary>
        static void DoAutoPiloting (FlightCtrlState state)
        {
            //if (FlightGlobals.ActiveVessel != vessel)
            //    return;

            Vector3d CoM = FlightGlobals.ActiveVessel.findWorldCenterOfMass ();
            Vector3d MoI = FlightGlobals.ActiveVessel.findLocalMOI (CoM);

            Quaternion delta = getErrorQuaternion ();

            Vector3d deltaEuler = ((Vector3d)delta.eulerAngles).ReduceAngles ();
            deltaEuler.y *= -1d;

            Vector3d torque = GetTorque (FlightGlobals.ActiveVessel, state.mainThrottle);
            Vector3d inertia = GetEffectiveInertia (FlightGlobals.ActiveVessel, torque);

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
        static Vector3d GetTorque (global::Vessel vessel, float thrust)
        {
            var CoM = vessel.findWorldCenterOfMass ();

            float pitchYaw = 0;
            float roll = 0;

            foreach (Part part in vessel.parts) {
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

                foreach (PartModule module in part.Modules) {
                    if (module is ModuleReactionWheel) {
                        pitchYaw += ((ModuleReactionWheel)module).PitchTorque;
                        roll += ((ModuleReactionWheel)module).RollTorque;
                    }
                }

                pitchYaw += (float)GetThrustTorque (part, vessel) * thrust;
            }

            return new Vector3d (pitchYaw, roll, pitchYaw);
        }

        /// <summary>
        // Calculate the amount of torque that can be provided by an engine
        /// </summary>
        static double GetThrustTorque (Part p, global::Vessel vessel)
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
        static Vector3d GetEffectiveInertia (global::Vessel vessel, Vector3d torque)
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
