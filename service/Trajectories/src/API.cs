using KRPC.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Logger = KRPC.Utils.Logger;

namespace KRPC.Trajectories.API
{
    /// <summary>
    /// APILoader wrapper for Trajectories API.
    /// </summary>
    internal static class API
    {
        public static bool IsAvailable { get; private set; }

        public static void Load()
        {
            IsAvailable = (APILoader.Load(typeof(API), "Trajectories", "Trajectories.API", new Version(2, 4)) != null);
        }

        public static Func<bool> get_AlwaysUpdate { get; internal set; }
        public static Action<bool> set_AlwaysUpdate { get; internal set; }

        public static Func<bool> HasTarget { get; internal set; }

        public static Func<Vector3?> PlannedDirection { get; internal set; }

        public static Func<Vector3?> CorrectedDirection { get; internal set; }

        public static Action<double, double, double?> SetTarget { get; internal set; }

        public static Func<Vector3d?> GetTarget { get; internal set; }

        public static Action ClearTarget { get; internal set; }

        public static Action UpdateTrajectory { get; internal set; }

        public static Action<double> ResetDescentProfile { get; internal set; }
    }

    /// <summary>
    /// APILoader wrapper for Trajectories Trajectory
    /// </summary>
    internal static class Trajectory
    {
        public static bool IsAvailable { get; private set; }

        public static void Load()
        {
            IsAvailable = (APILoader.Load(typeof(Trajectory), "Trajectories", "Trajectories.Trajectory", new Version(2, 4)) != null);
            IsAvailable &= VesselState.Load();
            IsAvailable &= Point.Load();
            IsAvailable &= Patch.Load();
        }

        /// <summary>
        /// APILoader wrapper for Trajectories VesselState
        /// </summary>
        public class VesselState
        {
            private object internalObj;
            private static Func<object, CelestialBody> ReferenceBodyProp { get; set; }
            private static Func<object, double> TimeProp { get; set; }

            public VesselState(object original)
            {
                internalObj = original;
            }

            public static bool Load()
            {
                System.Type internalType = null;
                AssemblyLoader.loadedAssemblies.TypeOperation(t =>
                {
                    if (t.FullName == "Trajectories.Trajectory+VesselState") { internalType = t; }
                });

                if (internalType == null)
                {
                    Logger.WriteLine("[Trajectories API] Failed to grab VesselState Type", Logger.Severity.Error);
                    return false;
                }

                ReferenceBodyProp = Accessor.Getter<object, CelestialBody>(internalType, "ReferenceBody");
                TimeProp = Accessor.Getter<object, double>(internalType, "Time");

                return true;
            }

            public CelestialBody ReferenceBody { get { return ReferenceBodyProp(internalObj); } }
            public double Time { get { return TimeProp(internalObj); } }
        }

        /// <summary>
        /// APILoader wrapper for Trajectories Point
        /// </summary>
        public class Point
        {
            private object internalObj;
            private static Field<object, Vector3d> posField { get; set; }
            private static Field<object, double> timeField { get; set; }

            public Point(object original)
            {
                internalObj = original;
            }

            public static bool Load()
            {
                System.Type internalType = null;
                AssemblyLoader.loadedAssemblies.TypeOperation(t =>
                {
                    if (t.FullName == "Trajectories.Trajectory+Point") { internalType = t; }
                });

                if (internalType == null)
                {
                    Logger.WriteLine("[Trajectories API] Failed to grab Point Type", Logger.Severity.Error);
                    return false;
                }

                posField = Accessor.Field<object, Vector3d>(internalType, "pos");
                timeField = Accessor.Field<object, double>(internalType, "time");

                return true;
            }

            public Vector3d pos { get { return posField.Get(internalObj); } }
            public double time { get { return timeField.Get(internalObj); } }
        }

        /// <summary>
        /// APILoader wrapper for Trajectories Patch
        /// </summary>
        public class Patch
        {
            private object internalObj;

            private static Func<object, object> StartingStateProp { get; set; }
            private static Func<object, double> EndTimeProp { get; set; }
            private static Func<object, bool> IsAtmosphericProp { get; set; }
            private static Func<object, IEnumerable> AtmosphericTrajectoryProp { get; set; }
            private static Func<object, Orbit> SpaceOrbitProp { get; set; }
            private static Func<object, Vector3d?> RawImpactPositionProp { get; set; }
            private static Func<object, Vector3d?> ImpactVelocityProp { get; set; }

            public Patch(object original = null)
            {
                internalObj = original;
            }

            public static bool Load()
            {
                System.Type internalType = null;
                AssemblyLoader.loadedAssemblies.TypeOperation(t =>
                {
                    if (t.FullName == "Trajectories.Trajectory+Patch") { internalType = t; }
                });

                if (internalType == null)
                {
                    Logger.WriteLine("[Trajectories API] Failed to grab Patch Type", Logger.Severity.Error);
                    return false;
                }

                StartingStateProp = Accessor.Getter<object, object>(internalType, "StartingState");
                EndTimeProp = Accessor.Getter<object, double>(internalType, "EndTime");

                IsAtmosphericProp = Accessor.Getter<object, bool>(internalType, "IsAtmospheric");
                AtmosphericTrajectoryProp = Accessor.Getter<object, IEnumerable>(internalType, "AtmosphericTrajectory");

                SpaceOrbitProp = Accessor.Getter<object, Orbit>(internalType, "SpaceOrbit");
                RawImpactPositionProp = Accessor.Getter<object, Vector3d?>(internalType, "RawImpactPosition");
                ImpactVelocityProp = Accessor.Getter<object, Vector3d?>(internalType, "ImpactVelocity");

                return true;
            }

            public VesselState StartingState
            {
                get
                {
                    var orig = StartingStateProp(internalObj);
                    if (orig == null)
                        return null;

                    return new VesselState(orig);
                }
            }
            public double EndTime
            {
                get
                {
                    return EndTimeProp(internalObj);
                }
            }

            public bool IsAtmospheric
            {
                get
                {
                    return IsAtmosphericProp(internalObj);
                }
            }

            public List<Point> AtmosphericTrajectory
            {
                get
                {
                    var origList = AtmosphericTrajectoryProp(internalObj);
                    if (origList == null)
                        return null;

                    var list = new List<Point>();
                    foreach (var orig in origList)
                    {
                        list.Add(new Point(orig));
                    }
                    return list;
                }
            }

            public Orbit SpaceOrbit
            {
                get
                {
                    return SpaceOrbitProp(internalObj);
                }
            }

            public Vector3d? RawImpactPosition
            {
                get
                {
                    return RawImpactPositionProp(internalObj);
                }
            }

            public Vector3d? ImpactVelocity
            {
                get
                {
                    return ImpactVelocityProp(internalObj);
                }
            }
        }

        public static Func<IEnumerable> get_Patches { get; internal set; }

        /// <summary>
        /// The number of patches in the current trajectory.
        /// </summary>
        public static int PatchCount
        {
            get
            {
                var patches = get_Patches() as IList;
                return patches == null ? 0 : patches.Count;
            }
        }

        /// <summary>
        /// The patch at the index in the current trajectory, or null without one.
        /// </summary>
        public static Patch PatchAt(int index)
        {
            var patches = get_Patches() as IList;
            if (patches == null || index < 0 || index >= patches.Count)
                return null;
            return new Patch(patches[index]);
        }

        /// <summary>
        /// The index of the first patch that ends in an impact, or -1 without one.
        /// </summary>
        public static int ImpactPatchIndex()
        {
            var patches = get_Patches() as IList;
            if (patches == null)
                return -1;
            for (int i = 0; i < patches.Count; i++)
            {
                if (new Patch(patches[i]).RawImpactPosition.HasValue)
                    return i;
            }
            return -1;
        }
    }

    /// <summary>
    /// APILoader wrapper for Trajectories TargetProfile
    /// </summary>
    internal static class TargetProfile
    {
        public static bool IsAvailable { get; private set; }

        public static void Load()
        {
            IsAvailable = (APILoader.Load(typeof(TargetProfile), "Trajectories", "Trajectories.TargetProfile", new Version(2, 4)) != null);
        }

        public static Func<Vector3d?> get_LocalPosition { get; internal set; }
        //public static Func<Vector3d?> get_WorldPosition { get; internal set; }

        public static Func<CelestialBody> get_Body { get; internal set; }
        public static Action<CelestialBody> set_Body { get; internal set; }

        public static Action<CelestialBody, Vector3d> SetFromLocalPos { get; internal set; }
        //public static Action<CelestialBody, Vector3d> SetFromWorldPos { get; internal set; }

        public static Action Clear { get; internal set; }
        public static Action Save { get; internal set; }
    }

    /// <summary>
    /// APILoader wrapper for Trajectories DescentProfile
    /// </summary>
    internal static class DescentProfile
    {
        public static bool IsAvailable { get; private set; }

        public static void Load()
        {
            IsAvailable = (APILoader.Load(typeof(DescentProfile), "Trajectories", "Trajectories.DescentProfile", new Version(2, 4)) != null);
            IsAvailable &= Node.Load();
        }

        /// <summary>
        /// APILoader wrapper for Trajectories Node
        /// </summary>
        public class Node
        {
            private object internalObj;
            private static Field<object, bool> horizonProp { get; set; }
            private static Field<object, double> angleDegProp { get; set; }
            private static Field<object, bool> retrogradeProp { get; set; }

            public Node(object original = null)
            {
                internalObj = original;
            }

            public static bool Load()
            {
                System.Type internalType = null;
                AssemblyLoader.loadedAssemblies.TypeOperation(t =>
                {
                    if (t.FullName == "Trajectories.DescentProfile+Node") { internalType = t; }
                });

                if (internalType == null)
                {
                    Logger.WriteLine("[Trajectories API] Failed to grab Node Type", Logger.Severity.Error);
                    return false;
                }

                horizonProp = Accessor.Field<object, bool>(internalType, "Horizon");
                angleDegProp = Accessor.Field<object, double>(internalType, "AngleDeg");
                retrogradeProp = Accessor.Field<object, bool>(internalType, "Retrograde");

                return true;
            }

            public bool Horizon
            {
                get
                {
                    return horizonProp.Get(internalObj);
                }

                set
                {
                    horizonProp.Set(internalObj, value);
                }
            }

            public double AngleDeg
            {
                get
                {
                    return angleDegProp.Get(internalObj);
                }

                set
                {
                    angleDegProp.Set(internalObj, value);
                }
            }

            public bool Retrograde
            {
                get
                {
                    return retrogradeProp.Get(internalObj);
                }

                set
                {
                    retrogradeProp.Set(internalObj, value);
                }
            }
        }

        public static Func<object> get_AtmosEntry { get; internal set; }
        public static Func<object> get_HighAltitude { get; internal set; }
        public static Func<object> get_LowAltitude { get; internal set; }
        public static Func<object> get_FinalApproach { get; internal set; }

        public static Node AtmosphericEntry
        {
            get
            {
                var orig = get_AtmosEntry();
                if (orig == null)
                    return null;

                return new Node(orig);
            }
        }

        public static Node HighAlt
        {
            get
            {
                var orig = get_HighAltitude();
                if (orig == null)
                    return null;

                return new Node(orig);
            }
        }

        public static Node LowAlt
        {
            get
            {
                var orig = get_LowAltitude();
                if (orig == null)
                    return null;

                return new Node(orig);
            }
        }

        public static Node Approach
        {
            get
            {
                var orig = get_FinalApproach();
                if (orig == null)
                    return null;

                return new Node(orig);
            }
        }

        public static Action Save { get; internal set; }
    }

    /// <summary>
    /// APILoader wrapper for Trajectories Settings
    /// </summary>
    internal static class Settings
    {
        public static bool IsAvailable { get; private set; }

        public static void Load()
        {
            IsAvailable = (APILoader.Load(typeof(Settings), "Trajectories", "Trajectories.Settings", new Version(2, 4)) != null);
        }

        public static Func<bool> get_BodyFixedMode { get; internal set; }
    }
}
