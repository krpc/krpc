using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The Primer Vector Guidance (RSS/RO) profile.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class AscentPVG : AscentBase
    {
        static Field<object, double> pitchStartVelocity;
        static Field<object, double> pitchRate;
        static Field<object, double> desiredApoapsis;
        static Field<object, bool> attachAltFlag;
        static Field<object, double> desiredAttachAlt;
        static Field<object, double> desiredAttachAltFixed;
        static Func<object, int> optimizeStage;
        static Field<object, double> dynamicPressureTrigger;
        static Field<object, int> stagingTrigger;
        static Field<object, bool> stagingTriggerFlag;
        static Field<object, bool> fixedCoast;
        static Field<object, double> fixedCoastLength;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleAscentSettings");
            pitchStartVelocity = Bindings.EditableDouble (type, "PitchStartVelocity");
            pitchRate = Bindings.EditableDouble (type, "PitchRate");
            desiredApoapsis = Bindings.EditableDouble (type, "DesiredApoapsis");
            attachAltFlag = type.Field<object, bool> ("AttachAltFlag");
            desiredAttachAlt = Bindings.EditableDouble (type, "DesiredAttachAlt");
            desiredAttachAltFixed = Bindings.EditableDouble (type, "DesiredAttachAltFixed");
            optimizeStage = type.Getter<object, int> ("OptimizeStage");
            dynamicPressureTrigger = Bindings.EditableDouble (type, "DynamicPressureTrigger");
            stagingTrigger = Bindings.EditableInt (type, "StagingTrigger");
            stagingTriggerFlag = type.Field<object, bool> ("StagingTriggerFlag");
            fixedCoast = type.Field<object, bool> ("FixedCoast");
            fixedCoastLength = Bindings.EditableDouble (type, "FixedCoastLength");
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
        Field<object, double> AttachAltitude {
            get { return optimizeStage (Instance) < 0 ? desiredAttachAltFixed : desiredAttachAlt; }
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
