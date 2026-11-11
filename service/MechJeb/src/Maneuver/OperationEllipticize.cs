using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Create a maneuver to change both periapsis and apoapsis
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationEllipticize : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationEllipticize";

		// Fields and methods
		private static FieldInfo newApAField;
		private static FieldInfo newPeAField;
		private static FieldInfo timeSelector;

		// Instance objects
		private object newApA;
		private object newPeA;

		internal static new void InitType(Type type) {
			newApAField = type.GetCheckedField("newApA");
			newPeAField = type.GetCheckedField("newPeA");
			timeSelector = GetTimeSelectorField(type);
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.newApA = newApAField.GetInstanceValue(instance);
			this.newPeA = newPeAField.GetInstanceValue(instance);
			this.InitTimeSelector(timeSelector);
		}

		[KRPCProperty]
		public double NewApoapsis {
			get => EditableDouble.Get(this.newApA);
			set => EditableDouble.Set(this.newApA, value);
		}

		[KRPCProperty]
		public double NewPeriapsis {
			get => EditableDouble.Get(this.newPeA);
			set => EditableDouble.Set(this.newPeA, value);
		}
	}
}
