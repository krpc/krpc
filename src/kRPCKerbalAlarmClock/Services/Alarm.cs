using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCKerbalAlarmClock.ExtensionMethods;

namespace KRPCKerbalAlarmClock.Services
{
    [KRPCClass (Service = "KerbalAlarmClock")]
    public sealed class Alarm : Equatable<Alarm>
    {
        readonly KACWrapper.KACAPI.KACAlarm alarm;

        internal Alarm (KACWrapper.KACAPI.KACAlarm alarm)
        {
            this.alarm = alarm;
        }

        public override bool Equals (Alarm obj)
        {
            return alarm == obj.alarm;
        }

        public override int GetHashCode ()
        {
            return alarm.GetHashCode ();
        }

        [KRPCProperty]
        public AlarmAction Action {
            get { return alarm.AlarmAction.ToAlarmAction (); }
            set { alarm.AlarmAction = value.FromAlarmAction (); }
        }

        [KRPCProperty]
        public double Margin {
            get { return alarm.AlarmMargin; }
            set { alarm.AlarmMargin = value; }
        }

        [KRPCProperty]
        public double Time {
            get { return alarm.AlarmTime; }
            set { alarm.AlarmTime = value; }
        }

        [KRPCProperty]
        public AlarmType Type {
            get { return alarm.AlarmType.ToAlarmType (); }
        }

        [KRPCProperty]
        public string ID {
            get { return alarm.ID; }
        }

        [KRPCProperty]
        public string Name {
            get { return alarm.Name; }
            set { alarm.Name = value; }
        }

        [KRPCProperty]
        public string Notes {
            get { return alarm.Notes; }
            set { alarm.Notes = value; }
        }

        [KRPCProperty]
        public double Remaining {
            get { return alarm.Remaining; }
        }

        [KRPCProperty]
        public bool Repeat {
            get { return alarm.RepeatAlarm; }
            set { alarm.RepeatAlarm = value; }
        }

        [KRPCProperty]
        public double RepeatPeriod {
            get { return alarm.RepeatAlarmPeriod; }
            set { alarm.RepeatAlarmPeriod = value; }
        }

        [KRPCProperty]
        public KRPCSpaceCenter.Services.Vessel Vessel {
            get {
                var vessel = FlightGlobals.Vessels.First (x => x.id.ToString () == alarm.VesselID);
                return new KRPCSpaceCenter.Services.Vessel (vessel);
            }
            set {
                alarm.VesselID = value.InternalVessel.id.ToString ();
            }
        }

        [KRPCProperty]
        public KRPCSpaceCenter.Services.CelestialBody XferOriginBody {
            get {
                var body = FlightGlobals.Bodies.First (x => alarm.XferOriginBodyName == x.bodyName);
                return new KRPCSpaceCenter.Services.CelestialBody (body);
            }
            set {
                alarm.XferOriginBodyName = value.InternalBody.bodyName;
            }
        }

        [KRPCProperty]
        public KRPCSpaceCenter.Services.CelestialBody XferTargetBody {
            get {
                var body = FlightGlobals.Bodies.First (x => alarm.XferTargetBodyName == x.bodyName);
                return new KRPCSpaceCenter.Services.CelestialBody (body);
            }
            set {
                alarm.XferTargetBodyName = value.InternalBody.bodyName;
            }
        }

        [KRPCMethod]
        public void Delete ()
        {
            // TODO: delete this object
            KACWrapper.KAC.DeleteAlarm (alarm.ID);
        }
    }
}
