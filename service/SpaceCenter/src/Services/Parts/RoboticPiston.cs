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
    /// A Robotic Piston Part. Obtained by calling <see cref="Part.RoboticPiston"/>
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
            Part = part;
            var internalPart = part.InternalPart;
            servo = internalPart.Module<ModuleRoboticServoPiston>();

            if (servo == null)
                throw new ArgumentException("Part is not a robotic piston");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticPiston other)
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
        /// The KSP Robotic Servo Piston object.
        /// </summary>
        public ModuleRoboticServoPiston InternalPiston
        {
            get { return servo; }
        }

        /// <summary>
        /// The part object for this robotic piston.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        ///Target Extension for robotic piston.
        /// </summary>
        [KRPCProperty]
        public float TargetPosition { get { return servo.targetExtension; } set { SetExtension(value);  } }

        /// <summary>
        ///Current Extension of piston
        /// </summary>
        [KRPCProperty]
        public float CurrentPosition { get { return servo.currentExtension; } }

        /// <summary>
        /// Target Movement Rate in Degrees/s
        /// </summary>
        [KRPCProperty]
        public float Rate { get { return servo.traverseVelocity; } set { servo.traverseVelocity = value; } }

        /// <summary>
        ///Damping Percentage>
        /// </summary>
        [KRPCProperty]
        public float Damping { get { return servo.pistonDamping; } set { servo.pistonDamping = value; } }

        /// <summary>
        /// Lock Movement
        /// </summary>
        [KRPCProperty]
        public bool PistonLocked
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
        /// Returns Piston to VAB Position
        /// </summary>
        [KRPCMethod]
        public void Home()
        {
            SetExtension(servo.launchPosition);
            
        }

        /// <summary>
        /// Set piston extension
        /// </summary>
        public void SetExtension(float value)
        {
            BaseAxisField field = (BaseAxisField)typeof(ModuleRoboticServoPiston)
                .GetField("targetExtensionAxisField", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(servo);
            field.SetValue((float)value, field.module);
        }

    }
}
