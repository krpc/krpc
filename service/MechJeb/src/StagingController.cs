using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	[KRPCClass(Service = "MechJeb")]
	public class StagingController : KRPCComputerModule {
		internal new const string MechJebType = "MuMech.MechJebModuleStagingController";

		// Fields and methods
		private static FieldInfo autostagePreDelayField;
		private static FieldInfo autostagePostDelayField;
		private static FieldInfo autostageLimitField;
		private static FieldInfo fairingMaxDynamicPressureField;
		private static FieldInfo fairingMinAltitudeField;
		private static FieldInfo clampAutoStageThrustPctField;
		private static FieldInfo fairingMaxAerothermalFluxField;

		private static FieldInfo hotStaging;
		private static FieldInfo hotStagingLeadTimeField;

		private static FieldInfo autostagingOnce;

		// Instance objects
		private object autostagePreDelay;
		private object autostagePostDelay;
		private object autostageLimit;
		private object fairingMaxDynamicPressure;
		private object fairingMinAltitude;
		private object clampAutoStageThrustPct;
		private object fairingMaxAerothermalFlux;

		private object hotStagingLeadTime;

		internal static new void InitType(Type type) {
			autostagePreDelayField = type.GetCheckedField("autostagePreDelay");
			autostagePostDelayField = type.GetCheckedField("autostagePostDelay");
			autostageLimitField = type.GetCheckedField("autostageLimit");
			fairingMaxDynamicPressureField = type.GetCheckedField("fairingMaxDynamicPressure");
			fairingMinAltitudeField = type.GetCheckedField("fairingMinAltitude");
			clampAutoStageThrustPctField = type.GetCheckedField("clampAutoStageThrustPct");
			fairingMaxAerothermalFluxField = type.GetCheckedField("fairingMaxAerothermalFlux");

			hotStaging = type.GetCheckedField("hotStaging");
			hotStagingLeadTimeField = type.GetCheckedField("hotStagingLeadTime");

			autostagingOnce = type.GetCheckedField("autostagingOnce");
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.autostagePreDelay = autostagePreDelayField.GetInstanceValue(instance);
			this.autostagePostDelay = autostagePostDelayField.GetInstanceValue(instance);
			this.autostageLimit = autostageLimitField.GetInstanceValue(instance);
			this.fairingMaxDynamicPressure = fairingMaxDynamicPressureField.GetInstanceValue(instance);
			this.fairingMinAltitude = fairingMinAltitudeField.GetInstanceValue(instance);
			this.clampAutoStageThrustPct = clampAutoStageThrustPctField.GetInstanceValue(instance);
			this.fairingMaxAerothermalFlux = fairingMaxAerothermalFluxField.GetInstanceValue(instance);

			this.hotStagingLeadTime = hotStagingLeadTimeField.GetInstanceValue(instance);
		}

		/// <summary>
		/// The autopilot will pause the actual staging before ? seconds for each stage.
		/// </summary>
		[KRPCProperty]
		public double AutostagePreDelay {
			get => EditableDouble.Get(this.autostagePreDelay);
			set => EditableDouble.Set(this.autostagePreDelay, value);
		}

		/// <summary>
		/// The autopilot will pause the actual staging after ? seconds for each stage.
		/// </summary>
		[KRPCProperty]
		public double AutostagePostDelay {
			get => EditableDouble.Get(this.autostagePostDelay);
			set => EditableDouble.Set(this.autostagePostDelay, value);
		}

		/// <summary>
		/// Stop at the selected stage - staging will not occur beyond this stage number.
		/// </summary>
		[KRPCProperty]
		public int AutostageLimit {
			get => EditableInt.Get(this.autostageLimit);
			set => EditableInt.Set(this.autostageLimit, value);
		}

		[KRPCProperty]
		public double FairingMaxDynamicPressure {
			get => EditableDouble.Get(this.fairingMaxDynamicPressure);
			set => EditableDouble.Set(this.fairingMaxDynamicPressure, value);
		}

		[KRPCProperty]
		public double FairingMinAltitude {
			get => EditableDouble.Get(this.fairingMinAltitude);
			set => EditableDouble.Set(this.fairingMinAltitude, value);
		}

		[KRPCProperty]
		public double ClampAutoStageThrustPct {
			get => EditableDouble.Get(this.clampAutoStageThrustPct);
			set => EditableDouble.Set(this.clampAutoStageThrustPct, value);
		}

		[KRPCProperty]
		public double FairingMaxAerothermalFlux {
			get => EditableDouble.Get(this.fairingMaxAerothermalFlux);
			set => EditableDouble.Set(this.fairingMaxAerothermalFlux, value);
		}

		[KRPCProperty]
		public bool HotStaging {
			get => (bool)hotStaging.GetValue(this.instance);
			set => hotStaging.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double HotStagingLeadTime {
			get => EditableDouble.Get(this.hotStagingLeadTime);
			set => EditableDouble.Set(this.hotStagingLeadTime, value);
		}

		/// <summary>
		/// The autostaging mode. If set to true, it will automatically disable itself after one staging action.
		/// </summary>
		/// <remarks>The controller needs to be enabled for this to work.</remarks>
		[KRPCProperty]
		public bool AutostagingOnce {
			get => (bool)autostagingOnce.GetValue(this.instance);
			set => autostagingOnce.SetValue(this.instance, value);
		}
	}
}
