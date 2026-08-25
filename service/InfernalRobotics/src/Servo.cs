using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// Represents a servo. Obtained using
    /// <see cref="ServoGroup.Servos"/>,
    /// <see cref="ServoGroup.ServoWithName"/>
    /// or <see cref="InfernalRobotics.ServoWithName"/>.
    /// </summary>
    [KRPCClass (Service = "InfernalRobotics")]
    public class Servo : Equatable<Servo>, IGameObjectState
    {
        // The part the servo is on, which is what identifies it: a servo is a module the mod
        // adds to a part, and the mod names one by its part. The wrapper the mod hands out
        // is built afresh every time a servo is listed, so holding one would leave two
        // objects for one servo comparing unequal, and would keep a destroyed part alive.
        readonly SpaceCenter.Services.Parts.Part part;

        internal Servo (SpaceCenter.Services.Parts.Part servoPart)
        {
            if (ReferenceEquals (servoPart, null))
                throw new ArgumentNullException (nameof (servoPart));
            part = servoPart;
        }

        internal Servo (IRWrapper.IServo innerServo)
            : this (new SpaceCenter.Services.Parts.Part (innerServo.HostPart))
        {
        }

        /// <summary>
        /// Check if servos are equivalent.
        /// </summary>
        public override bool Equals (Servo other)
        {
            return !ReferenceEquals (other, null) && part == other.part;
        }

        /// <summary>
        /// Hash the servo.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        /// <summary>
        /// The state of the servo. It takes the state of its part, and is destroyed once a
        /// live part no longer carries the servo module.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = part.GameObjectState;
                if (state != GameObjectState.Live)
                    return state;
                return IRWrapper.ServoOnPart (part.InternalPart) != null
                    ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        // The servo the mod has on the part now. Every member reaches the mod through this,
        // so a servo taken before a game state was replaced reads the one that stands in
        // its place rather than the one it was built from.
        IRWrapper.IServo Internal {
            get {
                var servo = IRWrapper.ServoOnPart (part.InternalPart);
                if (servo == null)
                    throw new ObjectDestroyedException (
                        "The servo no longer exists, as its part no longer has one.");
                return servo;
            }
        }

        /// <summary>
        /// The name of the servo.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return Internal.Name; }
            set { Internal.Name = value; }
        }

        /// <summary>
        /// The unique identifier of the servo.
        /// </summary>
        [KRPCProperty]
        public uint UID {
            get { return Internal.UID; }
        }

        /// <summary>
        /// The part containing the servo.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Parts.Part Part {
            get { return part; }
        }

        /// <summary>
        /// Whether the part acts as a servo or a rotor.
        /// </summary>
        [KRPCProperty]
        public ServoMode Mode {
            get { return (ServoMode)Internal.Mode; }
        }

        /// <summary>
        /// Whether the servo should be highlighted in-game.
        /// </summary>
        [KRPCProperty]
        public bool Highlight {
            set { Internal.Highlight = value; }
        }

        /// <summary>
        /// The position of the servo.
        /// </summary>
        [KRPCProperty]
        public float Position {
            get { return Internal.Position; }
        }

        /// <summary>
        /// The minimum position of the servo, specified by the part configuration.
        /// </summary>
        [KRPCProperty]
        public float MinConfigPosition {
            get { return Internal.MinPosition; }
        }

        /// <summary>
        /// The maximum position of the servo, specified by the part configuration.
        /// </summary>
        [KRPCProperty]
        public float MaxConfigPosition {
            get { return Internal.MaxPosition; }
        }

        /// <summary>
        /// The minimum position of the servo, specified by the in-game tweak menu.
        /// </summary>
        [KRPCProperty]
        public float MinPosition {
            get { return Internal.MinPositionLimit; }
            set { Internal.MinPositionLimit = value; }
        }

        /// <summary>
        /// The maximum position of the servo, specified by the in-game tweak menu.
        /// </summary>
        [KRPCProperty]
        public float MaxPosition {
            get { return Internal.MaxPositionLimit; }
            set { Internal.MaxPositionLimit = value; }
        }

        /// <summary>
        /// The speed multiplier of the servo, specified by the part configuration.
        /// </summary>
        [KRPCProperty]
        public float ConfigSpeed {
            get { return Internal.DefaultSpeed; }
        }

        /// <summary>
        /// The speed multiplier of the servo, specified by the in-game tweak menu.
        /// </summary>
        [KRPCProperty]
        public float Speed {
            get { return Internal.SpeedLimit; }
            set { Internal.SpeedLimit = value; }
        }

        /// <summary>
        /// The current speed at which the servo is moving.
        /// </summary>
        [KRPCProperty]
        public float CurrentSpeed {
            get { return Internal.CommandedSpeed; }
        }

        /// <summary>
        /// The current speed multiplier set in the UI.
        /// </summary>
        [KRPCProperty]
        public float Acceleration {
            get { return Internal.AccelerationLimit; }
            set { Internal.AccelerationLimit = value; }
        }

        /// <summary>
        /// Whether the servo is moving.
        /// </summary>
        [KRPCProperty]
        public bool IsMoving {
            get { return Internal.IsMoving; }
        }

        /// <summary>
        /// Whether the servo is freely moving.
        /// </summary>
        [KRPCProperty]
        public bool IsFreeMoving {
            get { return Internal.IsFreeMoving; }
        }

        /// <summary>
        /// Whether the servo is locked.
        /// </summary>
        [KRPCProperty]
        public bool IsLocked {
            get { return Internal.IsLocked; }
            set { Internal.IsLocked = value; }
        }

        /// <summary>
        /// Whether the servos axis is inverted.
        /// </summary>
        [KRPCProperty]
        public bool IsAxisInverted {
            get { return Internal.IsInverted; }
            set { Internal.IsInverted = value; }
        }

        /// <summary>
        /// The target position the servo is moving towards.
        /// </summary>
        [KRPCProperty]
        public float TargetPosition {
            get { return Internal.TargetPosition; }
        }

        /// <summary>
        /// The target speed the servo is moving at.
        /// </summary>
        [KRPCProperty]
        public float TargetSpeed {
            get { return Internal.TargetSpeed; }
        }

        /// <summary>
        /// The position the servo is currently being commanded to move to.
        /// </summary>
        [KRPCProperty]
        public float CommandedPosition {
            get { return Internal.CommandedPosition; }
        }

        /// <summary>
        /// The default (built) position of the servo.
        /// </summary>
        [KRPCProperty]
        public float DefaultPosition {
            get { return Internal.DefaultPosition; }
        }

        /// <summary>
        /// The force limit of the servo, as a percentage of the maximum force.
        /// </summary>
        [KRPCProperty]
        public float ForceLimit {
            get { return Internal.ForceLimit; }
            set { Internal.ForceLimit = value; }
        }

        /// <summary>
        /// The maximum force the servo can generate.
        /// </summary>
        [KRPCProperty]
        public float MaxForce {
            get { return Internal.MaxForce; }
        }

        /// <summary>
        /// The maximum acceleration the servo can achieve.
        /// </summary>
        [KRPCProperty]
        public float MaxAcceleration {
            get { return Internal.MaxAcceleration; }
        }

        /// <summary>
        /// The maximum speed the servo can achieve.
        /// </summary>
        [KRPCProperty]
        public float MaxSpeed {
            get { return Internal.MaxSpeed; }
        }

        /// <summary>
        /// The rate at which the servo consumes electric charge, in units per second, when moving.
        /// </summary>
        [KRPCProperty]
        public float ElectricChargeRequired {
            get { return Internal.ElectricChargeRequired; }
        }

        /// <summary>
        /// The strength of the servo's spring, when it has one.
        /// </summary>
        [KRPCProperty]
        public float SpringPower {
            get { return Internal.SpringPower; }
            set { Internal.SpringPower = value; }
        }

        /// <summary>
        /// The strength of the servo's damping.
        /// </summary>
        [KRPCProperty]
        public float DampingPower {
            get { return Internal.DampingPower; }
            set { Internal.DampingPower = value; }
        }

        /// <summary>
        /// The acceleration of the servo when operating as a rotor.
        /// </summary>
        [KRPCProperty]
        public float RotorAcceleration {
            get { return Internal.RotorAcceleration; }
            set { Internal.RotorAcceleration = value; }
        }

        /// <summary>
        /// Whether the servo's range of movement is limited to the configured minimum and
        /// maximum positions.
        /// </summary>
        [KRPCProperty]
        public bool IsLimited {
            get { return Internal.IsLimited; }
            set { Internal.IsLimited = value; }
        }

        /// <summary>
        /// Whether the servo moves rotationally (as opposed to linearly).
        /// </summary>
        [KRPCProperty]
        public bool IsRotational {
            get { return Internal.IsRotational; }
        }

        /// <summary>
        /// Whether the part is operating as a servo (rather than a rotor).
        /// </summary>
        [KRPCProperty]
        public bool IsServo {
            get { return Internal.IsServo; }
        }

        /// <summary>
        /// Whether the servo can have its range of movement limited.
        /// </summary>
        [KRPCProperty]
        public bool CanHaveLimits {
            get { return Internal.CanHaveLimits; }
        }

        /// <summary>
        /// Whether the servo has a spring.
        /// </summary>
        [KRPCProperty]
        public bool HasSpring {
            get { return Internal.HasSpring; }
        }

        /// <summary>
        /// Whether the servo is running, when operating as a rotor.
        /// </summary>
        [KRPCProperty]
        public bool IsRunning {
            get { return Internal.IsRunning; }
        }

        /// <summary>
        /// The list of preset positions configured for the servo.
        /// </summary>
        [KRPCProperty]
        public IList<float> PresetPositions {
            get { return Internal.PresetPositions; }
        }

        /// <summary>
        /// Moves the servo to the right.
        /// </summary>
        [KRPCMethod]
        public void MoveRight ()
        {
            Internal.MoveRight ();
        }

        /// <summary>
        /// Moves the servo to the left.
        /// </summary>
        [KRPCMethod]
        public void MoveLeft ()
        {
            Internal.MoveLeft ();
        }

        /// <summary>
        /// Moves the servo to the center.
        /// </summary>
        [KRPCMethod]
        public void MoveCenter ()
        {
            Internal.MoveCenter ();
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
            Internal.MoveTo (position, speed);
        }

        /// <summary>
        /// Stops the servo.
        /// </summary>
        [KRPCMethod]
        public void Stop ()
        {
            Internal.Stop ();
        }

        /// <summary>
        /// Adds a preset position to the servo.
        /// </summary>
        /// <param name="position">The position of the preset.</param>
        [KRPCMethod]
        public void AddPreset (float position)
        {
            Internal.AddPreset (position);
        }

        /// <summary>
        /// Removes the preset position at the given index.
        /// </summary>
        /// <param name="index">The index of the preset to remove.</param>
        [KRPCMethod]
        public void RemovePresetAt (int index)
        {
            Internal.RemovePresetAt (index);
        }

        /// <summary>
        /// Sorts the preset positions of the servo into ascending order.
        /// </summary>
        [KRPCMethod]
        public void SortPresets ()
        {
            Internal.SortPresets ();
        }
    }
}
