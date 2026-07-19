#pragma warning disable 1591

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
		protected internal static MethodInfo ToServosMethod { get; set; }
		protected internal static FieldInfo GroupNameField { get; set; }

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
			ToServosMethod = ServoHelperType != null
				? ServoHelperType.GetMethod ("ToServos", new [] { typeof(Vessel) })
				: null;
			GroupNameField = ModuleServoType != null
				? ModuleServoType.GetField ("groupName")
				: null;

			LogFormatted("Got Assembly Types, grabbing Instance");

			try
			{
				var propertyInfo = IRControllerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);

				if(propertyInfo == null)
					LogFormatted("[IR Wrapper] Cannot find Instance Property");
				else
					ActualController = propertyInfo.GetValue(null, null);
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
			private PropertyInfo apiReady;
			private object actualServoGroups;

			public InfernalRoboticsAPI()
			{
				DetermineReady();
				BuildServoGroups();
			}

			private void BuildServoGroups()
			{
				var servoGroupsField = IRControllerType.GetField("ServoGroups");
				if(servoGroupsField == null)
					LogFormatted("Failed Getting ServoGroups fieldinfo");
				else if(IRWrapper.ActualController == null)
					LogFormatted("ServoController Instance not found");
				else
					actualServoGroups = servoGroupsField.GetValue(IRWrapper.ActualController);
			}

			private void DetermineReady()
			{
				LogFormatted("Getting APIReady Object");
				apiReady = IRControllerType.GetProperty("APIReady", BindingFlags.Public | BindingFlags.Static);
				LogFormatted("Success: " + (apiReady != null));
			}

			public bool Ready
			{
				get
				{
					if(apiReady == null || actualServoGroups == null)
						return false;

					return (bool)apiReady.GetValue(null, null);
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

			private PropertyInfo nameProperty;
			private PropertyInfo vesselProperty;
			private PropertyInfo expandedProperty;
			private PropertyInfo groupSpeedFactorProperty;

			private PropertyInfo forwardKeyProperty;
			private PropertyInfo reverseKeyProperty;

			private PropertyInfo movingDirectionProperty;
			private PropertyInfo advancedModeProperty;
			private PropertyInfo totalElectricChargeRequirementProperty;
			private PropertyInfo buildAidProperty;
			private PropertyInfo ikActiveProperty;

			private MethodInfo moveRightMethod;
			private MethodInfo moveLeftMethod;
			private MethodInfo moveCenterMethod;
			private MethodInfo moveNextPresetMethod;
			private MethodInfo movePrevPresetMethod;
			private MethodInfo stopMethod;

			public IRServoGroup (object cg)
			{
				actualControlGroup = cg;
				FindProperties();
				FindMethods();
			}

			private void FindProperties()
			{
				nameProperty = IRServoGroupType.GetProperty("Name");
				vesselProperty = IRServoGroupType.GetProperty("Vessel");
				forwardKeyProperty = IRServoGroupType.GetProperty("ForwardKey");
				reverseKeyProperty = IRServoGroupType.GetProperty("ReverseKey");
				groupSpeedFactorProperty = IRServoGroupType.GetProperty("GroupSpeedFactor");
				expandedProperty = IRServoGroupType.GetProperty("Expanded");
				movingDirectionProperty = IRServoGroupType.GetProperty("MovingDirection");
				advancedModeProperty = IRServoGroupType.GetProperty("AdvancedMode");
				totalElectricChargeRequirementProperty = IRServoGroupType.GetProperty("TotalElectricChargeRequirement");
				buildAidProperty = IRServoGroupType.GetProperty("BuildAid");
				ikActiveProperty = IRServoGroupType.GetProperty("IKActive");

				var servosProperty = IRServoGroupType.GetProperty("Servos");
				ActualServos = servosProperty.GetValue(actualControlGroup, null);
			}

			private void FindMethods()
			{
				moveRightMethod = IRServoGroupType.GetMethod ("MoveRight", BindingFlags.Public | BindingFlags.Instance);
				moveLeftMethod = IRServoGroupType.GetMethod ("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
				moveCenterMethod = IRServoGroupType.GetMethod ("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
				moveNextPresetMethod = IRServoGroupType.GetMethod ("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
				movePrevPresetMethod = IRServoGroupType.GetMethod ("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
				stopMethod = IRServoGroupType.GetMethod ("Stop", BindingFlags.Public | BindingFlags.Instance);
			}

			public string Name
			{
				get { return (string)nameProperty.GetValue(actualControlGroup, null); }
				set { nameProperty.SetValue (actualControlGroup, value, null); }
			}

			public Vessel Vessel
			{
				get { return vesselProperty != null ? (Vessel)vesselProperty.GetValue(actualControlGroup, null) : null; }
			}

			public string ForwardKey
			{
				get { return (string)forwardKeyProperty.GetValue(actualControlGroup, null); }
				set { forwardKeyProperty.SetValue (actualControlGroup, value, null); }
			}

			public string ReverseKey
			{
				get { return (string)reverseKeyProperty.GetValue(actualControlGroup, null); }
				set { reverseKeyProperty.SetValue (actualControlGroup, value, null); }
			}

			public float GroupSpeedFactor
			{
				get { return (float)groupSpeedFactorProperty.GetValue(actualControlGroup, null); }
				set { groupSpeedFactorProperty.SetValue (actualControlGroup, value, null); }
			}

			public bool Expanded
			{
				get { return (bool)expandedProperty.GetValue(actualControlGroup, null); }
				set { expandedProperty.SetValue (actualControlGroup, value, null); }
			}

			public int MovingDirection
			{
				get { return (int)movingDirectionProperty.GetValue(actualControlGroup, null); }
			}

			public bool AdvancedMode
			{
				get { return (bool)advancedModeProperty.GetValue(actualControlGroup, null); }
				set { advancedModeProperty.SetValue (actualControlGroup, value, null); }
			}

			public float TotalElectricChargeRequirement
			{
				get { return (float)totalElectricChargeRequirementProperty.GetValue(actualControlGroup, null); }
			}

			public bool BuildAid
			{
				get { return (bool)buildAidProperty.GetValue(actualControlGroup, null); }
				set { buildAidProperty.SetValue (actualControlGroup, value, null); }
			}

			public bool IKActive
			{
				get { return (bool)ikActiveProperty.GetValue(actualControlGroup, null); }
				set { ikActiveProperty.SetValue (actualControlGroup, value, null); }
			}

			private object ActualServos { get; set; }

			public IList<IServo> Servos
			{
				get { return ExtractServos (ActualServos); }
			}

			public void MoveRight()
			{
				moveRightMethod.Invoke(actualControlGroup, new object [] { });
			}

			public void MoveLeft()
			{
				moveLeftMethod.Invoke(actualControlGroup, new object [] { });
			}

			public void MoveCenter()
			{
				moveCenterMethod.Invoke(actualControlGroup, new object [] { });
			}

			public void MoveNextPreset()
			{
				moveNextPresetMethod.Invoke(actualControlGroup, new object [] { });
			}

			public void MovePrevPreset()
			{
				movePrevPresetMethod.Invoke(actualControlGroup, new object [] { });
			}

			public void Stop()
			{
				stopMethod.Invoke (actualControlGroup, new object [] { });
			}

			private IList<IServo> ExtractServos (object actualServos)
			{
				var listToReturn = new List<IServo>();

				if(actualServos == null)
					return listToReturn;

				try {
					//iterate each "value" in the dictionary
					foreach(var item in (IList)actualServos)
						listToReturn.Add(new IRServo (item));
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
			private PropertyInfo nameProperty;
			private PropertyInfo UIDProperty;
			private PropertyInfo HostPartProperty;
			private PropertyInfo highlightProperty;

			private PropertyInfo minPositionProperty;
			private PropertyInfo maxPositionProperty;
			private PropertyInfo isFreeMovingProperty;

			private PropertyInfo minPositionLimitProperty;
			private PropertyInfo maxPositionLimitProperty;

			private PropertyInfo forceLimitProperty;
			private PropertyInfo accelerationLimitProperty;
			private PropertyInfo speedLimitProperty;
			private PropertyInfo defaultSpeedProperty;

			private PropertyInfo isMovingProperty;
			private PropertyInfo isLockedProperty;
			private PropertyInfo isInvertedProperty;

			private PropertyInfo commandedPositionProperty;
			private PropertyInfo commandedSpeedProperty;

			private PropertyInfo positionProperty;

			private PropertyInfo forwardKeyProperty;
			private PropertyInfo reverseKeyProperty;

			private PropertyInfo targetPositionProperty;
			private PropertyInfo targetSpeedProperty;
			private PropertyInfo defaultPositionProperty;
			private PropertyInfo maxForceProperty;
			private PropertyInfo maxAccelerationProperty;
			private PropertyInfo maxSpeedProperty;
			private PropertyInfo electricChargeRequiredProperty;
			private PropertyInfo springPowerProperty;
			private PropertyInfo dampingPowerProperty;
			private PropertyInfo rotorAccelerationProperty;
			private PropertyInfo isLimitedProperty;
			private PropertyInfo isRotationalProperty;
			private PropertyInfo isServoProperty;
			private PropertyInfo canHaveLimitsProperty;
			private PropertyInfo hasSpringProperty;
			private PropertyInfo isRunningProperty;
			private PropertyInfo modeProperty;
			private PropertyInfo presetPositionsProperty;
			private PropertyInfo presetsProperty;
			private MethodInfo presetAddMethod;
			private MethodInfo presetRemoveAtMethod;
			private MethodInfo presetSortMethod;

			private MethodInfo moveLeftMethod;
			private MethodInfo moveCenterMethod;
			private MethodInfo moveRightMethod;
			private MethodInfo moveToMethod;
			private MethodInfo stopMethod;

			public IRServo(object s)
			{
				actualServo = s;

				FindProperties();
				FindMethods();
			}

			private void FindProperties()
			{
				nameProperty = IRServoType.GetProperty("Name");
				UIDProperty = IRServoType.GetProperty("UID");
				HostPartProperty = IRServoType.GetProperty("HostPart");
				highlightProperty = IRServoType.GetProperty("Highlight");

				minPositionProperty = IRServoType.GetProperty("MinPosition");
				maxPositionProperty = IRServoType.GetProperty("MaxPosition");
				isFreeMovingProperty = IRServoType.GetProperty("IsFreeMoving");

				minPositionLimitProperty = IRServoType.GetProperty("MinPositionLimit");
				maxPositionLimitProperty = IRServoType.GetProperty("MaxPositionLimit");

				forceLimitProperty = IRServoType.GetProperty("ForceLimit");
				accelerationLimitProperty = IRServoType.GetProperty("AccelerationLimit");
				speedLimitProperty = IRServoType.GetProperty("SpeedLimit");
				defaultSpeedProperty = IRServoType.GetProperty("DefaultSpeed");

				isMovingProperty = IRServoType.GetProperty("IsMoving");
				isLockedProperty = IRServoType.GetProperty("IsLocked");
				isInvertedProperty = IRServoType.GetProperty("IsInverted");

				commandedPositionProperty = IRServoType.GetProperty("CommandedPosition");
				commandedSpeedProperty = IRServoType.GetProperty("CommandedSpeed");

				positionProperty = IRServoType.GetProperty("Position");

				forwardKeyProperty = IRServoType.GetProperty("ForwardKey");
				reverseKeyProperty = IRServoType.GetProperty("ReverseKey");

				targetPositionProperty = IRServoType.GetProperty("TargetPosition");
				targetSpeedProperty = IRServoType.GetProperty("TargetSpeed");
				defaultPositionProperty = IRServoType.GetProperty("DefaultPosition");
				maxForceProperty = IRServoType.GetProperty("MaxForce");
				maxAccelerationProperty = IRServoType.GetProperty("MaxAcceleration");
				maxSpeedProperty = IRServoType.GetProperty("MaxSpeed");
				electricChargeRequiredProperty = IRServoType.GetProperty("ElectricChargeRequired");
				springPowerProperty = IRServoType.GetProperty("SpringPower");
				dampingPowerProperty = IRServoType.GetProperty("DampingPower");
				rotorAccelerationProperty = IRServoType.GetProperty("RotorAcceleration");
				isLimitedProperty = IRServoType.GetProperty("IsLimited");
				isRotationalProperty = IRServoType.GetProperty("IsRotational");
				isServoProperty = IRServoType.GetProperty("IsServo");
				canHaveLimitsProperty = IRServoType.GetProperty("CanHaveLimits");
				hasSpringProperty = IRServoType.GetProperty("HasSpring");
				isRunningProperty = IRServoType.GetProperty("IsRunning");
				modeProperty = IRServoType.GetProperty("Mode");
				presetPositionsProperty = IRServoType.GetProperty("PresetPositions");
				presetsProperty = IRServoType.GetProperty("Presets");
				if (presetsProperty != null) {
					var presetableType = presetsProperty.PropertyType;
					presetAddMethod = presetableType.GetMethod("Add");
					presetRemoveAtMethod = presetableType.GetMethod("RemoveAt");
					presetSortMethod = presetableType.GetMethod("Sort");
				}
			}

			private void FindMethods()
			{
				moveLeftMethod = IRServoType.GetMethod("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
				moveCenterMethod = IRServoType.GetMethod("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
				moveRightMethod = IRServoType.GetMethod("MoveRight", BindingFlags.Public | BindingFlags.Instance);
				moveToMethod = IRServoType.GetMethod("MoveTo", new [] { typeof(float), typeof(float) });
				stopMethod = IRServoType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
			}

			private readonly object actualServo;


			public string Name
			{
				get { return (string)nameProperty.GetValue(actualServo, null); }
				set { nameProperty.SetValue(actualServo, value, null); }
			}

			public uint UID
			{
				get { return (uint)UIDProperty.GetValue(actualServo, null); }
			}

			public Part HostPart
			{
				get { return (Part)HostPartProperty.GetValue(actualServo, null); }
			}

			public bool Highlight
			{
				//get { return (bool)HighlightProperty.GetValue(actualServo, null); }
				set { highlightProperty.SetValue(actualServo, value, null); }
			}

			public float MinPosition
			{
				get { return (float)minPositionProperty.GetValue(actualServo, null); }
				set { minPositionProperty.SetValue(actualServo, value, null); }
			}

			public float MaxPosition
			{
				get { return (float)maxPositionProperty.GetValue(actualServo, null); }
				set { maxPositionProperty.SetValue(actualServo, value, null); }
			}

			public bool IsFreeMoving
			{
				get { return (bool)isFreeMovingProperty.GetValue(actualServo, null); }
			}

			public float MinPositionLimit
			{
				get { return (float)minPositionLimitProperty.GetValue(actualServo, null); }
				set { minPositionLimitProperty.SetValue(actualServo, value, null); }
			}

			public float MaxPositionLimit
			{
				get { return (float)maxPositionLimitProperty.GetValue(actualServo, null); }
				set { maxPositionLimitProperty.SetValue(actualServo, value, null); }
			}

			public float ForceLimit
			{
				get { return (float)forceLimitProperty.GetValue(actualServo, null); }
				set { forceLimitProperty.SetValue(actualServo, value, null); }
			}

			public float AccelerationLimit
			{
				get { return (float)accelerationLimitProperty.GetValue(actualServo, null); }
				set { accelerationLimitProperty.SetValue(actualServo, value, null); }
			}

			public float SpeedLimit
			{
				get { return (float)speedLimitProperty.GetValue(actualServo, null); }
				set { speedLimitProperty.SetValue(actualServo, value, null); }
			}

			public float DefaultSpeed
			{
				get { return (float)defaultSpeedProperty.GetValue(actualServo, null); }
				set { defaultSpeedProperty.SetValue(actualServo, value, null); }
			}

			public bool IsMoving
			{
				get { return (bool)isMovingProperty.GetValue(actualServo, null); }
			}

			public bool IsLocked
			{
				get { return (bool)isLockedProperty.GetValue(actualServo, null); }
				set { isLockedProperty.SetValue(actualServo, value, null); }
			}

			public bool IsInverted
			{
				get { return (bool)isInvertedProperty.GetValue(actualServo, null); }
				set { isInvertedProperty.SetValue(actualServo, value, null); }
			}

			public float CommandedPosition
			{
				get { return (float)commandedPositionProperty.GetValue(actualServo, null); }
			}

			public float CommandedSpeed
			{
				get { return (float)commandedSpeedProperty.GetValue(actualServo, null); }
			}

			public float Position
			{
				get { return (float)positionProperty.GetValue(actualServo, null); }
			}

			public float TargetPosition
			{
				get { return (float)targetPositionProperty.GetValue(actualServo, null); }
			}

			public float TargetSpeed
			{
				get { return (float)targetSpeedProperty.GetValue(actualServo, null); }
			}

			public float DefaultPosition
			{
				get { return (float)defaultPositionProperty.GetValue(actualServo, null); }
			}

			public float MaxForce
			{
				get { return (float)maxForceProperty.GetValue(actualServo, null); }
			}

			public float MaxAcceleration
			{
				get { return (float)maxAccelerationProperty.GetValue(actualServo, null); }
			}

			public float MaxSpeed
			{
				get { return (float)maxSpeedProperty.GetValue(actualServo, null); }
			}

			public float ElectricChargeRequired
			{
				get { return (float)electricChargeRequiredProperty.GetValue(actualServo, null); }
			}

			public float SpringPower
			{
				get { return (float)springPowerProperty.GetValue(actualServo, null); }
				set { springPowerProperty.SetValue(actualServo, value, null); }
			}

			public float DampingPower
			{
				get { return (float)dampingPowerProperty.GetValue(actualServo, null); }
				set { dampingPowerProperty.SetValue(actualServo, value, null); }
			}

			public float RotorAcceleration
			{
				get { return (float)rotorAccelerationProperty.GetValue(actualServo, null); }
				set { rotorAccelerationProperty.SetValue(actualServo, value, null); }
			}

			public bool IsLimited
			{
				get { return (bool)isLimitedProperty.GetValue(actualServo, null); }
				set { isLimitedProperty.SetValue(actualServo, value, null); }
			}

			public bool IsRotational
			{
				get { return (bool)isRotationalProperty.GetValue(actualServo, null); }
			}

			public bool IsServo
			{
				get { return (bool)isServoProperty.GetValue(actualServo, null); }
			}

			public bool CanHaveLimits
			{
				get { return (bool)canHaveLimitsProperty.GetValue(actualServo, null); }
			}

			public bool HasSpring
			{
				get { return (bool)hasSpringProperty.GetValue(actualServo, null); }
			}

			public bool IsRunning
			{
				get { return (bool)isRunningProperty.GetValue(actualServo, null); }
			}

			// IR-Next's ModeType enum: servo = 1, rotor = 2. Returned as its integer value; the
			// service maps it to the ServoMode enum.
			public int Mode
			{
				get { return Convert.ToInt32(modeProperty.GetValue(actualServo, null)); }
			}

			public IList<float> PresetPositions
			{
				get { return (IList<float>)presetPositionsProperty.GetValue(actualServo, null); }
			}

			public void AddPreset(float position)
			{
				presetAddMethod.Invoke(presetsProperty.GetValue(actualServo, null), new object [] { position });
			}

			public void RemovePresetAt(int index)
			{
				presetRemoveAtMethod.Invoke(presetsProperty.GetValue(actualServo, null), new object [] { index });
			}

			public void SortPresets()
			{
				presetSortMethod.Invoke(presetsProperty.GetValue(actualServo, null), new object [] { null });
			}

			// kRPC-local patch (diverges from the upstream vendored IRWrapper): IR-Next's
			// IServo.MoveLeft/MoveCenter/MoveRight take a target speed, where older IR versions
			// took no argument. Pass the servo's default speed to preserve the original
			// no-argument behaviour. The group-level MoveLeft/MoveRight remain parameterless.
			public void MoveLeft()
			{
				moveLeftMethod.Invoke(actualServo, new object [] { DefaultSpeed });
			}

			public void MoveCenter()
			{
				moveCenterMethod.Invoke(actualServo, new object [] { DefaultSpeed });
			}

			public void MoveRight()
			{
				moveRightMethod.Invoke(actualServo, new object [] { DefaultSpeed });
			}
			public void MoveTo(float position, float speed)
			{
				moveToMethod.Invoke(actualServo, new object [] { position, speed });
			}

			public void Stop()
			{
				stopMethod.Invoke(actualServo, new object [] { });
			}

			public string ForwardKey
			{
				get { return (string)forwardKeyProperty.GetValue(actualServo, null); }
				set { forwardKeyProperty.SetValue(actualServo, value, null); }
			}

			public string ReverseKey
			{
				get { return (string)reverseKeyProperty.GetValue(actualServo, null); }
				set { reverseKeyProperty.SetValue(actualServo, value, null); }
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

		// Servos for a given vessel. IR's Controller only tracks the active vessel, so prefer
		// its data (full fidelity) when it holds the requested vessel, and otherwise fall back
		// to enumerating the vessel's servo modules directly - which works for any loaded,
		// off-rails vessel.
		internal static IList<IServo> ServosForVessel (Vessel vessel)
		{
			if (EnsureReady ()) {
				var fromController = new List<IServo> ();
				foreach (var group in IRController.ServoGroups)
					if (group.Vessel != null && group.Vessel.id == vessel.id)
						foreach (var servo in group.Servos)
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
			if (ToServosMethod == null)
				return result;
			var servos = ToServosMethod.Invoke (null, new object [] { vessel }) as IEnumerable;
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
				var raw = GroupNameField != null ? GroupNameField.GetValue (servo) as string : null;
				if (string.IsNullOrEmpty (raw))
					continue;
				foreach (var membership in raw.Split ('|')) {
					var parts = membership.Split (';');
					var name = parts [0];
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
					if (GroupNameField != null) {
						foreach (var servo in rawServos) {
							var raw = GroupNameField.GetValue (servo) as string ?? string.Empty;
							var memberships = raw.Split ('|');
							for (int i = 0; i < memberships.Length; i++) {
								var parts = memberships [i].Split (';');
								if (parts [0] == name) {
									parts [0] = value;
									memberships [i] = string.Join (";", parts);
								}
							}
							GroupNameField.SetValue (servo, string.Join ("|", memberships));
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
			message = string.Format (message, strParams);

			string strMessageLine = declaringType != null ?
				string.Format ("{0},{2}-{3},{1}", DateTime.Now, message, assemblyName, declaringType.Name) :
				string.Format ("{0},{2}-NO-DECLARE,{1}", DateTime.Now, message, assemblyName);

			UnityEngine.Debug.Log (strMessageLine);
		}

		#endregion Logging Stuff
	}
}