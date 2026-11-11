using System;
using System.Reflection;

using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Match velocities with target
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationKillRelVel : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationKillRelVel";

		// Fields and methods
		private static FieldInfo timeSelector;

		internal static new void InitType(Type type) {
			timeSelector = GetTimeSelectorField(type);
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);
			this.InitTimeSelector(timeSelector);
		}
	}
}
