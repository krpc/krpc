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
    /// A robotic hinge. Obtained by calling <see cref="Part.RoboticHinge"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class RoboticHinge : Equatable<RoboticHinge>, IGameObjectState
    {
        ModuleRef servoRef;

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
            servoRef = ModuleRef.ForType<ModuleRoboticServoHinge> (internalPart);
        }

        ModuleRoboticServoHinge InternalServo {
            get { return (ModuleRoboticServoHinge)servoRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// The state of the part carrying the hinge servo, or destroyed once that part
        /// loses the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return servoRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticHinge other)
        {
            return !ReferenceEquals(other, null) && Part == other.Part && servoRef == other.servoRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return Hash.Of (Part).And (servoRef);
        }

        /// <summary>
        /// The part object for this robotic hinge.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Target angle.
        /// </summary>
        [KRPCProperty]
        public float TargetAngle {
            get { return InternalServo.targetAngle; }
            set { InternalServo.targetAngle = value; }
        }

        /// <summary>
        /// Current angle.
        /// </summary>
        [KRPCProperty]
        public float CurrentAngle
        {
            get
            {
                return InternalServo.modelInitialAngle + (float)typeof(ModuleRoboticServoHinge)
                    .GetMethod("currentTransformAngle", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(InternalServo, null);
            }
        }

        /// <summary>
        /// The minimum angle the hinge can move to, in degrees.
        /// </summary>
        [KRPCProperty]
        public float MinAngle
        {
            get { return InternalServo.softMinMaxAngles.x; }
            set { InternalServo.SetSoftLimits("targetAngle", new Vector2(value, InternalServo.softMinMaxAngles.y)); }
        }

        /// <summary>
        /// The maximum angle the hinge can move to, in degrees.
        /// </summary>
        [KRPCProperty]
        public float MaxAngle
        {
            get { return InternalServo.softMinMaxAngles.y; }
            set { InternalServo.SetSoftLimits("targetAngle", new Vector2(InternalServo.softMinMaxAngles.x, value)); }
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
        /// Lock movement.
        /// </summary>
        [KRPCProperty]
        public bool Locked
        {
            get { return InternalServo.servoIsLocked; }
            set {
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
        /// Move hinge to its built position.
        /// </summary>
        [KRPCMethod]
        public void MoveHome()
        {
            InternalServo.targetAngle = InternalServo.modelInitialAngle;
        }
    }
}
