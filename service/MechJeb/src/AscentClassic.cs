using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// A wrapper of the settings of an ascent path, which MechJeb holds in its ascent settings.
    /// </summary>
    public abstract class AscentBase : ComputerModule
    {
        internal AscentBase () : base ("MechJebModuleAscentSettings")
        {
        }
    }

    /// <summary>
    /// The classic ascent profile.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class AscentClassic : AscentBase
    {
        static Value<double> turnStartAltitude;
        static Value<double> turnStartVelocity;
        static Value<double> turnEndAltitude;
        static Value<double> turnEndAngle;
        static Value<double> turnShapeExponent;
        static Value<bool> autoPath;
        static Value<float> autoTurnPerc;
        static Value<float> autoTurnSpdFactor;
        static Value<double> autoTurnStartAltitude;
        static Value<double> autoTurnStartVelocity;
        static Value<double> autoTurnEndAltitude;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleAscentSettings");
            turnStartAltitude = Reflection.EditableDouble (type, "TurnStartAltitude");
            turnStartVelocity = Reflection.EditableDouble (type, "TurnStartVelocity");
            turnEndAltitude = Reflection.EditableDouble (type, "TurnEndAltitude");
            turnEndAngle = Reflection.EditableDouble (type, "TurnEndAngle");
            turnShapeExponent = Reflection.EditableDouble (type, "TurnShapeExponent");
            autoPath = Reflection.Field<bool> (type, "AutoPath");
            autoTurnPerc = Reflection.Field<float> (type, "AutoTurnPerc");
            autoTurnSpdFactor = Reflection.Field<float> (type, "AutoTurnSpdFactor");
            autoTurnStartAltitude = Reflection.Field<double> (type, "AutoTurnStartAltitude");
            autoTurnStartVelocity = Reflection.Field<double> (type, "AutoTurnStartVelocity");
            autoTurnEndAltitude = Reflection.Field<double> (type, "AutoTurnEndAltitude");
        }

        /// <summary>
        /// The altitude at which the turn starts, in meters.
        /// </summary>
        [KRPCProperty]
        public double TurnStartAltitude {
            get { return turnStartAltitude.Get (Instance); }
            set { turnStartAltitude.Set (Instance, value); }
        }

        /// <summary>
        /// The speed at which the turn starts, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double TurnStartVelocity {
            get { return turnStartVelocity.Get (Instance); }
            set { turnStartVelocity.Set (Instance, value); }
        }

        /// <summary>
        /// The altitude at which the turn ends, in meters.
        /// </summary>
        [KRPCProperty]
        public double TurnEndAltitude {
            get { return turnEndAltitude.Get (Instance); }
            set { turnEndAltitude.Set (Instance, value); }
        }

        /// <summary>
        /// The final flight path angle, in degrees.
        /// </summary>
        [KRPCProperty]
        public double TurnEndAngle {
            get { return turnEndAngle.Get (Instance); }
            set { turnEndAngle.Set (Instance, value); }
        }

        /// <summary>
        /// The steepness of the turn, between 0 and 1.
        /// </summary>
        [KRPCProperty]
        public double TurnShapeExponent {
            get { return turnShapeExponent.Get (Instance); }
            set { turnShapeExponent.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the autopilot chooses the turn altitudes and speed.
        /// </summary>
        [KRPCProperty]
        public bool AutoPath {
            get { return autoPath.Get (Instance); }
            set { autoPath.Set (Instance, value); }
        }

        /// <summary>
        /// The altitude at which an automatic turn starts, as a fraction of the height of the
        /// atmosphere.
        /// </summary>
        /// <remarks>Requires <see cref="AutoPath"/>.</remarks>
        [KRPCProperty]
        public float AutoTurnPercent {
            get { return autoTurnPerc.Get (Instance); }
            set { autoTurnPerc.Set (Instance, value); }
        }

        /// <summary>
        /// The factor that sets the speed at which an automatic turn starts.
        /// </summary>
        /// <remarks>Requires <see cref="AutoPath"/>.</remarks>
        [KRPCProperty]
        public float AutoTurnSpeedFactor {
            get { return autoTurnSpdFactor.Get (Instance); }
            set { autoTurnSpdFactor.Set (Instance, value); }
        }

        /// <summary>
        /// The altitude at which an automatic turn starts, in meters.
        /// </summary>
        [KRPCProperty]
        public double AutoTurnStartAltitude {
            get { return autoTurnStartAltitude.Get (Instance); }
        }

        /// <summary>
        /// The speed at which an automatic turn starts, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double AutoTurnStartVelocity {
            get { return autoTurnStartVelocity.Get (Instance); }
        }

        /// <summary>
        /// The altitude at which an automatic turn ends, in meters.
        /// </summary>
        [KRPCProperty]
        public double AutoTurnEndAltitude {
            get { return autoTurnEndAltitude.Get (Instance); }
        }
    }
}
