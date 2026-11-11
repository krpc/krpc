using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	[KRPCClass(Service = "MechJeb")]
	public class DockingAutopilot : KRPCComputerModule {
		internal new const string MechJebType = "MuMech.MechJebModuleDockingAutopilot";

		// Fields and methods
		private static FieldInfo status;
		private static FieldInfo speedLimitField;
		private static FieldInfo forceRoll;
		private static FieldInfo rollField;
		private static FieldInfo overrideSafeDistance;
		private static FieldInfo overridenSafeDistanceField;
		private static FieldInfo overrideTargetSize;
		private static FieldInfo overridenTargetSizeField;

		private static FieldInfo safeDistance;
		private static FieldInfo targetSize;

		// Instance objects
		private object speedLimit;
		private object roll;
		private object overridenSafeDistance;
		private object overridenTargetSize;

		internal static new void InitType(Type type) {
			status = type.GetCheckedField("status");
			speedLimitField = type.GetCheckedField("speedLimit");
			forceRoll = type.GetCheckedField("forceRol");
			rollField = type.GetCheckedField("rol");
			overrideSafeDistance = type.GetCheckedField("overrideSafeDistance");
			overridenSafeDistanceField = type.GetCheckedField("overridenSafeDistance");
			overrideTargetSize = type.GetCheckedField("overrideTargetSize");
			overridenTargetSizeField = type.GetCheckedField("overridenTargetSize");
			safeDistance = type.GetCheckedField("safeDistance");
			targetSize = type.GetCheckedField("targetSize");
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.speedLimit = speedLimitField.GetInstanceValue(instance);
			this.roll = rollField.GetInstanceValue(instance);
			this.overridenSafeDistance = overridenSafeDistanceField.GetInstanceValue(instance);
			this.overridenTargetSize = overridenTargetSizeField.GetInstanceValue(instance);
		}

		[KRPCProperty]
		public string Status => status.GetValue(this.instance).ToString();

		[KRPCProperty]
		public double SpeedLimit {
			get => EditableDouble.Get(this.speedLimit);
			set => EditableDouble.Set(this.speedLimit, value);
		}

		[KRPCProperty]
		public bool ForceRoll {
			get => (bool)forceRoll.GetValue(this.instance);
			set => forceRoll.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double Roll {
			get => EditableDouble.Get(this.roll);
			set => EditableDouble.Set(this.roll, value);
		}

		[KRPCProperty]
		public bool OverrideSafeDistance {
			get => (bool)overrideSafeDistance.GetValue(this.instance);
			set => overrideSafeDistance.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double OverridenSafeDistance {
			get => EditableDouble.Get(this.overridenSafeDistance);
			set => EditableDouble.Set(this.overridenSafeDistance, value);
		}

		[KRPCProperty]
		public bool OverrideTargetSize {
			get => (bool)overrideTargetSize.GetValue(this.instance);
			set => overrideTargetSize.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double OverridenTargetSize {
			get => EditableDouble.Get(this.overridenTargetSize);
			set => EditableDouble.Set(this.overridenTargetSize, value);
		}

		[KRPCProperty]
		public float SafeDistance => (float)safeDistance.GetValue(this.instance);

		[KRPCProperty]
		public float TargetSize => (float)targetSize.GetValue(this.instance);
	}
}
