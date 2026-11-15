using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The PVG Settings window, which holds the settings of the Primer Vector Guidance ascent path.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class PVGSettings : AscentBase
    {
        static Field<object, double> pitchStartVelocity;
        static Field<object, double> pitchRate;
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
        /// <remarks>Requires a coast stage, set in MechJeb's PVG settings window.</remarks>
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
