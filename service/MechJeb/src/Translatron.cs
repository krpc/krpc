using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	/// <summary>
	/// The Translatron module controls the vessel's throttle/velocity.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class Translatron : DisplayModule {
		internal new const string MechJebType = "MuMech.MechJebModuleTranslatron";

		// Fields and methods
		private static FieldInfo transSpd;

		private static MethodInfo setMode;
		private static MethodInfo panicSwitch;

		internal static new void InitType(Type type) {
			transSpd = type.GetCheckedField("trans_spd");

			setMode = type.GetCheckedMethod("SetMode");
			panicSwitch = type.GetCheckedMethod("PanicSwitch");
		}

		/// <summary>
		/// Speed which trasnlatron will hold
		/// </summary>
		[KRPCProperty]
		public double TranslationSpeed {
			get => EditableDouble.Get(transSpd, this.instance);
			set {
				EditableDouble.Set(transSpd, this.instance, value);
				ThrustController.transSpdAct.SetValue(MechJeb.ThrustController.instance, (float)value);
			}
		}

		/// <summary>
		/// Kill horizontal speed
		/// </summary>
		[KRPCProperty]
		public bool KillHorizontalSpeed {
			get => (bool)ThrustController.transKillH.GetValue(MechJeb.ThrustController.instance);
			set => ThrustController.transKillH.SetValue(MechJeb.ThrustController.instance, value);
		}

		/// <summary>
		/// Current translatron mode.
		/// </summary>
		[KRPCProperty]
		public TranslatronMode Mode {
			get => (TranslatronMode)ThrustController.tMode.GetValue(MechJeb.ThrustController.instance, null);
			set {
				if(value == TranslatronMode.KeepRelative || value == TranslatronMode.Direct)
					throw new MJServiceException("Cannot set TranslatronMode to internal values");

				setMode.Invoke(this.instance, new object[] { (int)value });
			}
		}

		/// <summary>
		/// Abort mission by seperating all but the last stage and activating landing autopilot.
		/// </summary>
		[KRPCMethod]
		public void PanicSwitch() {
			panicSwitch.Invoke(this.instance, null);
		}

		[KRPCEnum(Service = "MechJeb")]
		public enum TranslatronMode {
			/// <summary>
			/// Switch off Translatron.
			/// </summary>
			Off,

			/// <summary>
			/// Keep orbital velocity.
			/// </summary>
			KeepOrbital,

			/// <summary>
			/// Keep surface velocity.
			/// </summary>
			KeepSurface,

			/// <summary>
			/// Keep vertical velocity (climb/descent speed).
			/// </summary>
			KeepVertical,

			/// <summary>
			/// Internal mode, do not set.
			/// </summary>
			KeepRelative,

			/// <summary>
			/// Internal mode, do not set.
			/// </summary>
			Direct
		}
	}
}
