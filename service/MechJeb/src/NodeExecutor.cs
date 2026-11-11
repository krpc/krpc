using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	[KRPCClass(Service = "MechJeb")]
	public class NodeExecutor : ComputerModule {
		internal new const string MechJebType = "MuMech.MechJebModuleNodeExecutor";

		// Fields and methods
		private static FieldInfo autowarp;
		private static FieldInfo leadTimeField;
		private static FieldInfo toleranceField;

		private static MethodInfo executeOneNode;
		private static MethodInfo executeAllNodes;
		private static MethodInfo abort;

		// Instance objects
		private object leadTime;
		private object tolerance;

		internal static new void InitType(Type type) {
			autowarp = type.GetCheckedField("autowarp");
			leadTimeField = type.GetCheckedField("leadTime");
			toleranceField = type.GetCheckedField("tolerance");

			executeOneNode = type.GetCheckedMethod("ExecuteOneNode");
			executeAllNodes = type.GetCheckedMethod("ExecuteAllNodes");
			abort = type.GetCheckedMethod("Abort");
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.leadTime = leadTimeField.GetInstanceValue(instance);
			this.tolerance = toleranceField.GetInstanceValue(instance);
		}

		[KRPCProperty]
		public override bool Enabled => base.Enabled;

		[KRPCProperty]
		public bool Autowarp {
			get => (bool)autowarp.GetValue(this.instance);
			set => autowarp.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double LeadTime {
			get => EditableDouble.Get(this.leadTime);
			set => EditableDouble.Set(this.leadTime, value);
		}

		[KRPCProperty]
		public double Tolerance {
			get => EditableDouble.Get(this.tolerance);
			set => EditableDouble.Set(this.tolerance, value);
		}

		[KRPCMethod]
		public void ExecuteOneNode() {
			executeOneNode.Invoke(this.instance, new object[] { this });
		}

		[KRPCMethod]
		public void ExecuteAllNodes() {
			executeAllNodes.Invoke(this.instance, new object[] { this });
		}

		[KRPCMethod]
		public void Abort() {
			abort.Invoke(this.instance, null);
		}
	}
}
