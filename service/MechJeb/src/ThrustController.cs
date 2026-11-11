using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The thrust controller, which limits the throttle.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class ThrustController : ComputerModule
    {
        static Value<bool> limitDynamicPressure;
        static Value<double> maxDynamicPressure;
        static Value<bool> limitToPreventOverheats;
        static Value<bool> limitToPreventFlameout;
        static Value<double> flameoutSafetyPct;
        static Value<bool> manageIntakes;
        static Value<bool> limitAcceleration;
        static Value<double> maxAcceleration;
        static Value<bool> limitThrottle;
        static Value<double> maxThrottle;
        static Value<bool> limiterMinThrottle;
        static Value<double> minThrottle;
        static Value<bool> differentialThrottle;
        static Value<int> differentialThrottleSuccess;
        static Value<bool> electricThrottle;
        static Value<double> electricThrottleLo;
        static Value<double> electricThrottleHi;

        // The Translatron settings, which MechJeb holds in the thrust controller
        internal static Value<int> TMode { get; private set; }
        internal static Value<float> TransSpdAct { get; private set; }
        internal static Value<bool> TransKillH { get; private set; }

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleThrustController");
            limitDynamicPressure = Reflection.Field<bool> (type, "LimitDynamicPressure");
            maxDynamicPressure = Reflection.EditableDouble (type, "MaxDynamicPressure");
            limitToPreventOverheats = Reflection.Field<bool> (type, "LimitToPreventOverheats");
            limitToPreventFlameout = Reflection.Field<bool> (type, "LimitToPreventFlameout");
            flameoutSafetyPct = Reflection.EditableDouble (type, "FlameoutSafetyPct");
            manageIntakes = Reflection.Field<bool> (type, "ManageIntakes");
            limitAcceleration = Reflection.Field<bool> (type, "LimitAcceleration");
            maxAcceleration = Reflection.EditableDouble (type, "MaxAcceleration");
            limitThrottle = Reflection.Field<bool> (type, "LimitThrottle");
            maxThrottle = Reflection.EditableDouble (type, "MaxThrottle");
            limiterMinThrottle = Reflection.Field<bool> (type, "LimiterMinThrottle");
            minThrottle = Reflection.EditableDouble (type, "MinThrottle");
            differentialThrottle = Reflection.Field<bool> (type, "DifferentialThrottle");
            differentialThrottleSuccess = Reflection.Field<int> (type, "DifferentialThrottleSuccess");
            electricThrottle = Reflection.Field<bool> (type, "ElectricThrottle");
            electricThrottleLo = Reflection.EditableDouble (type, "ElectricThrottleLo");
            electricThrottleHi = Reflection.EditableDouble (type, "ElectricThrottleHi");
            TMode = Reflection.Field<int> (type, "Tmode");
            TransSpdAct = Reflection.Field<float> (type, "TransSpdAct");
            TransKillH = Reflection.Field<bool> (type, "TransKillH");
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
            get { return (DifferentialThrottleStatus)differentialThrottleSuccess.Get (Instance); }
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
