using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	[KRPCEnum(Service = "MechJeb")]
	public enum TimeReference {
		/// <summary>
		/// At the optimum time.
		/// </summary>
		Computed,

		/// <summary>
		/// After a fixed <see cref="TimeSelector.LeadTime" />.
		/// </summary>
		XFromNow,

		/// <summary>
		/// At the next apoapsis.
		/// </summary>
		Apoapsis,

		/// <summary>
		/// At the next periapsis.
		/// </summary>
		Periapsis,

		/// <summary>
		/// At the selected <see cref="TimeSelector.CircularizeAltitude" />.
		/// </summary>
		Altitude,

		/// <summary>
		/// At the equatorial ascending node.
		/// </summary>
		EqAscending,

		/// <summary>
		/// At the equatorial descending node.
		/// </summary>
		EqDescending,

		/// <summary>
		/// At the next ascending node with the target.
		/// </summary>
		RelAscending,

		/// <summary>
		/// At the next descending node with the target.
		/// </summary>
		RelDescending,

		/// <summary>
		/// At the closest approach to the target.
		/// </summary>
		ClosestApproach,

		/// <summary>
		/// At the cheapest equatorial AN/DN.
		/// </summary>
		EqHighestAd,

		/// <summary>
		/// At the nearest equatorial AN/DN.
		/// </summary>
		EqNearestAd,

		/// <summary>
		/// At the cheapest AN/DN with the target.
		/// </summary>
		RelHighestAd,

		/// <summary>
		/// At the nearest AN/DN with the target.
		/// </summary>
		RelNearestAd
	}

	[KRPCClass(Service = "MechJeb")]
	public class TimeSelector {
		internal const string MechJebType = "MuMech.TimeSelector";

		// Fields and methods
		private static FieldInfo allowedTimeRefField;
		private static FieldInfo currentTimeRef;
		private static FieldInfo leadTimeField;
		private static FieldInfo circularizeAltitudeField;

		// Instance objects
		internal object instance;

		private int[] allowedTimeRef; //MuMech.TimeReference enum
		private object leadTime;
		private object circularizeAltitude;

		internal static void InitType(Type type) {
			allowedTimeRefField = type.GetCheckedField("allowedTimeRef", BindingFlags.NonPublic | BindingFlags.Instance);
			currentTimeRef = type.GetCheckedField("currentTimeRef", BindingFlags.NonPublic | BindingFlags.Instance);
			leadTimeField = type.GetCheckedField("leadTime");
			circularizeAltitudeField = type.GetCheckedField("circularizeAltitude");
		}

		protected internal void InitInstance(object instance) {
			this.instance = instance;

			this.allowedTimeRef = (int[])allowedTimeRefField.GetInstanceValue(instance);
			this.leadTime = leadTimeField.GetInstanceValue(instance);
			this.circularizeAltitude = circularizeAltitudeField.GetInstanceValue(instance);
		}

		[KRPCProperty]
		public TimeReference TimeReference {
			get => (TimeReference)this.allowedTimeRef[(int)currentTimeRef.GetValue(this.instance)];
			set => currentTimeRef.SetValue(this.instance, this.GetTimeRefIndex(value));
		}

		private int GetTimeRefIndex(TimeReference timeRef) {
			for(int i = 0; i < this.allowedTimeRef.Length; i++)
				if(this.allowedTimeRef[i] == (int)timeRef)
					return i;
			throw new OperationException("This TimeReference is not allowed: " + timeRef);
		}

		/// <summary>
		/// To be used with <see cref="TimeReference.XFromNow" />.
		/// </summary>
		[KRPCProperty]
		public double LeadTime {
			get => EditableDouble.Get(this.leadTime);
			set => EditableDouble.Set(this.leadTime, value);
		}

		/// <summary>
		/// To be used with <see cref="TimeReference.Altitude" />.
		/// </summary>
		[KRPCProperty]
		public double CircularizeAltitude {
			get => EditableDouble.Get(this.circularizeAltitude);
			set => EditableDouble.Set(this.circularizeAltitude, value);
		}
	}
}
