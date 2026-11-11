using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/**
	 * <summary>Change longitude of ascending node</summary>
	 */
	[KRPCClass(Service = "MechJeb")]
	public class OperationLan : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationLan";

		// Fields and methods
		private static FieldInfo newLANField;
		private static FieldInfo timeSelector;

		// Instance objects
		private object newLAN;

		internal static new void InitType(Type type) {
			newLANField = type.GetCheckedField("newLAN");
			timeSelector = GetTimeSelectorField(type);
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.newLAN = newLANField.GetInstanceValue(instance);
			this.InitTimeSelector(timeSelector);
		}

		[KRPCProperty]
		public double NewLAN {
			get => EditableDouble.Get(this.newLAN);
			set => EditableDouble.Set(this.newLAN, value);
		}
	}
}
