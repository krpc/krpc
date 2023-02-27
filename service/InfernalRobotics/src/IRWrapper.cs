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
		protected internal static Type IRMotorGroupType { get; set; }

		protected internal static Type IRServoType { get; set; }
		protected internal static Type IRMotorType { get; set; }

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

			IRMotorType = null;
			AssemblyLoader.loadedAssemblies.TypeOperation (t => {
				if(t.FullName == "InfernalRobotics_v3.Interfaces.IMotor") { IRMotorType = t; } });

			if(IRMotorType == null)
			{
				LogFormatted("[IR Wrapper] Failed to grab Motor Type");
				return false;
			}

			IRServoType = null;
			AssemblyLoader.loadedAssemblies.TypeOperation (t => {
				if(t.FullName == "InfernalRobotics_v3.Interfaces.IServo") { IRServoType = t; } });

			if(IRServoType == null)
			{
				LogFormatted("[IR Wrapper] Failed to grab Servo Type");
				return false;
			}

			IRMotorGroupType = null;
			AssemblyLoader.loadedAssemblies.TypeOperation (t => {
				if(t.FullName == "InfernalRobotics_v3.Command.IMotorGroup") { IRMotorGroupType = t; } });

			if(IRMotorGroupType == null)
			{
				LogFormatted("[IR Wrapper] Failed to grab MotorGroup Type");
				return false;
			}

			IRServoGroupType = null;
			AssemblyLoader.loadedAssemblies.TypeOperation (t => {
				if(t.FullName == "InfernalRobotics_v3.Command.IServoGroup") { IRServoGroupType = t; } });

			if(IRServoGroupType == null)
			{
				LogFormatted("[IR Wrapper] Failed to grab ServoGroup Type");
				return false;
			}

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

			private PropertyInfo torqueLimitProperty;
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

			private MethodInfo moveLeftMethod;
			private MethodInfo moveCenterMethod;
			private MethodInfo moveRightMethod;
			private MethodInfo moveNextPresetMethod;
			private MethodInfo movePrevPresetMethod;
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

				torqueLimitProperty = IRMotorType.GetProperty("TorqueLimit");
				accelerationLimitProperty = IRMotorType.GetProperty("AccelerationLimit");
				speedLimitProperty = IRMotorType.GetProperty("SpeedLimit");
				defaultSpeedProperty = IRMotorType.GetProperty("DefaultSpeed");

				isMovingProperty = IRServoType.GetProperty("IsMoving");
				isLockedProperty = IRServoType.GetProperty("IsLocked");
				isInvertedProperty = IRMotorType.GetProperty("IsInverted");

				commandedPositionProperty = IRMotorType.GetProperty("CommandedPosition");
				commandedSpeedProperty = IRMotorType.GetProperty("CommandedSpeed");

				positionProperty = IRServoType.GetProperty("Position");

				forwardKeyProperty = IRServoType.GetProperty("ForwardKey");
				reverseKeyProperty = IRServoType.GetProperty("ReverseKey");
			}

			private void FindMethods()
			{
				moveLeftMethod = IRMotorType.GetMethod("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
				moveCenterMethod = IRMotorType.GetMethod("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
				moveRightMethod = IRMotorType.GetMethod("MoveRight", BindingFlags.Public | BindingFlags.Instance);
				moveNextPresetMethod = IRMotorType.GetMethod("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
				movePrevPresetMethod = IRMotorType.GetMethod("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
				moveToMethod = IRMotorType.GetMethod("MoveTo", new [] { typeof(float), typeof(float) });
				stopMethod = IRMotorType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
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

			public float TorqueLimit
			{
				get { return (float)torqueLimitProperty.GetValue(actualServo, null); }
				set { torqueLimitProperty.SetValue(actualServo, value, null); }
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

			public void MoveLeft()
			{
				moveLeftMethod.Invoke(actualServo, new object [] {});
			}

			public void MoveCenter()
			{
				moveCenterMethod.Invoke(actualServo, new object [] {});
			}

			public void MoveRight()
			{
				moveRightMethod.Invoke(actualServo, new object [] {});
			}

			public void MoveNextPreset()
			{
				moveNextPresetMethod.Invoke(actualServo, new object [] {});
			}

			public void MovePrevPreset()
			{
				movePrevPresetMethod.Invoke(actualServo, new object [] {});
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

			float TorqueLimit { get; set;}

			float AccelerationLimit { get; set; }

			float SpeedLimit { get; set; }

			float DefaultSpeed { get; set; }

			bool IsMoving { get; }
			bool IsLocked { get; set; }

			bool IsInverted { get; set; }

			float CommandedPosition { get; }

			float CommandedSpeed { get; }

			float Position { get; }

			void MoveLeft();
			void MoveCenter();
			void MoveRight();

			void MoveNextPreset();
			void MovePrevPreset();

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