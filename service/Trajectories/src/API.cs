using System;
using System.Collections.Generic;
using UnityEngine;
using KRPC.Utils;

namespace KRPC.Trajectories
{
    /// <summary>
    /// APILoader wrapper for Trajectories API.
    /// </summary>
    static class API
    {
        public static bool IsAvailable { get; private set; }

        public static void Load()
        {
            IsAvailable = (APILoader.Load(typeof(API), "Trajectories", "Trajectories.API", new Version(2, 4)) != null);
        }

        public static Func<string> get_GetVersion { get; internal set; }
        public static Func<int> get_GetVersionMajor { get; internal set; }
        public static Func<int> get_GetVersionMinor { get; internal set; }
        public static Func<int> get_GetVersionPatch { get; internal set; }

        public static Func<bool> get_AlwaysUpdate { get; internal set; }
        public static Action<bool> set_AlwaysUpdate { get; internal set; }

        public static Func<double?> GetEndTime { get; internal set; }

        public static Func<double?> GetTimeTillImpact { get; internal set; }

        public static Func<Vector3?> GetImpactPosition { get; internal set; }

        public static Func<Vector3?> GetImpactVelocity { get; internal set; }

        public static Func<Orbit> GetSpaceOrbit { get; internal set; }

        public static Func<bool> HasTarget { get; internal set; }

        public static Func<Vector3?> PlannedDirection { get; internal set; }

        public static Func<Vector3?> CorrectedDirection { get; internal set; }

        public static Action<double, double, double?> SetTarget { get; internal set; }

        public static Func<Vector3d?> GetTarget { get; internal set; }

        public static Action ClearTarget { get; internal set; }

        public static Func<bool?> get_ProgradeEntry { get; internal set; }
        public static Action<bool?> set_ProgradeEntry { get; internal set; }

        public static Func<bool?> get_RetrogradeEntry { get; internal set; }
        public static Action<bool?> set_RetrogradeEntry { get; internal set; }

        public static Action<double> ResetDescentProfile { get; internal set; }

        public static Func<List<double>> get_DescentProfileAngles { get; internal set; }
        public static Action<List<double>> set_DescentProfileAngles { get; internal set; }

        public static Func<List<bool>> get_DescentProfileModes { get; internal set; }
        public static Action<List<bool>> set_DescentProfileModes { get; internal set; }

        public static Func<List<bool>> get_DescentProfileGrades { get; internal set;}
        public static Action<List<bool>> set_DescentProfileGrades { get; internal set; }

        public static Action UpdateTrajectory { get; internal set; }
    }
}