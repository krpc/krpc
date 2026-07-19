using System;
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
        readonly string id;
        KACWrapper.KACAPI.KACAlarm alarm;

        internal Alarm (KACWrapper.KACAPI.KACAlarm innerAlarm)
        {
            alarm = innerAlarm;
            id = innerAlarm.ID;
        }

        /// <summary>
        /// Check if two alarms are equal. Compares the underlying alarm
        /// identifiers, so two objects referring to the same alarm are equal.
        /// </summary>
        public override bool Equals (Alarm other)
        {
            return !ReferenceEquals (other, null) && id == other.id;
        }

        /// <summary>
        /// Hash the alarm.
        /// </summary>
        public override int GetHashCode ()
        {
            return id.GetHashCode ();
        }

        void CheckExists ()
        {
            if (alarm == null)
                throw new InvalidOperationException ("Alarm does not exist");
        }

        /// <summary>
        /// The action that the alarm triggers.
        /// </summary>
        [KRPCProperty]
        public AlarmAction Action {
            get { CheckExists (); return alarm.AlarmAction.ToAlarmAction (); }
            set { CheckExists (); alarm.AlarmAction = value.FromAlarmAction (); }
        }

        /// <summary>
        /// The number of seconds before the event that the alarm will fire.
        /// </summary>
        [KRPCProperty]
        public double Margin {
            get { CheckExists (); return alarm.AlarmMargin; }
            set { CheckExists (); alarm.AlarmMargin = value; }
        }

        /// <summary>
        /// The time at which the alarm will fire.
        /// </summary>
        [KRPCProperty]
        public double Time {
            get { CheckExists (); return alarm.AlarmTime; }
            set { CheckExists (); alarm.AlarmTime = value; }
        }

        /// <summary>
        /// The type of the alarm.
        /// </summary>
        [KRPCProperty]
        public AlarmType Type {
            get { CheckExists (); return alarm.AlarmType.ToAlarmType (); }
        }

        /// <summary>
        /// The unique identifier for the alarm.
        /// </summary>
        [KRPCProperty]
        public string ID {
            get { CheckExists (); return alarm.ID; }
        }

        /// <summary>
        /// The short name of the alarm.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { CheckExists (); return alarm.Name; }
            set { CheckExists (); alarm.Name = value; }
        }

        /// <summary>
        /// The long description of the alarm.
        /// </summary>
        [KRPCProperty]
        public string Notes {
            get { CheckExists (); return alarm.Notes; }
            set { CheckExists (); alarm.Notes = value; }
        }

        /// <summary>
        /// The number of seconds until the alarm will fire.
        /// </summary>
        [KRPCProperty]
        public double Remaining {
            // Computed from the alarm time rather than read through the wrapper:
            // the mod stores its remaining time as a KSPTimeSpan object, which the
            // wrapper cannot unbox to a double.
            get { CheckExists (); return alarm.AlarmTime - Planetarium.GetUniversalTime (); }
        }

        /// <summary>
        /// Whether the alarm is enabled. A disabled alarm does not fire.
        /// </summary>
        [KRPCProperty]
        public bool Enabled {
            get { CheckExists (); return alarm.Enabled; }
            set { CheckExists (); alarm.Enabled = value; }
        }

        /// <summary>
        /// Whether the alarm plays a sound when it fires.
        /// </summary>
        [KRPCProperty]
        public bool PlaySound {
            get { CheckExists (); return alarm.PlaySound; }
            set { CheckExists (); alarm.PlaySound = value; }
        }

        /// <summary>
        /// Whether the alarm has fired. Remains true once the alarm has fired;
        /// stream this or use it in an event expression to react to the alarm
        /// firing.
        /// </summary>
        [KRPCProperty]
        public bool Triggered {
            get { CheckExists (); return alarm.Triggered; }
        }

        /// <summary>
        /// Whether the alarm will be repeated after it has fired.
        /// Only has an effect for alarm types that support repeating
        /// (see <see cref="SupportsRepeat"/>).
        /// </summary>
        [KRPCProperty]
        public bool Repeat {
            get { CheckExists (); return alarm.RepeatAlarm; }
            set { CheckExists (); alarm.RepeatAlarm = value; }
        }

        /// <summary>
        /// Whether this alarm's type supports repeating
        /// (see <see cref="Repeat"/>).
        /// </summary>
        [KRPCProperty]
        public bool SupportsRepeat {
            get { CheckExists (); return alarm.SupportsRepeat; }
        }

        /// <summary>
        /// The time delay to automatically create an alarm after it has fired.
        /// Only has an effect for alarm types that support a repeat period
        /// (see <see cref="SupportsRepeatPeriod"/>).
        /// </summary>
        [KRPCProperty]
        public double RepeatPeriod {
            get { CheckExists (); return alarm.RepeatAlarmPeriod; }
            set { CheckExists (); alarm.RepeatAlarmPeriod = value; }
        }

        /// <summary>
        /// Whether this alarm's type supports a repeat period
        /// (see <see cref="RepeatPeriod"/>).
        /// </summary>
        [KRPCProperty]
        public bool SupportsRepeatPeriod {
            get { CheckExists (); return alarm.SupportsRepeatPeriod; }
        }

        /// <summary>
        /// The vessel that the alarm is attached to.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Vessel Vessel {
            get {
                CheckExists ();
                var vessel = FlightGlobals.Vessels.First (x => x.id.ToString () == alarm.VesselID);
                return new SpaceCenter.Services.Vessel (vessel);
            }
            set {
                CheckExists ();
                alarm.VesselID = value.Id.ToString ();
            }
        }

        /// <summary>
        /// The celestial body the vessel is departing from.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.CelestialBody XferOriginBody {
            get {
                CheckExists ();
                var body = FlightGlobals.Bodies.First (x => alarm.XferOriginBodyName == x.bodyName);
                return new SpaceCenter.Services.CelestialBody (body);
            }
            set {
                CheckExists ();
                alarm.XferOriginBodyName = value.InternalBody.bodyName;
            }
        }

        /// <summary>
        /// The celestial body the vessel is arriving at.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.CelestialBody XferTargetBody {
            get {
                CheckExists ();
                var body = FlightGlobals.Bodies.First (x => alarm.XferTargetBodyName == x.bodyName);
                return new SpaceCenter.Services.CelestialBody (body);
            }
            set {
                CheckExists ();
                alarm.XferTargetBodyName = value.InternalBody.bodyName;
            }
        }

        /// <summary>
        /// Removes the alarm. Any further use of this object throws an exception.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            CheckExists ();
            KACWrapper.KAC.DeleteAlarm (alarm.ID);
            alarm = null;
        }
    }
}
