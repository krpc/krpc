using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {

	[KRPCClass(Service = "MechJeb")]
	public class AirplaneAutopilot : KRPCComputerModule {
		internal new const string MechJebType = "MuMech.MechJebModuleAirplaneAutopilot";

		// Fields and methods
		private static FieldInfo headingHoldEnabled;
		private static FieldInfo altitudeHoldEnabled;
		private static FieldInfo vertSpeedHoldEnabled;
		private static FieldInfo rollHoldEnabled;
		private static FieldInfo speedHoldEnabled;

		private static FieldInfo altitudeTarget;
		private static FieldInfo headingTarget;
		private static FieldInfo rollTarget;
		private static FieldInfo speedTarget;
		private static FieldInfo vertSpeedTarget;
		private static FieldInfo bankAngle;

		private static FieldInfo accKpField;
		private static FieldInfo accKiField;
		private static FieldInfo accKdField;

		private static FieldInfo pitKpField;
		private static FieldInfo pitKiField;
		private static FieldInfo pitKdField;

		private static FieldInfo rolKpField;
		private static FieldInfo rolKiField;
		private static FieldInfo rolKdField;

		private static FieldInfo yawKpField;
		private static FieldInfo yawKiField;
		private static FieldInfo yawKdField;

		private static FieldInfo yawLimitField;
		private static FieldInfo rollLimitField;
		private static FieldInfo pitchUpLimitField;
		private static FieldInfo pitchDownLimitField;

		// Instance objects
		private object accKp;
		private object accKi;
		private object accKd;

		private object pitKp;
		private object pitKi;
		private object pitKd;

		private object rolKp;
		private object rolKi;
		private object rolKd;

		private object yawKp;
		private object yawKi;
		private object yawKd;

		private object yawLimit;
		private object rollLimit;
		private object pitchUpLimit;
		private object pitchDownLimit;

		internal static new void InitType(Type type) {
			headingHoldEnabled = type.GetCheckedField("HeadingHoldEnabled");
			altitudeHoldEnabled = type.GetCheckedField("AltitudeHoldEnabled");
			vertSpeedHoldEnabled = type.GetCheckedField("VertSpeedHoldEnabled");
			rollHoldEnabled = type.GetCheckedField("RollHoldEnabled");
			speedHoldEnabled = type.GetCheckedField("SpeedHoldEnabled");

			altitudeTarget = type.GetCheckedField("AltitudeTarget");
			headingTarget = type.GetCheckedField("HeadingTarget");
			rollTarget = type.GetCheckedField("RollTarget");
			speedTarget = type.GetCheckedField("SpeedTarget");
			vertSpeedTarget = type.GetCheckedField("VertSpeedTarget");
			bankAngle = type.GetCheckedField("BankAngle");

			accKpField = type.GetCheckedField("AccKp");
			accKiField = type.GetCheckedField("AccKi");
			accKdField = type.GetCheckedField("AccKd");

			pitKpField = type.GetCheckedField("PitKp");
			pitKiField = type.GetCheckedField("PitKi");
			pitKdField = type.GetCheckedField("PitKd");

			rolKpField = type.GetCheckedField("RolKp");
			rolKiField = type.GetCheckedField("RolKi");
			rolKdField = type.GetCheckedField("RolKd");

			yawKpField = type.GetCheckedField("YawKp");
			yawKiField = type.GetCheckedField("YawKi");
			yawKdField = type.GetCheckedField("YawKd");

			yawLimitField = type.GetCheckedField("YawLimit");
			rollLimitField = type.GetCheckedField("RollLimit");
			pitchUpLimitField = type.GetCheckedField("PitchUpLimit");
			pitchDownLimitField = type.GetCheckedField("PitchDownLimit");
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);
			this.accKp = accKpField.GetInstanceValue(instance);
			this.accKi = accKiField.GetInstanceValue(instance);
			this.accKd = accKdField.GetInstanceValue(instance);

			this.pitKp = pitKpField.GetInstanceValue(instance);
			this.pitKi = pitKiField.GetInstanceValue(instance);
			this.pitKd = pitKdField.GetInstanceValue(instance);

			this.rolKp = rolKpField.GetInstanceValue(instance);
			this.rolKi = rolKiField.GetInstanceValue(instance);
			this.rolKd = rolKdField.GetInstanceValue(instance);

			this.yawKp = yawKpField.GetInstanceValue(instance);
			this.yawKi = yawKiField.GetInstanceValue(instance);
			this.yawKd = yawKdField.GetInstanceValue(instance);

			this.yawLimit = yawLimitField.GetInstanceValue(instance);
			this.rollLimit = rollLimitField.GetInstanceValue(instance);
			this.pitchUpLimit = pitchUpLimitField.GetInstanceValue(instance);
			this.pitchDownLimit = pitchDownLimitField.GetInstanceValue(instance);
		}

		[KRPCProperty]
		public bool HeadingHoldEnabled {
			get => (bool)headingHoldEnabled.GetValue(this.instance);
			set => headingHoldEnabled.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public bool AltitudeHoldEnabled {
			get => (bool)altitudeHoldEnabled.GetValue(this.instance);
			set => altitudeHoldEnabled.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public bool VertSpeedHoldEnabled {
			get => (bool)vertSpeedHoldEnabled.GetValue(this.instance);
			set => vertSpeedHoldEnabled.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public bool RollHoldEnabled {
			get => (bool)rollHoldEnabled.GetValue(this.instance);
			set => rollHoldEnabled.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public bool SpeedHoldEnabled {
			get => (bool)speedHoldEnabled.GetValue(this.instance);
			set => speedHoldEnabled.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double AltitudeTarget {
			get => (double)altitudeTarget.GetValue(this.instance);
			set => altitudeTarget.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double HeadingTarget {
			get => (double)headingTarget.GetValue(this.instance);
			set => headingTarget.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double RollTarget {
			get => (double)rollTarget.GetValue(this.instance);
			set => rollTarget.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double SpeedTarget {
			get => (double)speedTarget.GetValue(this.instance);
			set => speedTarget.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double VertSpeedTarget {
			get => (double)vertSpeedTarget.GetValue(this.instance);
			set => vertSpeedTarget.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double BankAngle {
			get => (double)bankAngle.GetValue(this.instance);
			set => bankAngle.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double AccKp {
			get => EditableDouble.Get(this.accKp);
			set => EditableDouble.Set(this.accKp, value);
		}

		[KRPCProperty]
		public double AccKi {
			get => EditableDouble.Get(this.accKi);
			set => EditableDouble.Set(this.accKi, value);
		}

		[KRPCProperty]
		public double AccKd {
			get => EditableDouble.Get(this.accKd);
			set => EditableDouble.Set(this.accKd, value);
		}

		[KRPCProperty]
		public double PitKp {
			get => EditableDouble.Get(this.pitKp);
			set => EditableDouble.Set(this.pitKp, value);
		}

		[KRPCProperty]
		public double PitKi {
			get => EditableDouble.Get(this.pitKi);
			set => EditableDouble.Set(this.pitKi, value);
		}

		[KRPCProperty]
		public double PitKd {
			get => EditableDouble.Get(this.pitKd);
			set => EditableDouble.Set(this.pitKd, value);
		}

		[KRPCProperty]
		public double RolKp {
			get => EditableDouble.Get(this.rolKp);
			set => EditableDouble.Set(this.rolKp, value);
		}

		[KRPCProperty]
		public double RolKi {
			get => EditableDouble.Get(this.rolKi);
			set => EditableDouble.Set(this.rolKi, value);
		}

		[KRPCProperty]
		public double RolKd {
			get => EditableDouble.Get(this.rolKd);
			set => EditableDouble.Set(this.rolKd, value);
		}

		[KRPCProperty]
		public double YawKp {
			get => EditableDouble.Get(this.yawKp);
			set => EditableDouble.Set(this.yawKp, value);
		}

		[KRPCProperty]
		public double YawKi {
			get => EditableDouble.Get(this.yawKi);
			set => EditableDouble.Set(this.yawKi, value);
		}

		[KRPCProperty]
		public double YawKd {
			get => EditableDouble.Get(this.yawKd);
			set => EditableDouble.Set(this.yawKd, value);
		}

		[KRPCProperty]
		public double YawLimit {
			get => EditableDouble.Get(this.yawLimit);
			set => EditableDouble.Set(this.yawLimit, value);
		}

		[KRPCProperty]
		public double RollLimit {
			get => EditableDouble.Get(this.rollLimit);
			set => EditableDouble.Set(this.rollLimit, value);
		}

		[KRPCProperty]
		public double PitchUpLimit {
			get => EditableDouble.Get(this.pitchUpLimit);
			set => EditableDouble.Set(this.pitchUpLimit, value);
		}

		[KRPCProperty]
		public double PitchDownLimit {
			get => EditableDouble.Get(this.pitchDownLimit);
			set => EditableDouble.Set(this.pitchDownLimit, value);
		}
	}
}
