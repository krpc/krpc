using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The airplane autopilot, which holds a heading, altitude, vertical speed, roll and speed.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class AirplaneAutopilot : KRPCComputerModule
    {
        static Value<bool> headingHoldEnabled;
        static Value<bool> altitudeHoldEnabled;
        static Value<bool> vertSpeedHoldEnabled;
        static Value<bool> rollHoldEnabled;
        static Value<bool> speedHoldEnabled;
        static Value<double> altitudeTarget;
        static Value<double> headingTarget;
        static Value<double> rollTarget;
        static Value<double> speedTarget;
        static Value<double> vertSpeedTarget;
        static Value<double> bankAngle;
        static Value<double> accKp;
        static Value<double> accKi;
        static Value<double> accKd;
        static Value<double> pitKp;
        static Value<double> pitKi;
        static Value<double> pitKd;
        static Value<double> rolKp;
        static Value<double> rolKi;
        static Value<double> rolKd;
        static Value<double> yawKp;
        static Value<double> yawKi;
        static Value<double> yawKd;
        static Value<double> yawLimit;
        static Value<double> rollLimit;
        static Value<double> pitchUpLimit;
        static Value<double> pitchDownLimit;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleAirplaneAutopilot");
            headingHoldEnabled = Reflection.Field<bool> (type, "HeadingHoldEnabled");
            altitudeHoldEnabled = Reflection.Field<bool> (type, "AltitudeHoldEnabled");
            vertSpeedHoldEnabled = Reflection.Field<bool> (type, "VertSpeedHoldEnabled");
            rollHoldEnabled = Reflection.Field<bool> (type, "RollHoldEnabled");
            speedHoldEnabled = Reflection.Field<bool> (type, "SpeedHoldEnabled");
            altitudeTarget = Reflection.Field<double> (type, "AltitudeTarget");
            headingTarget = Reflection.Field<double> (type, "HeadingTarget");
            rollTarget = Reflection.Field<double> (type, "RollTarget");
            speedTarget = Reflection.Field<double> (type, "SpeedTarget");
            vertSpeedTarget = Reflection.Field<double> (type, "VertSpeedTarget");
            bankAngle = Reflection.Field<double> (type, "BankAngle");
            accKp = Reflection.EditableDouble (type, "AccKp");
            accKi = Reflection.EditableDouble (type, "AccKi");
            accKd = Reflection.EditableDouble (type, "AccKd");
            pitKp = Reflection.EditableDouble (type, "PitKp");
            pitKi = Reflection.EditableDouble (type, "PitKi");
            pitKd = Reflection.EditableDouble (type, "PitKd");
            rolKp = Reflection.EditableDouble (type, "RolKp");
            rolKi = Reflection.EditableDouble (type, "RolKi");
            rolKd = Reflection.EditableDouble (type, "RolKd");
            yawKp = Reflection.EditableDouble (type, "YawKp");
            yawKi = Reflection.EditableDouble (type, "YawKi");
            yawKd = Reflection.EditableDouble (type, "YawKd");
            yawLimit = Reflection.EditableDouble (type, "YawLimit");
            rollLimit = Reflection.EditableDouble (type, "RollLimit");
            pitchUpLimit = Reflection.EditableDouble (type, "PitchUpLimit");
            pitchDownLimit = Reflection.EditableDouble (type, "PitchDownLimit");
        }

        internal AirplaneAutopilot () : base ("MechJebModuleAirplaneAutopilot")
        {
        }

        /// <summary>
        /// Whether the autopilot holds the heading.
        /// </summary>
        [KRPCProperty]
        public bool HeadingHoldEnabled {
            get { return headingHoldEnabled.Get (Instance); }
            set { headingHoldEnabled.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the autopilot holds the altitude.
        /// </summary>
        [KRPCProperty]
        public bool AltitudeHoldEnabled {
            get { return altitudeHoldEnabled.Get (Instance); }
            set { altitudeHoldEnabled.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the autopilot holds the vertical speed.
        /// </summary>
        [KRPCProperty]
        public bool VertSpeedHoldEnabled {
            get { return vertSpeedHoldEnabled.Get (Instance); }
            set { vertSpeedHoldEnabled.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the autopilot holds the roll.
        /// </summary>
        [KRPCProperty]
        public bool RollHoldEnabled {
            get { return rollHoldEnabled.Get (Instance); }
            set { rollHoldEnabled.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the autopilot holds the speed.
        /// </summary>
        [KRPCProperty]
        public bool SpeedHoldEnabled {
            get { return speedHoldEnabled.Get (Instance); }
            set { speedHoldEnabled.Set (Instance, value); }
        }

        /// <summary>
        /// The target altitude, in meters.
        /// </summary>
        [KRPCProperty]
        public double AltitudeTarget {
            get { return altitudeTarget.Get (Instance); }
            set { altitudeTarget.Set (Instance, value); }
        }

        /// <summary>
        /// The target heading, in degrees.
        /// </summary>
        [KRPCProperty]
        public double HeadingTarget {
            get { return headingTarget.Get (Instance); }
            set { headingTarget.Set (Instance, value); }
        }

        /// <summary>
        /// The target roll, in degrees.
        /// </summary>
        [KRPCProperty]
        public double RollTarget {
            get { return rollTarget.Get (Instance); }
            set { rollTarget.Set (Instance, value); }
        }

        /// <summary>
        /// The target speed, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double SpeedTarget {
            get { return speedTarget.Get (Instance); }
            set { speedTarget.Set (Instance, value); }
        }

        /// <summary>
        /// The target vertical speed, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double VertSpeedTarget {
            get { return vertSpeedTarget.Get (Instance); }
            set { vertSpeedTarget.Set (Instance, value); }
        }

        /// <summary>
        /// The bank angle of a turn, in degrees.
        /// </summary>
        [KRPCProperty]
        public double BankAngle {
            get { return bankAngle.Get (Instance); }
            set { bankAngle.Set (Instance, value); }
        }

        /// <summary>
        /// The proportional gain of the speed controller.
        /// </summary>
        [KRPCProperty]
        public double AccKp {
            get { return accKp.Get (Instance); }
            set { accKp.Set (Instance, value); }
        }

        /// <summary>
        /// The integral gain of the speed controller.
        /// </summary>
        [KRPCProperty]
        public double AccKi {
            get { return accKi.Get (Instance); }
            set { accKi.Set (Instance, value); }
        }

        /// <summary>
        /// The derivative gain of the speed controller.
        /// </summary>
        [KRPCProperty]
        public double AccKd {
            get { return accKd.Get (Instance); }
            set { accKd.Set (Instance, value); }
        }

        /// <summary>
        /// The proportional gain of the pitch controller.
        /// </summary>
        [KRPCProperty]
        public double PitKp {
            get { return pitKp.Get (Instance); }
            set { pitKp.Set (Instance, value); }
        }

        /// <summary>
        /// The integral gain of the pitch controller.
        /// </summary>
        [KRPCProperty]
        public double PitKi {
            get { return pitKi.Get (Instance); }
            set { pitKi.Set (Instance, value); }
        }

        /// <summary>
        /// The derivative gain of the pitch controller.
        /// </summary>
        [KRPCProperty]
        public double PitKd {
            get { return pitKd.Get (Instance); }
            set { pitKd.Set (Instance, value); }
        }

        /// <summary>
        /// The proportional gain of the roll controller.
        /// </summary>
        [KRPCProperty]
        public double RolKp {
            get { return rolKp.Get (Instance); }
            set { rolKp.Set (Instance, value); }
        }

        /// <summary>
        /// The integral gain of the roll controller.
        /// </summary>
        [KRPCProperty]
        public double RolKi {
            get { return rolKi.Get (Instance); }
            set { rolKi.Set (Instance, value); }
        }

        /// <summary>
        /// The derivative gain of the roll controller.
        /// </summary>
        [KRPCProperty]
        public double RolKd {
            get { return rolKd.Get (Instance); }
            set { rolKd.Set (Instance, value); }
        }

        /// <summary>
        /// The proportional gain of the yaw controller.
        /// </summary>
        [KRPCProperty]
        public double YawKp {
            get { return yawKp.Get (Instance); }
            set { yawKp.Set (Instance, value); }
        }

        /// <summary>
        /// The integral gain of the yaw controller.
        /// </summary>
        [KRPCProperty]
        public double YawKi {
            get { return yawKi.Get (Instance); }
            set { yawKi.Set (Instance, value); }
        }

        /// <summary>
        /// The derivative gain of the yaw controller.
        /// </summary>
        [KRPCProperty]
        public double YawKd {
            get { return yawKd.Get (Instance); }
            set { yawKd.Set (Instance, value); }
        }

        /// <summary>
        /// The largest yaw, in degrees.
        /// </summary>
        [KRPCProperty]
        public double YawLimit {
            get { return yawLimit.Get (Instance); }
            set { yawLimit.Set (Instance, value); }
        }

        /// <summary>
        /// The largest roll, in degrees.
        /// </summary>
        [KRPCProperty]
        public double RollLimit {
            get { return rollLimit.Get (Instance); }
            set { rollLimit.Set (Instance, value); }
        }

        /// <summary>
        /// The largest pitch up, in degrees.
        /// </summary>
        [KRPCProperty]
        public double PitchUpLimit {
            get { return pitchUpLimit.Get (Instance); }
            set { pitchUpLimit.Set (Instance, value); }
        }

        /// <summary>
        /// The largest pitch down, in degrees.
        /// </summary>
        [KRPCProperty]
        public double PitchDownLimit {
            get { return pitchDownLimit.Get (Instance); }
            set { pitchDownLimit.Set (Instance, value); }
        }
    }
}
