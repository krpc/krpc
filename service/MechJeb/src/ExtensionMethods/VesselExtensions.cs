using System;
using System.Reflection;

namespace KRPC.MechJeb.ExtensionMethods {
	public static class VesselExtensions {
		internal const string MechJebType = "MuMech.VesselExtensions";

		// Fields and methods
		private static MethodInfo getMasterMechJeb;
		private static MethodInfo placeManeuverNode;
		private static MethodInfo removeAllManeuverNodes;

		internal static void InitType(Type type) {
			getMasterMechJeb = type.GetCheckedMethod("GetMasterMechJeb", BindingFlags.Public | BindingFlags.Static);
			placeManeuverNode = type.GetCheckedMethod("PlaceManeuverNode", BindingFlags.Public | BindingFlags.Static);
			removeAllManeuverNodes = type.GetCheckedMethod("RemoveAllManeuverNodes", BindingFlags.Public | BindingFlags.Static);
		}

		public static PartModule GetMasterMechJeb(this Vessel vessel) {
			return (PartModule)getMasterMechJeb.Invoke(null, new object[] { vessel });
		}

		public static ManeuverNode PlaceManeuverNode(this Vessel vessel, Orbit patch, Vector3d dV, double UT) {
			return (ManeuverNode)placeManeuverNode.Invoke(null, new object[] { vessel, patch, dV, UT });
		}

		public static void RemoveAllManeuverNodes(this Vessel vessel) {
			removeAllManeuverNodes.Invoke(null, new object[] { vessel });
		}
	}
}
