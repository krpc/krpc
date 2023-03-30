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
    /// A robotic rotor. Obtained by calling <see cref="Part.RoboticRotor"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class RoboticRotor : Equatable<RoboticRotor>
    {
        readonly ModuleRoboticServoRotor servo;

        internal static bool Is(Part part)
        {
            return part.InternalPart.HasModule<ModuleRoboticServoRotor>();
        }

        internal RoboticRotor(Part part)
        {
            if (!Is(part))
                throw new ArgumentException("Part is not a robotic rotor");
            Part = part;
            var internalPart = part.InternalPart;
            servo = internalPart.Module<ModuleRoboticServoRotor>();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticRotor other)
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
        /// The part object for this robotic rotor.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Target RPM.
        /// </summary>
        [KRPCProperty]
        public float TargetRPM
        {
            get { return servo.rpmLimit; }
            set {
                var field = (BaseAxisField)typeof(ModuleRoboticServoRotor).GetField("rpmLimitAxisField", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(servo);
                field.SetValue(value, field.module);
            }
        }

        /// <summary>
        /// Current RPM.
        /// </summary>
        [KRPCProperty]
        public float CurrentRPM
        {
            get { return servo.currentRPM; }
        }

        /// <summary>
        /// Whether the rotor direction is inverted.
        /// </summary>
        [KRPCProperty]
        public bool Inverted
        {
            get { return servo.inverted; }
            set { servo.inverted = value; }
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
        /// Torque limit percentage.
        /// </summary>
        [KRPCProperty]
        public float TorqueLimit
        {
            get { return servo.servoMotorLimit; }
            set { servo.Fields["servoMotorLimit"].SetValue((float)value, servo); }
        }

    }
}
