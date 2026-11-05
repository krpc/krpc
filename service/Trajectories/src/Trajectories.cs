using KRPC.Service;
using KRPC.Service.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;

using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Trajectories
{
    /// <summary>
    /// Service for accessing Trajectories.
    /// </summary>
    [KRPCService(GameScene = GameScene.Flight)]
    public static class Trajectories
    {
        /// <summary>
        /// Check if the Trajectories API is available.
        /// </summary>
        [KRPCProperty]
        public static bool Available
        {
            get { return API.IsAvailable; }
        }

        /// <summary>
        /// Version number of Trajectories in a string formatted as Major.Minor.Patch i.e. 2.1.0.
        /// </summary>
        [KRPCProperty]
        public static string Version
        {
            get
            {
                return API.get_GetVersion();
            }
        }

        /// <summary>
        /// Major version number of Trajectories.
        /// </summary>
        [KRPCProperty]
        public static int VersionMajor
        {
            get
            {
                return API.get_GetVersionMajor();
            }
        }

        /// <summary>
        /// Minor version number of Trajectories.
        /// </summary>
        [KRPCProperty]
        public static int VersionMinor
        {
            get
            {
                return API.get_GetVersionMinor();
            }
        }

        /// <summary>
        /// Patch version number of Trajectories.
        /// </summary>
        [KRPCProperty]
        public static int VersionPatch
        {
            get
            {
                return API.get_GetVersionPatch();
            }
        }

        /// <summary>
        /// Modifies AlwaysUpdate in the settings page.
        /// </summary>
        /// <remarks>
        /// If <c>false</c> then <see cref="UpdateTrajectory"/> must be called to update the predicted trajectory.
        /// </remarks>
        [KRPCProperty]
        public static bool AlwaysUpdate
        {
            get
            {
                return API.get_AlwaysUpdate();
            }
            set
            {
                API.set_AlwaysUpdate(value);
            }
        }

        /// <summary>
        /// Time in seconds till end of current trajectory calculation or <c>null</c> if no active vessel or calculated trajectory.
        /// 
        /// See <see cref="TimeTillImpact"/> for remaining time until impact.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public static double? EndTime
        {
            get
            {
                return API.GetEndTime();
            }
        }

        /// <summary>
        /// Remaining time until impact in seconds or <c>null</c> if no active vessel or calculated trajectory.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public static double? TimeTillImpact
        {
            get
            {
                return API.GetTimeTillImpact();
            }
        }

        /// <summary>
        /// Calculated impact position of the trajectory.
        /// </summary>
        /// <returns>
        /// Impact position or <c>null</c> if no active vessel or calculated trajectory.
        /// </returns>
        /// <param name="referenceFrame">Reference frame to use</param>
        [KRPCProcedure(Nullable = true)]
        public static Tuple3 ImpactPosition(KRPC.SpaceCenter.Services.ReferenceFrame referenceFrame)
        {
            Vector3? impactVect = API.GetImpactPosition();
            if (impactVect.HasValue)
            {
                KRPC.SpaceCenter.Services.Vessel vessel = new KRPC.SpaceCenter.Services.Vessel(FlightGlobals.ActiveVessel);

                // GetImpactPosition() has the position offset but not rotation of Celestial Body Reference Frame.
                Vector3 vecImpactPos = referenceFrame.PositionFromWorldSpace(impactVect.Value + vessel.InternalVessel.orbit.referenceBody.position);

                return new Tuple3(vecImpactPos.x, vecImpactPos.y, vecImpactPos.z);
            }
            else
                return null;
        }

        /// <summary>
        /// Calculated impact velocity of the trajectory or <c>null</c> if no active vessel or calculated trajectory.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public static Tuple3 ImpactVelocity
        {
            get
            {
                Vector3? impactVelocity = API.GetImpactVelocity();
                if (impactVelocity.HasValue)
                    return new Tuple3(impactVelocity.Value.x, impactVelocity.Value.y, impactVelocity.Value.z);
                else
                    return null;
            }
        }

        /// <summary>
        /// <see cref="KRPC.SpaceCenter.Services.Orbit"/> of the calculated trajectory or <c>null</c> if orbit is atmospheric or there's no active vessel or calculated trajectory.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public static KRPC.SpaceCenter.Services.Orbit SpaceOrbit
        {
            get
            {
                Orbit orbit = API.GetSpaceOrbit();

                if (orbit != null)
                    return new KRPC.SpaceCenter.Services.Orbit(orbit);
                else
                    return null;
            }
        }

        /// <summary>
        /// <c>true</c> if a target has been set, <c>false</c> if not, or <c>null</c> if no active vessel.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public static bool? HasTarget
        {
            get
            {
                return API.HasTarget();
            }
        }

        /// <summary>
        /// Direction vessel should be pointing in according to descent profile.
        /// </summary>
        /// <returns>
        /// Planned direction or <c>null</c> if no active vessel or no set target.
        /// </returns>
        /// <param name="referenceFrame">Reference frame to use</param>
        [KRPCProcedure(Nullable = true)]
        public static Tuple3 PlannedDirection(KRPC.SpaceCenter.Services.ReferenceFrame referenceFrame)
        {
            Vector3? plannedDirection = API.PlannedDirection();
            if (plannedDirection.HasValue)
            {
                KRPC.SpaceCenter.Services.Vessel vessel = new KRPC.SpaceCenter.Services.Vessel(FlightGlobals.ActiveVessel);
                Vector3 vecPlannedDirection = referenceFrame.DirectionFromWorldSpace(plannedDirection.Value);

                return new Tuple3(vecPlannedDirection.x, vecPlannedDirection.y, vecPlannedDirection.z);
            }
            else
                return null;
        }

        /// <summary>
        /// <see cref="PlannedDirection"/> with correction to steer towards target.
        /// </summary>
        /// <returns>
        /// Corrected direction or <c>null</c> if no active vessel or no set target.
        /// </returns>
        /// <param name="referenceFrame">Reference frame to use</param>
        [KRPCProcedure(Nullable = true)]
        public static Tuple3 CorrectedDirection(KRPC.SpaceCenter.Services.ReferenceFrame referenceFrame)
        {
            Vector3? correctedDirection = API.CorrectedDirection();
            if (correctedDirection.HasValue)
            {
                KRPC.SpaceCenter.Services.Vessel vessel = new KRPC.SpaceCenter.Services.Vessel(FlightGlobals.ActiveVessel);
                Vector3 vecCorrectedDirection = referenceFrame.DirectionFromWorldSpace(correctedDirection.Value);

                return new Tuple3(vecCorrectedDirection.x, vecCorrectedDirection.y, vecCorrectedDirection.z);
            }
            else
                return null;
        }

        /// <summary>
        /// Set the Trajectories target to a latitude, longitude and altitude at the active vessels celestial body.
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <param name="alt">Altitude</param>
        [KRPCProcedure]
        public static void SetTarget(double lat, double lon, double alt)
        {
            API.SetTarget(lat, lon, alt);
        }

        /// <summary>
        /// Trajectories target as latitude, longitude and altitude at the active vessels celestial body or <c>null</c> if no active vessel or no set target.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public static Tuple3 Target
        {
            get
            {
                Vector3d? target = API.GetTarget();
                if (target.HasValue)
                    return new Tuple3(target.Value.x, target.Value.y, target.Value.z);
                else
                    return null;
            }
        }

        /// <summary>
        /// Clears the Trajectories target.
        /// </summary>
        [KRPCProcedure]
        public static void ClearTarget()
        {
            API.ClearTarget();
        }

        /// <summary> 
        /// Resets all Trajectories descent profile nodes to prograde at 0° if <c>true</c> or retrograde at 0° if <c>false</c>.
        /// 
        /// <c>true</c> if all nodes are prograde, <c>null</c> if no active vessel.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public static bool? ProgradeEntry
        {
            get
            {
                return API.get_ProgradeEntry();
            }
            set
            {
                API.set_ProgradeEntry(value);
            }
        }

        /// <summary> 
        /// Resets all Trajectories descent profile nodes to retrograde at 0° if <c>true</c> or prograde at 0° if <c>false</c>.
        /// 
        /// <c>true</c> if all nodes are retrograde, <c>null</c> if no active vessel.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public static bool? RetrogradeEntry
        {
            get
            {
                return API.get_RetrogradeEntry();
            }
            set
            {
                API.set_RetrogradeEntry(value);
            }
        }

        /// <summary>
        /// Resets Trajectories descent profile to the passed angle of attack and 
        /// sets retrograde if angle is greater than <math>\frac{\pi}{2}</math> (90°) otherwise sets to prograde.
        /// </summary>
        /// <param name="aoa">Angle of attack in radians, default value is retrograde (<math>\pi</math> = 180°)</param>
        [KRPCProcedure]
        public static void ResetDescentProfile(double aoa = Math.PI)
        {
            API.ResetDescentProfile(aoa);
        }

        /// <summary>
        /// Trajectories descent profile angle in radians.
        /// 
        /// List order (0 = entry node, 1 = high altitude node, 2 = low altitude node, 3 = final approach node)
        /// 
        /// <c>null</c> if no active vessel.
        /// </summary>
        /// <remarks>
        /// Also use with the <see cref="DescentProfileOrientations"/> property if using angle of attack values as displayed in the GUI with max <math>\frac{\pi}{2}</math> (90°).
        /// </remarks>
        [KRPCProperty(Nullable = true)]
        public static List<double> DescentProfileAngles
        {
            get
            {
                return API.get_DescentProfileAngles();
            }
            set
            {
                API.set_DescentProfileAngles(value);
            }
        }

        /// <summary>
        /// Trajectories descent profile modes, <c>true</c> = angle of attack, <c>false</c> = angle relative to horizon.
        /// 
        /// List order (0 = entry node, 1 = high altitude node, 2 = low altitude node, 3 = final approach node)
        /// 
        /// <c>null</c> if no active vessel.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public static List<bool> DescentProfileModes
        {
            get
            {
                return API.get_DescentProfileModes();
            }
            set
            {
                API.set_DescentProfileModes(value);
            }
        }

        /// <summary>
        /// Trajectories descent profile orientations, <c>true</c> = retrograde, <c>false</c> = prograde.
        /// 
        /// List order (0 = entry node, 1 = high altitude node, 2 = low altitude node, 3 = final approach node)
        /// 
        /// <c>null</c> if no active vessel.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public static List<bool> DescentProfileOrientations
        {
            get
            {
                return API.get_DescentProfileGrades();
            }
            set
            {
                API.set_DescentProfileGrades(value);
            }
        }

        /// <summary>
        /// Triggers a recalculation of the trajectory.
        /// </summary>
        [KRPCProcedure]
        public static void UpdateTrajectory()
        {
            API.UpdateTrajectory();
        }
    }
}
