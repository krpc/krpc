using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	[KRPCClass(Service = "MechJeb")]
	public class ThrustController : ComputerModule {
		internal new const string MechJebType = "MuMech.MechJebModuleThrustController";

		// Fields and methods
		private static FieldInfo limitDynamicPressure;
		private static FieldInfo maxDynamicPressureField;
		private static FieldInfo limitToPreventOverheats;
		private static FieldInfo smoothThrottle;
		private static FieldInfo throttleSmoothingTime;
		private static FieldInfo limitToPreventFlameout;
		//private static FieldInfo limitToPreventUnstableIgnition;
		//private static FieldInfo autoRCSUllaging;
		private static FieldInfo flameoutSafetyPctField;
		private static FieldInfo manageIntakes;
		private static FieldInfo limitAcceleration;
		private static FieldInfo maxAccelerationField;
		private static FieldInfo limitThrottle;
		private static FieldInfo maxThrottleField;
		private static FieldInfo limiterMinThrottle;
		private static FieldInfo minThrottleField;
		private static FieldInfo differentialThrottle;
		private static FieldInfo differentialThrottleSuccess;
		private static FieldInfo electricThrottle;
		private static FieldInfo electricThrottleLoField;
		private static FieldInfo electricThrottleHiField;

		// Translatron fields
		internal static PropertyInfo tMode;
		internal static FieldInfo transSpdAct;
		internal static FieldInfo transKillH;

		// Instance objects
		private object maxDynamicPressure;
		private object flameoutSafetyPct;
		private object maxAcceleration;
		private object maxThrottle;
		private object minThrottle;
		private object electricThrottleLo;
		private object electricThrottleHi;

		internal static new void InitType(Type type) {
			limitDynamicPressure = type.GetCheckedField("limitDynamicPressure");
			maxDynamicPressureField = type.GetCheckedField("maxDynamicPressure");
			limitToPreventOverheats = type.GetCheckedField("limitToPreventOverheats");
			smoothThrottle = type.GetCheckedField("smoothThrottle");
			throttleSmoothingTime = type.GetCheckedField("throttleSmoothingTime");
			limitToPreventFlameout = type.GetCheckedField("limitToPreventFlameout");
			flameoutSafetyPctField = type.GetCheckedField("flameoutSafetyPct");
			manageIntakes = type.GetCheckedField("manageIntakes");
			limitAcceleration = type.GetCheckedField("limitAcceleration");
			maxAccelerationField = type.GetCheckedField("maxAcceleration");
			limitThrottle = type.GetCheckedField("limitThrottle");
			maxThrottleField = type.GetCheckedField("maxThrottle");
			limiterMinThrottle = type.GetCheckedField("limiterMinThrottle");
			minThrottleField = type.GetCheckedField("minThrottle");
			differentialThrottle = type.GetCheckedField("differentialThrottle");
			differentialThrottleSuccess = type.GetCheckedField("differentialThrottleSuccess");
			electricThrottle = type.GetCheckedField("electricThrottle");
			electricThrottleLoField = type.GetCheckedField("electricThrottleLo");
			electricThrottleHiField = type.GetCheckedField("electricThrottleHi");

			// Translatron fields
			tMode = type.GetCheckedProperty("tmode");
			transSpdAct = type.GetCheckedField("trans_spd_act");
			transKillH = type.GetCheckedField("trans_kill_h");
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.maxDynamicPressure = maxDynamicPressureField.GetInstanceValue(instance);
			this.flameoutSafetyPct = flameoutSafetyPctField.GetInstanceValue(instance);
			this.maxAcceleration = maxAccelerationField.GetInstanceValue(instance);
			this.maxThrottle = maxThrottleField.GetInstanceValue(instance);
			this.minThrottle = minThrottleField.GetInstanceValue(instance);
			this.electricThrottleLo = electricThrottleLoField.GetInstanceValue(instance);
			this.electricThrottleHi = electricThrottleHiField.GetInstanceValue(instance);
		}

		[KRPCProperty]
		public bool LimitDynamicPressure {
			get => (bool)limitDynamicPressure.GetValue(this.instance);
			set => limitDynamicPressure.SetValue(this.instance, value);
		}

		/// <summary>
		/// Limit the maximal dynamic pressure in Pa.
		/// This avoids that pieces break off during launch because of atmospheric pressure.
		/// </summary>
		/// <remarks><see cref="LimitDynamicPressure" /> needs to be enabled.</remarks>
		[KRPCProperty]
		public double MaxDynamicPressure {
			get => EditableDouble.Get(this.maxDynamicPressure);
			set => EditableDouble.Set(this.maxDynamicPressure, value);
		}

		/// <summary>
		/// Limits the throttle to prevent parts from overheating.
		/// </summary>
		[KRPCProperty]
		public bool LimitToPreventOverheats {
			get => (bool)limitToPreventOverheats.GetValue(this.instance);
			set => limitToPreventOverheats.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public bool LimitAcceleration {
			get => (bool)limitAcceleration.GetValue(this.instance);
			set => limitAcceleration.SetValue(this.instance, value);
		}

		/// <summary>
		/// Limit acceleration to [m/s^2] (never exceed the acceleration during ascent).
		/// </summary>
		/// <remarks><see cref="LimitAcceleration" /> needs to be enabled.</remarks>
		[KRPCProperty]
		public double MaxAcceleration {
			get => EditableDouble.Get(this.maxAcceleration);
			set => EditableDouble.Set(this.maxAcceleration, value);
		}

		[KRPCProperty]
		public bool LimitThrottle {
			get => (bool)limitThrottle.GetValue(this.instance);
			set => limitThrottle.SetValue(this.instance, value);
		}

		/// <summary>
		/// Never exceed the percentage of the throttle during ascent (value between 0 and 1).
		/// </summary>
		/// <remarks><see cref="LimitThrottle" /> needs to be enabled.</remarks>
		[KRPCProperty]
		public double MaxThrottle {
			get => EditableDouble.Get(this.maxThrottle);
			set => EditableDouble.Set(this.maxThrottle, value);
		}

		[KRPCProperty]
		public bool LimiterMinThrottle {
			get => (bool)limiterMinThrottle.GetValue(this.instance);
			set => limiterMinThrottle.SetValue(this.instance, value);
		}

		/// <summary>
		/// Never go below the percentage of the throttle during ascent (value between 0 and 1).
		/// </summary>
		/// <remarks><see cref="LimiterMinThrottle" /> needs to be enabled.</remarks>
		[KRPCProperty]
		public double MinThrottle {
			get => EditableDouble.Get(this.minThrottle);
			set => EditableDouble.Set(this.minThrottle, value);
		}

		[KRPCProperty]
		public bool SmoothThrottle {
			get => (bool)smoothThrottle.GetValue(this.instance);
			set => smoothThrottle.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public double ThrottleSmoothingTime {
			get => (double)throttleSmoothingTime.GetValue(this.instance);
			set => throttleSmoothingTime.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public bool LimitToPreventFlameout {
			get => (bool)limitToPreventFlameout.GetValue(this.instance);
			set => limitToPreventFlameout.SetValue(this.instance, value);
		}

		/// <summary>
		/// The jet safety margin. A value between 0 and 1.
		/// </summary>
		[KRPCProperty]
		public double FlameoutSafetyPct {
			get => EditableDouble.Get(this.flameoutSafetyPct);
			set => EditableDouble.Set(this.flameoutSafetyPct, value);
		}

		[KRPCProperty]
		public bool ManageIntakes {
			get => (bool)manageIntakes.GetValue(this.instance);
			set => manageIntakes.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public bool DifferentialThrottle {
			get => (bool)differentialThrottle.GetValue(this.instance);
			set => differentialThrottle.SetValue(this.instance, value);
		}

		[KRPCProperty]
		public DifferentialThrottleStatus DifferentialThrottleStatus => (DifferentialThrottleStatus)differentialThrottleSuccess.GetValue(this.instance);

		[KRPCProperty]
		public bool ElectricThrottle {
			get => (bool)electricThrottle.GetValue(this.instance);
			set => electricThrottle.SetValue(this.instance, value);
		}

		/// <remarks><see cref="ElectricThrottle" /> needs to be enabled.</remarks>
		[KRPCProperty]
		public double ElectricThrottleLo {
			get => EditableDouble.Get(this.electricThrottleLo);
			set => EditableDouble.Set(this.electricThrottleLo, value);
		}

		/// <remarks><see cref="ElectricThrottle" /> needs to be enabled.</remarks>
		[KRPCProperty]
		public double ElectricThrottleHi {
			get => EditableDouble.Get(this.electricThrottleHi);
			set => EditableDouble.Set(this.electricThrottleHi, value);
		}
	}

	[KRPCEnum(Service = "MechJeb")]
	public enum DifferentialThrottleStatus {
		Success,
		AllEnginesOff,
		MoreEnginesRequired,
		SolverFailed
	}
}
