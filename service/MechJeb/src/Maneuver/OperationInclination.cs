using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Create a maneuver to change inclination
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationInclination : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationInclination";

		// Fields and methods
		private static FieldInfo newIncField;
		private static FieldInfo timeSelector;

		// Instance objects
		private object newInc;

		internal static new void InitType(Type type) {
			newIncField = type.GetCheckedField("newInc");
			timeSelector = GetTimeSelectorField(type);
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.newInc = newIncField.GetInstanceValue(instance);
			this.InitTimeSelector(timeSelector);
		}

		[KRPCProperty]
		public double NewInclination {
			get => EditableDouble.Get(this.newInc);
			set => EditableDouble.Set(this.newInc, value);
		}
	}
}
