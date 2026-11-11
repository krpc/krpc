using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;

namespace KRPC.MechJeb.Util {
	internal static class LaunchTiming {
		internal const string MechJebType = "MuMech.LaunchTiming";

		// Fields and methods
		private static MethodInfo timeToPhaseAngle;

		internal static void InitType(Type type) {
			timeToPhaseAngle = type.GetCheckedMethod("TimeToPhaseAngle");
		}

		public static double TimeToPhaseAngle(double launchPhaseAngle) {
			return (double)timeToPhaseAngle.Invoke(null, new object[] { launchPhaseAngle, FlightGlobals.ActiveVessel.mainBody, MechJeb.vesselState.Longitude, MechJeb.TargetController.TargetOrbit.InternalOrbit });
		}
	}

	internal static class MathFunctions {
		internal const string MechJebType = "MechJebLib.Maths.Functions";

		// Fields and methods
		private static MethodInfo timeToPlane;

		internal static void InitType(Type type) {
			timeToPlane = type.GetCheckedMethod("TimeToPlane");
		}

		public static Tuple<double, double> MinimumTimeToPlane(double lan, double inclination) {
			double normal = TimeToPlane(lan, inclination);
			double inverted = TimeToPlane(lan, -inclination);
			return normal < inverted ? Tuple.Create(normal, inclination) : Tuple.Create(inverted, -inclination);
		}

		public static double TimeToPlane(double lan, double inclination) {
			return (double)timeToPlane.Invoke(null, new object[] { FlightGlobals.ActiveVessel.mainBody.rotationPeriod, MechJeb.vesselState.Latitude, MechJeb.vesselState.CelestialLongitude, lan, inclination });
		}
	}
}
