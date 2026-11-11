using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	[KRPCClass(Service = "MechJeb")]
	public class RCSController : ComputerModule {
		internal new const string MechJebType = "MuMech.MechJebModuleRCSController";

		// Fields and methods
		private static FieldInfo rcsThrottle;
		private static FieldInfo rcsForRotation;

		internal static new void InitType(Type type) {
			rcsThrottle = type.GetCheckedField("rcsThrottle");
			rcsForRotation = type.GetCheckedField("rcsForRotation");
		}

		[KRPCProperty]
		public bool RCSThrottle {
			get => (bool)rcsThrottle.GetValue(this.instance);
			set => rcsThrottle.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public bool RCSForRotation {
			get => (bool)rcsForRotation.GetValue(this.instance);
			set => rcsForRotation.SetValue(this.instance, value);
		}
	}
}
