using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Create a maneuver to set a new apoapsis
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationApoapsis : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationApoapsis";

		// Fields and methods
		private static FieldInfo newApAField;
		private static FieldInfo timeSelector;

		// Instance objects
		private object newApA;

		internal static new void InitType(Type type) {
			newApAField = type.GetCheckedField("newApA");
			timeSelector = GetTimeSelectorField(type);
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.newApA = newApAField.GetInstanceValue(instance);
			this.InitTimeSelector(timeSelector);
		}

		[KRPCProperty]
		public double NewApoapsis {
			get => EditableDouble.Get(this.newApA);
			set => EditableDouble.Set(this.newApA, value);
		}
	}
}
