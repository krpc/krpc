using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	/// <summary>
	/// This profile is similar to the gravity turn mod. It is a 3-burn to orbit style of launch that can get to orbit with about 2800 dV on stock Kerbin.
	/// If you want to have fun make a rocket that is basically a nose cone, a jumbo-64 a mainsail and some fairly big fins, have the pitch program flip it over aggressively (uncheck the AoA limiter, set the values to like 0.5 / 50 / 40 / 45 / 1) and let it rip.
	/// </summary>
	/// <remarks>
	/// It's not precisely the GT mod algorithm and it does not do any pitch-up during the intermediate burn right now, so it won't handle low TWR upper stages.
	/// </remarks>
	[KRPCClass(Service = "MechJeb")]
	public class AscentGT : AscentBase {
		internal new const string MechJebType = "MuMech.MechJebModuleAscentGT";

		// Fields and methods
		private static FieldInfo turnStartAltitudeField;
		private static FieldInfo turnStartVelocityField;
		private static FieldInfo turnStartPitchField;
		private static FieldInfo intermediateAltitudeField;
		private static FieldInfo holdAPTimeField;

		// Instance objects
		private object turnStartAltitude;
		private object turnStartVelocity;
		private object turnStartPitch;
		private object intermediateAltitude;
		private object holdAPTime;

		internal static new void InitType(Type type) {
			turnStartAltitudeField = type.GetCheckedField("turnStartAltitude");
			turnStartVelocityField = type.GetCheckedField("turnStartVelocity");
			turnStartPitchField = type.GetCheckedField("turnStartPitch");
			intermediateAltitudeField = type.GetCheckedField("intermediateAltitude");
			holdAPTimeField = type.GetCheckedField("holdAPTime");
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);
			this.turnStartAltitude = turnStartAltitudeField.GetInstanceValue(instance);
			this.turnStartVelocity = turnStartVelocityField.GetInstanceValue(instance);
			this.turnStartPitch = turnStartPitchField.GetInstanceValue(instance);
			this.intermediateAltitude = intermediateAltitudeField.GetInstanceValue(instance);
			this.holdAPTime = holdAPTimeField.GetInstanceValue(instance);
		}

		/// <summary>
		/// Altitude in km to pitch over and initiate the Gravity Turn (higher values for lower-TWR rockets).
		/// </summary>
		[KRPCProperty]
		public double TurnStartAltitude {
			get => EditableDouble.Get(this.turnStartAltitude);
			set => EditableDouble.Set(this.turnStartAltitude, value);
		}

		/// <summary>
		/// Velocity in m/s which triggers pitch over and initiates the Gravity Turn (higher values for lower-TWR rockets).
		/// </summary>
		[KRPCProperty]
		public double TurnStartVelocity {
			get => EditableDouble.Get(this.turnStartVelocity);
			set => EditableDouble.Set(this.turnStartVelocity, value);
		}

		/// <summary>
		/// Pitch that the pitch program immediately applies.
		/// </summary>
		[KRPCProperty]
		public double TurnStartPitch {
			get => EditableDouble.Get(this.turnStartPitch);
			set => EditableDouble.Set(this.turnStartPitch, value);
		}

		/// <summary>
		/// Intermediate apoapsis altitude to coast to and then raise the apoapsis up to the eventual final target. May be set to equal the final target in order to skip the intermediate phase.
		/// </summary>
		[KRPCProperty]
		public double IntermediateAltitude {
			get => EditableDouble.Get(this.intermediateAltitude);
			set => EditableDouble.Set(this.intermediateAltitude, value);
		}

		/// <summary>
		/// At the intermediate altitude with this much time-to-apoapsis left the engine will start burning prograde to lift the apoapsis.
		/// The engine will throttle down in order to burn closer to the apoapsis.
		/// This is very similar to the lead-time of a maneuver node in concept, but with throttling down in the case where the player has initiated the burn too early (the corollary is that if you see lots of throttling down at the start, you likely need less HoldAP time).
		/// </summary>
		[KRPCProperty]
		public double HoldAPTime {
			get => EditableDouble.Get(this.holdAPTime);
			set => EditableDouble.Set(this.holdAPTime, value);
		}
	}
}
