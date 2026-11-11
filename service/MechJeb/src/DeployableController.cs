using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	[KRPCClass(Service = "MechJeb")]
	public class DeployableController : ComputerModule {
		internal new const string MechJebType = "MuMech.MechJebModuleDeployableController";

		// Fields and methods
		private static FieldInfo autoDeploy;

		private static MethodInfo extendAll;
		private static MethodInfo retractAll;
		private static MethodInfo allRetracted;

		internal static new void InitType(Type type) {
			autoDeploy = type.GetCheckedField("autoDeploy");

			extendAll = type.GetCheckedMethod("ExtendAll");
			retractAll = type.GetCheckedMethod("RetractAll");
			allRetracted = type.GetCheckedMethod("AllRetracted");
		}

		/// <summary>
		/// Automatically deploy modules of this type when controlled by a MechJeb autopilot
		/// </summary>
		[KRPCProperty]
		public bool AutoDeploy {
			get => (bool)autoDeploy.GetValue(this.instance);
			set => autoDeploy.SetValue(this.instance, value);
		}

		/// <summary>
		/// Extend all deployable modules of this type.
		/// </summary>
		[KRPCMethod]
		public void ExtendAll() {
			extendAll.Invoke(this.instance, null);
		}

		/// <summary>
		/// Retract all deployable modules of this type.
		/// </summary>
		[KRPCMethod]
		public void RetractAll() {
			retractAll.Invoke(this.instance, null);
		}

		/// <summary>
		/// Check if all deployable modules of this type are retracted.
		/// </summary>
		/// <returns>True if all modules are retracted; False otherwise</returns>
		[KRPCMethod]
		public bool AllRetracted() {
			return (bool)allRetracted.Invoke(this.instance, null);
		}
	}
}
