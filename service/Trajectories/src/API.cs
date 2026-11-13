using System;
using System.Collections;
using System.Collections.Generic;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.Trajectories.API
{
    /// <summary>
    /// The Trajectories integration.
    /// </summary>
    internal sealed class TrajectoriesAPI : ModAPI
    {
        public TrajectoriesAPI() : base(ServiceInfo.Mods.Trajectories.Name, ServiceInfo.Mods.Trajectories.Assembly)
        {
        }

        protected override void Bind(ModBinder mod)
        {
            mod.Statics(typeof(API), "Trajectories.API");
            mod.Statics(typeof(Trajectory), "Trajectories.Trajectory");
            Trajectory.VesselState.Load(mod);
            Trajectory.Point.Load(mod);
            Trajectory.Patch.Load(mod);
            mod.Statics(typeof(TargetProfile), "Trajectories.TargetProfile");
            mod.Statics(typeof(DescentProfile), "Trajectories.DescentProfile");
            DescentProfile.Node.Load(mod);
            mod.Statics(typeof(Settings), "Trajectories.Settings");
        }
    }

    /// <summary>
    /// The static members of Trajectories' API class.
    /// </summary>
    internal static class API
    {
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
    /// Bindings for Trajectories' Trajectory
    /// </summary>
    internal static class Trajectory
    {
        /// <summary>
        /// Bindings for Trajectories' VesselState
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

            public static void Load(ModBinder mod)
            {
                var type = mod.Type("Trajectories.Trajectory+VesselState");
                ReferenceBodyProp = type.Getter<object, CelestialBody>("ReferenceBody");
                TimeProp = type.Getter<object, double>("Time");
            }

            public CelestialBody ReferenceBody { get { return ReferenceBodyProp(internalObj); } }
            public double Time { get { return TimeProp(internalObj); } }
        }

        /// <summary>
        /// Bindings for Trajectories' Point
        /// </summary>
        public class Point
        {
            private object internalObj;
            private static Func<object, Vector3d> posField { get; set; }
            private static Func<object, double> timeField { get; set; }

            public Point(object original)
            {
                internalObj = original;
            }

            public static void Load(ModBinder mod)
            {
                var type = mod.Type("Trajectories.Trajectory+Point");
                posField = type.Getter<object, Vector3d>("pos");
                timeField = type.Getter<object, double>("time");
            }

            public Vector3d pos { get { return posField(internalObj); } }
            public double time { get { return timeField(internalObj); } }
        }

        /// <summary>
        /// Bindings for Trajectories' Patch
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

            public static void Load(ModBinder mod)
            {
                var type = mod.Type("Trajectories.Trajectory+Patch");
                StartingStateProp = type.Getter<object, object>("StartingState");
                EndTimeProp = type.Getter<object, double>("EndTime");

                IsAtmosphericProp = type.Getter<object, bool>("IsAtmospheric");
                AtmosphericTrajectoryProp = type.Getter<object, IEnumerable>("AtmosphericTrajectory");

                SpaceOrbitProp = type.Getter<object, Orbit>("SpaceOrbit");
                RawImpactPositionProp = type.Getter<object, Vector3d?>("RawImpactPosition");
                ImpactVelocityProp = type.Getter<object, Vector3d?>("ImpactVelocity");
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
    /// Bindings for Trajectories' TargetProfile
    /// </summary>
    internal static class TargetProfile
    {
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
    /// Bindings for Trajectories' DescentProfile
    /// </summary>
    internal static class DescentProfile
    {
        /// <summary>
        /// Bindings for Trajectories' Node
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

            public static void Load(ModBinder mod)
            {
                var type = mod.Type("Trajectories.DescentProfile+Node");
                horizonProp = type.Field<object, bool>("Horizon");
                angleDegProp = type.Field<object, double>("AngleDeg");
                retrogradeProp = type.Field<object, bool>("Retrograde");
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
    /// Bindings for Trajectories' Settings
    /// </summary>
    internal static class Settings
    {
        public static Func<bool> get_BodyFixedMode { get; internal set; }
    }
}
