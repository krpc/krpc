using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Create a maneuver to set the chosen time
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationLambert : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationLambert";

		// Fields and methods
		private static FieldInfo interceptIntervalField;
		private static FieldInfo timeSelector;

		// Instance objects
		private object interceptInterval;

		internal static new void InitType(Type type) {
			interceptIntervalField = type.GetCheckedField("interceptInterval");
			timeSelector = GetTimeSelectorField(type);
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.interceptInterval = interceptIntervalField.GetInstanceValue(instance);
			this.InitTimeSelector(timeSelector);
		}

		[KRPCProperty]
		public double InterceptInterval {
			get => EditableDouble.Get(this.interceptInterval);
			set => EditableDouble.Set(this.interceptInterval, value);
		}
	}
}
