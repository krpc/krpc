using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// Represents a servo. Obtained using
    /// <see cref="ServoGroup.Servos"/>,
    /// <see cref="ServoGroup.ServoWithName"/>
    /// or <see cref="InfernalRobotics.ServoWithName"/>.
    /// </summary>
    [KRPCClass (Service = "InfernalRobotics")]
    public class Servo : Equatable<Servo>
    {
        readonly IRWrapper.IServo servo;

        internal Servo (IRWrapper.IServo innerServo)
        {
            servo = innerServo;
        }

        /// <summary>
        /// Check if servos are equivalent.
        /// </summary>
        public override bool Equals (Servo other)
        {
            return !ReferenceEquals (other, null) && servo == other.servo;
        }

        /// <summary>
        /// Hash the servo.
        /// </summary>
        public override int GetHashCode ()
        {
            return servo.GetHashCode ();
        }

        /// <summary>
        /// The name of the servo.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return servo.Name; }
            set { servo.Name = value; }
        }

        /// <summary>
        /// The unique identifier of the servo.
        /// </summary>
        [KRPCProperty]
        public uint UID {
            get { return servo.UID; }
        }

        /// <summary>
        /// The part containing the servo.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Parts.Part Part {
            get { return new SpaceCenter.Services.Parts.Part (servo.HostPart); }
        }

        /// <summary>
        /// Whether the part acts as a servo or a rotor.
        /// </summary>
        [KRPCProperty]
        public ServoMode Mode {
            get { return (ServoMode)servo.Mode; }
        }

        /// <summary>
        /// Whether the servo should be highlighted in-game.
        /// </summary>
        [KRPCProperty]
        public bool Highlight {
            set { servo.Highlight = value; }
        }

        /// <summary>
        /// The position of the servo.
        /// </summary>
        [KRPCProperty]
        public float Position {
            get { return servo.Position; }
        }

        /// <summary>
        /// The minimum position of the servo, specified by the part configuration.
        /// </summary>
        [KRPCProperty]
        public float MinConfigPosition {
            get { return servo.MinPosition; }
        }

        /// <summary>
        /// The maximum position of the servo, specified by the part configuration.
        /// </summary>
        [KRPCProperty]
        public float MaxConfigPosition {
            get { return servo.MaxPosition; }
        }

        /// <summary>
        /// The minimum position of the servo, specified by the in-game tweak menu.
        /// </summary>
        [KRPCProperty]
        public float MinPosition {
            get { return servo.MinPositionLimit; }
            set { servo.MinPositionLimit = value; }
        }

        /// <summary>
        /// The maximum position of the servo, specified by the in-game tweak menu.
        /// </summary>
        [KRPCProperty]
        public float MaxPosition {
            get { return servo.MaxPositionLimit; }
            set { servo.MaxPositionLimit = value; }
        }

        /// <summary>
        /// The speed multiplier of the servo, specified by the part configuration.
        /// </summary>
        [KRPCProperty]
        public float ConfigSpeed {
            get { return servo.DefaultSpeed; }
        }

        /// <summary>
        /// The speed multiplier of the servo, specified by the in-game tweak menu.
        /// </summary>
        [KRPCProperty]
        public float Speed {
            get { return servo.SpeedLimit; }
            set { servo.SpeedLimit = value; }
        }

        /// <summary>
        /// The current speed at which the servo is moving.
        /// </summary>
        [KRPCProperty]
        public float CurrentSpeed {
            get { return servo.CommandedSpeed; }
        }

        /// <summary>
        /// The current speed multiplier set in the UI.
        /// </summary>
        [KRPCProperty]
        public float Acceleration {
            get { return servo.AccelerationLimit; }
            set { servo.AccelerationLimit = value; }
        }

        /// <summary>
        /// Whether the servo is moving.
        /// </summary>
        [KRPCProperty]
        public bool IsMoving {
            get { return servo.IsMoving; }
        }

        /// <summary>
        /// Whether the servo is freely moving.
        /// </summary>
        [KRPCProperty]
        public bool IsFreeMoving {
            get { return servo.IsFreeMoving; }
        }

        /// <summary>
        /// Whether the servo is locked.
        /// </summary>
        [KRPCProperty]
        public bool IsLocked {
            get { return servo.IsLocked; }
            set { servo.IsLocked = value; }
        }

        /// <summary>
        /// Whether the servos axis is inverted.
        /// </summary>
        [KRPCProperty]
        public bool IsAxisInverted {
            get { return servo.IsInverted; }
            set { servo.IsInverted = value; }
        }

        /// <summary>
        /// The target position the servo is moving towards.
        /// </summary>
        [KRPCProperty]
        public float TargetPosition {
            get { return servo.TargetPosition; }
        }

        /// <summary>
        /// The target speed the servo is moving at.
        /// </summary>
        [KRPCProperty]
        public float TargetSpeed {
            get { return servo.TargetSpeed; }
        }

        /// <summary>
        /// The position the servo is currently being commanded to move to.
        /// </summary>
        [KRPCProperty]
        public float CommandedPosition {
            get { return servo.CommandedPosition; }
        }

        /// <summary>
        /// The default (built) position of the servo.
        /// </summary>
        [KRPCProperty]
        public float DefaultPosition {
            get { return servo.DefaultPosition; }
        }

        /// <summary>
        /// The force limit of the servo, as a percentage of the maximum force.
        /// </summary>
        [KRPCProperty]
        public float ForceLimit {
            get { return servo.ForceLimit; }
            set { servo.ForceLimit = value; }
        }

        /// <summary>
        /// The maximum force the servo can generate.
        /// </summary>
        [KRPCProperty]
        public float MaxForce {
            get { return servo.MaxForce; }
        }

        /// <summary>
        /// The maximum acceleration the servo can achieve.
        /// </summary>
        [KRPCProperty]
        public float MaxAcceleration {
            get { return servo.MaxAcceleration; }
        }

        /// <summary>
        /// The maximum speed the servo can achieve.
        /// </summary>
        [KRPCProperty]
        public float MaxSpeed {
            get { return servo.MaxSpeed; }
        }

        /// <summary>
        /// The rate at which the servo consumes electric charge, in units per second, when moving.
        /// </summary>
        [KRPCProperty]
        public float ElectricChargeRequired {
            get { return servo.ElectricChargeRequired; }
        }

        /// <summary>
        /// The strength of the servo's spring, when it has one.
        /// </summary>
        [KRPCProperty]
        public float SpringPower {
            get { return servo.SpringPower; }
            set { servo.SpringPower = value; }
        }

        /// <summary>
        /// The strength of the servo's damping.
        /// </summary>
        [KRPCProperty]
        public float DampingPower {
            get { return servo.DampingPower; }
            set { servo.DampingPower = value; }
        }

        /// <summary>
        /// The acceleration of the servo when operating as a rotor.
        /// </summary>
        [KRPCProperty]
        public float RotorAcceleration {
            get { return servo.RotorAcceleration; }
            set { servo.RotorAcceleration = value; }
        }

        /// <summary>
        /// Whether the servo's range of movement is limited to the configured minimum and
        /// maximum positions.
        /// </summary>
        [KRPCProperty]
        public bool IsLimited {
            get { return servo.IsLimited; }
            set { servo.IsLimited = value; }
        }

        /// <summary>
        /// Whether the servo moves rotationally (as opposed to linearly).
        /// </summary>
        [KRPCProperty]
        public bool IsRotational {
            get { return servo.IsRotational; }
        }

        /// <summary>
        /// Whether the part is operating as a servo (rather than a rotor).
        /// </summary>
        [KRPCProperty]
        public bool IsServo {
            get { return servo.IsServo; }
        }

        /// <summary>
        /// Whether the servo can have its range of movement limited.
        /// </summary>
        [KRPCProperty]
        public bool CanHaveLimits {
            get { return servo.CanHaveLimits; }
        }

        /// <summary>
        /// Whether the servo has a spring.
        /// </summary>
        [KRPCProperty]
        public bool HasSpring {
            get { return servo.HasSpring; }
        }

        /// <summary>
        /// Whether the servo is running, when operating as a rotor.
        /// </summary>
        [KRPCProperty]
        public bool IsRunning {
            get { return servo.IsRunning; }
        }

        /// <summary>
        /// The list of preset positions configured for the servo.
        /// </summary>
        [KRPCProperty]
        public IList<float> PresetPositions {
            get { return servo.PresetPositions; }
        }

        /// <summary>
        /// Moves the servo to the right.
        /// </summary>
        [KRPCMethod]
        public void MoveRight ()
        {
            servo.MoveRight ();
        }

        /// <summary>
        /// Moves the servo to the left.
        /// </summary>
        [KRPCMethod]
        public void MoveLeft ()
        {
            servo.MoveLeft ();
        }

        /// <summary>
        /// Moves the servo to the center.
        /// </summary>
        [KRPCMethod]
        public void MoveCenter ()
        {
            servo.MoveCenter ();
        }

        /// <summary>
        /// Moves the servo to <paramref name="position"/> and sets the
        /// speed multiplier to <paramref name="speed"/>.
        /// </summary>
        /// <param name="position">The position to move the servo to.</param>
        /// <param name="speed">Speed multiplier for the movement.</param>
        [KRPCMethod]
        public void MoveTo (float position, float speed)
        {
            servo.MoveTo (position, speed);
        }

        /// <summary>
        /// Stops the servo.
        /// </summary>
        [KRPCMethod]
        public void Stop ()
        {
            servo.Stop ();
        }

        /// <summary>
        /// Adds a preset position to the servo.
        /// </summary>
        /// <param name="position">The position of the preset.</param>
        [KRPCMethod]
        public void AddPreset (float position)
        {
            servo.AddPreset (position);
        }

        /// <summary>
        /// Removes the preset position at the given index.
        /// </summary>
        /// <param name="index">The index of the preset to remove.</param>
        [KRPCMethod]
        public void RemovePresetAt (int index)
        {
            servo.RemovePresetAt (index);
        }

        /// <summary>
        /// Sorts the preset positions of the servo into ascending order.
        /// </summary>
        [KRPCMethod]
        public void SortPresets ()
        {
            servo.SortPresets ();
        }
    }
}
