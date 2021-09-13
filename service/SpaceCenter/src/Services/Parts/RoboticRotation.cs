using System;
using Expansions.Serenity;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;


namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A Robotic Rotation servo. Obtained by calling <see cref="Part.RoboticRotation"/>
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class RoboticRotation : Equatable<RoboticRotation>
    {
        readonly ModuleRoboticRotationServo servo;

        internal static bool Is(Part part)
        {
            return part.InternalPart.HasModule<ModuleRoboticRotationServo>();
        }

        internal RoboticRotation(Part part)
        {
            Part = part;
            var internalPart = part.InternalPart;
            servo = internalPart.Module<ModuleRoboticRotationServo>();

            if (servo == null)
                throw new ArgumentException("Part is not a robotic piston");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticRotation other)
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
        /// The KSP Robotic Rotation Servo object.
        /// </summary>
        public ModuleRoboticRotationServo InternalRotation
        {
            get { return servo; }
        }

        /// <summary>
        /// The part object for this robotic servo.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        ///Target Angle for Robotic Servo
        /// </summary>
        [KRPCProperty]
        public float TargetPosition { get { return servo.targetAngle; } set { servo.targetAngle= value; } }

        /// <summary>
        ///Current Angle for Robotic Hinge
        /// </summary>
        [KRPCProperty]
        public float CurrentPosition { get { return servo.currentAngle; } }

        /// <summary>
        /// Target Movement Rate in Degrees/s
        /// </summary>
        [KRPCProperty]
        public float Rate { get { return servo.traverseVelocity; } set { servo.traverseVelocity = value; } }

        /// <summary>
        ///Damping Percentage>
        /// </summary>
        [KRPCProperty]
        public float Damping { get { return servo.hingeDamping; } set { servo.hingeDamping = value; } }

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
        /// Returns Servo to Build Angle Position
        /// </summary>
        [KRPCMethod]
        public void Home()
        {
            servo.targetAngle = servo.launchPosition;
        }

    }
}
