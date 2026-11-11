using KRPC.Service.Attributes;
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
    /// A patch of the trajectory, at its position in <see cref="Trajectories.Patches"/>.
    /// </summary>
    /// <remarks>
    /// Trajectories computes the trajectory again as the vessel flies, and the patch reads
    /// the latest one. Throws an exception if the trajectory has fewer patches.
    /// </remarks>
    [KRPCClass(Service = "Trajectories")]
    public class Patch : Equatable<Patch>
    {
        readonly int index;

        internal Patch(int index)
        {
            this.index = index;
        }

        /// <summary>
        /// The mod's patch at this position in the current trajectory.
        /// </summary>
        API.Trajectory.Patch InternalPatch
        {
            get
            {
                var patch = API.Trajectory.PatchAt(index);
                if (patch == null)
                    throw new InvalidOperationException(
                        "The trajectory has no patch at index " + index + ", as it has " +
                        API.Trajectory.PatchCount + " patches");
                return patch;
            }
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
        static bool RotatesWithBody(CelestialBody body, ReferenceFrame referenceFrame)
        {
            // Relative, as the frame of a landed vessel takes its angular velocity from physics
            return (referenceFrame.AngularVelocity - body.angularVelocity).magnitude <=
                0.01 * body.angularVelocity.magnitude;
        }

        static ReferenceFrame BodyFrame(CelestialBody body)
        {
            return new SpaceCenter.Services.CelestialBody(body).NonRotatingReferenceFrame;
        }

        /// <summary>
        /// The impact position of a patch of the mod, or null without an impact.
        /// </summary>
        internal static Tuple3 ImpactPosition(API.Trajectory.Patch patch, ReferenceFrame referenceFrame)
        {
            Vector3d? localPos = patch.RawImpactPosition;
            if (!localPos.HasValue)
                return null;
            var body = patch.StartingState.ReferenceBody;
            var bodyFrame = BodyFrame(body);
            if (RotatesWithBody(body, referenceFrame))
            {
                Vector3d worldPos = bodyFrame.Position + CalculateRotatedVector(body, localPos.Value, patch.EndTime);
                return referenceFrame.PositionFromWorldSpace(worldPos).ToTuple();
            }
            return referenceFrame.PositionFromWorldSpace(bodyFrame.Position + localPos.Value).ToTuple();
        }

        /// <summary>
        /// The impact velocity of a patch of the mod, or null without an impact.
        /// </summary>
        internal static Tuple3 ImpactVelocity(API.Trajectory.Patch patch, ReferenceFrame referenceFrame)
        {
            Vector3d? localPos = patch.RawImpactPosition;
            Vector3d? localVel = patch.ImpactVelocity;
            if (!localPos.HasValue || !localVel.HasValue)
                return null;
            var body = patch.StartingState.ReferenceBody;
            var bodyFrame = BodyFrame(body);
            if (RotatesWithBody(body, referenceFrame))
            {
                Vector3d worldPos = bodyFrame.Position + CalculateRotatedVector(body, localPos.Value, patch.EndTime);
                Vector3d worldVel = bodyFrame.Velocity + CalculateRotatedVector(body, localVel.Value, patch.EndTime);
                return referenceFrame.VelocityFromWorldSpace(worldPos, worldVel).ToTuple();
            }
            return referenceFrame.VelocityFromWorldSpace(
                bodyFrame.Position + localPos.Value, bodyFrame.Velocity + localVel.Value).ToTuple();
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
            return ImpactPosition(InternalPatch, referenceFrame);
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
            return ImpactVelocity(InternalPatch, referenceFrame);
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
            var patch = InternalPatch;
            if (!patch.IsAtmospheric)
                return null;
            var body = patch.StartingState.ReferenceBody;
            var bodyPos = BodyFrame(body).Position;
            var rotates = RotatesWithBody(body, referenceFrame);
            var bodyFixed = API.Settings.get_BodyFixedMode();
            IList<Tuple4> ret = new List<Tuple4>();
            foreach (API.Trajectory.Point point in patch.AtmosphericTrajectory)
            {
                double time = point.time;
                Vector3d worldPos;
                // When BodyFixedMode is enabled positions are stored with rotation
                if (rotates)
                    worldPos = bodyPos + (bodyFixed ? point.pos : CalculateRotatedVector(body, point.pos, time));
                else
                    worldPos = bodyPos + (bodyFixed ? CalculateInverseRotatedVector(body, point.pos, time) : point.pos);
                Vector3d refPos = referenceFrame.PositionFromWorldSpace(worldPos);
                ret.Add(new Tuple4(time, refPos.x, refPos.y, refPos.z));
            }
            return ret;
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
                var patch = InternalPatch;
                if (!patch.IsAtmospheric)
                    return new KRPC.SpaceCenter.Services.Orbit(patch.SpaceOrbit);
                else
                    return null;
            }
        }

        /// <summary>
        /// Whether both are at the same position in the trajectory.
        /// </summary>
        public override bool Equals(Patch other)
        {
            return !ReferenceEquals(other, null) && index == other.index;
        }

        /// <summary>
        /// The hash code of the position in the trajectory.
        /// </summary>
        public override int GetHashCode()
        {
            return index;
        }
    }
}
