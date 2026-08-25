using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// An alarm. Can be accessed using <see cref="SpaceCenter.AlarmManager"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class Alarm : Equatable<Alarm>, IGameObjectState
    {
        readonly uint id;

        /// <summary>
        /// Create a alarm object from a KSP AlarmTypeBase
        /// </summary>
        public Alarm(AlarmTypeBase alarm)
        {
            if (alarm == null)
                throw new ArgumentNullException(nameof(alarm));
            id = alarm.Id;
        }

        /// <summary>
        /// The KSP Alarm, found again from the id it is known by. The game destroys and
        /// recreates an alarm when it is edited, and builds every alarm again when a game
        /// is loaded, so the alarm this stands for is whichever one now answers to the id.
        /// </summary>
        public AlarmTypeBase InternalAlarm
        {
            get
            {
                var alarm = Find();
                if (alarm == null)
                    throw NotResolvable();
                return alarm;
            }
        }

        /// <summary>
        /// The state of the alarm. It is live while an alarm with the id is in the game's
        /// alarm clock, and destroyed once a running alarm clock has none. A game with no
        /// alarm clock running leaves the alarm dormant.
        /// </summary>
        public GameObjectState GameObjectState
        {
            get
            {
                if (AlarmClockScenario.Instance == null)
                    return GameObjectState.Dormant;
                return Find() != null ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        /// <summary>
        /// The alarm the game currently has under the id, or null if it has none.
        /// </summary>
        AlarmTypeBase Find()
        {
            if (AlarmClockScenario.Instance == null)
                return null;
            AlarmTypeBase alarm;
            AlarmClockScenario.TryGetAlarm(id, out alarm);
            return alarm;
        }

        /// <summary>
        /// The error to raise when no alarm answers to the id, which
        /// <see cref="GameObjectState" /> decides between.
        /// </summary>
        Exception NotResolvable()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                return new ObjectDestroyedException(
                    "The alarm no longer exists, as the game no longer has an alarm with its id.");
            return new InvalidOperationException(
                "The alarm is not loaded, as the game has no alarm clock running. " +
                "It can be used again once the game does.");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(Alarm other)
        {
            return !ReferenceEquals(other, null) && id == other.id;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        /// <summary>
        /// Unique identifier of the alarm.
        /// KSP destroys and recreates an alarm when it is edited.
        /// This id will remain constant between the old and new alarms.
        /// </summary>
        [KRPCProperty]
        public uint ID
        {
            get { return id; }
        }

        /// <summary>
        /// Type of alarm.
        /// </summary>
        [KRPCProperty]
        public AlarmType Type
        {
            get {
                return InternalAlarm.ToAlarmType();
            }
        }

        /// <summary>
        /// Title of the alarm.
        /// </summary>
        [KRPCProperty]
        public string Title
        {
            get {
                return InternalAlarm.title;
            }
            set {
                InternalAlarm.title = value;
            }
        }

        /// <summary>
        /// Description of the alarm.
        /// </summary>
        [KRPCProperty]
        public string Description
        {
            get {
                return InternalAlarm.description;
            }
            set {
                InternalAlarm.description = value;
            }
        }

        /// <summary>
        /// Time the alarm will trigger, in seconds since epoch.
        /// </summary>
        [KRPCProperty]
        public double Time
        {
            get {
                return InternalAlarm.ut;
            }
            set {
                InternalAlarm.ut = value;
            }
        }

        /// <summary>
        /// Time until the alarm triggers, in seconds.
        /// </summary>
        [KRPCProperty]
        public double TimeUntil
        {
            get {
                return InternalAlarm.TimeToAlarm;
            }
        }

        /// <summary>
        /// Seconds between the alarm going off and the event it references.
        /// </summary>
        [KRPCProperty]
        public double EventOffset
        {
            get {
                return InternalAlarm.eventOffset;
            }
            set {
                InternalAlarm.eventOffset = value;
            }
        }

        /// <summary>
        /// Vessel the alarm references. <c>null</c> if it does not reference a vessel.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public Vessel Vessel
        {
            get
            {
                var vessel = InternalAlarm.Vessel;
                return vessel != null ? new Vessel(vessel) : null;
            }
        }

        /// <summary>
        /// Maneuver node the alarm references. Only valid for alarms of type
        /// <see cref="AlarmType.Maneuver"/>.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the alarm is not of
        /// type <see cref="AlarmType.Maneuver"/>.
        /// </remarks>
        [KRPCProperty]
        public Node Node
        {
            get
            {
                var alarm = InternalAlarm;
                var maneuverAlarm = alarm as AlarmTypeManeuver;
                if (maneuverAlarm == null)
                    throw new InvalidOperationException(
                        "Alarm is not a Maneuver alarm, it has no associated maneuver node.");
                var vessel = alarm.Vessel;
                var node = maneuverAlarm.Maneuver;
                if (vessel == null || node == null)
                    throw new InvalidOperationException(
                        "Alarm has no associated maneuver node.");
                return new Node(vessel, node);
            }
            set
            {
                var maneuverAlarm = InternalAlarm as AlarmTypeManeuver;
                if (maneuverAlarm == null)
                    throw new InvalidOperationException(
                        "Alarm is not a Maneuver alarm, it has no associated maneuver node.");
                maneuverAlarm.Maneuver = value.InternalNode;
            }
        }

        /// <summary>
        /// Origin body for the transfer window. Only valid for alarms of type
        /// <see cref="AlarmType.TransferWindow"/>.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the alarm is not of
        /// type <see cref="AlarmType.TransferWindow"/>.
        /// </remarks>
        [KRPCProperty]
        public CelestialBody OriginBody
        {
            get
            {
                var transferAlarm = InternalAlarm as AlarmTypeTransferWindow;
                if (transferAlarm == null)
                    throw new InvalidOperationException(
                        "Alarm is not a TransferWindow alarm, it has no associated origin body.");
                var body = transferAlarm.source;
                if (body == null)
                    throw new InvalidOperationException(
                        "Alarm has no associated origin body.");
                return new CelestialBody(body);
            }
            set
            {
                var transferAlarm = InternalAlarm as AlarmTypeTransferWindow;
                if (transferAlarm == null)
                    throw new InvalidOperationException(
                        "Alarm is not a TransferWindow alarm, it has no associated origin body.");
                transferAlarm.source = value.InternalBody;
            }
        }

        /// <summary>
        /// Destination body for the transfer window. Only valid for alarms of type
        /// <see cref="AlarmType.TransferWindow"/>.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the alarm is not of
        /// type <see cref="AlarmType.TransferWindow"/>.
        /// </remarks>
        [KRPCProperty]
        public CelestialBody DestinationBody
        {
            get
            {
                var transferAlarm = InternalAlarm as AlarmTypeTransferWindow;
                if (transferAlarm == null)
                    throw new InvalidOperationException(
                        "Alarm is not a TransferWindow alarm, it has no associated destination body.");
                var body = transferAlarm.dest;
                if (body == null)
                    throw new InvalidOperationException(
                        "Alarm has no associated destination body.");
                return new CelestialBody(body);
            }
            set
            {
                var transferAlarm = InternalAlarm as AlarmTypeTransferWindow;
                if (transferAlarm == null)
                    throw new InvalidOperationException(
                        "Alarm is not a TransferWindow alarm, it has no associated destination body.");
                transferAlarm.dest = value.InternalBody;
            }
        }

        /// <summary>
        /// The action taken on time warp when the alarm fires.
        /// </summary>
        [KRPCProperty]
        public AlarmWarpAction WarpAction
        {
            get
            {
                return InternalAlarm.actions.warp.ToAlarmWarpAction();
            }
            set
            {
                InternalAlarm.actions.warp = value.FromAlarmWarpAction();
            }
        }

        /// <summary>
        /// The on-screen message behavior when the alarm fires.
        /// </summary>
        [KRPCProperty]
        public AlarmMessageAction MessageAction
        {
            get
            {
                return InternalAlarm.actions.message.ToAlarmMessageAction();
            }
            set
            {
                InternalAlarm.actions.message = value.FromAlarmMessageAction();
            }
        }

        /// <summary>
        /// Whether the alarm plays a sound when it fires.
        /// </summary>
        [KRPCProperty]
        public bool PlaySound
        {
            get
            {
                return InternalAlarm.actions.playSound;
            }
            set
            {
                InternalAlarm.actions.playSound = value;
            }
        }

        /// <summary>
        /// Whether the alarm is deleted automatically once the player has dismissed
        /// the triggered message.
        /// </summary>
        [KRPCProperty]
        public bool DeleteOnDismiss
        {
            get
            {
                return InternalAlarm.actions.deleteWhenDone;
            }
            set
            {
                InternalAlarm.actions.deleteWhenDone = value;
            }
        }

        /// <summary>
        /// Whether the time of the alarm has passed and its actions have been triggered.
        /// </summary>
        [KRPCProperty]
        public bool Triggered
        {
            get
            {
                return InternalAlarm.Triggered;
            }
        }

        /// <summary>
        /// Whether the alarm's actions were triggered and then completed or closed.
        /// </summary>
        [KRPCProperty]
        public bool Actioned
        {
            get
            {
                return InternalAlarm.Actioned;
            }
        }

        /// <summary>
        /// Removes the alarm.
        /// </summary>
        /// <remarks>
        /// Any further use of this object throws an exception.
        /// </remarks>
        [KRPCMethod]
        public void Remove()
        {
            // Resolve first, so that removing an alarm the game does not have reports that
            // rather than passing an id the alarm clock knows nothing about.
            var alarm = InternalAlarm;
            AlarmClockScenario.DeleteAlarm(alarm.Id);
        }
    }
}
