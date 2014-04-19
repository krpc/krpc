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
        /// Returns a rotation for the reference frame specified by a forward and up vector
        /// </summary>
        static Quaternion FromForwardAndUp (Vector3 forward, Vector3 up)
        {
            Vector3.OrthoNormalize (ref forward, ref up);
            return Quaternion.LookRotation (forward, up);
        }

        /// <summary>
        /// Returns the reference rotation quaternion for the given mode.
        /// Applying the rotation converts from the given reference frame to world space. TODO: is this correct?!?
        /// </summary>
        public static Quaternion Get (ReferenceFrame referenceFrame, Vessel vessel)
        {
            Vector3 up = vessel.mainBody.position - vessel.CoM;

            switch (referenceFrame) {
            // Relative to the orbital velocity vector
            case ReferenceFrame.Orbital:
                return FromForwardAndUp (vessel.GetObtVelocity (), up);
            // Relative to the surface velocity vector
            case ReferenceFrame.SurfaceVelocity:
                return FromForwardAndUp (vessel.GetSrfVelocity (), up);
            // Relative to the surface / navball
            case ReferenceFrame.Surface:
                {
                    Vector3 forward = Vector3.Exclude (up, vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius - vessel.CoM);
                    return FromForwardAndUp (forward, up);
                }
            // Relative to the direction of the burn for a maneuver node, or relative to the orbit if there is no node
            case ReferenceFrame.Maneuver:
                {
                    if (vessel.patchedConicSolver.maneuverNodes.Count > 0) {
                        Vector3 forward = vessel.patchedConicSolver.maneuverNodes [0].GetBurnVector (vessel.orbit);
                        return FromForwardAndUp (forward, up);
                    } else
                        return Get (ReferenceFrame.Orbital, vessel);
                }
            // Relative to the target's velocity vector, or relative to the orbit if there is no target
            case ReferenceFrame.TargetVelocity:
                {
                    var target = FlightGlobals.fetch.VesselTarget;
                    if (target != null)
                        return FromForwardAndUp (vessel.GetObtVelocity () - target.GetObtVelocity (), up);
                    else
                        return Get (ReferenceFrame.Orbital, vessel);
                }
            // Relative to the direction to the target, or relative to the orbit if there is no target
            case ReferenceFrame.Target:
                {
                    var target = FlightGlobals.fetch.VesselTarget;
                    if (target != null)
                        // TODO: use the center of control instead of v.CoM?
                        return FromForwardAndUp (target.GetTransform ().position - vessel.CoM, up);
                    else
                        return Get (ReferenceFrame.Orbital, vessel);
                }
            // Relative to the negative surface normal of the target docking port, or relative to the target if the target is not a docking port
            case ReferenceFrame.Docking:
                {
                    var target = FlightGlobals.fetch.VesselTarget;
                    if (target != null && target is ModuleDockingNode) {
                        // Who knows why this Quaternion.Euler(90, 0, 0) rotation is required?
                        // Note that the delta-calculation in AutoPilotAddon seems to reverse this rotation.
                        // So it seems like a problem with what Quaternion.LookRotation does.
                        return (target as ModuleDockingNode).transform.rotation * Quaternion.Euler (90, 0, 0);
                    } else {
                        return Get (ReferenceFrame.Target, vessel);
                    }
                }
            default:
                throw new ArgumentException ("No such reference frame");
            }
        }
    }
}

