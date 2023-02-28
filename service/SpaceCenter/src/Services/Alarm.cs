using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// An Alarm. Can be accessed using <see cref="SpaceCenter.AlarmClock"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class Alarm : Equatable<Alarm>
    {
        /// <summary>
        /// Create a alarm object from a KSP alarmTypeBase
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
        /// Type of Alarm
        /// </summary>
        [KRPCProperty]
        public string Type
        {
            get { UpdateAlarm(); return InternalAlarm.TypeName; }
        }

        /// <summary>
        /// Title of the Alarm
        /// </summary>
        [KRPCProperty]
        public string Title
        {
            get { UpdateAlarm(); return InternalAlarm.title; }


        }

        /// <summary>
        /// Description of the contract.
        /// </summary>
        [KRPCProperty]
        public string Description
        {
            get { UpdateAlarm(); return InternalAlarm.description; }
        }

        /// <summary>
        /// Time the Alarm will trigger
        /// </summary>
        [KRPCProperty]
        public double UT
        {
            get { UpdateAlarm(); return InternalAlarm.ut; }
        }

        /// <summary>
        /// Time until the alarm triggers
        /// </summary>
        [KRPCProperty]
        public double TimeTill
        {
            get { UpdateAlarm(); return InternalAlarm.TimeToAlarm; }
        }

        /// <summary>
        /// Seconds betwen the alarm going off and the event it references
        /// </summary>
        [KRPCProperty]
        public double EventOffset
        {
            get { UpdateAlarm(); return InternalAlarm.eventOffset; }
        }

        /// <summary>
        /// Vessel the alarm references
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public Vessel Vessel
        {
            get
            {
                UpdateAlarm();
                KRPC.SpaceCenter.Services.Vessel V = new Vessel(InternalAlarm.Vessel);


                return V;
            }
        }


        /// <summary>
        /// Unique ID of alarm
        /// KSP destroys an old alarm and creates a new one each time an alarm is edited.
        /// This ID will remain constant between the old and new alarms though, so this is the value
        /// you want to store and each time you want to access an alarm, get the current alarm with
        /// the correct ID value.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public int ID
        {
            get
            {
                UpdateAlarm();
                return (int)InternalAlarm.Id;
            }
        }


        private void UpdateAlarm()
        {
            AlarmTypeBase newalarm;
            AlarmClockScenario.TryGetAlarm(InternalAlarm.Id, out newalarm);
            InternalAlarm = newalarm;
        }

    }
}
