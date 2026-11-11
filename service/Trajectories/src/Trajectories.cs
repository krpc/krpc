using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Trajectories
{
    /// <summary>
    /// This service provides functionality to interact with
    /// <a href="https://forum.kerbalspaceprogram.com/topic/162324-112x-trajectories-v245-2023-08-22-atmospheric-predictions/">Trajectories</a>.
    /// 
    /// Trajectories calculates using active vessel, doing other vessels is not supported.
    /// </summary>
    [KRPCService(Id = 13, GameScene = GameScene.Flight)]
    public static class Trajectories
    {
        private static DescentProfile descentProfile;

        /// <summary>
        /// Check if the Trajectories API is available.
        /// </summary>
        [KRPCProperty]
        public static bool Available
        {
            get
            {
                return API.API.IsAvailable && API.DescentProfile.IsAvailable &&
                    API.Trajectory.IsAvailable && API.TargetProfile.IsAvailable &&
                    API.Settings.IsAvailable;
            }
        }

        /// <summary>
        /// Calculated impact patch of the trajectory.
        /// </summary>
        /// <returns>
        /// <see cref="KRPC.Trajectories.Patch"/> or <c>null</c> if no impact.
        /// </returns>
        [KRPCProcedure(Nullable = true)]
        public static Patch ImpactPatch()
        {
            int index = API.Trajectory.ImpactPatchIndex();
            return index < 0 ? null : new Patch(index);
        }

        /// <summary>
        /// The position of the impact, or <c>null</c> without one.
        /// </summary>
        /// <remarks>
        /// In a reference frame that rotates with the body, the position includes the
        /// rotation of the body until the impact.
        /// </remarks>
        /// <param name="referenceFrame">The reference frame for the position.</param>
        [KRPCProcedure(Nullable = true)]
        public static Tuple3 ImpactPosition(ReferenceFrame referenceFrame)
        {
            var patch = API.Trajectory.PatchAt(API.Trajectory.ImpactPatchIndex());
            return patch == null ? null : Patch.ImpactPosition(patch, referenceFrame);
        }

        /// <summary>
        /// The velocity at the impact, or <c>null</c> without one.
        /// </summary>
        /// <remarks>
        /// In a reference frame that rotates with the body, the velocity includes the
        /// rotation of the body until the impact.
        /// </remarks>
        /// <param name="referenceFrame">The reference frame for the velocity.</param>
        [KRPCProcedure(Nullable = true)]
        public static Tuple3 ImpactVelocity(ReferenceFrame referenceFrame)
        {
            var patch = API.Trajectory.PatchAt(API.Trajectory.ImpactPatchIndex());
            return patch == null ? null : Patch.ImpactVelocity(patch, referenceFrame);
        }

        /// <summary>
        /// Trajectory patches
        /// </summary>
        [KRPCProcedure]
        public static IList<Patch> Patches()
        {
            IList<Patch> patches = new List<Patch>();
            int count = API.Trajectory.PatchCount;
            for (int i = 0; i < count; i++)
                patches.Add(new Patch(i));
            return patches;
        }

        /// <summary>
        /// Direction vessel should be pointing in according to descent profile.
        /// </summary>
        /// <returns>
        /// Planned direction or <c>null</c> if no set target.
        /// </returns>
        /// <param name="referenceFrame">Reference frame to use</param>
        [KRPCProcedure(Nullable = true)]
        public static Tuple3 PlannedDirection(ReferenceFrame referenceFrame)
        {
            Vector3? plannedDirection = API.API.PlannedDirection();
            if (plannedDirection.HasValue)
            {
                Vector3 vecPlannedDirection = referenceFrame.DirectionFromWorldSpace(plannedDirection.Value);

                return vecPlannedDirection.ToTuple();
            }
            else
                return null;
        }

        /// <summary>
        /// <see cref="PlannedDirection"/> with correction to steer towards target. This can be used as input to autopilot target direction.
        /// </summary>
        /// <returns>
        /// Corrected direction or <c>null</c> if no set target.
        /// </returns>
        /// <param name="referenceFrame">Reference frame to use</param>
        [KRPCProcedure(Nullable = true)]
        public static Tuple3 CorrectedDirection(ReferenceFrame referenceFrame)
        {
            Vector3? correctedDirection = API.API.CorrectedDirection();
            if (correctedDirection.HasValue)
            {
                Vector3 vecCorrectedDirection = referenceFrame.DirectionFromWorldSpace(correctedDirection.Value);

                return vecCorrectedDirection.ToTuple();
            }
            else
                return null;
        }

        /// <summary>
        /// Set Trajectories target.
        /// </summary>
        /// <param name="body">A <see cref="KRPC.SpaceCenter.Services.CelestialBody"/>, if <c>null</c> uses what's currently influencing the active vessel.</param>
        /// <param name="referenceFrame"><see cref="ReferenceFrame"/> to use for <paramref name="position"/></param>
        /// <param name="position">Target position</param>
        [KRPCProcedure]
        public static void SetTarget(ReferenceFrame referenceFrame, Tuple3 position, KRPC.SpaceCenter.Services.CelestialBody body = null)
        {
            KRPC.SpaceCenter.Services.CelestialBody targetBody;
            Vector3d worldPos = referenceFrame.PositionToWorldSpace(position.ToVector());

            if (body == null)
                targetBody = new KRPC.SpaceCenter.Services.CelestialBody(FlightGlobals.ActiveVessel.mainBody);
            else
                targetBody = body;

            Vector3d localPos = targetBody.ReferenceFrame.PositionFromWorldSpace(worldPos);
            API.TargetProfile.SetFromLocalPos(targetBody.InternalBody, localPos);
            API.TargetProfile.Save();
        }

        /// <summary>
        /// Trajectories target <see cref="KRPC.SpaceCenter.Services.CelestialBody"/>.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public static KRPC.SpaceCenter.Services.CelestialBody TargetBody
        {
            get
            {
                CelestialBody body = API.TargetProfile.get_Body();

                if (body != null)
                    return new KRPC.SpaceCenter.Services.CelestialBody(body);

                return null;
            }
        }

        /// <summary>
        /// Trajectories target position.
        /// </summary>
        /// <param name="referenceFrame"><see cref="ReferenceFrame"/> to use</param>
        [KRPCProcedure(Nullable = true)]
        public static Tuple3 TargetPosition(ReferenceFrame referenceFrame)
        {
            Vector3d? localPos = API.TargetProfile.get_LocalPosition();

            if (localPos.HasValue)
            {
                Vector3d worldPos = TargetBody.ReferenceFrame.PositionToWorldSpace(localPos.Value);
                return referenceFrame.PositionFromWorldSpace(worldPos).ToTuple();
            }

            return null;
        }

        /// <summary>
        /// Clears the Trajectories target.
        /// </summary>
        [KRPCProcedure]
        public static void ClearTarget()
        {
            API.API.ClearTarget();
            API.TargetProfile.Save();
        }

        /// <summary>
        /// Descent profile
        /// </summary>
        [KRPCProperty]
        public static DescentProfile DescentProfile 
        { 
            get
            {
                if (descentProfile == null)
                    descentProfile = new KRPC.Trajectories.DescentProfile();

                return descentProfile;
            }
        }

        /// <summary>
        /// Automatically update the trajectory.
        /// </summary>
        /// <remarks>
        /// If <c>false</c> then <see cref="UpdateTrajectory"/> must be called to update the predicted trajectory.
        /// </remarks>
        [KRPCProperty]
        public static bool AlwaysUpdate
        {
            get
            {
                return API.API.get_AlwaysUpdate();
            }
            set
            {
                API.API.set_AlwaysUpdate(value);
            }
        }

        /// <summary>
        /// Triggers a recalculation of the trajectory.
        /// </summary>
        [KRPCProcedure]
        public static void UpdateTrajectory()
        {
            API.API.UpdateTrajectory();
        }
    }
}
