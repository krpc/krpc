using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	/// <summary>
	/// The Primer Vector Guidance (RSS/RO) profile.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class AscentPVG : AscentBase {
		internal new const string MechJebType = "MuMech.MechJebModuleAscentPVG";

		// Fields and methods
		private static FieldInfo pitchStartVelocityField;
		private static FieldInfo pitchRateField;
		private static FieldInfo desiredApoapsisField;
		private static FieldInfo attachAltFlag;
		private static FieldInfo desiredAttachAltField;
		private static FieldInfo dynamicPressureTriggerField;
		private static FieldInfo stagingTriggerField;
		private static FieldInfo stagingTriggerFlag;
		private static FieldInfo fixedCoast;
		private static FieldInfo fixedCoastLengthField;

		// Instance objects
		private object pitchStartVelocity;
		private object pitchRate;
		private object desiredApoapsis;
		private object desiredAttachAlt;
		private object dynamicPressureTrigger;
		private object stagingTrigger;
		private object fixedCoastLength;

		internal static new void InitType(Type type) {
			pitchStartVelocityField = type.GetCheckedField("PitchStartVelocity");
			pitchRateField = type.GetCheckedField("PitchRate");
			desiredApoapsisField = type.GetCheckedField("DesiredApoapsis");
			attachAltFlag = type.GetCheckedField("AttachAltFlag");
			desiredAttachAltField = type.GetCheckedField("DesiredAttachAlt");
			dynamicPressureTriggerField = type.GetCheckedField("DynamicPressureTrigger");
			stagingTriggerField = type.GetCheckedField("StagingTrigger");
			stagingTriggerFlag = type.GetCheckedField("StagingTriggerFlag");
			fixedCoast = type.GetCheckedField("FixedCoast");
			fixedCoastLengthField = type.GetCheckedField("FixedCoastLength");
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.pitchStartVelocity = pitchStartVelocityField.GetInstanceValue(instance);
			this.pitchRate = pitchRateField.GetInstanceValue(instance);
			this.desiredApoapsis = desiredApoapsisField.GetInstanceValue(instance);
			this.desiredAttachAlt = desiredAttachAltField.GetInstanceValue(instance);
			this.dynamicPressureTrigger = dynamicPressureTriggerField.GetInstanceValue(instance);
			this.stagingTrigger = stagingTriggerField.GetInstanceValue(instance);
			this.fixedCoastLength = fixedCoastLengthField.GetInstanceValue(instance);
		}

		[KRPCProperty]
		public double PitchStartVelocity {
			get => EditableDouble.Get(this.pitchStartVelocity);
			set => EditableDouble.Set(this.pitchStartVelocity, value);
		}

		[KRPCProperty]
		public double PitchRate {
			get => EditableDouble.Get(this.pitchRate);
			set => EditableDouble.Set(this.pitchRate, value);
		}

		/// <summary>
		/// The target apoapsis in meters.
		/// </summary>
		[KRPCProperty]
		public double DesiredApoapsis {
			get => EditableDouble.Get(this.desiredApoapsis);
			set => EditableDouble.Set(this.desiredApoapsis, value);
		}

		[KRPCProperty]
		public bool AttachAltFlag {
			get => (bool)attachAltFlag.GetValue(this.instance);
			set => attachAltFlag.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double DesiredAttachAlt {
			get => EditableDouble.Get(this.desiredAttachAlt);
			set => EditableDouble.Set(this.desiredAttachAlt, value);
		}

		[KRPCProperty]
		public double DynamicPressureTrigger {
			get => EditableDouble.Get(this.dynamicPressureTrigger);
			set => EditableDouble.Set(this.dynamicPressureTrigger, value);
		}

		[KRPCProperty]
		public int StagingTrigger {
			get => EditableInt.Get(this.stagingTrigger);
			set => EditableInt.Set(this.stagingTrigger, value);
		}

		[KRPCProperty]
		public bool StagingTriggerFlag {
			get => (bool)stagingTriggerFlag.GetValue(this.instance);
			set => stagingTriggerFlag.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public bool FixedCoast {
			get => (bool)fixedCoast.GetValue(this.instance);
			set => fixedCoast.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double FixedCoastLength {
			get => EditableDouble.Get(this.fixedCoastLength);
			set => EditableDouble.Set(this.fixedCoastLength, value);
		}
	}
}
