using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;

namespace KRPC.MechJeb {
	internal class VesselState {
		internal const string MechJebType = "MuMech.VesselState";

		// Fields and methods
		private static FieldInfo time;
		private static FieldInfo celestialLongitudeField;
		private static FieldInfo latitudeField;
		private static FieldInfo longitudeField;

		// Instance objects
		private object instance;
		private object celestialLongitude;
		private object latitude;
		private object longitude;

		internal static void InitType(Type type) {
			time = type.GetCheckedField("time");
			celestialLongitudeField = type.GetCheckedField("celestialLongitude");
			latitudeField = type.GetCheckedField("latitude");
			longitudeField = type.GetCheckedField("longitude");
		}

		internal void InitInstance(object instance) {
			this.instance = instance;
			this.celestialLongitude = celestialLongitudeField.GetInstanceValue(instance);
			this.latitude = latitudeField.GetInstanceValue(instance);
			this.longitude = longitudeField.GetInstanceValue(instance);
		}

		public double Time => (double)time.GetInstanceValue(this.instance);
		public double CelestialLongitude => MovingAverage.Get(this.celestialLongitude);
		public double Latitude => MovingAverage.Get(this.latitude);
		public double Longitude => MovingAverage.Get(this.longitude);
	}
}
