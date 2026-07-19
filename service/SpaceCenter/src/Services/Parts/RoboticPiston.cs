using System;
using Expansions.Serenity;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using System.Reflection;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A robotic piston part. Obtained by calling <see cref="Part.RoboticPiston"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class RoboticPiston : Equatable<RoboticPiston>
    {
        readonly ModuleRoboticServoPiston servo;

        internal static bool Is(Part part)
        {
            return part.InternalPart.HasModule<ModuleRoboticServoPiston>();
        }

        internal RoboticPiston(Part part)
        {
            if (!Is(part))
                throw new ArgumentException("Part is not a robotic piston");
            Part = part;
            var internalPart = part.InternalPart;
            servo = internalPart.Module<ModuleRoboticServoPiston>();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticPiston other)
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
        /// The part object for this robotic piston.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Target extension of the piston.
        /// </summary>
        [KRPCProperty]
        public float TargetExtension
        {
            get { return servo.targetExtension; }
            set { SetExtension(value); }
        }

        /// <summary>
        /// Current extension of the piston.
        /// </summary>
        [KRPCProperty]
        public float CurrentExtension
        {
            get { return servo.currentExtension; }
        }

        /// <summary>
        /// The minimum extension of the piston, in meters.
        /// </summary>
        [KRPCProperty]
        public float MinExtension
        {
            get { return servo.softMinMaxExtension.x; }
            set { servo.SetSoftLimits("targetExtension", new Vector2(value, servo.softMinMaxExtension.y)); }
        }

        /// <summary>
        /// The maximum extension of the piston, in meters.
        /// </summary>
        [KRPCProperty]
        public float MaxExtension
        {
            get { return servo.softMinMaxExtension.y; }
            set { servo.SetSoftLimits("targetExtension", new Vector2(servo.softMinMaxExtension.x, value)); }
        }

        /// <summary>
        /// Target movement rate in meters per second.
        /// </summary>
        [KRPCProperty]
        public float Rate {
            get { return servo.traverseVelocity; }
            set { servo.traverseVelocity = value; }
        }

        /// <summary>
        /// Damping percentage.
        /// </summary>
        [KRPCProperty]
        public float Damping {
            get { return servo.pistonDamping; }
            set { servo.pistonDamping = value; }
        }

        /// <summary>
        /// Lock movement.
        /// </summary>
        [KRPCProperty]
        public bool Locked
        {
            get { return servo.servoIsLocked; }
            set
            {
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
        /// Move piston to its built position.
        /// </summary>
        [KRPCMethod]
        public void MoveHome()
        {
            SetExtension(servo.launchPosition);
        }

        private void SetExtension(float value)
        {
            BaseAxisField field = (BaseAxisField)typeof(ModuleRoboticServoPiston)
                .GetField("targetExtensionAxisField", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(servo);
            field.SetValue((float)value, field.module);
        }
    }
}
