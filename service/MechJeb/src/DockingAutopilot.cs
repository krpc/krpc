using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The docking autopilot, which docks with the target docking port.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class DockingAutopilot : KRPCComputerModule
    {
        static Value<string> status;
        static Value<double> speedLimit;
        static Value<bool> forceRoll;
        static Value<double> roll;
        static Value<bool> overrideSafeDistance;
        static Value<double> overridenSafeDistance;
        static Value<bool> overrideTargetSize;
        static Value<double> overridenTargetSize;
        static Value<float> safeDistance;
        static Value<float> targetSize;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleDockingAutopilot");
            status = Reflection.Field<string> (type, "status");
            speedLimit = Reflection.EditableDouble (type, "speedLimit");
            forceRoll = Reflection.Field<bool> (type, "forceRol");
            roll = Reflection.EditableDouble (type, "rol");
            overrideSafeDistance = Reflection.Field<bool> (type, "overrideSafeDistance");
            overridenSafeDistance = Reflection.EditableDouble (type, "overridenSafeDistance");
            overrideTargetSize = Reflection.Field<bool> (type, "overrideTargetSize");
            overridenTargetSize = Reflection.EditableDouble (type, "overridenTargetSize");
            safeDistance = Reflection.Field<float> (type, "safeDistance");
            targetSize = Reflection.Field<float> (type, "targetSize");
        }

        internal DockingAutopilot () : base ("MechJebModuleDockingAutopilot")
        {
        }

        /// <summary>
        /// The status of the autopilot.
        /// </summary>
        [KRPCProperty]
        public string Status {
            get { return status.Get (Instance); }
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
            get { return safeDistance.Get (Instance); }
        }

        /// <summary>
        /// The size of the target that the autopilot uses, in meters.
        /// </summary>
        [KRPCProperty]
        public float TargetSize {
            get { return targetSize.Get (Instance); }
        }
    }
}
