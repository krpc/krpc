using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The docking autopilot, which docks with the target docking port.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class DockingAutopilot : KRPCComputerModule
    {
        static Func<object, string> status;
        static Field<object, double> speedLimit;
        static Field<object, bool> forceRoll;
        static Field<object, double> roll;
        static Field<object, bool> overrideSafeDistance;
        static Field<object, double> overridenSafeDistance;
        static Field<object, bool> overrideTargetSize;
        static Field<object, double> overridenTargetSize;
        static Func<object, float> safeDistance;
        static Func<object, float> targetSize;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleDockingAutopilot");
            status = type.Getter<object, string> ("status");
            speedLimit = Bindings.EditableDouble (type, "speedLimit");
            forceRoll = type.Field<object, bool> ("forceRol");
            roll = Bindings.EditableDouble (type, "rol");
            overrideSafeDistance = type.Field<object, bool> ("overrideSafeDistance");
            overridenSafeDistance = Bindings.EditableDouble (type, "overridenSafeDistance");
            overrideTargetSize = type.Field<object, bool> ("overrideTargetSize");
            overridenTargetSize = Bindings.EditableDouble (type, "overridenTargetSize");
            safeDistance = type.Getter<object, float> ("safeDistance");
            targetSize = type.Getter<object, float> ("targetSize");
        }

        internal DockingAutopilot () : base ("MechJebModuleDockingAutopilot")
        {
        }

        /// <summary>
        /// The status of the autopilot.
        /// </summary>
        [KRPCProperty]
        public string Status {
            get { return status (Instance); }
        }

        /// <summary>
        /// The largest speed relative to the target, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double SpeedLimit {
            get { return speedLimit.Get (Instance); }
            set { speedLimit.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the autopilot controls the roll.
        /// </summary>
        [KRPCProperty]
        public bool ForceRoll {
            get { return forceRoll.Get (Instance); }
            set { forceRoll.Set (Instance, value); }
        }

        /// <summary>
        /// The roll, in degrees.
        /// </summary>
        /// <remarks>Requires <see cref="ForceRoll"/>.</remarks>
        [KRPCProperty]
        public double Roll {
            get { return roll.Get (Instance); }
            set { roll.Set (Instance, value); }
        }

        /// <summary>
        /// Whether <see cref="OverridenSafeDistance"/> replaces the computed safe distance.
        /// </summary>
        [KRPCProperty]
        public bool OverrideSafeDistance {
            get { return overrideSafeDistance.Get (Instance); }
            set { overrideSafeDistance.Set (Instance, value); }
        }

        /// <summary>
        /// The safe distance from the target, in meters.
        /// </summary>
        /// <remarks>Requires <see cref="OverrideSafeDistance"/>.</remarks>
        [KRPCProperty]
        public double OverridenSafeDistance {
            get { return overridenSafeDistance.Get (Instance); }
            set { overridenSafeDistance.Set (Instance, value); }
        }

        /// <summary>
        /// Whether <see cref="OverridenTargetSize"/> replaces the computed size of the target.
        /// </summary>
        [KRPCProperty]
        public bool OverrideTargetSize {
            get { return overrideTargetSize.Get (Instance); }
            set { overrideTargetSize.Set (Instance, value); }
        }

        /// <summary>
        /// The size of the target, in meters.
        /// </summary>
        /// <remarks>Requires <see cref="OverrideTargetSize"/>.</remarks>
        [KRPCProperty]
        public double OverridenTargetSize {
            get { return overridenTargetSize.Get (Instance); }
            set { overridenTargetSize.Set (Instance, value); }
        }

        /// <summary>
        /// The safe distance from the target that the autopilot uses, in meters.
        /// </summary>
        [KRPCProperty]
        public float SafeDistance {
            get { return safeDistance (Instance); }
        }

        /// <summary>
        /// The size of the target that the autopilot uses, in meters.
        /// </summary>
        [KRPCProperty]
        public float TargetSize {
            get { return targetSize (Instance); }
        }
    }
}
