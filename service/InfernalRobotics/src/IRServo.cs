using System;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// A servo, an IServo of the mod. Equal to another wrapping the same servo.
    /// </summary>
    sealed class IRServo : IEquatable<IRServo>
    {
        static Field<object, string> name;
        static Func<object, uint> uid;
        static Func<object, global::Part> hostPart;
        static Func<object, float> minPosition;
        static Func<object, float> maxPosition;
        static Func<object, bool> isFreeMoving;
        static Field<object, float> minPositionLimit;
        static Field<object, float> maxPositionLimit;
        static Field<object, float> forceLimit;
        static Field<object, float> accelerationLimit;
        static Field<object, float> speedLimit;
        static Field<object, float> defaultSpeed;
        static Func<object, bool> isMoving;
        static Field<object, bool> isLocked;
        static Field<object, bool> isInverted;
        static Func<object, float> commandedPosition;
        static Func<object, float> commandedSpeed;
        static Func<object, float> position;
        static Func<object, float> targetPosition;
        static Func<object, float> targetSpeed;
        static Func<object, float> defaultPosition;
        static Func<object, float> maxForce;
        static Func<object, float> maxAcceleration;
        static Func<object, float> maxSpeed;
        static Func<object, float> electricChargeRequired;
        static Field<object, float> springPower;
        static Field<object, float> dampingPower;
        static Field<object, float> rotorAcceleration;
        static Field<object, bool> isLimited;
        static Func<object, bool> isRotational;
        static Func<object, bool> isServo;
        static Func<object, bool> canHaveLimits;
        static Func<object, bool> hasSpring;
        static Func<object, bool> isRunning;
        static Func<object, IList<float>> presetPositions;
        static Action<object, bool> highlight;
        // IR-Next's ModeType enum: servo = 1, rotor = 2
        static Func<object, int> mode;
        static Func<object, object> presets;
        static Action<object, float> presetAdd;
        static Action<object, int> presetRemoveAt;
        static Action<object, IComparer<float>> presetSort;
        static Action<object, float> moveLeft;
        static Action<object, float> moveCenter;
        static Action<object, float> moveRight;
        static Action<object, float, float> moveTo;
        static Action<object> stop;

        internal static void Bind (ModBinder mod)
        {
            var servo = mod.Type ("InfernalRobotics_v3.Interfaces.IServo");
            name = servo.Field<object, string> ("Name");
            uid = servo.Getter<object, uint> ("UID");
            hostPart = servo.Getter<object, global::Part> ("HostPart");
            minPosition = servo.Getter<object, float> ("MinPosition");
            maxPosition = servo.Getter<object, float> ("MaxPosition");
            isFreeMoving = servo.Getter<object, bool> ("IsFreeMoving");
            minPositionLimit = servo.Field<object, float> ("MinPositionLimit");
            maxPositionLimit = servo.Field<object, float> ("MaxPositionLimit");
            forceLimit = servo.Field<object, float> ("ForceLimit");
            accelerationLimit = servo.Field<object, float> ("AccelerationLimit");
            speedLimit = servo.Field<object, float> ("SpeedLimit");
            defaultSpeed = servo.Field<object, float> ("DefaultSpeed");
            isMoving = servo.Getter<object, bool> ("IsMoving");
            isLocked = servo.Field<object, bool> ("IsLocked");
            isInverted = servo.Field<object, bool> ("IsInverted");
            commandedPosition = servo.Getter<object, float> ("CommandedPosition");
            commandedSpeed = servo.Getter<object, float> ("CommandedSpeed");
            position = servo.Getter<object, float> ("Position");
            targetPosition = servo.Getter<object, float> ("TargetPosition");
            targetSpeed = servo.Getter<object, float> ("TargetSpeed");
            defaultPosition = servo.Getter<object, float> ("DefaultPosition");
            maxForce = servo.Getter<object, float> ("MaxForce");
            maxAcceleration = servo.Getter<object, float> ("MaxAcceleration");
            maxSpeed = servo.Getter<object, float> ("MaxSpeed");
            electricChargeRequired = servo.Getter<object, float> ("ElectricChargeRequired");
            springPower = servo.Field<object, float> ("SpringPower");
            dampingPower = servo.Field<object, float> ("DampingPower");
            rotorAcceleration = servo.Field<object, float> ("RotorAcceleration");
            isLimited = servo.Field<object, bool> ("IsLimited");
            isRotational = servo.Getter<object, bool> ("IsRotational");
            isServo = servo.Getter<object, bool> ("IsServo");
            canHaveLimits = servo.Getter<object, bool> ("CanHaveLimits");
            hasSpring = servo.Getter<object, bool> ("HasSpring");
            isRunning = servo.Getter<object, bool> ("IsRunning");
            presetPositions = servo.Getter<object, IList<float>> ("PresetPositions");
            highlight = servo.Setter<object, bool> ("Highlight");
            mode = servo.Getter<object, int> ("Mode");
            presets = servo.Getter<object, object> ("Presets");
            var presetable = mod.Type ("InfernalRobotics_v3.Interfaces.IPresetable");
            presetAdd = presetable.Method<Action<object, float>> ("Add");
            presetRemoveAt = presetable.Method<Action<object, int>> ("RemoveAt");
            presetSort = presetable.Method<Action<object, IComparer<float>>> ("Sort");
            moveLeft = servo.Method<Action<object, float>> ("MoveLeft");
            moveCenter = servo.Method<Action<object, float>> ("MoveCenter");
            moveRight = servo.Method<Action<object, float>> ("MoveRight");
            moveTo = servo.Method<Action<object, float, float>> ("MoveTo");
            stop = servo.Method<Action<object>> ("Stop");
        }

        readonly object module;

        public IRServo (object instance)
        {
            module = instance;
        }

        public string Name {
            get { return name.Get (module); }
            set { name.Set (module, value); }
        }

        public uint UID {
            get { return uid (module); }
        }

        public global::Part HostPart {
            get { return hostPart (module); }
        }

        public float MinPosition {
            get { return minPosition (module); }
        }

        public float MaxPosition {
            get { return maxPosition (module); }
        }

        public bool IsFreeMoving {
            get { return isFreeMoving (module); }
        }

        public float MinPositionLimit {
            get { return minPositionLimit.Get (module); }
            set { minPositionLimit.Set (module, value); }
        }

        public float MaxPositionLimit {
            get { return maxPositionLimit.Get (module); }
            set { maxPositionLimit.Set (module, value); }
        }

        public float ForceLimit {
            get { return forceLimit.Get (module); }
            set { forceLimit.Set (module, value); }
        }

        public float AccelerationLimit {
            get { return accelerationLimit.Get (module); }
            set { accelerationLimit.Set (module, value); }
        }

        public float SpeedLimit {
            get { return speedLimit.Get (module); }
            set { speedLimit.Set (module, value); }
        }

        public float DefaultSpeed {
            get { return defaultSpeed.Get (module); }
            set { defaultSpeed.Set (module, value); }
        }

        public bool IsMoving {
            get { return isMoving (module); }
        }

        public bool IsLocked {
            get { return isLocked.Get (module); }
            set { isLocked.Set (module, value); }
        }

        public bool IsInverted {
            get { return isInverted.Get (module); }
            set { isInverted.Set (module, value); }
        }

        public float CommandedPosition {
            get { return commandedPosition (module); }
        }

        public float CommandedSpeed {
            get { return commandedSpeed (module); }
        }

        public float Position {
            get { return position (module); }
        }

        public float TargetPosition {
            get { return targetPosition (module); }
        }

        public float TargetSpeed {
            get { return targetSpeed (module); }
        }

        public float DefaultPosition {
            get { return defaultPosition (module); }
        }

        public float MaxForce {
            get { return maxForce (module); }
        }

        public float MaxAcceleration {
            get { return maxAcceleration (module); }
        }

        public float MaxSpeed {
            get { return maxSpeed (module); }
        }

        public float ElectricChargeRequired {
            get { return electricChargeRequired (module); }
        }

        public float SpringPower {
            get { return springPower.Get (module); }
            set { springPower.Set (module, value); }
        }

        public float DampingPower {
            get { return dampingPower.Get (module); }
            set { dampingPower.Set (module, value); }
        }

        public float RotorAcceleration {
            get { return rotorAcceleration.Get (module); }
            set { rotorAcceleration.Set (module, value); }
        }

        public bool IsLimited {
            get { return isLimited.Get (module); }
            set { isLimited.Set (module, value); }
        }

        public bool IsRotational {
            get { return isRotational (module); }
        }

        public bool IsServo {
            get { return isServo (module); }
        }

        public bool CanHaveLimits {
            get { return canHaveLimits (module); }
        }

        public bool HasSpring {
            get { return hasSpring (module); }
        }

        public bool IsRunning {
            get { return isRunning (module); }
        }

        public IList<float> PresetPositions {
            get { return presetPositions (module); }
        }

        public bool Highlight {
            set { highlight (module, value); }
        }

        public int Mode {
            get { return mode (module); }
        }

        public void AddPreset (float position)
        {
            presetAdd (presets (module), position);
        }

        public void RemovePresetAt (int index)
        {
            presetRemoveAt (presets (module), index);
        }

        public void SortPresets ()
        {
            presetSort (presets (module), null);
        }

        // IR-Next's MoveLeft, MoveCenter and MoveRight take a speed. The servo's default speed
        // moves it as the group's parameterless moves do.
        public void MoveLeft ()
        {
            moveLeft (module, DefaultSpeed);
        }

        public void MoveCenter ()
        {
            moveCenter (module, DefaultSpeed);
        }

        public void MoveRight ()
        {
            moveRight (module, DefaultSpeed);
        }

        public void MoveTo (float position, float speed)
        {
            moveTo (module, position, speed);
        }

        public void Stop ()
        {
            stop (module);
        }

        public bool Equals (IRServo other)
        {
            return other != null && Equals (module, other.module);
        }

        public override bool Equals (object obj)
        {
            return Equals (obj as IRServo);
        }

        public override int GetHashCode ()
        {
            return module.GetHashCode ();
        }
    }
}
