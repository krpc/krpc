using System.Linq;
using KRPC.KerbalAlarmClock.ExtensionMethods;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.KerbalAlarmClock
{
    /// <summary>
    /// Represents an alarm. Obtained by calling
    /// <see cref="KerbalAlarmClock.Alarms"/>,
    /// <see cref="KerbalAlarmClock.AlarmWithName"/> or
    /// <see cref="KerbalAlarmClock.AlarmsWithType"/>.
    /// </summary>
    [KRPCClass (Service = "KerbalAlarmClock")]
    public class Alarm : Equatable<Alarm>
    {
        readonly KACWrapper.KACAPI.KACAlarm alarm;

        internal Alarm (KACWrapper.KACAPI.KACAlarm innerAlarm)
        {
            alarm = innerAlarm;
        }

        /// <summary>
        /// Check if two alarms are equal.
        /// </summary>
        public override bool Equals (Alarm other)
        {
            return !ReferenceEquals (other, null) && alarm == other.alarm;
        }

        /// <summary>
        /// Hash the alarm.
        /// </summary>
        public override int GetHashCode ()
        {
            return alarm.GetHashCode ();
        }

        /// <summary>
        /// The action that the alarm triggers.
        /// </summary>
        [KRPCProperty]
        public AlarmAction Action {
            get { return alarm.AlarmAction.ToAlarmAction (); }
            set { alarm.AlarmAction = value.FromAlarmAction (); }
        }

        /// <summary>
        /// The number of seconds before the event that the alarm will fire.
        /// </summary>
        [KRPCProperty]
        public double Margin {
            get { return alarm.AlarmMargin; }
            set { alarm.AlarmMargin = value; }
        }

        /// <summary>
        /// The time at which the alarm will fire.
        /// </summary>
        [KRPCProperty]
        public double Time {
            get { return alarm.AlarmTime; }
            set { alarm.AlarmTime = value; }
        }

        /// <summary>
        /// The type of the alarm.
        /// </summary>
        [KRPCProperty]
        public AlarmType Type {
            get { return alarm.AlarmType.ToAlarmType (); }
        }

        /// <summary>
        /// The unique identifier for the alarm.
        /// </summary>
        [KRPCProperty]
        public string ID {
            get { return alarm.ID; }
        }

        /// <summary>
        /// The short name of the alarm.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return alarm.Name; }
            set { alarm.Name = value; }
        }

        /// <summary>
        /// The long description of the alarm.
        /// </summary>
        [KRPCProperty]
        public string Notes {
            get { return alarm.Notes; }
            set { alarm.Notes = value; }
        }

        /// <summary>
        /// The number of seconds until the alarm will fire.
        /// </summary>
        [KRPCProperty]
        public double Remaining {
            get { return alarm.Remaining; }
        }

        /// <summary>
        /// Whether the alarm will be repeated after it has fired.
        /// </summary>
        [KRPCProperty]
        public bool Repeat {
            get { return alarm.RepeatAlarm; }
            set { alarm.RepeatAlarm = value; }
        }

        /// <summary>
        /// The time delay to automatically create an alarm after it has fired.
        /// </summary>
        [KRPCProperty]
        public double RepeatPeriod {
            get { return alarm.RepeatAlarmPeriod; }
            set { alarm.RepeatAlarmPeriod = value; }
        }

        /// <summary>
        /// The vessel that the alarm is attached to.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Vessel Vessel {
            get {
                var vessel = FlightGlobals.Vessels.First (x => x.id.ToString () == alarm.VesselID);
                return new SpaceCenter.Services.Vessel (vessel);
            }
            set {
                alarm.VesselID = value.Id.ToString ();
            }
        }

        /// <summary>
        /// The celestial body the vessel is departing from.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.CelestialBody XferOriginBody {
            get {
                var body = FlightGlobals.Bodies.First (x => alarm.XferOriginBodyName == x.bodyName);
                return new SpaceCenter.Services.CelestialBody (body);
            }
            set {
                alarm.XferOriginBodyName = value.InternalBody.bodyName;
            }
        }

        /// <summary>
        /// The celestial body the vessel is arriving at.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.CelestialBody XferTargetBody {
            get {
                var body = FlightGlobals.Bodies.First (x => alarm.XferTargetBodyName == x.bodyName);
                return new SpaceCenter.Services.CelestialBody (body);
            }
            set {
                alarm.XferTargetBodyName = value.InternalBody.bodyName;
            }
        }

        /// <summary>
        /// Removes the alarm.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            // TODO: delete this object
            KACWrapper.KAC.DeleteAlarm (alarm.ID);
        }
    }
}
