using System;
using System.Reflection;
using Expansions.Serenity;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A robotic hinge. Obtained by calling <see cref="Part.RoboticHinge"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class RoboticHinge : Equatable<RoboticHinge>
    {
        readonly ModuleRoboticServoHinge servo;

        internal static bool Is(Part part)
        {
            return part.InternalPart.HasModule<ModuleRoboticServoHinge>();
        }

        internal RoboticHinge(Part part)
        {
            if (!Is(part))
                throw new ArgumentException("Part is not a robotic hinge");
            Part = part;
            var internalPart = part.InternalPart;
            servo = internalPart.Module<ModuleRoboticServoHinge>();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticHinge other)
        {
            return !ReferenceEquals(other, null) && Part == other.Part && servo.Equals(other.servo);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return Part.GetHashCode() ^ servo.GetHashCode();
        }

        /// <summary>
        /// The part object for this robotic hinge.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Target angle.
        /// </summary>
        [KRPCProperty]
        public float TargetAngle {
            get { return servo.targetAngle; }
            set { servo.targetAngle = value; }
        }

        /// <summary>
        /// Current angle.
        /// </summary>
        [KRPCProperty]
        public float CurrentAngle
        {
            get
            {
                return servo.modelInitialAngle + (float)typeof(ModuleRoboticServoHinge)
                    .GetMethod("currentTransformAngle", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(servo, null);
            }
        }

        /// <summary>
        /// The minimum angle the hinge can move to, in degrees.
        /// </summary>
        [KRPCProperty]
        public float MinAngle
        {
            get { return servo.softMinMaxAngles.x; }
            set { servo.SetSoftLimits("targetAngle", new Vector2(value, servo.softMinMaxAngles.y)); }
        }

        /// <summary>
        /// The maximum angle the hinge can move to, in degrees.
        /// </summary>
        [KRPCProperty]
        public float MaxAngle
        {
            get { return servo.softMinMaxAngles.y; }
            set { servo.SetSoftLimits("targetAngle", new Vector2(servo.softMinMaxAngles.x, value)); }
        }

        /// <summary>
        /// Target movement rate in degrees per second.
        /// </summary>
        [KRPCProperty]
        public float Rate
        {
            get { return servo.traverseVelocity; }
            set { servo.traverseVelocity = value; }
        }

        /// <summary>
        /// Damping percentage.
        /// </summary>
        [KRPCProperty]
        public float Damping
        {
            get { return servo.hingeDamping; }
            set { servo.hingeDamping = value; }
        }

        /// <summary>
        /// Lock movement.
        /// </summary>
        [KRPCProperty]
        public bool Locked
        {
            get { return servo.servoIsLocked; }
            set {
                if (value == true)
                    servo.EngageServoLock();
                else
                    servo.DisengageServoLock();
            }
        }

        /// <summary>
        /// Whether the motor is engaged.
        /// </summary>
        [KRPCProperty]
        public bool MotorEngaged
        {
            get { return servo.servoMotorIsEngaged; }
            set
            {
                if (value == true)
                    servo.EngageMotor();
                else
                    servo.DisengageMotor();
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
                    .Invoke(servo, null);
            }
        }

        /// <summary>
        /// Move hinge to its built position.
        /// </summary>
        [KRPCMethod]
        public void MoveHome()
        {
            servo.targetAngle = servo.modelInitialAngle;
        }
    }
}
