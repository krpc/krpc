using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        static Field<object, double> minDeltaV;
        static Field<object, int> lastStage;
        static Field<object, bool> optimizeStageFlag;
        static Field<object, int> optimizeStage;
        static Field<object, bool> unguidedStagesFlag;
        static Func<object, object> unguidedStages;
        static Func<object, List<int>> intListValue;
        static Action<object, string> setIntListText;
        static Field<object, bool> coastStageFlag;
        static Field<object, int> coastStage;
        static Field<object, bool> coastBeforeFlag;
        static Field<object, bool> spinupStageFlag;
        static Field<object, int> spinupStage;
        static Field<object, double> spinupAngularVelocity;
        static Field<object, double> spinupLeadTime;
        static Field<object, double> limitQa;
        static Field<object, double> ullageLeadTime;

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
            minDeltaV = Bindings.EditableDouble (type, "MinDeltaV");
            lastStage = Bindings.EditableInt (type, "LastStage");
            optimizeStageFlag = type.Field<object, bool> ("OptimizeStageFlag");
            optimizeStage = Bindings.EditableInt (type, "OptimizeStageInternal");
            unguidedStagesFlag = type.Field<object, bool> ("UnguidedStagesFlag");
            unguidedStages = type.Getter<object, object> ("UnguidedStagesInternal");
            var intList = Bindings.Type ("MuMech.EditableIntList");
            intListValue = intList.Getter<object, List<int>> ("Val");
            setIntListText = intList.Setter<object, string> ("Text");
            coastStageFlag = type.Field<object, bool> ("CoastStageFlag");
            coastStage = Bindings.EditableInt (type, "CoastStageInternal");
            coastBeforeFlag = type.Field<object, bool> ("CoastBeforeFlag");
            spinupStageFlag = type.Field<object, bool> ("SpinupStageFlag");
            spinupStage = Bindings.EditableInt (type, "SpinupStageInternal");
            spinupAngularVelocity = Bindings.EditableDouble (type, "SpinupAngularVelocity");
            spinupLeadTime = Bindings.EditableDouble (type, "SpinupLeadTime");
            limitQa = Bindings.EditableDouble (type, "LimitQa");
            ullageLeadTime = Bindings.EditableDouble (Bindings.Type ("MuMech.MechJebModuleGuidanceController"), "UllageLeadTime");
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
        /// <remarks>Requires <see cref="CoastStageFlag"/>.</remarks>
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

        /// <summary>
        /// The smallest delta-v of a stage that guidance plans with, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double MinDeltaV {
            get { return minDeltaV.Get (Instance); }
            set { minDeltaV.Set (Instance, value); }
        }

        /// <summary>
        /// The last stage that guidance plans with.
        /// </summary>
        [KRPCProperty]
        public int LastStage {
            get { return lastStage.Get (Instance); }
            set { lastStage.Set (Instance, value); }
        }

        /// <summary>
        /// Whether guidance may shut off <see cref="OptimizeStage"/> early.
        /// </summary>
        [KRPCProperty]
        public bool OptimizeStageFlag {
            get { return optimizeStageFlag.Get (Instance); }
            set { optimizeStageFlag.Set (Instance, value); }
        }

        /// <summary>
        /// The stage that guidance may shut off early.
        /// </summary>
        /// <remarks>Requires <see cref="OptimizeStageFlag"/>. -1 is no stage.</remarks>
        [KRPCProperty]
        public int OptimizeStage {
            get { return optimizeStage.Get (Instance); }
            set { optimizeStage.Set (Instance, value); }
        }

        /// <summary>
        /// Whether guidance leaves <see cref="UnguidedStages"/> unguided.
        /// </summary>
        [KRPCProperty]
        public bool UnguidedStagesFlag {
            get { return unguidedStagesFlag.Get (Instance); }
            set { unguidedStagesFlag.Set (Instance, value); }
        }

        /// <summary>
        /// The stages that guidance leaves unguided.
        /// </summary>
        /// <remarks>Requires <see cref="UnguidedStagesFlag"/>.</remarks>
        [KRPCProperty]
        public IList<int> UnguidedStages {
            get { return new List<int> (intListValue (unguidedStages (Instance))); }
            set {
                if (value.Any (stage => stage < 0))
                    throw new ArgumentException ("A stage number is negative", nameof (value));
                // The window shows the text, and MechJeb parses the stages from it
                setIntListText (unguidedStages (Instance),
                    string.Join (",", value.Select (stage => stage.ToString (CultureInfo.InvariantCulture)).ToArray ()));
            }
        }

        /// <summary>
        /// Whether guidance coasts in <see cref="CoastStage"/>.
        /// </summary>
        [KRPCProperty]
        public bool CoastStageFlag {
            get { return coastStageFlag.Get (Instance); }
            set { coastStageFlag.Set (Instance, value); }
        }

        /// <summary>
        /// The stage before or after which guidance coasts.
        /// </summary>
        /// <remarks>Requires <see cref="CoastStageFlag"/>.</remarks>
        [KRPCProperty]
        public int CoastStage {
            get { return coastStage.Get (Instance); }
            set { coastStage.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the coast comes before <see cref="CoastStage"/>, rather than after it.
        /// </summary>
        /// <remarks>Requires <see cref="CoastStageFlag"/>.</remarks>
        [KRPCProperty]
        public bool CoastBeforeFlag {
            get { return coastBeforeFlag.Get (Instance); }
            set { coastBeforeFlag.Set (Instance, value); }
        }

        /// <summary>
        /// The time before an ignition at which RCS settles the propellant, in seconds.
        /// </summary>
        [KRPCProperty]
        public double UllageLeadTime {
            get { return ullageLeadTime.Get (GuidanceModule); }
            set { ullageLeadTime.Set (GuidanceModule, value); }
        }

        /// <summary>
        /// Whether to spin up the vessel before <see cref="SpinupStage"/>.
        /// </summary>
        [KRPCProperty]
        public bool SpinupStageFlag {
            get { return spinupStageFlag.Get (Instance); }
            set { spinupStageFlag.Set (Instance, value); }
        }

        /// <summary>
        /// The stage before which to spin up the vessel.
        /// </summary>
        /// <remarks>Requires <see cref="SpinupStageFlag"/>.</remarks>
        [KRPCProperty]
        public int SpinupStage {
            get { return spinupStage.Get (Instance); }
            set { spinupStage.Set (Instance, value); }
        }

        /// <summary>
        /// The angular velocity of the spin-up, in <unit>rad/s</unit>.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="SpinupStageFlag"/>. MechJeb's window shows it in revolutions per
        /// minute.
        /// </remarks>
        [KRPCProperty]
        public double SpinupAngularVelocity {
            get { return spinupAngularVelocity.Get (Instance); }
            set { spinupAngularVelocity.Set (Instance, value); }
        }

        /// <summary>
        /// The time before staging at which the spin-up starts, in seconds.
        /// </summary>
        /// <remarks>Requires <see cref="SpinupStageFlag"/>.</remarks>
        [KRPCProperty]
        public double SpinupLeadTime {
            get { return spinupLeadTime.Get (Instance); }
            set { spinupLeadTime.Set (Instance, value); }
        }

        /// <summary>
        /// The limit on dynamic pressure times angle of attack, in <unit>Pa.rad</unit>.
        /// </summary>
        /// <remarks>MechJeb clamps it to between 0 and 10000.</remarks>
        [KRPCProperty]
        public double LimitQa {
            get { return limitQa.Get (Instance); }
            set { limitQa.Set (Instance, value); }
        }

        static object GuidanceModule {
            get { return MechJeb.Module ("MechJebModuleGuidanceController"); }
        }
    }
}
