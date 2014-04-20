using System;
using UnityEngine;
using KSP.IO;
using KRPCServices.Services;

namespace KRPCServices
{
    /// <remarks>
    /// Adapted from MJ2/RT2/forum discussion; credit goes to r4m0n, Cilph and mic_e
    /// https://github.com/MuMech/MechJeb2
    /// https://github.com/Cilph/RemoteTech2
    /// http://forum.kerbalspaceprogram.com/threads/69313-WIP-kRPC-A-language-agnostic-Remote-Procedure-Call-server-for-KSP?p=1021721&viewfull=1#post1021721
    /// </remarks>
    static class ReferenceFrameRotation
    {
        /// <summary>
        /// Returns the up vector for the given reference frame in world coordinates
        /// Vector is not normalized
        /// </summary>
        static Vector3 GetUpNotNormalized (ReferenceFrame referenceFrame, Vessel vessel)
        {
            switch (referenceFrame) {
            case ReferenceFrame.Orbital:
            case ReferenceFrame.SurfaceVelocity:
            case ReferenceFrame.Surface:
            case ReferenceFrame.Maneuver:
            case ReferenceFrame.TargetVelocity:
            case ReferenceFrame.Target:
                return vessel.CoM - vessel.mainBody.position;
            // Relative to the negative surface normal of the target docking port, or relative to the target if the target is not a docking port
            case ReferenceFrame.Docking:
                throw new NotImplementedException ();
            default:
                throw new ArgumentException ("No such reference frame");
            }
        }

        /// <summary>
        /// Returns the forward vector for the given reference frame in world coordinates
        /// Vector is not normalized
        /// </summary>
        static Vector3 GetForwardNotNormalized (ReferenceFrame referenceFrame, Vessel vessel)
        {
            switch (referenceFrame) {
            // Relative to the orbital velocity vector
            case ReferenceFrame.Orbital:
                return vessel.GetObtVelocity ();
            // Relative to the surface velocity vector
            case ReferenceFrame.SurfaceVelocity:
                return vessel.GetSrfVelocity ();
            // Relative to the surface / navball
            case ReferenceFrame.Surface:
                return Vector3.Exclude (GetUp (referenceFrame, vessel), vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius - vessel.CoM);
            // Relative to the direction of the burn for a maneuver node, or relative to the orbit if there is no node
            case ReferenceFrame.Maneuver:
                {
                    if (vessel.patchedConicSolver.maneuverNodes.Count > 0)
                        return vessel.patchedConicSolver.maneuverNodes [0].GetBurnVector (vessel.orbit);
                    else
                        return GetForward (ReferenceFrame.Orbital, vessel);
                }
            // Relative to the target's velocity vector, or relative to the orbit if there is no target
            case ReferenceFrame.TargetVelocity:
                {
                    var target = FlightGlobals.fetch.VesselTarget;
                    if (target != null)
                        return vessel.GetObtVelocity () - target.GetObtVelocity ();
                    else
                        return GetForward (ReferenceFrame.Orbital, vessel);
                }
            // Relative to the direction to the target, or relative to the orbit if there is no target
            case ReferenceFrame.Target:
                {
                    var target = FlightGlobals.fetch.VesselTarget;
                    if (target != null)
                        // TODO: use the center of control instead of v.CoM?
                        return target.GetTransform ().position - vessel.CoM;
                    else
                        return GetForward (ReferenceFrame.Orbital, vessel);
                }
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
        public static Vector3 GetUp (ReferenceFrame referenceFrame, Vessel vessel)
        {
            return GetUpNotNormalized (referenceFrame, vessel).normalized;
        }

        /// <summary>
        /// Returns the forward vector for the given reference frame in world coordinates
        /// </summary>
        public static Vector3 GetForward (ReferenceFrame referenceFrame, Vessel vessel)
        {
            return GetForwardNotNormalized (referenceFrame, vessel).normalized;
        }

        /// <summary>
        /// Returns the reference rotation quaternion for the given mode.
        /// Applying the rotation converts from the given reference frame to world space. TODO: is this correct?!?
        /// </summary>
        public static Quaternion GetRotation (ReferenceFrame referenceFrame, Vessel vessel)
        {
            if (referenceFrame == ReferenceFrame.Docking) {
                var target = FlightGlobals.fetch.VesselTarget;
                if (target != null && target is ModuleDockingNode) {
                    // Who knows why this Quaternion.Euler(90, 0, 0) rotation is required?
                    // Note that the delta-calculation in AutoPilotAddon seems to reverse this rotation.
                    // So it seems like a problem with what Quaternion.LookRotation does.
                    return (target as ModuleDockingNode).transform.rotation * Quaternion.Euler (90, 0, 0);
                } else {
                    return GetRotation (ReferenceFrame.Target, vessel);
                }
            }
            var forward = GetForwardNotNormalized (referenceFrame, vessel);
            // Note: forward is along the z-axis, up is along the negative y-axis
            var up = -GetUpNotNormalized (referenceFrame, vessel);
            Vector3.OrthoNormalize (ref forward, ref up);
            return Quaternion.LookRotation (forward, -up);
        }
    }
}

