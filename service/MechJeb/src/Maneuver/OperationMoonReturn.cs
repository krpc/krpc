using System;
using System.Reflection;
using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Create a maneuver to return from a moon approximately at the specified altitude
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationMoonReturn : Operation {
		internal new const string MechJebType = "MuMech.OperationMoonReturn";

		// Fields and methods
		private static FieldInfo moonReturnAltitudeField;

		// Instance objects
		private object moonReturnAltitude;

		internal static new void InitType(Type type) {
			moonReturnAltitudeField = type.GetCheckedField("moonReturnAltitude");
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.moonReturnAltitude = moonReturnAltitudeField.GetInstanceValue(instance);
		}

		/// <summary>
		/// Approximate return altitude from a moon (from an orbiting body to the parent body).
		/// </summary>
		[KRPCProperty]
		public double MoonReturnAltitude {
			get => EditableDouble.Get(this.moonReturnAltitude);
			set => EditableDouble.Set(this.moonReturnAltitude, value);
		}
	}
}
