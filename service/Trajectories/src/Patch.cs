using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.KRPC;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using KRPC.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.Trajectories
{
    /// <summary>
    /// Trajectories patch.
    /// </summary>
    [KRPCClass(Service = "Trajectories")]
    public class Patch : Equatable<Patch>, IGameObjectState
    {
        readonly ReferenceFrame patchFrame;
        private readonly API.Trajectory.Patch internalPatch;

        /// <summary>
        /// Internal Trajectories Patch
        /// </summary>
        internal API.Trajectory.Patch InternalPatch
        {
            get
            {
                var patch = internalPatch;
                if (patch != null)
                    return patch;

                throw NotResolvable();
            }
        }

        internal Patch(API.Trajectory.Patch patch)
        {
            this.internalPatch = patch;
            this.patchFrame = new SpaceCenter.Services.CelestialBody(patch.StartingState.ReferenceBody).NonRotatingReferenceFrame;
        }

        private static Vector3d CalculateRotatedVector(CelestialBody body, Vector3d relativePosition, double time)
        {
            float angle = (float)(-(time - Planetarium.GetUniversalTime()) * body.angularVelocity.magnitude / Math.PI * 180.0);
            Quaternion bodyRotation = Quaternion.AngleAxis(angle, body.angularVelocity.normalized);
            return bodyRotation * relativePosition;
        }

        private static Vector3d CalculateInverseRotatedVector(CelestialBody body, Vector3d relativePosition, double time)
        {
            float angle = (float)(-(time - Planetarium.GetUniversalTime()) * body.angularVelocity.magnitude / Math.PI * 180.0);
            Quaternion bodyRotation = Quaternion.AngleAxis(angle, body.angularVelocity.normalized);
            return bodyRotation.Inverse() * relativePosition;
        }

        /// <summary>
        /// Whether the reference frame turns at the rate of the body, to within 1%, such as
        /// the body's own frame or a frame fixed to its surface.
        /// </summary>
        bool RotatesWithBody(ReferenceFrame referenceFrame)
        {
            Vector3d bodyAngularVelocity = InternalPatch.StartingState.ReferenceBody.angularVelocity;
            // Relative, as the frame of a landed vessel takes its angular velocity from physics
            return (referenceFrame.AngularVelocity - bodyAngularVelocity).magnitude <=
                0.01 * bodyAngularVelocity.magnitude;
        }

        /// <summary>
        /// Impact position, <c>null</c> if no impact.
        /// 
        /// If the reference frame rotates with <see cref="Patch.Body"/>, such as its own reference frame, then position includes rotation with time.
        /// </summary>
        /// <param name="referenceFrame">Reference frame to use</param>
        [KRPCMethod(Nullable = true)]
        public Tuple3 Position(ReferenceFrame referenceFrame)
        {
            Vector3d? localPos = InternalPatch.RawImpactPosition;

            if (localPos.HasValue)
            {
                if (RotatesWithBody(referenceFrame))
                {
                    Vector3d worldPos = patchFrame.Position + CalculateRotatedVector(Body.InternalBody, localPos.Value, EndUT);
                    return referenceFrame.PositionFromWorldSpace(worldPos).ToTuple();
                }
                else
                    return referenceFrame.PositionFromWorldSpace(patchFrame.Position + localPos.Value).ToTuple();
            }
            else
                return null;
        }

        /// <summary>
        /// Impact velocity, <c>null</c> if no impact.
        /// 
        /// If the reference frame rotates with <see cref="Patch.Body"/>, such as its own reference frame, then velocity includes rotation with time.
        /// </summary>
        /// <param name="referenceFrame">Reference frame to use</param>
        [KRPCMethod(Nullable = true)]
        public Tuple3 Velocity(ReferenceFrame referenceFrame)
        {
            Vector3d? localPos = InternalPatch.RawImpactPosition;
            Vector3d? localVel = InternalPatch.ImpactVelocity;

            if (localVel.HasValue)
            {
                if (RotatesWithBody(referenceFrame))
                {
                    Vector3d worldPos = patchFrame.Position + CalculateRotatedVector(Body.InternalBody, localPos.Value, EndUT);
                    Vector3d worldVel = patchFrame.Velocity + CalculateRotatedVector(Body.InternalBody, localVel.Value, EndUT);
                    return referenceFrame.VelocityFromWorldSpace(worldPos, worldVel).ToTuple();
                }
                else
                {
                    Vector3d worldPos = patchFrame.Position + localPos.Value;
                    return referenceFrame.VelocityFromWorldSpace(worldPos, patchFrame.Velocity + localVel.Value).ToTuple();
                }
            }
            else
                return null;
        }

        /// <summary>
        /// <see cref="KRPC.SpaceCenter.Services.CelestialBody"/> used for calculation.
        /// </summary>
        [KRPCProperty]
        public KRPC.SpaceCenter.Services.CelestialBody Body
        {
            get
            {
                return new KRPC.SpaceCenter.Services.CelestialBody(InternalPatch.StartingState.ReferenceBody);
            }
        }

        /// <summary>
        /// Start of patch in UT.
        /// </summary>
        [KRPCProperty]
        public double StartUT
        {
            get
            {
                return InternalPatch.StartingState.Time;
            }
        }

        /// <summary>
        /// End of patch in UT.
        /// 
        /// For impact this also happens to be the impact time.
        /// </summary>
        [KRPCProperty]
        public double EndUT
        {
            get
            {
                return InternalPatch.EndTime;
            }
        }

        /// <summary>
        /// If atmospheric.
        /// </summary>
        [KRPCProperty]
        public bool Atmospheric
        {
            get
            {
                return InternalPatch.IsAtmospheric;
            }
        }

        /// <summary>
        /// Array of trajectory points, <c>null</c> if <see cref="Atmospheric"/> is <c>false</c>.
        /// 
        /// If the reference frame rotates with <see cref="Patch.Body"/>, such as its own reference frame, then position includes rotation with time.
        /// </summary>
        /// <param name="referenceFrame">Reference frame to use</param>
        [KRPCMethod(Nullable = true)]
        public IList<Tuple4> AtmosphericTrajectory(ReferenceFrame referenceFrame)
        {
            if (Atmospheric)
            {
                IList<Tuple4> ret = new List<Tuple4>();

                foreach (API.Trajectory.Point point in InternalPatch.AtmosphericTrajectory)
                {
                    double time = point.time;

                    Vector3d worldPos;

                    // When BodyFixedMode is enabled positions are stored with rotation
                    if (RotatesWithBody(referenceFrame))
                        if (API.Settings.get_BodyFixedMode())
                            worldPos = patchFrame.Position + point.pos;
                        else
                            worldPos = patchFrame.Position + CalculateRotatedVector(Body.InternalBody, point.pos, time);
                    else
                        if (API.Settings.get_BodyFixedMode())
                            worldPos = patchFrame.Position + CalculateInverseRotatedVector(Body.InternalBody, point.pos, time);
                        else
                            worldPos = patchFrame.Position + point.pos;

                    Vector3d refPos = referenceFrame.PositionFromWorldSpace(worldPos);
                    ret.Add(new Tuple4(time, refPos.x, refPos.y, refPos.z));
                }
                return ret;
            }
            else
                return null;
        }

        /// <summary>
        /// <see cref="KRPC.SpaceCenter.Services.Orbit"/> containing orbital trajectory, <c>null</c> if <see cref="Atmospheric"/> is <c>true</c>.
        /// 
        /// Unlike all other properties it does not depend on a <see cref="ReferenceFrame"/>.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public KRPC.SpaceCenter.Services.Orbit OrbitalTrajectory
        {
            get
            {
                if (!Atmospheric)
                    return new KRPC.SpaceCenter.Services.Orbit(InternalPatch.SpaceOrbit);
                else
                    return null;
            }
        }

        /// <summary>
        /// Check that patches are the same.
        /// </summary>
        public override bool Equals(Patch other)
        {
            return !ReferenceEquals(other, null) && InternalPatch == other.InternalPatch;
        }

        /// <summary>
        /// Hash the Patch.
        /// </summary>
        public override int GetHashCode()
        {
            return InternalPatch.GetHashCode();
        }

        /// <summary>
        /// The error to raise when the patch cannot be found in Trajectories patch
        /// which <see cref="GameObjectState" /> decides.
        /// </summary>
        Exception NotResolvable()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                return new ObjectDestroyedException(
                    "Trajectories patch no longer exists as it's not in the current calculated trajectory.");
            return new ObjectNotLoadedException(
                "Trajectories patch is not loaded because Trajectories is not available.");
        }

        /// <summary>
        /// Keep GameObject live while it's in Trajectories patches and destroy if not.
        /// If Trajectories not available or GameState.Settled it's kept dormant.
        /// </summary>
        public GameObjectState GameObjectState
        {
            get
            {
                if (!Trajectories.Available || GameState.Settled)
                    return GameObjectState.Dormant;

                // Compares with internal Patch from Trajectories.
                return API.Trajectory.PatchesList.Contains(internalPatch) ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }
    }
}
