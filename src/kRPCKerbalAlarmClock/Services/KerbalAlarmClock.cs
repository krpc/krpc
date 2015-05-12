using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPCKerbalAlarmClock.ExtensionMethods;

namespace KRPCKerbalAlarmClock.Services
{
    [KRPCService (GameScene = GameScene.Flight)]
    public static class KerbalAlarmClock
    {
        static void CheckAPI ()
        {
            if (!KACWrapper.APIReady)
                throw new InvalidOperationException ("Kerbal Alarm Clock is not available");
        }

        [KRPCProperty]
        public static IList<Alarm> Alarms {
            get {
                CheckAPI ();
                return KACWrapper.KAC.Alarms.Select (x => new Alarm (x)).ToList ();
            }
        }

        [KRPCProcedure]
        public static Alarm AlarmWithName (string name)
        {
            CheckAPI ();
            var alarm = KACWrapper.KAC.Alarms.FirstOrDefault (x => x.Name == name);
            return alarm != null ? new Alarm (alarm) : null;
        }

        [KRPCProcedure]
        public static IList<Alarm> AlarmsWithType (AlarmType type)
        {
            CheckAPI ();
            return KACWrapper.KAC.Alarms
                .Where (x => x.AlarmType == type.FromAlarmType ())
                .Select (x => new Alarm (x))
                .ToList ();
        }

        [KRPCProcedure]
        public static Alarm CreateAlarm (AlarmType type, string name, double ut)
        {
            var id = KACWrapper.KAC.CreateAlarm (type.FromAlarmType (), name, ut);
            return new Alarm (KACWrapper.KAC.Alarms.First (x => x.ID == id));
        }
    }
}
