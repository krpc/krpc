using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The Ascent Settings window, which holds the settings shared by the ascent paths.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class AscentSettings : AscentBase
    {
        static Field<object, bool> correctiveSteering;
        static Field<object, double> correctiveSteeringGain;
        static Field<object, bool> forceRoll;
        static Field<object, double> verticalRoll;
        static Field<object, double> turnRoll;
        static Field<object, bool> autodeploySolarPanels;
        static Field<object, bool> autoDeployAntennas;
        static Field<object, bool> skipCircularization;
        static Field<object, bool> autostage;
        static Field<object, bool> limitAoA;
        static Field<object, double> maxAoA;
        static Field<object, double> aoaLimitFadeoutPressure;
        static Field<object, double> rollAltitude;
        static Field<object, bool> limitingAoA;
        static Func<object, double> currentMaxAoA;

        internal static new void Load ()
        {
            var settings = Bindings.Type ("MuMech.MechJebModuleAscentSettings");
            correctiveSteering = settings.Field<object, bool> ("CorrectiveSteering");
            correctiveSteeringGain = Bindings.EditableDouble (settings, "CorrectiveSteeringGain");
            forceRoll = settings.Field<object, bool> ("ForceRoll");
            verticalRoll = Bindings.EditableDouble (settings, "VerticalRoll");
            turnRoll = Bindings.EditableDouble (settings, "TurnRoll");
            autodeploySolarPanels = settings.Field<object, bool> ("AutodeploySolarPanels");
            autoDeployAntennas = settings.Field<object, bool> ("AutoDeployAntennas");
            skipCircularization = settings.Field<object, bool> ("SkipCircularization");
            autostage = settings.Field<object, bool> ("Autostage");
            limitAoA = settings.Field<object, bool> ("LimitAoA");
            maxAoA = Bindings.EditableDouble (settings, "MaxAoA");
            aoaLimitFadeoutPressure = Bindings.EditableDouble (settings, "AOALimitFadeoutPressure");
            rollAltitude = Bindings.EditableDouble (settings, "RollAltitude");
            limitingAoA = settings.Field<object, bool> ("LimitingAoA");
            currentMaxAoA = Bindings.Type ("MuMech.MechJebModuleAscentBaseAutopilot").Getter<object, double> ("CurrentMaxAoA");
        }

        /// <summary>
        /// Whether the autopilot steers by the velocity vector rather than the position vector.
        /// </summary>
        /// <remarks>
        /// It is more accurate, though large craft may do better without it. Applies on the
        /// <see cref="AscentType.Classic"/> path.
        /// </remarks>
        [KRPCProperty]
        public bool CorrectiveSteering {
            get { return correctiveSteering.Get (Instance); }
            set { correctiveSteering.Set (Instance, value); }
        }

        /// <summary>
        /// The gain of the corrective steering.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="CorrectiveSteering"/>, and applies on the <see
        /// cref="AscentType.Classic"/> path.
        /// </remarks>
        [KRPCProperty]
        public double CorrectiveSteeringGain {
            get { return correctiveSteeringGain.Get (Instance); }
            set { correctiveSteeringGain.Set (Instance, value); }
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
        /// The roll during the vertical climb, in degrees.
        /// </summary>
        /// <remarks>Requires <see cref="ForceRoll"/>.</remarks>
        [KRPCProperty]
        public double VerticalRoll {
            get { return verticalRoll.Get (Instance); }
            set { verticalRoll.Set (Instance, value); }
        }

        /// <summary>
        /// The roll during the turn, in degrees.
        /// </summary>
        /// <remarks>Requires <see cref="ForceRoll"/>.</remarks>
        [KRPCProperty]
        public double TurnRoll {
            get { return turnRoll.Get (Instance); }
            set { turnRoll.Set (Instance, value); }
        }

        /// <summary>
        /// The altitude above which the autopilot controls the roll, in meters.
        /// </summary>
        /// <remarks>Requires <see cref="ForceRoll"/>.</remarks>
        [KRPCProperty]
        public double RollAltitude {
            get { return rollAltitude.Get (Instance); }
            set { rollAltitude.Set (Instance, value); }
        }

        /// <summary>
        /// Whether to deploy the solar panels when the ascent finishes.
        /// </summary>
        [KRPCProperty]
        public bool AutodeploySolarPanels {
            get { return autodeploySolarPanels.Get (Instance); }
            set { autodeploySolarPanels.Set (Instance, value); }
        }

        /// <summary>
        /// Whether to deploy the antennas when the ascent finishes.
        /// </summary>
        [KRPCProperty]
        public bool AutoDeployAntennas {
            get { return autoDeployAntennas.Get (Instance); }
            set { autoDeployAntennas.Set (Instance, value); }
        }

        /// <summary>
        /// Whether to skip the circularization burn.
        /// </summary>
        [KRPCProperty]
        public bool SkipCircularization {
            get { return skipCircularization.Get (Instance); }
            set { skipCircularization.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the autopilot stages when the current stage runs out of fuel.
        /// </summary>
        /// <remarks><see cref="Staging"/> holds the staging settings.</remarks>
        [KRPCProperty]
        public bool Autostage {
            get { return autostage.Get (Instance); }
            set { autostage.Set (Instance, value); }
        }

        /// <summary>
        /// The autostaging settings. The same object as <see cref="Utilities.Staging"/>.
        /// </summary>
        [KRPCProperty]
        public Autostaging Staging {
            get { return MechJeb.Utilities.Staging; }
        }

        /// <summary>
        /// The throttle limiters. The same object as <see cref="Utilities.Thrust"/>.
        /// </summary>
        [KRPCProperty]
        public ThrustLimiter Thrust {
            get { return MechJeb.Utilities.Thrust; }
        }

        /// <summary>
        /// Whether to limit the angle of attack.
        /// </summary>
        /// <remarks>Applies on the <see cref="AscentType.Classic"/> path.</remarks>
        [KRPCProperty]
        public bool LimitAngleOfAttack {
            get { return limitAoA.Get (Instance); }
            set { limitAoA.Set (Instance, value); }
        }

        /// <summary>
        /// The largest angle of attack, in degrees.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="LimitAngleOfAttack"/>, and applies on the <see cref="AscentType.Classic"/>
        /// path.
        /// </remarks>
        [KRPCProperty]
        public double MaxAngleOfAttack {
            get { return maxAoA.Get (Instance); }
            set { maxAoA.Set (Instance, value); }
        }

        /// <summary>
        /// The dynamic pressure below which the angle of attack limit fades out, in pascals.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="LimitAngleOfAttack"/>, and applies on the <see cref="AscentType.Classic"/>
        /// path.
        /// </remarks>
        [KRPCProperty]
        public double AngleOfAttackLimitFadeoutPressure {
            get { return aoaLimitFadeoutPressure.Get (Instance); }
            set { aoaLimitFadeoutPressure.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the angle of attack limit is limiting the steering.
        /// </summary>
        [KRPCProperty]
        public bool LimitingAngleOfAttack {
            get { return limitingAoA.Get (Instance); }
        }

        /// <summary>
        /// The largest angle of attack the limit allows now, in degrees.
        /// </summary>
        [KRPCProperty]
        public double CurrentMaxAngleOfAttack {
            get { return currentMaxAoA (MechJeb.AscentGuidance.Instance); }
        }

        /// <summary>
        /// Whether to warp time during the ascent.
        /// </summary>
        /// <remarks>The same setting as <see cref="ManeuverPlanner.Autowarp"/>.</remarks>
        [KRPCProperty]
        public bool Autowarp {
            get { return NodeExecutor.Autowarp; }
            set { NodeExecutor.Autowarp = value; }
        }
    }
}
