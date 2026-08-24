using System;
using Expansions.Serenity;
using KRPC.Service;
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
    [KRPCClass(Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class RoboticPiston : Equatable<RoboticPiston>, IGameObjectState
    {
        ModuleRef servoRef;

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
            servoRef = ModuleRef.ForType<ModuleRoboticServoPiston> (internalPart);
        }

        ModuleRoboticServoPiston InternalServo {
            get { return (ModuleRoboticServoPiston)servoRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// What the game holds for the piston servo: the state of the part
        /// carrying it, or destroyed once that part no longer has the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return servoRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticPiston other)
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
        /// The part object for this robotic piston.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Target extension of the piston.
        /// </summary>
        [KRPCProperty]
        public float TargetExtension
        {
            get { return InternalServo.targetExtension; }
            set { SetExtension(value); }
        }

        /// <summary>
        /// Current extension of the piston.
        /// </summary>
        [KRPCProperty]
        public float CurrentExtension
        {
            get { return InternalServo.currentExtension; }
        }

        /// <summary>
        /// The minimum extension of the piston, in meters.
        /// </summary>
        [KRPCProperty]
        public float MinExtension
        {
            get { return InternalServo.softMinMaxExtension.x; }
            set { InternalServo.SetSoftLimits("targetExtension", new Vector2(value, InternalServo.softMinMaxExtension.y)); }
        }

        /// <summary>
        /// The maximum extension of the piston, in meters.
        /// </summary>
        [KRPCProperty]
        public float MaxExtension
        {
            get { return InternalServo.softMinMaxExtension.y; }
            set { InternalServo.SetSoftLimits("targetExtension", new Vector2(InternalServo.softMinMaxExtension.x, value)); }
        }

        /// <summary>
        /// Target movement rate in meters per second.
        /// </summary>
        [KRPCProperty]
        public float Rate {
            get { return InternalServo.traverseVelocity; }
            set { InternalServo.traverseVelocity = value; }
        }

        /// <summary>
        /// Damping percentage.
        /// </summary>
        [KRPCProperty]
        public float Damping {
            get { return InternalServo.pistonDamping; }
            set { InternalServo.pistonDamping = value; }
        }

        /// <summary>
        /// Lock movement.
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
        /// Move piston to its built position.
        /// </summary>
        [KRPCMethod]
        public void MoveHome()
        {
            SetExtension(InternalServo.launchPosition);
        }

        private void SetExtension(float value)
        {
            BaseAxisField field = (BaseAxisField)typeof(ModuleRoboticServoPiston)
                .GetField("targetExtensionAxisField", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(InternalServo);
            field.SetValue((float)value, field.module);
        }
    }
}
