using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;

using UnityEngine;

using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.MechJeb {
	[KRPCClass(Service = "MechJeb")]
	public class TargetController : ComputerModule {
		internal new const string MechJebType = "MuMech.MechJebModuleTargetController";

		// Fields and methods
		private static MethodInfo setPositionTarget;
		private static MethodInfo getPositionTargetString;
		private static MethodInfo getPositionTargetPosition;
		private static MethodInfo setDirectionTarget;
		private static MethodInfo pickPositionTargetOnMap;
		private static MethodInfo updateDirectionTarget;

		private static PropertyInfo normalTargetExists;
		private static PropertyInfo positionTargetExists;
		private static PropertyInfo canAlign;
		//private static PropertyInfo target;
		private static PropertyInfo targetOrbit;
		private static PropertyInfo position;
		private static PropertyInfo distance;
		private static PropertyInfo relativeVelocity;
		private static PropertyInfo relativePosition;
		//private static PropertyInfo transform;
		private static PropertyInfo dockingAxis;

		internal static new void InitType(Type type) {
			setPositionTarget = type.GetCheckedMethod("SetPositionTarget");
			getPositionTargetString = type.GetCheckedMethod("GetPositionTargetString");
			getPositionTargetPosition = type.GetCheckedMethod("GetPositionTargetPosition");
			setDirectionTarget = type.GetCheckedMethod("SetDirectionTarget");
			pickPositionTargetOnMap = type.GetCheckedMethod("PickPositionTargetOnMap");
			updateDirectionTarget = type.GetCheckedMethod("UpdateDirectionTarget");

			normalTargetExists = type.GetCheckedProperty("NormalTargetExists");
			positionTargetExists = type.GetCheckedProperty("PositionTargetExists");
			canAlign = type.GetCheckedProperty("CanAlign");
			//target = type.GetProperty("Target");
			targetOrbit = type.GetCheckedProperty("TargetOrbit");
			position = type.GetCheckedProperty("Position");
			distance = type.GetCheckedProperty("Distance");
			relativeVelocity = type.GetCheckedProperty("RelativeVelocity");
			relativePosition = type.GetCheckedProperty("RelativePosition");
			//transform = type.GetProperty("Transform");
			dockingAxis = type.GetCheckedProperty("DockingAxis");
		}

		[KRPCMethod]
		public void SetPositionTarget(SpaceCenter.Services.CelestialBody body, double latitude, double longitude) {
			setPositionTarget.Invoke(this.instance, new object[] { body.InternalBody, latitude, longitude });
		}

		[KRPCMethod]
		public string GetPositionTargetString() {
			return getPositionTargetString.Invoke(this.instance, null).ToString();
		}

		[KRPCMethod]
		public Tuple3 GetPositionTargetPosition() {
			return ((Vector3d)getPositionTargetPosition.Invoke(this.instance, null)).ToTuple();
		}

		[KRPCMethod]
		public void SetDirectionTarget(string name) {
			setDirectionTarget.Invoke(this.instance, new object[] { name });
		}

		[KRPCMethod]
		public void PickPositionTargetOnMap() {
			pickPositionTargetOnMap.Invoke(this.instance, null);
		}

		[KRPCMethod]
		public void UpdateDirectionTarget(Tuple3 direction) {
			updateDirectionTarget.Invoke(this.instance, new object[] { direction.ToVector() });
		}

		[KRPCProperty]
		public bool NormalTargetExists => (bool)normalTargetExists.GetValue(this.instance, null);

		[KRPCProperty]
		public bool PositionTargetExists => (bool)positionTargetExists.GetValue(this.instance, null);

		[KRPCProperty]
		public bool CanAlign => (bool)canAlign.GetValue(this.instance, null);

		internal Orbit InternalTargetOrbit => (Orbit)targetOrbit.GetValue(this.instance, null);

		[KRPCProperty]
		public SpaceCenter.Services.Orbit TargetOrbit => new SpaceCenter.Services.Orbit(this.InternalTargetOrbit);

		[KRPCProperty]
		public Tuple3 Position => ((Vector3)position.GetValue(this.instance, null)).ToTuple();

		[KRPCProperty]
		public float Distance => (float)distance.GetValue(this.instance, null);

		[KRPCProperty]
		public Tuple3 RelativeVelocity => ((Vector3)relativeVelocity.GetValue(this.instance, null)).ToTuple();

		[KRPCProperty]
		public Tuple3 RelativePosition => ((Vector3)relativePosition.GetValue(this.instance, null)).ToTuple();

		[KRPCProperty]
		public Tuple3 DockingAxis => ((Vector3)dockingAxis.GetValue(this.instance, null)).ToTuple();
	}
}
