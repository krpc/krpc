using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The Primer Vector Guidance (RSS/RO) profile.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class AscentPVG : AscentBase
    {
        static Value<double> pitchStartVelocity;
        static Value<double> pitchRate;
        static Value<double> desiredApoapsis;
        static Value<bool> attachAltFlag;
        static Value<double> desiredAttachAlt;
        static Value<double> desiredAttachAltFixed;
        static Value<int> optimizeStage;
        static Value<double> dynamicPressureTrigger;
        static Value<int> stagingTrigger;
        static Value<bool> stagingTriggerFlag;
        static Value<bool> fixedCoast;
        static Value<double> fixedCoastLength;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleAscentSettings");
            pitchStartVelocity = Reflection.EditableDouble (type, "PitchStartVelocity");
            pitchRate = Reflection.EditableDouble (type, "PitchRate");
            desiredApoapsis = Reflection.EditableDouble (type, "DesiredApoapsis");
            attachAltFlag = Reflection.Field<bool> (type, "AttachAltFlag");
            desiredAttachAlt = Reflection.EditableDouble (type, "DesiredAttachAlt");
            desiredAttachAltFixed = Reflection.EditableDouble (type, "DesiredAttachAltFixed");
            optimizeStage = Reflection.Field<int> (type, "OptimizeStage");
            dynamicPressureTrigger = Reflection.EditableDouble (type, "DynamicPressureTrigger");
            stagingTrigger = Reflection.EditableInt (type, "StagingTrigger");
            stagingTriggerFlag = Reflection.Field<bool> (type, "StagingTriggerFlag");
            fixedCoast = Reflection.Field<bool> (type, "FixedCoast");
            fixedCoastLength = Reflection.EditableDouble (type, "FixedCoastLength");
        }

        /// <summary>
        /// The speed at which the booster starts to pitch over, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double PitchStartVelocity {
            get { return pitchStartVelocity.Get (Instance); }
            set { pitchStartVelocity.Set (Instance, value); }
        }

        /// <summary>
        /// The pitch rate of the booster, in <unit>deg/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double PitchRate {
            get { return pitchRate.Get (Instance); }
            set { pitchRate.Set (Instance, value); }
        }

        /// <summary>
        /// The target apoapsis, in meters.
        /// </summary>
        [KRPCProperty]
        public double DesiredApoapsis {
            get { return desiredApoapsis.Get (Instance); }
            set { desiredApoapsis.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the orbit is attached at <see cref="DesiredAttachAlt"/>.
        /// </summary>
        /// <remarks>
        /// MechJeb reads this setting only when its PVG settings window has an early shutoff
        /// stage. Without one, MechJeb always attaches the orbit.
        /// </remarks>
        [KRPCProperty]
        public bool AttachAltFlag {
            get { return attachAltFlag.Get (Instance); }
            set { attachAltFlag.Set (Instance, value); }
        }

        /// <summary>
        /// The altitude at which the orbit is attached, in meters.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="AttachAltFlag"/>, unless MechJeb's PVG settings window has no early
        /// shutoff stage.
        /// </remarks>
        [KRPCProperty]
        public double DesiredAttachAlt {
            get { return AttachAltitude.Get (Instance); }
            set { AttachAltitude.Set (Instance, value); }
        }

        // MechJeb keeps a separate attach altitude for an ascent with no early shutoff stage
        Value<double> AttachAltitude {
            get { return optimizeStage.Get (Instance) < 0 ? desiredAttachAltFixed : desiredAttachAlt; }
        }

        /// <summary>
        /// The dynamic pressure below which guidance starts, in pascals.
        /// </summary>
        [KRPCProperty]
        public double DynamicPressureTrigger {
            get { return dynamicPressureTrigger.Get (Instance); }
            set { dynamicPressureTrigger.Set (Instance, value); }
        }

        /// <summary>
        /// The stage after which guidance starts.
        /// </summary>
        /// <remarks>Requires <see cref="StagingTriggerFlag"/>.</remarks>
        [KRPCProperty]
        public int StagingTrigger {
            get { return stagingTrigger.Get (Instance); }
            set { stagingTrigger.Set (Instance, value); }
        }

        /// <summary>
        /// Whether guidance starts after <see cref="StagingTrigger"/>.
        /// </summary>
        [KRPCProperty]
        public bool StagingTriggerFlag {
            get { return stagingTriggerFlag.Get (Instance); }
            set { stagingTriggerFlag.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the coast has a fixed length.
        /// </summary>
        [KRPCProperty]
        public bool FixedCoast {
            get { return fixedCoast.Get (Instance); }
            set { fixedCoast.Set (Instance, value); }
        }

        /// <summary>
        /// The length of the coast, in seconds.
        /// </summary>
        /// <remarks>Requires <see cref="FixedCoast"/>.</remarks>
        [KRPCProperty]
        public double FixedCoastLength {
            get { return fixedCoastLength.Get (Instance); }
            set { fixedCoastLength.Set (Instance, value); }
        }
    }
}
