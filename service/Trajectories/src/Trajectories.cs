using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using KRPC.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Trajectories
{
    /// <summary>
    /// Provides functionality to interact with Trajectories.
    /// </summary>
    /// <remarks>
    /// Trajectories predicts the trajectory of the active vessel only.
    /// </remarks>
    [KRPCService(Id = ServiceInfo.Id, GameScene = GameScene.Flight)]
    [RequiresMod(typeof(API.TrajectoriesAPI))]
    public static class Trajectories
    {
        private static DescentProfile descentProfile;

        /// <summary>
        /// Whether Trajectories is available.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.All)]
        public static bool Available
        {
            get
            {
                return ModAPI.Get<API.TrajectoriesAPI>().IsAvailable;
            }
        }

        /// <summary>
        /// The patch of the trajectory that ends in an impact.
        /// </summary>
        /// <returns>The patch, or <c>null</c> without an impact.</returns>
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
        /// The patches of the trajectory.
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
        /// The direction that the descent profile sets for the vessel.
        /// </summary>
        /// <returns>The direction, or <c>null</c> without a target.</returns>
        /// <param name="referenceFrame">The reference frame for the direction.</param>
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
        /// The <see cref="PlannedDirection"/>, corrected to steer towards the target.
        /// </summary>
        /// <remarks>
        /// Suitable as the target direction of the auto-pilot.
        /// </remarks>
        /// <returns>The direction, or <c>null</c> without a target.</returns>
        /// <param name="referenceFrame">The reference frame for the direction.</param>
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
        /// Sets the target.
        /// </summary>
        /// <param name="referenceFrame">The reference frame for the position.</param>
        /// <param name="position">The position of the target.</param>
        /// <param name="body">The body of the target, by default the body the active vessel
        /// orbits.</param>
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
        /// The body of the target, or <c>null</c> without a target.
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
        /// The position of the target, or <c>null</c> without a target.
        /// </summary>
        /// <param name="referenceFrame">The reference frame for the position.</param>
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
        /// Clears the target.
        /// </summary>
        [KRPCProcedure]
        public static void ClearTarget()
        {
            API.API.ClearTarget();
            API.TargetProfile.Save();
        }

        /// <summary>
        /// The descent profile.
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
        /// Whether Trajectories updates the trajectory continuously.
        /// </summary>
        /// <remarks>
        /// When <c>false</c>, <see cref="UpdateTrajectory"/> updates the trajectory.
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
        /// Updates the trajectory.
        /// </summary>
        [KRPCProcedure]
        public static void UpdateTrajectory()
        {
            API.API.UpdateTrajectory();
        }
    }
}
