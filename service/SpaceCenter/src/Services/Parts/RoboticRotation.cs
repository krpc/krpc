using System;
using System.Reflection;
using Expansions.Serenity;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A robotic rotation servo. Obtained by calling <see cref="Part.RoboticRotation"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class RoboticRotation : Equatable<RoboticRotation>, IGameObjectState
    {
        ModuleRef servoRef;

        internal static bool Is(Part part)
        {
            return part.InternalPart.HasModule<ModuleRoboticRotationServo>();
        }

        internal RoboticRotation(Part part)
        {
            if (!Is(part))
                throw new ArgumentException("Part is not a robotic rotation servo");
            Part = part;
            var internalPart = part.InternalPart;
            servoRef = ModuleRef.ForType<ModuleRoboticRotationServo> (internalPart);
        }

        ModuleRoboticRotationServo InternalServo {
            get { return (ModuleRoboticRotationServo)servoRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// What the game holds for the rotation servo: the state of the part
        /// carrying it, or destroyed once that part no longer has the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return servoRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticRotation other)
        {
            return !ReferenceEquals(other, null) && Part == other.Part && servoRef == other.servoRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return Part.GetHashCode() ^ servoRef.GetHashCode();
        }

        /// <summary>
        /// The part object for this robotic rotation servo.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Target angle.
        /// </summary>
        [KRPCProperty]
        public float TargetAngle
        {
            get { return InternalServo.targetAngle; }
            set { InternalServo.targetAngle= value; }
        }

        /// <summary>
        /// Current angle.
        /// </summary>
        [KRPCProperty]
        public float CurrentAngle {
            // servo.currentAngle is only refreshed while the part action window is open, so read
            // the live transform angle directly (this is the value KSP copies into currentAngle).
            get
            {
                return (float)typeof(ModuleRoboticRotationServo)
                    .GetMethod("currentTransformAngle", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(InternalServo, null);
            }
        }

        /// <summary>
        /// The minimum angle the servo can rotate to, in degrees.
        /// </summary>
        [KRPCProperty]
        public float MinAngle
        {
            get { return InternalServo.softMinMaxAngles.x; }
            set { InternalServo.SetSoftLimits("targetAngle", new Vector2(value, InternalServo.softMinMaxAngles.y)); }
        }

        /// <summary>
        /// The maximum angle the servo can rotate to, in degrees.
        /// </summary>
        [KRPCProperty]
        public float MaxAngle
        {
            get { return InternalServo.softMinMaxAngles.y; }
            set { InternalServo.SetSoftLimits("targetAngle", new Vector2(InternalServo.softMinMaxAngles.x, value)); }
        }

        /// <summary>
        /// Whether the servo is allowed to rotate freely through a full revolution,
        /// ignoring the angle limits.
        /// </summary>
        [KRPCProperty]
        public bool AllowFullRotation
        {
            get { return InternalServo.allowFullRotation; }
            set { InternalServo.allowFullRotation = value; }
        }

        /// <summary>
        /// Target movement rate in degrees per second.
        /// </summary>
        [KRPCProperty]
        public float Rate
        {
            get { return InternalServo.traverseVelocity; }
            set { InternalServo.traverseVelocity = value; }
        }

        /// <summary>
        /// Damping percentage.
        /// </summary>
        [KRPCProperty]
        public float Damping
        {
            get { return InternalServo.hingeDamping; }
            set { InternalServo.hingeDamping = value; }
        }

        /// <summary>
        /// Lock Movement
        /// </summary>
        [KRPCProperty]
        public bool Locked
        {
            get { return InternalServo.servoIsLocked; }
            set
            {
                if (value == true)
                    InternalServo.EngageServoLock();
                else
                    InternalServo.DisengageServoLock();
            }
        }

        /// <summary>
        /// Whether the motor is engaged.
        /// </summary>
        [KRPCProperty]
        public bool MotorEngaged
        {
            get { return InternalServo.servoMotorIsEngaged; }
            set
            {
                if (value == true)
                    InternalServo.EngageMotor();
                else
                    InternalServo.DisengageMotor();
            }
        }

        /// <summary>
        /// Whether the servo is currently moving.
        /// </summary>
        [KRPCProperty]
        public bool IsMoving
        {
            get
            {
                return (bool)typeof(BaseServo)
                    .GetMethod("IsMoving", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(InternalServo, null);
            }
        }

        /// <summary>
        /// Move rotation servo to its built position.
        /// </summary>
        [KRPCMethod]
        public void MoveHome()
        {
            InternalServo.targetAngle = InternalServo.launchPosition;
        }
    }
}

