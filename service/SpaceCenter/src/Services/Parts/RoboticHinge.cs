using System;
using Expansions.Serenity;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;


namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A Robotic Hinge Part. Obtained by calling <see cref="Part.RoboticHinge"/>
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
            Part = part;
            var internalPart = part.InternalPart;
            servo = internalPart.Module<ModuleRoboticServoHinge>();

            if (servo == null)
                throw new ArgumentException("Part is not a robotic servo");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticHinge other)
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
        /// The KSP Robotic Servo Hinge object.
        /// </summary>
        public ModuleRoboticServoHinge InternalHinge
        {
            get { return servo; }
        }

        /// <summary>
        /// The part object for this robotic hinge.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        ///Target Angle for Robotic Hinge
        /// </summary>
        [KRPCProperty]
        public float TargetAngle { get { return servo.targetAngle; } set { servo.targetAngle = value; } }

        /// <summary>
        ///Current Angle for Robotic Hinge
        /// </summary>
        [KRPCProperty]
        public float CurrentAngle { get { return servo.currentAngle; } }

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
        public bool HingeLocked { get { return servo.servoIsLocked; } set {
                if (value == true) servo.EngageServoLock();
                else servo.DisengageServoLock();
            } }

        /// <summary>
        /// Engage/Disengage Motor
        /// </summary>
        [KRPCProperty]
        public bool MotorEngaged { get { return servo.servoMotorIsEngaged; } set
            {
                if (value == true) servo.EngageMotor();
                else servo.DisengageMotor();
            } }

        /// <summary>
        ///Torque Limit Percentage
        /// </summary>
        [KRPCProperty]
        public float TorqueLimit { get { return servo.servoMotorLimit; } set { servo.servoMotorLimit = value; } }

        /// <summary>
        /// Returns Hinge to Build Angle Position
        /// </summary>
        [KRPCMethod]
        public void Home()
        { 
            servo.targetAngle = servo.modelInitialAngle;
        }

    }
}
