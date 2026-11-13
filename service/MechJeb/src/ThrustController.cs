using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The thrust controller, which limits the throttle.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class ThrustController : ComputerModule
    {
        static Field<object, bool> limitDynamicPressure;
        static Field<object, double> maxDynamicPressure;
        static Field<object, bool> limitToPreventOverheats;
        static Field<object, bool> limitToPreventFlameout;
        static Field<object, double> flameoutSafetyPct;
        static Field<object, bool> manageIntakes;
        static Field<object, bool> limitAcceleration;
        static Field<object, double> maxAcceleration;
        static Field<object, bool> limitThrottle;
        static Field<object, double> maxThrottle;
        static Field<object, bool> limiterMinThrottle;
        static Field<object, double> minThrottle;
        static Field<object, bool> differentialThrottle;
        static Func<object, int> differentialThrottleSuccess;
        static Field<object, bool> electricThrottle;
        static Field<object, double> electricThrottleLo;
        static Field<object, double> electricThrottleHi;

        // The Translatron settings, which MechJeb holds in the thrust controller
        internal static Func<object, int> TMode { get; private set; }
        internal static Field<object, float> TransSpdAct { get; private set; }
        internal static Field<object, bool> TransKillH { get; private set; }

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleThrustController");
            limitDynamicPressure = type.Field<object, bool> ("LimitDynamicPressure");
            maxDynamicPressure = Bindings.EditableDouble (type, "MaxDynamicPressure");
            limitToPreventOverheats = type.Field<object, bool> ("LimitToPreventOverheats");
            limitToPreventFlameout = type.Field<object, bool> ("LimitToPreventFlameout");
            flameoutSafetyPct = Bindings.EditableDouble (type, "FlameoutSafetyPct");
            manageIntakes = type.Field<object, bool> ("ManageIntakes");
            limitAcceleration = type.Field<object, bool> ("LimitAcceleration");
            maxAcceleration = Bindings.EditableDouble (type, "MaxAcceleration");
            limitThrottle = type.Field<object, bool> ("LimitThrottle");
            maxThrottle = Bindings.EditableDouble (type, "MaxThrottle");
            limiterMinThrottle = type.Field<object, bool> ("LimiterMinThrottle");
            minThrottle = Bindings.EditableDouble (type, "MinThrottle");
            differentialThrottle = type.Field<object, bool> ("DifferentialThrottle");
            differentialThrottleSuccess = type.Getter<object, int> ("DifferentialThrottleSuccess");
            electricThrottle = type.Field<object, bool> ("ElectricThrottle");
            electricThrottleLo = Bindings.EditableDouble (type, "ElectricThrottleLo");
            electricThrottleHi = Bindings.EditableDouble (type, "ElectricThrottleHi");
            TMode = type.Getter<object, int> ("Tmode");
            TransSpdAct = type.Field<object, float> ("TransSpdAct");
            TransKillH = type.Field<object, bool> ("TransKillH");
        }

        internal ThrustController () : base ("MechJebModuleThrustController")
        {
        }

        /// <summary>
        /// Whether to limit the dynamic pressure.
        /// </summary>
        [KRPCProperty]
        public bool LimitDynamicPressure {
            get { return limitDynamicPressure.Get (Instance); }
            set { limitDynamicPressure.Set (Instance, value); }
        }

        /// <summary>
        /// The largest dynamic pressure, in pascals.
        /// </summary>
        /// <remarks>Requires <see cref="LimitDynamicPressure"/>.</remarks>
        [KRPCProperty]
        public double MaxDynamicPressure {
            get { return maxDynamicPressure.Get (Instance); }
            set { maxDynamicPressure.Set (Instance, value); }
        }

        /// <summary>
        /// Whether to limit the throttle to keep parts from overheating.
        /// </summary>
        [KRPCProperty]
        public bool LimitToPreventOverheats {
            get { return limitToPreventOverheats.Get (Instance); }
            set { limitToPreventOverheats.Set (Instance, value); }
        }

        /// <summary>
        /// Whether to limit the acceleration.
        /// </summary>
        [KRPCProperty]
        public bool LimitAcceleration {
            get { return limitAcceleration.Get (Instance); }
            set { limitAcceleration.Set (Instance, value); }
        }

        /// <summary>
        /// The largest acceleration, in <unit>m/s^2</unit>.
        /// </summary>
        /// <remarks>Requires <see cref="LimitAcceleration"/>.</remarks>
        [KRPCProperty]
        public double MaxAcceleration {
            get { return maxAcceleration.Get (Instance); }
            set { maxAcceleration.Set (Instance, value); }
        }

        /// <summary>
        /// Whether to limit the throttle.
        /// </summary>
        [KRPCProperty]
        public bool LimitThrottle {
            get { return limitThrottle.Get (Instance); }
            set { limitThrottle.Set (Instance, value); }
        }

        /// <summary>
        /// The largest throttle, between 0 and 1.
        /// </summary>
        /// <remarks>Requires <see cref="LimitThrottle"/>.</remarks>
        [KRPCProperty]
        public double MaxThrottle {
            get { return maxThrottle.Get (Instance); }
            set { maxThrottle.Set (Instance, value); }
        }

        /// <summary>
        /// Whether to keep the throttle above <see cref="MinThrottle"/>.
        /// </summary>
        [KRPCProperty]
        public bool LimiterMinThrottle {
            get { return limiterMinThrottle.Get (Instance); }
            set { limiterMinThrottle.Set (Instance, value); }
        }

        /// <summary>
        /// The smallest throttle, between 0 and 1.
        /// </summary>
        /// <remarks>Requires <see cref="LimiterMinThrottle"/>.</remarks>
        [KRPCProperty]
        public double MinThrottle {
            get { return minThrottle.Get (Instance); }
            set { minThrottle.Set (Instance, value); }
        }

        /// <summary>
        /// Whether to limit the throttle to keep jet engines from flaming out.
        /// </summary>
        [KRPCProperty]
        public bool LimitToPreventFlameout {
            get { return limitToPreventFlameout.Get (Instance); }
            set { limitToPreventFlameout.Set (Instance, value); }
        }

        /// <summary>
        /// The safety margin of the jet engines' air intake, as a percentage.
        /// </summary>
        /// <remarks>Requires <see cref="LimitToPreventFlameout"/>.</remarks>
        [KRPCProperty]
        public double FlameoutSafetyPct {
            get { return flameoutSafetyPct.Get (Instance); }
            set { flameoutSafetyPct.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the controller opens and closes the air intakes.
        /// </summary>
        [KRPCProperty]
        public bool ManageIntakes {
            get { return manageIntakes.Get (Instance); }
            set { manageIntakes.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the controller throttles the engines differentially to steer.
        /// </summary>
        [KRPCProperty]
        public bool DifferentialThrottle {
            get { return differentialThrottle.Get (Instance); }
            set { differentialThrottle.Set (Instance, value); }
        }

        /// <summary>
        /// The result of the differential throttle.
        /// </summary>
        [KRPCProperty]
        public DifferentialThrottleStatus DifferentialThrottleStatus {
            get { return (DifferentialThrottleStatus)differentialThrottleSuccess (Instance); }
        }

        /// <summary>
        /// Whether to limit the throttle of electric engines to the available power.
        /// </summary>
        [KRPCProperty]
        public bool ElectricThrottle {
            get { return electricThrottle.Get (Instance); }
            set { electricThrottle.Set (Instance, value); }
        }

        /// <summary>
        /// The charge level at which electric engines stop, between 0 and 1.
        /// </summary>
        /// <remarks>Requires <see cref="ElectricThrottle"/>.</remarks>
        [KRPCProperty]
        public double ElectricThrottleLo {
            get { return electricThrottleLo.Get (Instance); }
            set { electricThrottleLo.Set (Instance, value); }
        }

        /// <summary>
        /// The charge level at which electric engines run at full throttle, between 0 and 1.
        /// </summary>
        /// <remarks>Requires <see cref="ElectricThrottle"/>.</remarks>
        [KRPCProperty]
        public double ElectricThrottleHi {
            get { return electricThrottleHi.Get (Instance); }
            set { electricThrottleHi.Set (Instance, value); }
        }
    }

    /// <summary>
    /// The result of the differential throttle. See
    /// <see cref="ThrustController.DifferentialThrottleStatus"/>.
    /// </summary>
    [KRPCEnum (Service = "MechJeb")]
    public enum DifferentialThrottleStatus
    {
        /// <summary>
        /// The throttles balance the thrust.
        /// </summary>
        Success,

        /// <summary>
        /// All the engines are off.
        /// </summary>
        AllEnginesOff,

        /// <summary>
        /// The vessel needs more engines.
        /// </summary>
        MoreEnginesRequired,

        /// <summary>
        /// The solver found no solution.
        /// </summary>
        SolverFailed
    }
}
