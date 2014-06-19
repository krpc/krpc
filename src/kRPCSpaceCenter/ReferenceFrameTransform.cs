using System;
using UnityEngine;
using KSP.IO;
using KRPCSpaceCenter.Services;

namespace KRPCSpaceCenter
{
    /// <summary>
    /// Used to compute transformations for conversion between different reference frames and world-space
    /// </summary>
    /// <remarks>
    /// Adapted from MJ2/RT2/forum discussion; credit goes to r4m0n, Cilph and mic_e
    /// https://github.com/MuMech/MechJeb2
    /// https://github.com/Cilph/RemoteTech2
    /// http://forum.kerbalspaceprogram.com/threads/69313-WIP-kRPC-A-language-agnostic-Remote-Procedure-Call-server-for-KSP?p=1021721&viewfull=1#post1021721
    /// </remarks>
    static class ReferenceFrameTransform
    {
        /// <summary>
        /// Returns the up vector for the given reference frame in world coordinates.
        /// The resulting vector is not normalized.
        /// </summary>
        static Vector3d GetUpNotNormalized (ReferenceFrame referenceFrame, Vessel vessel)
        {
            switch (referenceFrame) {
            case ReferenceFrame.Orbital:
            case ReferenceFrame.Surface:
            case ReferenceFrame.Maneuver:
            case ReferenceFrame.Target:
                return ((Vector3d)vessel.CoM) - vessel.mainBody.position;
            // Relative to the negative surface normal of the target docking port, or relative to the target if the target is not a docking port
            case ReferenceFrame.Docking:
                throw new NotImplementedException ();
            default:
                throw new ArgumentException ("No such reference frame");
            }
        }

        /// <summary>
        /// Returns the forward vector for the given reference frame in world coordinates.
        /// The resulting vector is not normalized.
        /// </summary>
        static Vector3d GetForwardNotNormalized (ReferenceFrame referenceFrame, Vessel vessel)
        {
            switch (referenceFrame) {
            // Relative to the orbital velocity vector
            //case ReferenceFrame.Orbital:
            //    return vessel.GetObtVelocity ();
            // Relative to the surface velocity vector
            //case ReferenceFrame.Surface:
            //    return vessel.GetSrfVelocity ();
            // Relative to the surface / navball
            case ReferenceFrame.Orbital:
            case ReferenceFrame.Surface:
                {
                    var up = GetUp (referenceFrame, vessel);
                    var exclude = vessel.mainBody.position + ((Vector3d)vessel.mainBody.transform.up) * vessel.mainBody.Radius - ((Vector3d)vessel.CoM);
                    return Vector3d.Exclude (up, exclude);
                }
            // Relative to the direction of the burn for a maneuver node, or relative to the orbit if there is no node
            case ReferenceFrame.Maneuver:
                {
                    if (vessel.patchedConicSolver.maneuverNodes.Count > 0)
                        return vessel.patchedConicSolver.maneuverNodes [0].GetBurnVector (vessel.orbit);
                    else
                        return GetForward (ReferenceFrame.Orbital, vessel);
                }
            // Relative to the target's velocity vector, or relative to the orbit if there is no target
            case ReferenceFrame.Target:
                {
                    var target = FlightGlobals.fetch.VesselTarget;
                    if (target != null)
                        return ((Vector3d)vessel.GetObtVelocity ()) - ((Vector3d)target.GetObtVelocity ());
                    else
                        return GetForward (ReferenceFrame.Orbital, vessel);
                }
            // Relative to the direction to the target, or relative to the orbit if there is no target
            //case ReferenceFrame.Target:
            //    {
            //        var target = FlightGlobals.fetch.VesselTarget;
            //        if (target != null)
            //            // TODO: use the center of control instead of v.CoM?
            //            return target.GetTransform ().position - vessel.CoM;
            //        else
            //            return GetForward (ReferenceFrame.Orbital, vessel);
            //    }
            // Relative to the negative surface normal of the target docking port, or relative to the target if the target is not a docking port
            case ReferenceFrame.Docking:
                throw new NotImplementedException ();
            default:
                throw new ArgumentException ("No such reference frame");
            }
        }

        /// <summary>
        /// Returns the up vector for the given reference frame in world coordinates
        /// </summary>
        public static Vector3d GetUp (ReferenceFrame referenceFrame, Vessel vessel)
        {
            return GetUpNotNormalized (referenceFrame, vessel).normalized;
        }

        /// <summary>
        /// Returns the forward vector for the given reference frame in world coordinates
        /// </summary>
        public static Vector3d GetForward (ReferenceFrame referenceFrame, Vessel vessel)
        {
            return GetForwardNotNormalized (referenceFrame, vessel).normalized;
        }

        /// <summary>
        /// Returns the rotation for the given frame of reference.
        /// Applying the rotation to a vector in reference-frame-space produces the corresponding vector in world-space.
        /// </summary>
        public static QuaternionD GetRotation (ReferenceFrame referenceFrame, Vessel vessel)
        {
            if (referenceFrame == ReferenceFrame.Docking) {
                var target = FlightGlobals.fetch.VesselTarget;
                if (target != null && target is ModuleDockingNode) {
                    // Who knows why this Quaternion.Euler(90, 0, 0) rotation is required?
                    // Note that the delta-calculation in AutoPilotAddon seems to reverse this rotation.
                    // So it seems like a problem with what Quaternion.LookRotation does.
                    // FIXME: Uses single precision floating point
                    return (target as ModuleDockingNode).transform.rotation * Quaternion.Euler (90, 0, 0);
                } else {
                    return GetRotation (ReferenceFrame.Target, vessel);
                }
            }
            var forward = GetForwardNotNormalized (referenceFrame, vessel);
            // Note: forward is along the z-axis, up is along the negative y-axis
            var up = -GetUpNotNormalized (referenceFrame, vessel);
            //FIXME: Vector3d.OrthoNormalize and QuaternionD.LookRotation methods are not found at run-time
            //Vector3d.OrthoNormalize (ref forward, ref up);
            //return QuaternionD.LookRotation (forward, up);
            Vector3 forward2 = forward;
            Vector3 up2 = up;
            Vector3.OrthoNormalize (ref forward2, ref up2);
            return Quaternion.LookRotation (forward2, up2);
        }

        /// <summary>
        /// Returns the velocity of the reference frame in world-space.
        /// </summary>
        public static Vector3d GetVelocity (ReferenceFrame referenceFrame, Vessel vessel)
        {
            switch (referenceFrame) {
            case ReferenceFrame.Orbital:
            case ReferenceFrame.Maneuver:
                return Vector3d.zero;
            // Relative to the surface velocity vector
            case ReferenceFrame.Surface:
                return ((Vector3d)vessel.GetObtVelocity ()) - ((Vector3d)vessel.GetSrfVelocity ());
            // Relative to the target's velocity vector, or relative to the orbit if there is no target
            case ReferenceFrame.Target:
                throw new NotImplementedException ();
            // Relative to the negative surface normal of the target docking port, or relative to the target if the target is not a docking port
            case ReferenceFrame.Docking:
                throw new NotImplementedException ();
            default:
                throw new ArgumentException ("No such reference frame");
            }
        }
    }
}

