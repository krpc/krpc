using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The airplane autopilot, which holds a heading, altitude, vertical speed, roll and speed.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class AirplaneAutopilot : KRPCComputerModule
    {
        static Field<object, bool> headingHoldEnabled;
        static Field<object, bool> altitudeHoldEnabled;
        static Field<object, bool> vertSpeedHoldEnabled;
        static Field<object, bool> rollHoldEnabled;
        static Field<object, bool> speedHoldEnabled;
        static Field<object, double> altitudeTarget;
        static Field<object, double> headingTarget;
        static Field<object, double> rollTarget;
        static Field<object, double> speedTarget;
        static Field<object, double> vertSpeedTarget;
        static Field<object, double> bankAngle;
        static Field<object, double> accKp;
        static Field<object, double> accKi;
        static Field<object, double> accKd;
        static Field<object, double> pitKp;
        static Field<object, double> pitKi;
        static Field<object, double> pitKd;
        static Field<object, double> rolKp;
        static Field<object, double> rolKi;
        static Field<object, double> rolKd;
        static Field<object, double> yawKp;
        static Field<object, double> yawKi;
        static Field<object, double> yawKd;
        static Field<object, double> yawLimit;
        static Field<object, double> rollLimit;
        static Field<object, double> pitchUpLimit;
        static Field<object, double> pitchDownLimit;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleAirplaneAutopilot");
            headingHoldEnabled = type.Field<object, bool> ("HeadingHoldEnabled");
            altitudeHoldEnabled = type.Field<object, bool> ("AltitudeHoldEnabled");
            vertSpeedHoldEnabled = type.Field<object, bool> ("VertSpeedHoldEnabled");
            rollHoldEnabled = type.Field<object, bool> ("RollHoldEnabled");
            speedHoldEnabled = type.Field<object, bool> ("SpeedHoldEnabled");
            altitudeTarget = type.Field<object, double> ("AltitudeTarget");
            headingTarget = type.Field<object, double> ("HeadingTarget");
            rollTarget = type.Field<object, double> ("RollTarget");
            speedTarget = type.Field<object, double> ("SpeedTarget");
            vertSpeedTarget = type.Field<object, double> ("VertSpeedTarget");
            bankAngle = type.Field<object, double> ("BankAngle");
            accKp = Bindings.EditableDouble (type, "AccKp");
            accKi = Bindings.EditableDouble (type, "AccKi");
            accKd = Bindings.EditableDouble (type, "AccKd");
            pitKp = Bindings.EditableDouble (type, "PitKp");
            pitKi = Bindings.EditableDouble (type, "PitKi");
            pitKd = Bindings.EditableDouble (type, "PitKd");
            rolKp = Bindings.EditableDouble (type, "RolKp");
            rolKi = Bindings.EditableDouble (type, "RolKi");
            rolKd = Bindings.EditableDouble (type, "RolKd");
            yawKp = Bindings.EditableDouble (type, "YawKp");
            yawKi = Bindings.EditableDouble (type, "YawKi");
            yawKd = Bindings.EditableDouble (type, "YawKd");
            yawLimit = Bindings.EditableDouble (type, "YawLimit");
            rollLimit = Bindings.EditableDouble (type, "RollLimit");
            pitchUpLimit = Bindings.EditableDouble (type, "PitchUpLimit");
            pitchDownLimit = Bindings.EditableDouble (type, "PitchDownLimit");
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
