using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	public abstract class Module {
		protected internal abstract void InitInstance(object instance);
	}

	public abstract class ComputerModule : Module {
		internal const string MechJebType = "MuMech.ComputerModule";

		// Methods needed for correct functionalify
		private static MethodInfo onFixedUpdate;

		// Fields and methods
		private static PropertyInfo enabled;
		private static FieldInfo usersField;

		// Instance objects
		protected internal object instance;

		private object users;

		internal static void InitType(Type type) {
			onFixedUpdate = type.GetCheckedMethod("OnFixedUpdate");

			enabled = type.GetCheckedProperty("enabled");
			usersField = type.GetCheckedField("users");
		}

		protected internal override void InitInstance(object instance) {
			this.instance = instance;

			this.users = usersField.GetInstanceValue(instance);
		}

		public virtual bool Enabled {
			get => (bool)enabled.GetValue(this.instance, null);
			set {
				if(value)
					UserPool.usersAdd.Invoke(this.users, new object[] { this });
				else
					UserPool.usersRemove.Invoke(this.users, new object[] { this });
			}
		}

		internal void OnFixedUpdate() {
			onFixedUpdate.Invoke(this.instance, null);
		}

		private static class UserPool {
			internal const string MechJebType = "MuMech.UserPool";

			internal static MethodInfo usersAdd;
			internal static MethodInfo usersRemove;

			internal static void InitType(Type type) {
				usersAdd = type.GetCheckedMethod("Add");
				usersRemove = type.GetCheckedMethod("Remove");
			}
		}
	}

	public abstract class KRPCComputerModule : ComputerModule {
		[KRPCProperty]
		public override bool Enabled {
			get => base.Enabled;
			set => base.Enabled = value;
		}
	}

	public abstract class AutopilotModule : KRPCComputerModule {
		internal new const string MechJebType = "MuMech.AutopilotModule";

		// Fields and methods
		internal static PropertyInfo status;

		[KRPCProperty]
		public string Status => (string)status.GetValue(this.instance, null);

		internal static new void InitType(Type type) {
			status = type.GetCheckedProperty("status");
		}
	}

	public abstract class DisplayModule : ComputerModule { }
}
