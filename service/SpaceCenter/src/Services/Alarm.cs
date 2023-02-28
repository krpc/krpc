using System.Collections.Generic;
using System.Linq;
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
        /// <summary>
        /// Create a alarm object from a KSP AlarmTypeBase
        /// </summary>
        public Alarm(AlarmTypeBase alarm)
        {
            InternalAlarm = alarm;
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
            return !ReferenceEquals(other, null) && InternalAlarm.Id == other.InternalAlarm.Id;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return InternalAlarm.GetHashCode();
        }

        /// <summary>
        /// Unique identifier of the alarm.
        /// KSP destroys and recreates an alarm when it is edited.
        /// This id will remain constant between the old and new alarms.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public uint ID
        {
            get
            {
                UpdateAlarm();
                return InternalAlarm.Id;
            }
        }

        /// <summary>
        /// Type of alarm
        /// </summary>
        [KRPCProperty]
        public string Type
        {
            get {
                UpdateAlarm();
                return InternalAlarm.TypeName;
            }
        }

        /// <summary>
        /// Title of the alarm
        /// </summary>
        [KRPCProperty]
        public string Title
        {
            get {
                UpdateAlarm();
                return InternalAlarm.title;
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
        }

        /// <summary>
        /// Time the alarm will trigger.
        /// </summary>
        [KRPCProperty]
        public double Time
        {
            get {
                UpdateAlarm();
                return InternalAlarm.ut;
            }
        }

        /// <summary>
        /// Time until the alarm triggers.
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
        }

        /// <summary>
        /// Vessel the alarm references. <c>null</c> if it does not reference a vesssel.
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

        private void UpdateAlarm()
        {
            AlarmTypeBase alarm;
            AlarmClockScenario.TryGetAlarm(InternalAlarm.Id, out alarm);
            InternalAlarm = alarm;
        }
    }
}
