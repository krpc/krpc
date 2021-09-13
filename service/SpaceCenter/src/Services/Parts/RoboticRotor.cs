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
    /// A Robotic Rotor Part. Obtained by calling <see cref="Part.RoboticRotor"/>
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
            Part = part;
            var internalPart = part.InternalPart;
            servo = internalPart.Module<ModuleRoboticServoRotor>();

            if (servo == null)
                throw new ArgumentException("Part is not a robotic rotor");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticRotor other)
        {
            return
            !ReferenceEquals(other, null) &&
            Part == other.Part &&
            servo.Equals(other.servo);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            int hash = Part.GetHashCode() ^ servo.GetHashCode();

            return hash;
        }

        /// <summary>
        /// The KSP Robotic Servo Rotor object.
        /// </summary>
        public ModuleRoboticServoRotor InternalRotor
        {
            get { return servo; }
        }

        /// <summary>
        /// The part object for this robotic rotor.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        ///Target RPM for Robotic Rotor
        /// </summary>
        [KRPCProperty]
        public float TargetRPM { get { return servo.rpmLimit; } set {
                BaseAxisField field = (BaseAxisField)typeof(ModuleRoboticServoRotor).GetField("rpmLimitAxisField", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(servo);
                field.SetValue(value, field.module);
            } }

        /// <summary>
        ///Current RPM for Robotic Rotor
        /// </summary>
        [KRPCProperty]
        public float CurrentRPM { get { return servo.currentRPM; } }

        /// <summary>
        ///Invert Rotor Direction?
        /// </summary>
        [KRPCProperty]
        public bool Inverted { get { return servo.inverted; } set { servo.inverted = value; } }


        /// <summary>
        /// Lock Movement
        /// </summary>
        [KRPCProperty]
        public bool RotationLocked
        {
            get { return servo.servoIsLocked; }
            set
            {
                if (value == true) servo.EngageServoLock();
                else servo.DisengageServoLock();
            }
        }

        /// <summary>
        /// Engage/Disengage Motor
        /// </summary>
        [KRPCProperty]
        public bool MotorEngaged
        {
            get { return servo.servoMotorIsEngaged; }
            set
            {
                if (value == true) servo.EngageMotor();
                else servo.DisengageMotor();
            }
        }

        /// <summary>
        ///Torque Limit Percentage
        /// </summary>
        [KRPCProperty]
        public float TorqueLimit { get { return servo.servoMotorLimit; } set { servo.Fields["servoMotorLimit"].SetValue((float)value, servo); } }


    }
}
