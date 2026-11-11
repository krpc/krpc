using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Create a maneuver to set a new semi-major axis
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationSemiMajor : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationSemiMajor";

		// Fields and methods
		private static FieldInfo newSMAField;
		private static FieldInfo timeSelector;

		// Instance objects
		private object newSMA;

		internal static new void InitType(Type type) {
			newSMAField = type.GetCheckedField("newSMA");
			timeSelector = GetTimeSelectorField(type);
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.newSMA = newSMAField.GetInstanceValue(instance);
			this.InitTimeSelector(timeSelector);
		}

		[KRPCProperty]
		public double NewSemiMajorAxis {
			get => EditableDouble.Get(this.newSMA);
			set => EditableDouble.Set(this.newSMA, value);
		}
	}
}
