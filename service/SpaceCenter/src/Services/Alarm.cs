using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// An alarm. Can be accessed using <see cref="SpaceCenter.AlarmManager"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class Alarm : Equatable<Alarm>
    {
        readonly uint id;

        /// <summary>
        /// Create a alarm object from a KSP AlarmTypeBase
        /// </summary>
        public Alarm(AlarmTypeBase alarm)
        {
            if (alarm == null)
                throw new ArgumentNullException(nameof(alarm));
            InternalAlarm = alarm;
            id = alarm.Id;
        }

        /// <summary>
        /// The KSP Alarm
        /// </summary>
        public AlarmTypeBase InternalAlarm { get; private set; }

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
            get
            {
                UpdateAlarm();
                return id;
            }
        }

        /// <summary>
        /// Type of alarm.
        /// </summary>
        [KRPCProperty]
        public AlarmType Type
        {
            get {
                UpdateAlarm();
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
                UpdateAlarm();
                return InternalAlarm.title;
            }
            set {
                UpdateAlarm();
                InternalAlarm.title = value;
                UpdateAlarm();
            }
        }

        /// <summary>
        /// Description of the alarm.
        /// </summary>
        [KRPCProperty]
        public string Description
        {
            get {
                UpdateAlarm();
                return InternalAlarm.description;
            }
            set {
                UpdateAlarm();
                InternalAlarm.description = value;
                UpdateAlarm();
            }
        }

        /// <summary>
        /// Time the alarm will trigger, in seconds since epoch.
        /// </summary>
        [KRPCProperty]
        public double Time
        {
            get {
                UpdateAlarm();
                return InternalAlarm.ut;
            }
            set {
                UpdateAlarm();
                InternalAlarm.ut = value;
                UpdateAlarm();
            }
        }

        /// <summary>
        /// Time until the alarm triggers, in seconds.
        /// </summary>
        [KRPCProperty]
        public double TimeUntil
        {
            get {
                UpdateAlarm();
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
                UpdateAlarm();
                return InternalAlarm.eventOffset;
            }
            set {
                UpdateAlarm();
                InternalAlarm.eventOffset = value;
                UpdateAlarm();
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
                UpdateAlarm();
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
        [KRPCProperty(Nullable = true)]
        public Node Node
        {
            get
            {
                UpdateAlarm();
                var maneuverAlarm = InternalAlarm as AlarmTypeManeuver;
                if (maneuverAlarm == null)
                    throw new InvalidOperationException(
                        "Alarm is not a Maneuver alarm, it has no associated maneuver node.");
                var vessel = InternalAlarm.Vessel;
                var node = maneuverAlarm.Maneuver;
                if (vessel == null || node == null)
                    return null;
                return new Node(vessel, node);
            }
            set
            {
                UpdateAlarm();
                var maneuverAlarm = InternalAlarm as AlarmTypeManeuver;
                if (maneuverAlarm == null)
                    throw new InvalidOperationException(
                        "Alarm is not a Maneuver alarm, it has no associated maneuver node.");
                if (ReferenceEquals(value, null))
                    throw new ArgumentNullException(nameof(value));
                maneuverAlarm.Maneuver = value.InternalNode;
                UpdateAlarm();
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
        [KRPCProperty(Nullable = true)]
        public CelestialBody OriginBody
        {
            get
            {
                UpdateAlarm();
                var transferAlarm = InternalAlarm as AlarmTypeTransferWindow;
                if (transferAlarm == null)
                    throw new InvalidOperationException(
                        "Alarm is not a TransferWindow alarm, it has no associated origin body.");
                var body = transferAlarm.source;
                return body != null ? new CelestialBody(body) : null;
            }
            set
            {
                UpdateAlarm();
                var transferAlarm = InternalAlarm as AlarmTypeTransferWindow;
                if (transferAlarm == null)
                    throw new InvalidOperationException(
                        "Alarm is not a TransferWindow alarm, it has no associated origin body.");
                if (ReferenceEquals(value, null))
                    throw new ArgumentNullException(nameof(value));
                transferAlarm.source = value.InternalBody;
                UpdateAlarm();
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
        [KRPCProperty(Nullable = true)]
        public CelestialBody DestinationBody
        {
            get
            {
                UpdateAlarm();
                var transferAlarm = InternalAlarm as AlarmTypeTransferWindow;
                if (transferAlarm == null)
                    throw new InvalidOperationException(
                        "Alarm is not a TransferWindow alarm, it has no associated destination body.");
                var body = transferAlarm.dest;
                return body != null ? new CelestialBody(body) : null;
            }
            set
            {
                UpdateAlarm();
                var transferAlarm = InternalAlarm as AlarmTypeTransferWindow;
                if (transferAlarm == null)
                    throw new InvalidOperationException(
                        "Alarm is not a TransferWindow alarm, it has no associated destination body.");
                if (ReferenceEquals(value, null))
                    throw new ArgumentNullException(nameof(value));
                transferAlarm.dest = value.InternalBody;
                UpdateAlarm();
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
                UpdateAlarm();
                return InternalAlarm.actions.warp.ToAlarmWarpAction();
            }
            set
            {
                UpdateAlarm();
                InternalAlarm.actions.warp = value.FromAlarmWarpAction();
                UpdateAlarm();
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
                UpdateAlarm();
                return InternalAlarm.actions.message.ToAlarmMessageAction();
            }
            set
            {
                UpdateAlarm();
                InternalAlarm.actions.message = value.FromAlarmMessageAction();
                UpdateAlarm();
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
                UpdateAlarm();
                return InternalAlarm.actions.playSound;
            }
            set
            {
                UpdateAlarm();
                InternalAlarm.actions.playSound = value;
                UpdateAlarm();
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
                UpdateAlarm();
                return InternalAlarm.actions.deleteWhenDone;
            }
            set
            {
                UpdateAlarm();
                InternalAlarm.actions.deleteWhenDone = value;
                UpdateAlarm();
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
                UpdateAlarm();
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
                UpdateAlarm();
                return InternalAlarm.Actioned;
            }
        }

        /// <summary>
        /// Removes the alarm.
        /// </summary>
        [KRPCMethod]
        public void Remove()
        {
            UpdateAlarm();
            AlarmClockScenario.DeleteAlarm(id);
            InternalAlarm = null;
        }

        private void UpdateAlarm()
        {
            if (InternalAlarm == null)
                throw new InvalidOperationException("Alarm does not exist");
            AlarmTypeBase alarm;
            AlarmClockScenario.TryGetAlarm(id, out alarm);
            if (alarm != null)
                InternalAlarm = alarm;
        }
    }
}
