#pragma warning disable 1591

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KRPC.InfernalRobotics.IRWrapper;

namespace KRPC.InfernalRobotics
{
    public class IROrigWrapper : IRWrapper.IRWrapper
    {
        static IROrigWrapper instance;
        internal object ActualController { get; set; }
        internal Type IRServoPartType { get; set; }
        internal Type IRServoMechanismType { get; set; }
        internal Type IRServoMotorType { get; set; }

        internal bool InternalInitWrapper ()
        {
            instance = this;
            isWrapped = false;
            ActualController = null;
            IRController = null;
            LogFormatted ("Attempting to Grab IR Types...");

            IRControllerType = AssemblyLoader.loadedAssemblies
                .Select (a => a.assembly.GetExportedTypes ())
                .SelectMany (t => t)
                .FirstOrDefault (t => t.FullName == "InfernalRobotics.Command.ServoController");

            if (IRControllerType == null) {
                return false;
            }

            LogFormatted ("IR Version:{0}", IRControllerType.Assembly.GetName ().Version.ToString ());

            IRServoMechanismType = AssemblyLoader.loadedAssemblies
               .Select (a => a.assembly.GetExportedTypes ())
               .SelectMany (t => t)
               .FirstOrDefault (t => t.FullName == "InfernalRobotics.Control.IMechanism");

            if (IRServoMechanismType == null) {
                LogFormatted ("[IR Wrapper] Failed to grab Mechanism Type");
                return false;
            }

            IRServoMotorType = AssemblyLoader.loadedAssemblies
                .Select (a => a.assembly.GetExportedTypes ())
                .SelectMany (t => t)
                .FirstOrDefault (t => t.FullName == "InfernalRobotics.Control.IServoMotor");

            if (IRServoMotorType == null) {
                LogFormatted ("[IR Wrapper] Failed to grab ServoMotor Type");
                return false;
            }

            IRServoType = AssemblyLoader.loadedAssemblies
                .Select (a => a.assembly.GetExportedTypes ())
                .SelectMany (t => t)
                .FirstOrDefault (t => t.FullName == "InfernalRobotics.Control.IServo");

            if (IRServoType == null) {
                LogFormatted ("[IR Wrapper] Failed to grab Servo Type");
                return false;
            }

            IRServoPartType = AssemblyLoader.loadedAssemblies
                .Select (a => a.assembly.GetExportedTypes ())
                .SelectMany (t => t)
                .FirstOrDefault (t => t.FullName == "InfernalRobotics.Control.IPart");

            if (IRServoType == null) {
                LogFormatted ("[IR Wrapper] Failed to grab ServoPart Type");
                return false;
            }

            IRControlGroupType = AssemblyLoader.loadedAssemblies
                .Select (a => a.assembly.GetExportedTypes ())
                .SelectMany (t => t)
                .FirstOrDefault (t => t.FullName == "InfernalRobotics.Command.ServoController+ControlGroup");

            if (IRControlGroupType == null) {
                var irassembly = AssemblyLoader.loadedAssemblies.FirstOrDefault (a => a.assembly.FullName.Contains ("InfernalRobotics"));
                if (irassembly == null) {
                    LogFormatted ("[IR Wrapper] cannot find InvernalRobotics.dll");
                    return false;
                }
                foreach (Type t in irassembly.assembly.GetExportedTypes()) {
                    LogFormatted ("[IR Wrapper] Exported type: " + t.FullName);
                }

                LogFormatted ("[IR Wrapper] Failed to grab ControlGroup Type");
                return false;
            }

            LogFormatted ("Got Assembly Types, grabbing Instance");

            try {
                var propertyInfo = IRControllerType.GetProperty ("Instance", BindingFlags.Public | BindingFlags.Static);

                if (propertyInfo == null)
                    LogFormatted ("[IR Wrapper] Cannot find Instance Property");
                else
                    ActualController = propertyInfo.GetValue (null, null);
            } catch (Exception e) {
                LogFormatted ("No Instance found, " + e.Message);
            }

            if (ActualController == null) {
                LogFormatted ("Failed grabbing Instance");
                return false;
            }

            LogFormatted ("Got Instance, Creating Wrapper Objects");
            IRController = new InfernalRoboticsAPI ();
            isWrapped = true;
            return true;
        }

        #region Private Implementation

        private class InfernalRoboticsAPI : IRAPI
        {
            private PropertyInfo apiReady;
            private object actualServoGroups;

            public InfernalRoboticsAPI ()
            {
                DetermineReady ();
                BuildServoGroups ();
            }

            private void BuildServoGroups ()
            {
                var servoGroupsField = instance.IRControllerType.GetField ("ServoGroups");
                if (servoGroupsField == null)
                    instance.LogFormatted ("Failed Getting ServoGroups fieldinfo");
                else if (instance.ActualController == null) {
                    instance.LogFormatted ("ServoController Instance not found");
                } else {
                    actualServoGroups = servoGroupsField.GetValue (instance.ActualController);
                }
            }

            private void DetermineReady ()
            {
                instance.LogFormatted ("Getting APIReady Object");
                apiReady = instance.IRControllerType.GetProperty ("APIReady", BindingFlags.Public | BindingFlags.Static);
                instance.LogFormatted ("Success: " + (apiReady != null));
            }

            public bool Ready {
                get {
                    if (apiReady == null || actualServoGroups == null)
                        return false;

                    return (bool)apiReady.GetValue (null, null);
                }
            }

            public IList<IControlGroup> ServoGroups {
                get {
                    BuildServoGroups ();
                    return ExtractServoGroups (actualServoGroups);
                }
            }

            private IList<IControlGroup> ExtractServoGroups (object servoGroups)
            {
                var listToReturn = new List<IControlGroup> ();

                if (servoGroups == null)
                    return listToReturn;

                try {
                    //iterate each "value" in the dictionary
                    foreach (var item in (IList)servoGroups) {
                        listToReturn.Add (new IRControlGroup (item));
                    }
                } catch (Exception ex) {
                    instance.LogFormatted ("Cannot list ServoGroups: {0}", ex.Message);
                }
                return listToReturn;
            }
        }

        private class IRControlGroup : IControlGroup
        {
            private readonly object actualControlGroup;

            private PropertyInfo nameProperty;
            private PropertyInfo vesselProperty;
            private PropertyInfo forwardKeyProperty;
            private PropertyInfo expandedProperty;
            private PropertyInfo speedProperty;
            private PropertyInfo reverseKeyProperty;

            private MethodInfo moveRightMethod;
            private MethodInfo moveLeftMethod;
            private MethodInfo moveCenterMethod;
            private MethodInfo moveNextPresetMethod;
            private MethodInfo movePrevPresetMethod;
            private MethodInfo stopMethod;

            public IRControlGroup (object cg)
            {
                actualControlGroup = cg;
                FindProperties ();
                FindMethods ();
            }

            private void FindProperties ()
            {
                nameProperty = instance.IRControlGroupType.GetProperty ("Name");
                vesselProperty = instance.IRControlGroupType.GetProperty ("Vessel");
                forwardKeyProperty = instance.IRControlGroupType.GetProperty ("ForwardKey");
                reverseKeyProperty = instance.IRControlGroupType.GetProperty ("ReverseKey");
                speedProperty = instance.IRControlGroupType.GetProperty ("Speed");
                expandedProperty = instance.IRControlGroupType.GetProperty ("Expanded");

                var servosProperty = instance.IRControlGroupType.GetProperty ("Servos");
                ActualServos = servosProperty.GetValue (actualControlGroup, null);
            }

            private void FindMethods ()
            {
                moveRightMethod = instance.IRControlGroupType.GetMethod ("MoveRight", BindingFlags.Public | BindingFlags.Instance);
                moveLeftMethod = instance.IRControlGroupType.GetMethod ("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
                moveCenterMethod = instance.IRControlGroupType.GetMethod ("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
                moveNextPresetMethod = instance.IRControlGroupType.GetMethod ("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
                movePrevPresetMethod = instance.IRControlGroupType.GetMethod ("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
                stopMethod = instance.IRControlGroupType.GetMethod ("Stop", BindingFlags.Public | BindingFlags.Instance);
            }

            public string Name {
                get { return (string)nameProperty.GetValue (actualControlGroup, null); }
                set { nameProperty.SetValue (actualControlGroup, value, null); }
            }

            public Vessel Vessel {
                get { return vesselProperty != null ? (Vessel)vesselProperty.GetValue (actualControlGroup, null) : null; }
            }

            public string ForwardKey {
                get { return (string)forwardKeyProperty.GetValue (actualControlGroup, null); }
                set { forwardKeyProperty.SetValue (actualControlGroup, value, null); }
            }

            public string ReverseKey {
                get { return (string)reverseKeyProperty.GetValue (actualControlGroup, null); }
                set { reverseKeyProperty.SetValue (actualControlGroup, value, null); }
            }

            public float Speed {
                get { return (float)speedProperty.GetValue (actualControlGroup, null); }
                set { speedProperty.SetValue (actualControlGroup, value, null); }
            }

            public bool Expanded {
                get { return (bool)expandedProperty.GetValue (actualControlGroup, null); }
                set { expandedProperty.SetValue (actualControlGroup, value, null); }
            }

            private object ActualServos { get; set; }

            public IList<IServo> Servos {
                get {
                    return ExtractServos (ActualServos);
                }
            }

            public void MoveRight ()
            {
                moveRightMethod.Invoke (actualControlGroup, new object[] { });
            }

            public void MoveLeft ()
            {
                moveLeftMethod.Invoke (actualControlGroup, new object[] { });
            }

            public void MoveCenter ()
            {
                moveCenterMethod.Invoke (actualControlGroup, new object[] { });
            }

            public void MoveNextPreset ()
            {
                moveNextPresetMethod.Invoke (actualControlGroup, new object[] { });
            }

            public void MovePrevPreset ()
            {
                movePrevPresetMethod.Invoke (actualControlGroup, new object[] { });
            }

            public void Stop ()
            {
                stopMethod.Invoke (actualControlGroup, new object[] { });
            }

            private IList<IServo> ExtractServos (object actualServos)
            {
                var listToReturn = new List<IServo> ();

                if (actualServos == null)
                    return listToReturn;

                try {
                    //iterate each "value" in the dictionary
                    foreach (var item in (IList)actualServos) {
                        listToReturn.Add (new IRServo (item));
                    }
                } catch (Exception ex) {
                    instance.LogFormatted ("Error extracting from actualServos: {0}", ex.Message);
                }
                return listToReturn;
            }

            public bool Equals (IControlGroup other)
            {
                var controlGroup = other as IRControlGroup;
                return controlGroup != null && Equals (controlGroup);
            }
        }

        public class IRServo : IServo
        {
            private object actualServoMechanism;
            private object actualServoMotor;

            private PropertyInfo maxConfigPositionProperty;
            private PropertyInfo minPositionProperty;
            private PropertyInfo maxPositionProperty;
            private PropertyInfo configSpeedProperty;
            private PropertyInfo speedProperty;
            private PropertyInfo currentSpeedProperty;
            private PropertyInfo accelerationProperty;
            private PropertyInfo isMovingProperty;
            private PropertyInfo isFreeMovingProperty;
            private PropertyInfo isLockedProperty;
            private PropertyInfo isAxisInvertedProperty;
            private PropertyInfo nameProperty;
            private PropertyInfo highlightProperty;
            private PropertyInfo positionProperty;
            private PropertyInfo minConfigPositionProperty;

            private PropertyInfo UIDProperty;
            private PropertyInfo HostPartProperty;

            private MethodInfo moveRightMethod;
            private MethodInfo moveLeftMethod;
            private MethodInfo moveCenterMethod;
            private MethodInfo moveNextPresetMethod;
            private MethodInfo movePrevPresetMethod;
            private MethodInfo moveToMethod;
            private MethodInfo stopMethod;

            public IRServo (object s)
            {
                actualServo = s;

                FindProperties ();
                FindMethods ();
            }

            private void FindProperties ()
            {
                nameProperty = instance.IRServoPartType.GetProperty ("Name");
                highlightProperty = instance.IRServoPartType.GetProperty ("Highlight");
                UIDProperty = instance.IRServoPartType.GetProperty ("UID");
                HostPartProperty = instance.IRServoPartType.GetProperty ("HostPart");

                var mechanismProperty = instance.IRServoType.GetProperty ("Mechanism");
                actualServoMechanism = mechanismProperty.GetValue (actualServo, null);

                var motorProperty = instance.IRServoType.GetProperty ("Motor");
                actualServoMotor = motorProperty.GetValue (actualServo, null);

                positionProperty = instance.IRServoMechanismType.GetProperty ("Position");
                minPositionProperty = instance.IRServoMechanismType.GetProperty ("MinPositionLimit");
                maxPositionProperty = instance.IRServoMechanismType.GetProperty ("MaxPositionLimit");

                minConfigPositionProperty = instance.IRServoMechanismType.GetProperty ("MinPosition");
                maxConfigPositionProperty = instance.IRServoMechanismType.GetProperty ("MaxPosition");

                isMovingProperty = instance.IRServoMechanismType.GetProperty ("IsMoving");
                isFreeMovingProperty = instance.IRServoMechanismType.GetProperty ("IsFreeMoving");
                isLockedProperty = instance.IRServoMechanismType.GetProperty ("IsLocked");

                speedProperty = instance.IRServoMotorType.GetProperty ("SpeedLimit");
                configSpeedProperty = instance.IRServoMotorType.GetProperty ("DefaultSpeed");
                currentSpeedProperty = instance.IRServoMotorType.GetProperty ("CurrentSpeed");
                accelerationProperty = instance.IRServoMotorType.GetProperty ("AccelerationLimit");
                isAxisInvertedProperty = instance.IRServoMotorType.GetProperty ("IsAxisInverted");
            }

            private void FindMethods ()
            {
                moveRightMethod = instance.IRServoMotorType.GetMethod ("MoveRight", BindingFlags.Public | BindingFlags.Instance);
                moveLeftMethod = instance.IRServoMotorType.GetMethod ("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
                moveCenterMethod = instance.IRServoMotorType.GetMethod ("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
                moveNextPresetMethod = instance.IRServoMotorType.GetMethod ("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
                movePrevPresetMethod = instance.IRServoMotorType.GetMethod ("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
                stopMethod = instance.IRServoMotorType.GetMethod ("Stop", BindingFlags.Public | BindingFlags.Instance);
                moveToMethod = instance.IRServoMotorType.GetMethod ("MoveTo", new[] { typeof(float), typeof(float) });
            }

            private readonly object actualServo;


            public string Name {
                get { return (string)nameProperty.GetValue (actualServo, null); }
                set { nameProperty.SetValue (actualServo, value, null); }
            }

            public uint UID {
                get { return (uint)UIDProperty.GetValue (actualServo, null); }
            }

            public Part HostPart {
                get { return (Part)HostPartProperty.GetValue (actualServo, null); }
            }

            public bool Highlight {
                //get { return (bool)HighlightProperty.GetValue(actualServo, null); }
                set { highlightProperty.SetValue (actualServo, value, null); }
            }

            public float Position {
                get { return (float)positionProperty.GetValue (actualServoMechanism, null); }
            }

            public float MinConfigPosition {
                get { return (float)minConfigPositionProperty.GetValue (actualServoMechanism, null); }
            }

            public float MaxConfigPosition {
                get { return (float)maxConfigPositionProperty.GetValue (actualServoMechanism, null); }
            }

            public float MinPosition {
                get { return (float)minPositionProperty.GetValue (actualServoMechanism, null); }
                set { minPositionProperty.SetValue (actualServoMechanism, value, null); }
            }

            public float MaxPosition {
                get { return (float)maxPositionProperty.GetValue (actualServoMechanism, null); }
                set { maxPositionProperty.SetValue (actualServoMechanism, value, null); }
            }

            public float ConfigSpeed {
                get { return (float)configSpeedProperty.GetValue (actualServoMotor, null); }
            }

            public float Speed {
                get { return (float)speedProperty.GetValue (actualServoMotor, null); }
                set { speedProperty.SetValue (actualServoMotor, value, null); }
            }

            public float CurrentSpeed {
                get { return (float)currentSpeedProperty.GetValue (actualServoMotor, null); }
                set { currentSpeedProperty.SetValue (actualServoMotor, value, null); }
            }

            public float Acceleration {
                get { return (float)accelerationProperty.GetValue (actualServoMotor, null); }
                set { accelerationProperty.SetValue (actualServoMotor, value, null); }
            }

            public bool IsMoving {
                get { return (bool)isMovingProperty.GetValue (actualServoMechanism, null); }
            }

            public bool IsFreeMoving {
                get { return (bool)isFreeMovingProperty.GetValue (actualServoMechanism, null); }
            }

            public bool IsLocked {
                get { return (bool)isLockedProperty.GetValue (actualServoMechanism, null); }
                set { isLockedProperty.SetValue (actualServoMechanism, value, null); }
            }

            public bool IsAxisInverted {
                get { return (bool)isAxisInvertedProperty.GetValue (actualServoMotor, null); }
                set { isAxisInvertedProperty.SetValue (actualServoMotor, value, null); }
            }

            public void MoveRight ()
            {
                moveRightMethod.Invoke (actualServoMotor, new object[] { });
            }

            public void MoveLeft ()
            {
                moveLeftMethod.Invoke (actualServoMotor, new object[] { });
            }

            public void MoveCenter ()
            {
                moveCenterMethod.Invoke (actualServoMotor, new object[] { });
            }

            public void MoveNextPreset ()
            {
                moveNextPresetMethod.Invoke (actualServoMotor, new object[] { });
            }

            public void MovePrevPreset ()
            {
                movePrevPresetMethod.Invoke (actualServoMotor, new object[] { });
            }

            public void MoveTo (float position, float speed)
            {
                moveToMethod.Invoke (actualServoMotor, new object[] { position, speed });
            }

            public void Stop ()
            {
                stopMethod.Invoke (actualServoMotor, new object[] { });
            }

            public bool Equals (IServo other)
            {
                var servo = other as IRServo;
                return servo != null && Equals (servo);
            }

            public override bool Equals (object o)
            {
                var servo = o as IRServo;
                return servo != null && actualServo.Equals (servo.actualServo);
            }

            public override int GetHashCode ()
            {
                return (actualServo != null ? actualServo.GetHashCode () : 0);
            }

            public static bool operator == (IRServo left, IRServo right)
            {
                return Equals (left, right);
            }

            public static bool operator != (IRServo left, IRServo right)
            {
                return !Equals (left, right);
            }

            protected bool Equals (IRServo other)
            {
                return Equals (actualServo, other.actualServo);
            }
        }
        #endregion Private Implementation
    }
}
