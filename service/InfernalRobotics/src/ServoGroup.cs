using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// A group of servos, obtained by calling <see cref="InfernalRobotics.ServoGroups"/>
    /// or <see cref="InfernalRobotics.ServoGroupWithName"/>. Represents the "Servo Groups"
    /// in the InfernalRobotics UI.
    /// </summary>
    [KRPCClass (Service = "InfernalRobotics")]
    public class ServoGroup : Equatable<ServoGroup>
    {
        readonly IRWrapper.IServoGroup servoGroup;

        internal ServoGroup (IRWrapper.IServoGroup innerServoGroup)
        {
            servoGroup = innerServoGroup;
        }

        /// <summary>
        /// Check if servo groups are equivalent.
        /// </summary>
        public override bool Equals (ServoGroup other)
        {
            return !ReferenceEquals (other, null) && servoGroup == other.servoGroup;
        }

        /// <summary>
        /// Hash the servo group.
        /// </summary>
        public override int GetHashCode ()
        {
            return servoGroup.GetHashCode ();
        }

        /// <summary>
        /// The name of the group.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return servoGroup.Name; }
            set { servoGroup.Name = value; }
        }

        /// <summary>
        /// The key assigned to be the "forward" key for the group.
        /// </summary>
        [KRPCProperty]
        public string ForwardKey {
            get { return servoGroup.ForwardKey; }
            set { servoGroup.ForwardKey = value; }
        }

        /// <summary>
        /// The key assigned to be the "reverse" key for the group.
        /// </summary>
        [KRPCProperty]
        public string ReverseKey {
            get { return servoGroup.ReverseKey; }
            set { servoGroup.ReverseKey = value; }
        }

        /// <summary>
        /// The speed multiplier for the group.
        /// </summary>
        [KRPCProperty]
        public float Speed {
            get { return servoGroup.GroupSpeedFactor; }
            set { servoGroup.GroupSpeedFactor = value; }
        }

        /// <summary>
        /// Whether the group is expanded in the InfernalRobotics UI.
        /// </summary>
        [KRPCProperty]
        public bool Expanded {
            get { return servoGroup.Expanded; }
            set { servoGroup.Expanded = value; }
        }

        /// <summary>
        /// The vessel the group belongs to, or <c>null</c> if it is not available.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public SpaceCenter.Services.Vessel Vessel {
            get {
                var vessel = servoGroup.Vessel;
                return vessel != null ? new SpaceCenter.Services.Vessel (vessel) : null;
            }
        }

        /// <summary>
        /// The direction the group is currently moving in: -1 for reverse, 0 for stopped
        /// and 1 for forward.
        /// </summary>
        [KRPCProperty]
        public int MovingDirection {
            get { return servoGroup.MovingDirection; }
        }

        /// <summary>
        /// Whether the group is in advanced mode.
        /// </summary>
        [KRPCProperty]
        public bool AdvancedMode {
            get { return servoGroup.AdvancedMode; }
            set { servoGroup.AdvancedMode = value; }
        }

        /// <summary>
        /// The total rate at which the servos in the group consume electric charge, in units
        /// per second, when moving.
        /// </summary>
        [KRPCProperty]
        public float ElectricChargeRequired {
            get { return servoGroup.TotalElectricChargeRequirement; }
        }

        /// <summary>
        /// Whether the build aid is enabled for the group.
        /// </summary>
        [KRPCProperty]
        public bool BuildAid {
            get { return servoGroup.BuildAid; }
            set { servoGroup.BuildAid = value; }
        }

        /// <summary>
        /// Whether inverse kinematics is active for the group.
        /// </summary>
        [KRPCProperty]
        public bool IKActive {
            get { return servoGroup.IKActive; }
            set { servoGroup.IKActive = value; }
        }

        /// <summary>
        /// The servos that are in the group.
        /// </summary>
        [KRPCProperty]
        public IList<Servo> Servos {
            get { return servoGroup.Servos.Select (x => new Servo (x)).ToList (); }
        }

        /// <summary>
        /// Returns the servo with the given <paramref name="name"/> from this group,
        /// or <c>null</c> if none exists.
        /// </summary>
        /// <param name="name">Name of servo to find.</param>
        [KRPCMethod (Nullable = true)]
        public Servo ServoWithName (string name)
        {
            var servo = servoGroup.Servos.FirstOrDefault (x => x.Name == name);
            return servo != null ? new Servo (servo) : null;
        }

        /// <summary>
        /// The parts containing the servos in the group.
        /// </summary>
        [KRPCProperty]
        public IList<SpaceCenter.Services.Parts.Part> Parts {
            get { return servoGroup.Servos.Select (x => new SpaceCenter.Services.Parts.Part (x.HostPart)).ToList (); }
        }

        /// <summary>
        /// Moves all of the servos in the group to the right.
        /// </summary>
        [KRPCMethod]
        public void MoveRight ()
        {
            servoGroup.MoveRight ();
        }

        /// <summary>
        /// Moves all of the servos in the group to the left.
        /// </summary>
        [KRPCMethod]
        public void MoveLeft ()
        {
            servoGroup.MoveLeft ();
        }

        /// <summary>
        /// Moves all of the servos in the group to the center.
        /// </summary>
        [KRPCMethod]
        public void MoveCenter ()
        {
            servoGroup.MoveCenter ();
        }

        /// <summary>
        /// Moves all of the servos in the group to the next preset.
        /// </summary>
        [KRPCMethod]
        public void MoveNextPreset ()
        {
            servoGroup.MoveNextPreset ();
        }

        /// <summary>
        /// Moves all of the servos in the group to the previous preset.
        /// </summary>
        [KRPCMethod]
        public void MovePrevPreset ()
        {
            servoGroup.MovePrevPreset ();
        }

        /// <summary>
        /// Stops the servos in the group.
        /// </summary>
        [KRPCMethod]
        public void Stop ()
        {
            servoGroup.Stop ();
        }
    }
}
