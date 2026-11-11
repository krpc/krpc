using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/**
	 * <summary>Change surface longitude of apsis</summary>
	 */
	[KRPCClass(Service = "MechJeb")]
	public class OperationLongitude : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationLongitude";

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
		public double NewSurfaceLongitude {
			get => EditableDouble.Get(this.newLAN);
			set => EditableDouble.Set(this.newLAN, value);
		}
	}
}
