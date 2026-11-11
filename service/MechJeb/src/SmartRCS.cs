using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	[KRPCClass(Service = "MechJeb")]
	public class SmartRCS : DisplayModule {
		internal const string MechJebType = "MuMech.MechJebModuleSmartRcs";

		// Fields and methods
		private static FieldInfo target;
		private static FieldInfo autoDisableSmartRCS;

		private static MethodInfo engage;

		internal static new void InitType(Type type) {
			target = type.GetCheckedField("target");
			autoDisableSmartRCS = type.GetCheckedField("autoDisableSmartRCS");

			engage = type.GetCheckedMethod("Engage");
		}

		[KRPCProperty]
		public bool AutoDisableSmartRCS {
			get => (bool)autoDisableSmartRCS.GetValue(this.instance);
			set => autoDisableSmartRCS.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public SmartRCSMode Mode {
			get => (SmartRCSMode)target.GetValue(this.instance);
			set {
				target.SetValue(this.instance, (int)value);
				engage.Invoke(this.instance, null);
			}
		}

		[KRPCProperty]
		public RCSController RCSController => MechJeb.RCSController;
	}

	[KRPCEnum(Service = "MechJeb")]
	public enum SmartRCSMode {
		Off,
		ZeroRelativeVelocity,
		ZeroVelocity
	}
}
