using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// A group of servos, obtained by calling <see cref="InfernalRobotics.ServoGroups"/>
    /// or <see cref="InfernalRobotics.ServoGroupWithName"/>. Represents the "Servo Groups"
    /// in the InfernalRobotics UI.
    /// </summary>
    [KRPCClass (Service = "InfernalRobotics")]
    public class ServoGroup : Equatable<ServoGroup>, IGameObjectState
    {
        // The vessel the group belongs to and the name that picks it out among that
        // vessel's groups, which is what the mod itself groups servos by. Both are fixed for
        // the object's lifetime: the object store compares and hashes the objects it holds,
        // so a name that moved would leave two objects naming one group, and the store
        // unable to tell which entry belongs to which. The group object the mod hands out is
        // built afresh every time the groups are listed, so holding one would instead leave
        // two objects for one group comparing unequal.
        readonly SpaceCenter.Services.Vessel vessel;
        readonly string name;

        internal ServoGroup (SpaceCenter.Services.Vessel groupVessel, string groupName)
        {
            if (ReferenceEquals (groupVessel, null))
                throw new ArgumentNullException (nameof (groupVessel));
            vessel = groupVessel;
            name = groupName;
        }

        /// <summary>
        /// Check if servo groups are equivalent.
        /// </summary>
        public override bool Equals (ServoGroup other)
        {
            return !ReferenceEquals (other, null) && vessel == other.vessel && name == other.name;
        }

        /// <summary>
        /// Hash the servo group.
        /// </summary>
        public override int GetHashCode ()
        {
            // A group the mod names with nothing at all still has to hash, as the object
            // store hashes every object it holds; a name of null counts as zero.
            return Hash.Of (vessel).And (name);
        }

        /// <summary>
        /// What the game holds for the group. It belongs to its vessel, so it is exactly as
        /// live, dormant or destroyed as the vessel, and destroyed when a vessel that the
        /// mod can be asked about has no group of the name. The mod not being ready says
        /// nothing about any group: its controller only ever tracks the active vessel.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = vessel.GameObjectState;
                if (state != GameObjectState.Live)
                    return state;
                if (!IRWrapper.AssemblyExists)
                    return GameObjectState.Dormant;
                return Find () != null ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        // The group the mod has on the vessel under the name, or null if it has none.
        IRWrapper.IServoGroup Find ()
        {
            var groups = IRWrapper.ServoGroupsForVessel (vessel.InternalVessel);
            for (var i = 0; i < groups.Count; i++) {
                if (groups [i].Name == name)
                    return groups [i];
            }
            return null;
        }

        // The group the mod has on the vessel now. Every member reaches the mod through
        // this, so a group taken before a game state was replaced drives the servos that
        // stand in its place rather than the ones it was built from.
        IRWrapper.IServoGroup Internal {
            get {
                var group = Find ();
                if (group == null)
                    throw NotResolvable ();
                return group;
            }
        }

        Exception NotResolvable ()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                return new ObjectDestroyedException (
                    "The servo group no longer exists, as its vessel no longer has a group " +
                    "of its name.");
            return new InvalidOperationException (
                "The servo group is not available, as Infernal Robotics is not installed.");
        }

        /// <summary>
        /// The name of the group.
        /// </summary>
        /// <remarks>
        /// Renaming a group renames it for every servo in it. The name is what this object
        /// names the group by, and is fixed for its lifetime, so every object for the group
        /// stands for one the vessel no longer has once it is renamed, the object the rename
        /// was made through included. An object for the group has to be obtained again under
        /// the new name.
        /// </remarks>
        [KRPCProperty]
        public string Name {
            get { return Internal.Name; }
            set { Internal.Name = value; }
        }

        /// <summary>
        /// The key assigned to be the "forward" key for the group.
        /// </summary>
        [KRPCProperty]
        public string ForwardKey {
            get { return Internal.ForwardKey; }
            set { Internal.ForwardKey = value; }
        }

        /// <summary>
        /// The key assigned to be the "reverse" key for the group.
        /// </summary>
        [KRPCProperty]
        public string ReverseKey {
            get { return Internal.ReverseKey; }
            set { Internal.ReverseKey = value; }
        }

        /// <summary>
        /// The speed multiplier for the group.
        /// </summary>
        [KRPCProperty]
        public float Speed {
            get { return Internal.GroupSpeedFactor; }
            set { Internal.GroupSpeedFactor = value; }
        }

        /// <summary>
        /// Whether the group is expanded in the InfernalRobotics UI.
        /// </summary>
        [KRPCProperty]
        public bool Expanded {
            get { return Internal.Expanded; }
            set { Internal.Expanded = value; }
        }

        /// <summary>
        /// The vessel the group belongs to.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Vessel Vessel {
            get { return vessel; }
        }

        /// <summary>
        /// The direction the group is currently moving in: -1 for reverse, 0 for stopped
        /// and 1 for forward.
        /// </summary>
        [KRPCProperty]
        public int MovingDirection {
            get { return Internal.MovingDirection; }
        }

        /// <summary>
        /// Whether the group is in advanced mode.
        /// </summary>
        [KRPCProperty]
        public bool AdvancedMode {
            get { return Internal.AdvancedMode; }
            set { Internal.AdvancedMode = value; }
        }

        /// <summary>
        /// The total rate at which the servos in the group consume electric charge, in units
        /// per second, when moving.
        /// </summary>
        [KRPCProperty]
        public float ElectricChargeRequired {
            get { return Internal.TotalElectricChargeRequirement; }
        }

        /// <summary>
        /// Whether the build aid is enabled for the group.
        /// </summary>
        [KRPCProperty]
        public bool BuildAid {
            get { return Internal.BuildAid; }
            set { Internal.BuildAid = value; }
        }

        /// <summary>
        /// Whether inverse kinematics is active for the group.
        /// </summary>
        [KRPCProperty]
        public bool IKActive {
            get { return Internal.IKActive; }
            set { Internal.IKActive = value; }
        }

        /// <summary>
        /// The servos that are in the group.
        /// </summary>
        [KRPCProperty]
        public IList<Servo> Servos {
            get { return Internal.Servos.Select (x => new Servo (x)).ToList (); }
        }

        /// <summary>
        /// Returns the servo with the given <paramref name="name"/> from this group,
        /// or <c>null</c> if none exists.
        /// </summary>
        /// <param name="name">Name of servo to find.</param>
        [KRPCMethod (Nullable = true)]
        public Servo ServoWithName (string name)
        {
            var servo = Internal.Servos.FirstOrDefault (x => x.Name == name);
            return servo != null ? new Servo (servo) : null;
        }

        /// <summary>
        /// The parts containing the servos in the group.
        /// </summary>
        [KRPCProperty]
        public IList<SpaceCenter.Services.Parts.Part> Parts {
            get { return Internal.Servos.Select (x => new SpaceCenter.Services.Parts.Part (x.HostPart)).ToList (); }
        }

        /// <summary>
        /// Moves all of the servos in the group to the right.
        /// </summary>
        [KRPCMethod]
        public void MoveRight ()
        {
            Internal.MoveRight ();
        }

        /// <summary>
        /// Moves all of the servos in the group to the left.
        /// </summary>
        [KRPCMethod]
        public void MoveLeft ()
        {
            Internal.MoveLeft ();
        }

        /// <summary>
        /// Moves all of the servos in the group to the center.
        /// </summary>
        [KRPCMethod]
        public void MoveCenter ()
        {
            Internal.MoveCenter ();
        }

        /// <summary>
        /// Moves all of the servos in the group to the next preset.
        /// </summary>
        [KRPCMethod]
        public void MoveNextPreset ()
        {
            Internal.MoveNextPreset ();
        }

        /// <summary>
        /// Moves all of the servos in the group to the previous preset.
        /// </summary>
        [KRPCMethod]
        public void MovePrevPreset ()
        {
            Internal.MovePrevPreset ();
        }

        /// <summary>
        /// Stops the servos in the group.
        /// </summary>
        [KRPCMethod]
        public void Stop ()
        {
            Internal.Stop ();
        }
    }
}
