// Adapted from Infernal Robotics (GPLv3, https://github.com/MagicSmokeIndustries/InfernalRobotics),
// IRWrapper.cs

#pragma warning disable 1591

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using KRPC.Utils;

namespace KRPC.InfernalRobotics
{
	public class IRWrapper
	{
		private static bool isWrapped;

		protected internal static Type IRControllerType { get; set; }

		protected internal static Type IRServoGroupType { get; set; }

		protected internal static Type IRServoType { get; set; }

		// kRPC-local addition (diverges from the upstream vendored IRWrapper): the pieces
		// needed to enumerate and control servos on loaded vessels *other* than the active
		// one. IR's Controller only ever populates its ServoGroups/servosState from the
		// active vessel, but a servo's motion is driven entirely by its own PartModule in
		// FixedUpdate (gated only on isOnRails/isLocked, never on isActiveVessel), so any
		// loaded, off-rails vessel's servos can be enumerated and commanded directly.
		protected internal static Type ServoHelperType { get; set; }
		protected internal static Type ModuleServoType { get; set; }
		protected internal static Func<Vessel, object> ToServos { get; set; }
		protected internal static Field<object, string> GroupName { get; set; }

		protected internal static object ActualController { get; set; }

		internal static IRAPI IRController { get; set; }
		internal static bool AssemblyExists { get { return (IRControllerType != null); } }
		internal static bool InstanceExists { get { return (IRController != null); } }
		internal static bool APIReady { get { return isWrapped && IRController.Ready; } }

		internal static bool InitWrapper()
		{
			isWrapped = false;
			ActualController = null;
			IRController = null;
			LogFormatted("Attempting to Grab IR Types...");

			IRControllerType = null;

			AssemblyLoader.loadedAssemblies.TypeOperation (t => {
				if(t.FullName == "InfernalRobotics_v3.Command.Controller") { IRControllerType = t; } });

			if(IRControllerType == null)
				return false;

			LogFormatted("IR Version:{0}", IRControllerType.Assembly.GetName().Version.ToString());

			IRServoType = null;
			AssemblyLoader.loadedAssemblies.TypeOperation (t => {
				if(t.FullName == "InfernalRobotics_v3.Interfaces.IServo") { IRServoType = t; } });

			if(IRServoType == null)
			{
				LogFormatted("[IR Wrapper] Failed to grab Servo Type");
				return false;
			}

			IRServoGroupType = null;
			AssemblyLoader.loadedAssemblies.TypeOperation (t => {
				if(t.FullName == "InfernalRobotics_v3.Interfaces.IServoGroup") { IRServoGroupType = t; } });

			if(IRServoGroupType == null)
			{
				LogFormatted("[IR Wrapper] Failed to grab ServoGroup Type");
				return false;
			}

			// kRPC-local addition: resolve the helper used to enumerate a specific vessel's
			// servo modules directly (ServoHelper.ToServos(Vessel) == vessel
			// .FindPartModulesImplementing<ModuleIRServo_v3>()), plus the module's groupName
			// field, so groups can be reconstructed for non-active vessels. These are only
			// used by the non-active-vessel fallback; a missing one leaves that path empty
			// but does not break the active-vessel Controller path, so do not fail here.
			ServoHelperType = null;
			AssemblyLoader.loadedAssemblies.TypeOperation (t => {
				if(t.FullName == "InfernalRobotics_v3.Utility.ServoHelper") { ServoHelperType = t; } });
			ModuleServoType = null;
			AssemblyLoader.loadedAssemblies.TypeOperation (t => {
				if(t.FullName == "InfernalRobotics_v3.Module.ModuleIRServo_v3") { ModuleServoType = t; } });
			ToServos = ServoHelperType != null
				? Accessor.Method<Func<Vessel, object>> (ServoHelperType, "ToServos")
				: null;
			GroupName = ModuleServoType != null
				? Accessor.Field<object, string> (ModuleServoType, "groupName")
				: null;

			// The wrappers bind the mod's members once and keep them for the assembly, so
			// anything bound against the types of a previous run is dropped here.
			IRServo.Unbind();
			IRServoGroup.Unbind();

			LogFormatted("Got Assembly Types, grabbing Instance");

			try
			{
				var instance = Accessor.StaticGetter<object> (IRControllerType, "Instance");

				if(instance == null)
					LogFormatted("[IR Wrapper] Cannot find Instance Property");
				else
					ActualController = instance ();
			}
			catch (Exception e)
			{
				LogFormatted("No Instance found, " + e.Message);
			}

			if(ActualController == null)
			{
				LogFormatted("Failed grabbing Instance");
				return false;
			}

			LogFormatted("Got Instance, Creating Wrapper Objects");
			IRController = new InfernalRoboticsAPI();
			isWrapped = true;
			return true;
		}

		#region Private Implementation

		private class InfernalRoboticsAPI : IRAPI
		{
			private Func<bool> apiReady;
			private Func<object, object> servoGroups;
			private object actualServoGroups;

			public InfernalRoboticsAPI()
			{
				DetermineReady();
				BuildServoGroups();
			}

			private void BuildServoGroups()
			{
				if(servoGroups == null)
					servoGroups = Accessor.Getter<object, object> (IRControllerType, "ServoGroups");
				if(servoGroups == null)
					LogFormatted("Failed Getting ServoGroups fieldinfo");
				else if(IRWrapper.ActualController == null)
					LogFormatted("ServoController Instance not found");
				else
					actualServoGroups = servoGroups (IRWrapper.ActualController);
			}

			private void DetermineReady()
			{
				LogFormatted("Getting APIReady Object");
				apiReady = Accessor.StaticGetter<bool> (IRControllerType, "APIReady");
				LogFormatted("Success: " + (apiReady != null));
			}

			public bool Ready
			{
				get
				{
					if(apiReady == null || actualServoGroups == null)
						return false;

					return apiReady ();
				}
			}

			public IList<IServoGroup> ServoGroups
			{
				get
				{
					BuildServoGroups();
					return ExtractServoGroups (actualServoGroups);
				}
			}

			private IList<IServoGroup> ExtractServoGroups (object servoGroups)
			{
				var listToReturn = new List<IServoGroup>();

				if(servoGroups == null)
					return listToReturn;

				try
				{
					//iterate each "value" in the dictionary
					foreach(var item in (IList)servoGroups)
						listToReturn.Add (new IRServoGroup (item));
				}
				catch (Exception ex)
				{
					LogFormatted("Cannot list ServoGroups: {0}", ex.Message);
				}

				return listToReturn;
			}
		}

		private class IRServoGroup : IServoGroup
		{
			private readonly object actualControlGroup;

			private static Field<object, string> nameProperty;
			private static Func<object, Vessel> vesselProperty;
			private static Field<object, bool> expandedProperty;
			private static Field<object, float> groupSpeedFactorProperty;

			private static Field<object, string> forwardKeyProperty;
			private static Field<object, string> reverseKeyProperty;

			private static Func<object, int> movingDirectionProperty;
			private static Field<object, bool> advancedModeProperty;
			private static Func<object, float> totalElectricChargeRequirementProperty;
			private static Field<object, bool> buildAidProperty;
			private static Field<object, bool> ikActiveProperty;

			private static Action<object> moveRightMethod;
			private static Action<object> moveLeftMethod;
			private static Action<object> moveCenterMethod;
			private static Action<object> moveNextPresetMethod;
			private static Action<object> movePrevPresetMethod;
			private static Action<object> stopMethod;

			public IRServoGroup (object cg)
			{
				actualControlGroup = cg;
				EnsureBound();
				ActualServos = servosProperty (actualControlGroup);
			}

			// The lookups below depend on the mod's group type alone, so they are made once
			// for the assembly rather than for each group wrapped. A group is wrapped every
			// time one is listed, and there are a dozen of them.
			private static bool bound;

			internal static void Unbind()
			{
				bound = false;
			}

			private static void EnsureBound()
			{
				if(bound)
					return;
				FindProperties();
				FindMethods();
				bound = true;
			}

			private static Func<object, object> servosProperty;

			private static void FindProperties()
			{
				nameProperty = Accessor.Field<object, string> (IRServoGroupType, "Name");
				vesselProperty = Accessor.Getter<object, Vessel> (IRServoGroupType, "Vessel");
				forwardKeyProperty = Accessor.Field<object, string> (IRServoGroupType, "ForwardKey");
				reverseKeyProperty = Accessor.Field<object, string> (IRServoGroupType, "ReverseKey");
				groupSpeedFactorProperty = Accessor.Field<object, float> (IRServoGroupType, "GroupSpeedFactor");
				expandedProperty = Accessor.Field<object, bool> (IRServoGroupType, "Expanded");
				movingDirectionProperty = Accessor.Getter<object, int> (IRServoGroupType, "MovingDirection");
				advancedModeProperty = Accessor.Field<object, bool> (IRServoGroupType, "AdvancedMode");
				totalElectricChargeRequirementProperty = Accessor.Getter<object, float> (IRServoGroupType, "TotalElectricChargeRequirement");
				buildAidProperty = Accessor.Field<object, bool> (IRServoGroupType, "BuildAid");
				ikActiveProperty = Accessor.Field<object, bool> (IRServoGroupType, "IKActive");

				servosProperty = Accessor.Getter<object, object> (IRServoGroupType, "Servos");
			}

			private static void FindMethods()
			{
				moveRightMethod = Accessor.Method<Action<object>> (IRServoGroupType, "MoveRight");
				moveLeftMethod = Accessor.Method<Action<object>> (IRServoGroupType, "MoveLeft");
				moveCenterMethod = Accessor.Method<Action<object>> (IRServoGroupType, "MoveCenter");
				moveNextPresetMethod = Accessor.Method<Action<object>> (IRServoGroupType, "MoveNextPreset");
				movePrevPresetMethod = Accessor.Method<Action<object>> (IRServoGroupType, "MovePrevPreset");
				stopMethod = Accessor.Method<Action<object>> (IRServoGroupType, "Stop");
			}

			public string Name
			{
				get { return nameProperty.Get (actualControlGroup); }
				set { nameProperty.Set (actualControlGroup, value); }
			}

			public Vessel Vessel
			{
				get { return vesselProperty != null ? vesselProperty (actualControlGroup) : null; }
			}

			public string ForwardKey
			{
				get { return forwardKeyProperty.Get (actualControlGroup); }
				set { forwardKeyProperty.Set (actualControlGroup, value); }
			}

			public string ReverseKey
			{
				get { return reverseKeyProperty.Get (actualControlGroup); }
				set { reverseKeyProperty.Set (actualControlGroup, value); }
			}

			public float GroupSpeedFactor
			{
				get { return groupSpeedFactorProperty.Get (actualControlGroup); }
				set { groupSpeedFactorProperty.Set (actualControlGroup, value); }
			}

			public bool Expanded
			{
				get { return expandedProperty.Get (actualControlGroup); }
				set { expandedProperty.Set (actualControlGroup, value); }
			}

			public int MovingDirection
			{
				get { return movingDirectionProperty (actualControlGroup); }
			}

			public bool AdvancedMode
			{
				get { return advancedModeProperty.Get (actualControlGroup); }
				set { advancedModeProperty.Set (actualControlGroup, value); }
			}

			public float TotalElectricChargeRequirement
			{
				get { return totalElectricChargeRequirementProperty (actualControlGroup); }
			}

			public bool BuildAid
			{
				get { return buildAidProperty.Get (actualControlGroup); }
				set { buildAidProperty.Set (actualControlGroup, value); }
			}

			public bool IKActive
			{
				get { return ikActiveProperty.Get (actualControlGroup); }
				set { ikActiveProperty.Set (actualControlGroup, value); }
			}

			private object ActualServos { get; set; }

			public IList<IServo> Servos
			{
				get { return ExtractServos (ActualServos); }
			}

			public void MoveRight()
			{
				moveRightMethod (actualControlGroup);
			}

			public void MoveLeft()
			{
				moveLeftMethod (actualControlGroup);
			}

			public void MoveCenter()
			{
				moveCenterMethod (actualControlGroup);
			}

			public void MoveNextPreset()
			{
				moveNextPresetMethod (actualControlGroup);
			}

			public void MovePrevPreset()
			{
				movePrevPresetMethod (actualControlGroup);
			}

			public void Stop()
			{
				stopMethod (actualControlGroup);
			}

			private IList<IServo> ExtractServos (object actualServos)
			{
				var listToReturn = new List<IServo>();

				if(actualServos == null)
					return listToReturn;

				try {
					// Skip a servo listed twice through a duplicate groupName token
					var seen = new HashSet<IServo> ();
					foreach(var item in (IList)actualServos) {
						var servo = new IRServo (item);
						if (seen.Add (servo))
							listToReturn.Add(servo);
					}
				} catch (Exception ex) {
					LogFormatted("Error extracting from actualServos: {0}", ex.Message);
				}
				return listToReturn;
			}

			public bool Equals (IServoGroup other)
			{
				var controlGroup = other as IRServoGroup;
				return controlGroup != null && Equals (controlGroup);
			}
		}

		public class IRServo : IServo
		{
			private static Field<object, string> nameProperty;
			private static Func<object, uint> UIDProperty;
			private static Func<object, Part> HostPartProperty;
			private static Action<object, bool> highlightProperty;

			private static Field<object, float> minPositionProperty;
			private static Field<object, float> maxPositionProperty;
			private static Func<object, bool> isFreeMovingProperty;

			private static Field<object, float> minPositionLimitProperty;
			private static Field<object, float> maxPositionLimitProperty;

			private static Field<object, float> forceLimitProperty;
			private static Field<object, float> accelerationLimitProperty;
			private static Field<object, float> speedLimitProperty;
			private static Field<object, float> defaultSpeedProperty;

			private static Func<object, bool> isMovingProperty;
			private static Field<object, bool> isLockedProperty;
			private static Field<object, bool> isInvertedProperty;

			private static Func<object, float> commandedPositionProperty;
			private static Func<object, float> commandedSpeedProperty;

			private static Func<object, float> positionProperty;

			private static Field<object, string> forwardKeyProperty;
			private static Field<object, string> reverseKeyProperty;

			private static Func<object, float> targetPositionProperty;
			private static Func<object, float> targetSpeedProperty;
			private static Func<object, float> defaultPositionProperty;
			private static Func<object, float> maxForceProperty;
			private static Func<object, float> maxAccelerationProperty;
			private static Func<object, float> maxSpeedProperty;
			private static Func<object, float> electricChargeRequiredProperty;
			private static Field<object, float> springPowerProperty;
			private static Field<object, float> dampingPowerProperty;
			private static Field<object, float> rotorAccelerationProperty;
			private static Field<object, bool> isLimitedProperty;
			private static Func<object, bool> isRotationalProperty;
			private static Func<object, bool> isServoProperty;
			private static Func<object, bool> canHaveLimitsProperty;
			private static Func<object, bool> hasSpringProperty;
			private static Func<object, bool> isRunningProperty;
			private static Func<object, int> modeProperty;
			private static Func<object, IList<float>> presetPositionsProperty;
			private static Func<object, object> presetsProperty;
			private static Action<object, float> presetAddMethod;
			private static Action<object, int> presetRemoveAtMethod;
			private static Action<object, IComparer<float>> presetSortMethod;

			private static Action<object, float> moveLeftMethod;
			private static Action<object, float> moveCenterMethod;
			private static Action<object, float> moveRightMethod;
			private static Action<object, float, float> moveToMethod;
			private static Action<object> stopMethod;

			public IRServo(object s)
			{
				actualServo = s;
				EnsureBound();
			}

			// The lookups below depend on the mod's servo type alone, so they are made once
			// for the assembly rather than for each servo wrapped. A servo is wrapped every
			// time one is listed, and there are around forty lookups.
			private static bool bound;

			internal static void Unbind()
			{
				bound = false;
			}

			private static void EnsureBound()
			{
				if(bound)
					return;
				FindProperties();
				FindMethods();
				bound = true;
			}

			private static void FindProperties()
			{
				nameProperty = Accessor.Field<object, string> (IRServoType, "Name");
				UIDProperty = Accessor.Getter<object, uint> (IRServoType, "UID");
				HostPartProperty = Accessor.Getter<object, Part> (IRServoType, "HostPart");
				highlightProperty = Accessor.Setter<object, bool> (IRServoType, "Highlight");

				minPositionProperty = Accessor.Field<object, float> (IRServoType, "MinPosition");
				maxPositionProperty = Accessor.Field<object, float> (IRServoType, "MaxPosition");
				isFreeMovingProperty = Accessor.Getter<object, bool> (IRServoType, "IsFreeMoving");

				minPositionLimitProperty = Accessor.Field<object, float> (IRServoType, "MinPositionLimit");
				maxPositionLimitProperty = Accessor.Field<object, float> (IRServoType, "MaxPositionLimit");

				forceLimitProperty = Accessor.Field<object, float> (IRServoType, "ForceLimit");
				accelerationLimitProperty = Accessor.Field<object, float> (IRServoType, "AccelerationLimit");
				speedLimitProperty = Accessor.Field<object, float> (IRServoType, "SpeedLimit");
				defaultSpeedProperty = Accessor.Field<object, float> (IRServoType, "DefaultSpeed");

				isMovingProperty = Accessor.Getter<object, bool> (IRServoType, "IsMoving");
				isLockedProperty = Accessor.Field<object, bool> (IRServoType, "IsLocked");
				isInvertedProperty = Accessor.Field<object, bool> (IRServoType, "IsInverted");

				commandedPositionProperty = Accessor.Getter<object, float> (IRServoType, "CommandedPosition");
				commandedSpeedProperty = Accessor.Getter<object, float> (IRServoType, "CommandedSpeed");

				positionProperty = Accessor.Getter<object, float> (IRServoType, "Position");

				forwardKeyProperty = Accessor.Field<object, string> (IRServoType, "ForwardKey");
				reverseKeyProperty = Accessor.Field<object, string> (IRServoType, "ReverseKey");

				targetPositionProperty = Accessor.Getter<object, float> (IRServoType, "TargetPosition");
				targetSpeedProperty = Accessor.Getter<object, float> (IRServoType, "TargetSpeed");
				defaultPositionProperty = Accessor.Getter<object, float> (IRServoType, "DefaultPosition");
				maxForceProperty = Accessor.Getter<object, float> (IRServoType, "MaxForce");
				maxAccelerationProperty = Accessor.Getter<object, float> (IRServoType, "MaxAcceleration");
				maxSpeedProperty = Accessor.Getter<object, float> (IRServoType, "MaxSpeed");
				electricChargeRequiredProperty = Accessor.Getter<object, float> (IRServoType, "ElectricChargeRequired");
				springPowerProperty = Accessor.Field<object, float> (IRServoType, "SpringPower");
				dampingPowerProperty = Accessor.Field<object, float> (IRServoType, "DampingPower");
				rotorAccelerationProperty = Accessor.Field<object, float> (IRServoType, "RotorAcceleration");
				isLimitedProperty = Accessor.Field<object, bool> (IRServoType, "IsLimited");
				isRotationalProperty = Accessor.Getter<object, bool> (IRServoType, "IsRotational");
				isServoProperty = Accessor.Getter<object, bool> (IRServoType, "IsServo");
				canHaveLimitsProperty = Accessor.Getter<object, bool> (IRServoType, "CanHaveLimits");
				hasSpringProperty = Accessor.Getter<object, bool> (IRServoType, "HasSpring");
				isRunningProperty = Accessor.Getter<object, bool> (IRServoType, "IsRunning");
				modeProperty = Accessor.Getter<object, int> (IRServoType, "Mode");
				presetPositionsProperty = Accessor.Getter<object, IList<float>> (IRServoType, "PresetPositions");
				presetsProperty = Accessor.Getter<object, object> (IRServoType, "Presets");
				var presetableType = IRServoType.Assembly.GetType ("InfernalRobotics_v3.Interfaces.IPresetable");
				if (presetableType != null) {
					presetAddMethod = Accessor.Method<Action<object, float>> (presetableType, "Add");
					presetRemoveAtMethod = Accessor.Method<Action<object, int>> (presetableType, "RemoveAt");
					presetSortMethod = Accessor.Method<Action<object, IComparer<float>>> (presetableType, "Sort");
				}
			}

			private static void FindMethods()
			{
				moveLeftMethod = Accessor.Method<Action<object, float>> (IRServoType, "MoveLeft");
				moveCenterMethod = Accessor.Method<Action<object, float>> (IRServoType, "MoveCenter");
				moveRightMethod = Accessor.Method<Action<object, float>> (IRServoType, "MoveRight");
				moveToMethod = Accessor.Method<Action<object, float, float>> (IRServoType, "MoveTo");
				stopMethod = Accessor.Method<Action<object>> (IRServoType, "Stop");
			}

			private readonly object actualServo;


			public string Name
			{
				get { return nameProperty.Get (actualServo); }
				set { nameProperty.Set (actualServo, value); }
			}

			public uint UID
			{
				get { return UIDProperty (actualServo); }
			}

			public Part HostPart
			{
				get { return HostPartProperty (actualServo); }
			}

			public bool Highlight
			{
				//get { return (bool)HighlightProperty.GetValue(actualServo, null); }
				set { highlightProperty (actualServo, value); }
			}

			public float MinPosition
			{
				get { return minPositionProperty.Get (actualServo); }
				set { minPositionProperty.Set (actualServo, value); }
			}

			public float MaxPosition
			{
				get { return maxPositionProperty.Get (actualServo); }
				set { maxPositionProperty.Set (actualServo, value); }
			}

			public bool IsFreeMoving
			{
				get { return isFreeMovingProperty (actualServo); }
			}

			public float MinPositionLimit
			{
				get { return minPositionLimitProperty.Get (actualServo); }
				set { minPositionLimitProperty.Set (actualServo, value); }
			}

			public float MaxPositionLimit
			{
				get { return maxPositionLimitProperty.Get (actualServo); }
				set { maxPositionLimitProperty.Set (actualServo, value); }
			}

			public float ForceLimit
			{
				get { return forceLimitProperty.Get (actualServo); }
				set { forceLimitProperty.Set (actualServo, value); }
			}

			public float AccelerationLimit
			{
				get { return accelerationLimitProperty.Get (actualServo); }
				set { accelerationLimitProperty.Set (actualServo, value); }
			}

			public float SpeedLimit
			{
				get { return speedLimitProperty.Get (actualServo); }
				set { speedLimitProperty.Set (actualServo, value); }
			}

			public float DefaultSpeed
			{
				get { return defaultSpeedProperty.Get (actualServo); }
				set { defaultSpeedProperty.Set (actualServo, value); }
			}

			public bool IsMoving
			{
				get { return isMovingProperty (actualServo); }
			}

			public bool IsLocked
			{
				get { return isLockedProperty.Get (actualServo); }
				set { isLockedProperty.Set (actualServo, value); }
			}

			public bool IsInverted
			{
				get { return isInvertedProperty.Get (actualServo); }
				set { isInvertedProperty.Set (actualServo, value); }
			}

			public float CommandedPosition
			{
				get { return commandedPositionProperty (actualServo); }
			}

			public float CommandedSpeed
			{
				get { return commandedSpeedProperty (actualServo); }
			}

			public float Position
			{
				get { return positionProperty (actualServo); }
			}

			public float TargetPosition
			{
				get { return targetPositionProperty (actualServo); }
			}

			public float TargetSpeed
			{
				get { return targetSpeedProperty (actualServo); }
			}

			public float DefaultPosition
			{
				get { return defaultPositionProperty (actualServo); }
			}

			public float MaxForce
			{
				get { return maxForceProperty (actualServo); }
			}

			public float MaxAcceleration
			{
				get { return maxAccelerationProperty (actualServo); }
			}

			public float MaxSpeed
			{
				get { return maxSpeedProperty (actualServo); }
			}

			public float ElectricChargeRequired
			{
				get { return electricChargeRequiredProperty (actualServo); }
			}

			public float SpringPower
			{
				get { return springPowerProperty.Get (actualServo); }
				set { springPowerProperty.Set (actualServo, value); }
			}

			public float DampingPower
			{
				get { return dampingPowerProperty.Get (actualServo); }
				set { dampingPowerProperty.Set (actualServo, value); }
			}

			public float RotorAcceleration
			{
				get { return rotorAccelerationProperty.Get (actualServo); }
				set { rotorAccelerationProperty.Set (actualServo, value); }
			}

			public bool IsLimited
			{
				get { return isLimitedProperty.Get (actualServo); }
				set { isLimitedProperty.Set (actualServo, value); }
			}

			public bool IsRotational
			{
				get { return isRotationalProperty (actualServo); }
			}

			public bool IsServo
			{
				get { return isServoProperty (actualServo); }
			}

			public bool CanHaveLimits
			{
				get { return canHaveLimitsProperty (actualServo); }
			}

			public bool HasSpring
			{
				get { return hasSpringProperty (actualServo); }
			}

			public bool IsRunning
			{
				get { return isRunningProperty (actualServo); }
			}

			// IR-Next's ModeType enum: servo = 1, rotor = 2. Returned as its integer value; the
			// service maps it to the ServoMode enum.
			public int Mode
			{
				get { return modeProperty (actualServo); }
			}

			public IList<float> PresetPositions
			{
				get { return presetPositionsProperty (actualServo); }
			}

			public void AddPreset(float position)
			{
				presetAddMethod (presetsProperty (actualServo), position);
			}

			public void RemovePresetAt(int index)
			{
				presetRemoveAtMethod (presetsProperty (actualServo), index);
			}

			public void SortPresets()
			{
				presetSortMethod (presetsProperty (actualServo), null);
			}

			// kRPC-local patch (diverges from the upstream vendored IRWrapper): IR-Next's
			// IServo.MoveLeft/MoveCenter/MoveRight take a target speed, where older IR versions
			// took no argument. Pass the servo's default speed to preserve the original
			// no-argument behaviour. The group-level MoveLeft/MoveRight remain parameterless.
			public void MoveLeft()
			{
				moveLeftMethod (actualServo, DefaultSpeed);
			}

			public void MoveCenter()
			{
				moveCenterMethod (actualServo, DefaultSpeed);
			}

			public void MoveRight()
			{
				moveRightMethod (actualServo, DefaultSpeed);
			}
			public void MoveTo(float position, float speed)
			{
				moveToMethod (actualServo, position, speed);
			}

			public void Stop()
			{
				stopMethod (actualServo);
			}

			public string ForwardKey
			{
				get { return forwardKeyProperty.Get (actualServo); }
				set { forwardKeyProperty.Set (actualServo, value); }
			}

			public string ReverseKey
			{
				get { return reverseKeyProperty.Get (actualServo); }
				set { reverseKeyProperty.Set (actualServo, value); }
			}

			protected bool Equals (IRServo other)
			{
				return Equals(actualServo, other.actualServo);
			}

			public bool Equals(IServo other)
			{
				var servo = other as IRServo;
				return servo != null && Equals (servo);
			}

			public override bool Equals(object o)
			{
				var servo = o as IRServo;
				return servo != null && actualServo.Equals (servo.actualServo);
			}

			public override int GetHashCode()
			{
				return (actualServo != null ? actualServo.GetHashCode() : 0);
			}

			public static bool operator == (IRServo left, IRServo right)
			{
				return Equals (left, right);
			}

			public static bool operator != (IRServo left, IRServo right)
			{
				return !Equals(left, right);
			}
		}

		#endregion Private Implementation

		#region Non-active vessel support (kRPC-local)

		// Wrap the IR assembly once, the first time it is needed (its types resolve as soon
		// as the mod is loaded; the singleton once the flight scene is up). This must gate on
		// isWrapped, NOT APIReady: APIReady reflects IR's Controller.Ready, which is only true
		// when the *active* vessel has servos (servosState is populated from the active vessel
		// alone). Gating on APIReady would re-run the full assembly-wide reflection scan in
		// InitWrapper on every RPC whenever the active vessel has no IR parts - exactly the
		// non-active-vessel case this whole path exists for - stalling the physics frame each
		// poll and making the controlled servo stutter. APIReady is still returned so callers
		// correctly fall through to the synthesized (non-active) path when the Controller does
		// not hold the requested vessel.
		private static bool EnsureReady ()
		{
			if (AssemblyExists && !isWrapped)
				InitWrapper ();
			return APIReady;
		}

		// The module that makes a part a servo. IServo.UID is the part's craft id, so the mod
		// names a servo by its part, and a part carries at most one.
		internal const string ServoModuleName = "ModuleIRServo_v3";

		// The servo on a given part, or null if it has none. This is what an object standing
		// for a servo resolves through, so that it finds the servo the game has now rather
		// than holding the one it was built from, which belongs to a part and a game state
		// that the game can replace.
		internal static IServo ServoOnPart (Part part)
		{
			if(part == null)
				return null;
			EnsureReady ();
			if(!isWrapped)
				return null;
			var modules = part.Modules;
			for(int i = 0; i < modules.Count; i++)
			{
				var module = modules [i];
				if(module != null && module.moduleName == ServoModuleName)
					return new IRServo (module);
			}
			return null;
		}

		// Servos for a given vessel. IR's Controller only tracks the active vessel, so prefer
		// its data (full fidelity) when it holds the requested vessel, and otherwise fall back
		// to enumerating the vessel's servo modules directly - which works for any loaded,
		// off-rails vessel.
		internal static IList<IServo> ServosForVessel (Vessel vessel)
		{
			if (EnsureReady ()) {
				var fromController = new List<IServo> ();
				var seen = new HashSet<IServo> ();
				foreach (var group in IRController.ServoGroups)
					if (group.Vessel != null && group.Vessel.id == vessel.id)
						foreach (var servo in group.Servos)
							if (seen.Add (servo))
								fromController.Add (servo);
				if (fromController.Count > 0)
					return fromController;
			}
			return SynthesizeServos (vessel);
		}

		// Servo groups for a given vessel; same active-vessel-first strategy as ServosForVessel.
		internal static IList<IServoGroup> ServoGroupsForVessel (Vessel vessel)
		{
			if (EnsureReady ()) {
				var fromController = new List<IServoGroup> ();
				foreach (var group in IRController.ServoGroups)
					if (group.Vessel != null && group.Vessel.id == vessel.id)
						fromController.Add (group);
				if (fromController.Count > 0)
					return fromController;
			}
			return SynthesizeServoGroups (vessel);
		}

		private static IList<object> RawServos (Vessel vessel)
		{
			var result = new List<object> ();
			if (ToServos == null)
				return result;
			var servos = ToServos (vessel) as IEnumerable;
			if (servos == null)
				return result;
			foreach (var servo in servos)
				result.Add (servo);
			return result;
		}

		private static IList<IServo> SynthesizeServos (Vessel vessel)
		{
			var result = new List<IServo> ();
			foreach (var servo in RawServos (vessel))
				result.Add (new IRServo (servo));
			return result;
		}

		private static IList<IServoGroup> SynthesizeServoGroups (Vessel vessel)
		{
			var result = new List<IServoGroup> ();
			var order = new List<string> ();
			var byGroup = new Dictionary<string, List<KeyValuePair<int, object>>> ();
			foreach (var servo in RawServos (vessel)) {
				// A servo's groupName encodes every group it belongs to, matching IR's own
				// parsing in Controller.RebuildServoGroupsFlight: memberships are separated
				// by '|', and each membership is "<group name>;<ordering index>".
				var raw = GroupName != null ? GroupName.Get (servo) : null;
				if (string.IsNullOrEmpty (raw))
					continue;
				// IR appends another token to groupName on every vessel activation, without
				// removing the previous one. We keep the first token per group, as the fix in
				// https://github.com/meirumeiru/InfernalRobotics/pull/5 does.
				var servoGroups = new HashSet<string> ();
				foreach (var membership in raw.Split ('|')) {
					var parts = membership.Split (';');
					var name = parts [0];
					if (!servoGroups.Add (name))
						continue;
					int index;
					if (parts.Length < 2 || !int.TryParse (parts [1], out index))
						index = int.MaxValue;
					List<KeyValuePair<int, object>> members;
					if (!byGroup.TryGetValue (name, out members)) {
						members = new List<KeyValuePair<int, object>> ();
						byGroup [name] = members;
						order.Add (name);
					}
					members.Add (new KeyValuePair<int, object> (index, servo));
				}
			}
			foreach (var name in order) {
				// OrderBy is a stable sort, so servos with an equal (or absent) index keep
				// their discovery order within the group.
				var servos = byGroup [name].OrderBy (m => m.Key).Select (m => m.Value).ToList ();
				result.Add (new SynthesizedServoGroup (vessel, name, servos));
			}
			return result;
		}

		// A servo group reconstructed by kRPC for a non-active (but loaded) vessel, backed
		// directly by the vessel's servo modules and grouped by their groupName. Movement is
		// delegated to the member servos, which drive themselves in FixedUpdate on any loaded,
		// off-rails vessel. Group state that lives only in IR's Controller for the active
		// vessel (preset lists, forward/reverse keys, group speed factor, UI expanded flag)
		// has no backing store here and throws when accessed.
		private class SynthesizedServoGroup : IServoGroup
		{
			private readonly Vessel vessel;
			private readonly IList<object> rawServos;
			private string name;

			public SynthesizedServoGroup (Vessel groupVessel, string groupName, IList<object> groupServos)
			{
				vessel = groupVessel;
				name = groupName;
				rawServos = groupServos;
			}

			private IList<IServo> WrapServos ()
			{
				var servos = new List<IServo> ();
				foreach (var servo in rawServos)
					servos.Add (new IRServo (servo));
				return servos;
			}

			public string Name
			{
				get { return name; }
				set {
					// Rewrite only this group's membership token on each servo, preserving the
					// "<name>;<index>" encoding and any other groups the servo belongs to.
					if (GroupName != null) {
						foreach (var servo in rawServos) {
							var raw = GroupName.Get (servo) ?? string.Empty;
							var memberships = raw.Split ('|');
							for (int i = 0; i < memberships.Length; i++) {
								var parts = memberships [i].Split (';');
								if (parts [0] == name) {
									parts [0] = value;
									memberships [i] = string.Join (";", parts);
								}
							}
							GroupName.Set (servo, string.Join ("|", memberships));
						}
					}
					name = value;
				}
			}

			public Vessel Vessel
			{
				get { return vessel; }
			}

			public IList<IServo> Servos
			{
				get { return WrapServos (); }
			}

			public void MoveLeft ()
			{
				foreach (var servo in WrapServos ())
					servo.MoveLeft ();
			}

			public void MoveCenter ()
			{
				foreach (var servo in WrapServos ())
					servo.MoveCenter ();
			}

			public void MoveRight ()
			{
				foreach (var servo in WrapServos ())
					servo.MoveRight ();
			}

			public void Stop ()
			{
				foreach (var servo in WrapServos ())
					servo.Stop ();
			}

			private static Exception NotAvailable ()
			{
				return new InvalidOperationException (
					"This member is not available for a servo group on a non-active vessel. " +
					"Such groups support Name, Vessel and Servos, movement (MoveLeft, MoveRight, " +
					"MoveCenter, Stop) and full per-servo control; preset, key, speed-factor, " +
					"expanded, moving-direction, advanced-mode, electric-charge, build-aid and " +
					"IK state are only available for the active vessel.");
			}

			public float GroupSpeedFactor
			{
				get { throw NotAvailable (); }
				set { throw NotAvailable (); }
			}

			public bool Expanded
			{
				get { throw NotAvailable (); }
				set { throw NotAvailable (); }
			}

			public int MovingDirection
			{
				get { throw NotAvailable (); }
			}

			public bool AdvancedMode
			{
				get { throw NotAvailable (); }
				set { throw NotAvailable (); }
			}

			public float TotalElectricChargeRequirement
			{
				get { throw NotAvailable (); }
			}

			public bool BuildAid
			{
				get { throw NotAvailable (); }
				set { throw NotAvailable (); }
			}

			public bool IKActive
			{
				get { throw NotAvailable (); }
				set { throw NotAvailable (); }
			}

			public void MoveNextPreset ()
			{
				throw NotAvailable ();
			}

			public void MovePrevPreset ()
			{
				throw NotAvailable ();
			}

			public string ForwardKey
			{
				get { throw NotAvailable (); }
				set { throw NotAvailable (); }
			}

			public string ReverseKey
			{
				get { throw NotAvailable (); }
				set { throw NotAvailable (); }
			}

			public bool Equals (IServoGroup other)
			{
				var group = other as SynthesizedServoGroup;
				return group != null && vessel == group.vessel && name == group.name;
			}
		}

		#endregion Non-active vessel support (kRPC-local)

		#region API Contract

		public interface IRAPI
		{
			bool Ready { get; }

			IList<IServoGroup> ServoGroups { get; }
		}

		public interface IServoGroup : IEquatable<IServoGroup>
		{
			string Name { get; set; }

			//can only be used in Flight, null checking is mandatory
			Vessel Vessel { get; }

			float GroupSpeedFactor { get; set; }

			bool Expanded { get; set; }

			int MovingDirection { get; }
			bool AdvancedMode { get; set; }
			float TotalElectricChargeRequirement { get; }
			bool BuildAid { get; set; }
			bool IKActive { get; set; }

			IList<IServo> Servos { get; }

			void MoveLeft();
			void MoveCenter();
			void MoveRight();

			void MoveNextPreset();
			void MovePrevPreset();

			void Stop();

			string ForwardKey { get; set; }
			string ReverseKey { get; set; }
		}

		public interface IServo : IEquatable<IServo>
		{
			string Name { get; set; }

			uint UID { get; }

			Part HostPart { get; }

			bool Highlight { set; }

			float MinPosition { get; set; }
			float MaxPosition { get; set; }

			bool IsFreeMoving { get; }

			float MinPositionLimit { get; set; }
			float MaxPositionLimit { get; set; }

			float ForceLimit { get; set;}

			float AccelerationLimit { get; set; }

			float SpeedLimit { get; set; }

			float DefaultSpeed { get; set; }

			bool IsMoving { get; }
			bool IsLocked { get; set; }

			bool IsInverted { get; set; }

			float CommandedPosition { get; }

			float CommandedSpeed { get; }

			float Position { get; }

			float TargetPosition { get; }
			float TargetSpeed { get; }
			float DefaultPosition { get; }
			float MaxForce { get; }
			float MaxAcceleration { get; }
			float MaxSpeed { get; }
			float ElectricChargeRequired { get; }
			float SpringPower { get; set; }
			float DampingPower { get; set; }
			float RotorAcceleration { get; set; }
			bool IsLimited { get; set; }
			bool IsRotational { get; }
			bool IsServo { get; }
			bool CanHaveLimits { get; }
			bool HasSpring { get; }
			bool IsRunning { get; }
			int Mode { get; }
			IList<float> PresetPositions { get; }

			void AddPreset (float position);
			void RemovePresetAt (int index);
			void SortPresets ();

			void MoveLeft();
			void MoveCenter();
			void MoveRight();

			void MoveTo (float position, float speed);

			void Stop();

			string ForwardKey { get; set; }
			string ReverseKey { get; set; }

			bool Equals (object o);

			int GetHashCode();
		}

		#endregion API Contract

		#region Logging Stuff

		/// <summary>
		/// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
		/// </summary>
		/// <param name="message">Text to be printed - can be formatted as per string.format</param>
		/// <param name="strParams">Objects to feed into a string.format</param>
		[System.Diagnostics.Conditional ("DEBUG")]
		internal static void LogFormatted_DebugOnly (string message, params object [] strParams)
		{
			LogFormatted(message, strParams);
		}

		/// <summary>
		/// Some Structured logging to the debug file
		/// </summary>
		/// <param name="message">Text to be printed - can be formatted as per string.format</param>
		/// <param name="strParams">Objects to feed into a string.format</param>
		internal static void LogFormatted(string message, params object [] strParams)
		{
			var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
			var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			message = string.Format (CultureInfo.InvariantCulture, message, strParams);

			string strMessageLine = declaringType != null ?
				string.Format (CultureInfo.InvariantCulture, "{0},{2}-{3},{1}", DateTime.Now, message, assemblyName, declaringType.Name) :
				string.Format (CultureInfo.InvariantCulture, "{0},{2}-NO-DECLARE,{1}", DateTime.Now, message, assemblyName);

			UnityEngine.Debug.Log (strMessageLine);
		}

		#endregion Logging Stuff
	}
}