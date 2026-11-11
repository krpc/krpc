using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.MechJeb.Maneuver;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	[KRPCClass(Service = "MechJeb")]
	public class ManeuverPlanner : Module {
		internal const string MechJebType = "MuMech.MechJebModuleManeuverPlanner";

		// Fields and methods
		private static FieldInfo operationsField;

		// Instance objects
		private readonly Dictionary<string, Operation> operations = new Dictionary<string, Operation>();

		public ManeuverPlanner() {
			this.operations.Add("OperationApoapsis", new OperationApoapsis());
			this.operations.Add("OperationCircularize", new OperationCircularize());
			this.operations.Add("OperationCourseCorrection", new OperationCourseCorrection());
			this.operations.Add("OperationEllipticize", new OperationEllipticize());
			this.operations.Add("OperationInclination", new OperationInclination());
			this.operations.Add("OperationInterplanetaryTransfer", new OperationInterplanetaryTransfer());
			this.operations.Add("OperationKillRelVel", new OperationKillRelVel());
			this.operations.Add("OperationLambert", new OperationLambert());
			this.operations.Add("OperationLan", new OperationLan());
			this.operations.Add("OperationLongitude", new OperationLongitude());
			this.operations.Add("OperationMoonReturn", new OperationMoonReturn());
			this.operations.Add("OperationPeriapsis", new OperationPeriapsis());
			this.operations.Add("OperationPlane", new OperationPlane());
			this.operations.Add("OperationResonantOrbit", new OperationResonantOrbit());
			this.operations.Add("OperationSemiMajor", new OperationSemiMajor());
			this.operations.Add("OperationGeneric", new OperationTransfer());
		}

		internal static void InitType(Type type) {
			operationsField = type.GetCheckedField("operation", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		protected internal override void InitInstance(object instance) {
			Dictionary<string, object> operations = instance != null ? ((object[])operationsField.GetValue(instance)).ToDictionary(el => el.GetType().FullName, el => el) : new Dictionary<string, object>();

			foreach(KeyValuePair<string, Operation> p in this.operations) {
				string operationType = "MuMech." + p.Key;
				if(instance == null)
					p.Value.InitInstance(null);
				else if(operations.TryGetValue(operationType, out object operationInstance))
					p.Value.InitInstance(operationInstance);
				else {
					string error = "Operation " + p.Value.GetType().Name + " cannot be initialized";
					Logger.Severe(error + ": " + operationType + " not found");
					MechJeb.errors.Add(error);
				}
			}
		}

		//TODO: OperationAdvancedTransfer

		[KRPCProperty]
		public OperationApoapsis OperationApoapsis => (OperationApoapsis)this.operations["OperationApoapsis"];

		[KRPCProperty]
		public OperationCircularize OperationCircularize => (OperationCircularize)this.operations["OperationCircularize"];

		[KRPCProperty]
		public OperationCourseCorrection OperationCourseCorrection => (OperationCourseCorrection)this.operations["OperationCourseCorrection"];

		[KRPCProperty]
		public OperationEllipticize OperationEllipticize => (OperationEllipticize)this.operations["OperationEllipticize"];

		[KRPCProperty]
		public OperationInclination OperationInclination => (OperationInclination)this.operations["OperationInclination"];

		[KRPCProperty]
		public OperationInterplanetaryTransfer OperationInterplanetaryTransfer => (OperationInterplanetaryTransfer)this.operations["OperationInterplanetaryTransfer"];

		[KRPCProperty]
		public OperationKillRelVel OperationKillRelVel => (OperationKillRelVel)this.operations["OperationKillRelVel"];

		[KRPCProperty]
		public OperationLambert OperationLambert => (OperationLambert)this.operations["OperationLambert"];

		[KRPCProperty]
		public OperationLan OperationLan => (OperationLan)this.operations["OperationLan"];

		[KRPCProperty]
		public OperationLongitude OperationLongitude => (OperationLongitude)this.operations["OperationLongitude"];

		[KRPCProperty]
		public OperationMoonReturn OperationMoonReturn => (OperationMoonReturn)this.operations["OperationMoonReturn"];

		[KRPCProperty]
		public OperationPeriapsis OperationPeriapsis => (OperationPeriapsis)this.operations["OperationPeriapsis"];

		[KRPCProperty]
		public OperationPlane OperationPlane => (OperationPlane)this.operations["OperationPlane"];

		[KRPCProperty]
		public OperationResonantOrbit OperationResonantOrbit => (OperationResonantOrbit)this.operations["OperationResonantOrbit"];

		[KRPCProperty]
		public OperationSemiMajor OperationSemiMajor => (OperationSemiMajor)this.operations["OperationSemiMajor"];

		[KRPCProperty]
		public OperationTransfer OperationTransfer => (OperationTransfer)this.operations["OperationGeneric"];
	}
}
