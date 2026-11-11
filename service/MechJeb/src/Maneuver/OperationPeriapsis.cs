using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Create a maneuver to set a new periapsis
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationPeriapsis : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationPeriapsis";

		// Fields and methods
		private static FieldInfo newPeAField;
		private static FieldInfo timeSelector;

		// Instance objects
		private object newPeA;

		internal static new void InitType(Type type) {
			newPeAField = type.GetCheckedField("newPeA");
			timeSelector = GetTimeSelectorField(type);
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.newPeA = newPeAField.GetInstanceValue(instance);
			this.InitTimeSelector(timeSelector);
		}

		[KRPCProperty]
		public double NewPeriapsis {
			get => EditableDouble.Get(this.newPeA);
			set => EditableDouble.Set(this.newPeA, value);
		}
	}
}
