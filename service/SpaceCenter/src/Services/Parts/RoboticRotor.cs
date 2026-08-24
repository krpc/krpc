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
    /// A robotic rotor. Obtained by calling <see cref="Part.RoboticRotor"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class RoboticRotor : Equatable<RoboticRotor>, IGameObjectState
    {
        ModuleRef servoRef;

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
            servoRef = ModuleRef.ForType<ModuleRoboticServoRotor> (internalPart);
        }

        ModuleRoboticServoRotor InternalServo {
            get { return (ModuleRoboticServoRotor)servoRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// What the game holds for the rotor servo: the state of the part
        /// carrying it, or destroyed once that part no longer has the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return servoRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticRotor other)
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
        /// The part object for this robotic rotor.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Target RPM.
        /// </summary>
        [KRPCProperty]
        public float TargetRPM
        {
            get { return InternalServo.rpmLimit; }
            set {
                var axisField = (BaseAxisField)typeof(ModuleRoboticServoRotor).GetField("rpmLimitAxisField", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(InternalServo);
                axisField.SetValue(value, axisField.module);
            }
        }

        /// <summary>
        /// Current RPM.
        /// </summary>
        [KRPCProperty]
        public float CurrentRPM
        {
            // servo.currentRPM is only refreshed while the part action window is open, so read the
            // live rate of motion directly (this is the value KSP copies into currentRPM).
            get
            {
                return (float)typeof(BaseServo)
                    .GetField("transformRateOfMotion", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(InternalServo);
            }
        }

        /// <summary>
        /// Whether the rotor direction is inverted.
        /// </summary>
        [KRPCProperty]
        public bool Inverted
        {
            get { return InternalServo.inverted; }
            set { InternalServo.inverted = value; }
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
        /// Torque limit percentage.
        /// </summary>
        [KRPCProperty]
        public float TorqueLimit
        {
            get { return InternalServo.servoMotorLimit; }
            set { InternalServo.Fields["servoMotorLimit"].SetValue((float)value, InternalServo); }
        }

        /// <summary>
        /// The maximum torque the rotor can generate, in kilonewtons.
        /// </summary>
        [KRPCProperty]
        public float MaxTorque
        {
            get { return InternalServo.maxTorque; }
            set { InternalServo.maxTorque = value; }
        }

        /// <summary>
        /// The percentage of braking force applied to the rotor.
        /// </summary>
        [KRPCProperty]
        public float BrakePercentage
        {
            get { return InternalServo.brakePercentage; }
            set { InternalServo.brakePercentage = value; }
        }

        /// <summary>
        /// Whether the rotor is currently moving.
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

    }
}
