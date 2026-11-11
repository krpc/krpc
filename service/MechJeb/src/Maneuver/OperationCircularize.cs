using System;
using System.Reflection;

using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// This mode creates a manevuer to match your apoapsis to periapsis.
	/// To match apoapsis to periapsis, set the time to <see cref="TimeReference.Periapsis" />; to match periapsis to apoapsis, set the time to <see cref="TimeReference.Apoapsis" />. These are the most efficient, but it can also create node at specific height or after specific time.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationCircularize : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationCircularize";

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
