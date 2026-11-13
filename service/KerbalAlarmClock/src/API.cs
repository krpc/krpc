using System;
using System.Collections;
using KRPC.Utils;

namespace KRPC.KerbalAlarmClock
{
    // Binds to the members of Kerbal Alarm Clock 3.14 by reflection. The mod creates its
    // instance in flight, the space center, the tracking station and the editors, the scenes
    // the service is limited to.
    sealed class API : ModAPI
    {
        public API () : base (ServiceInfo.Mods.KerbalAlarmClock.Name, ServiceInfo.Mods.KerbalAlarmClock.Assembly)
        {
        }

        // The mod's AlarmTypeEnum, by value
        internal enum AlarmTypeEnum
        {
            Raw,
            Maneuver,
            ManeuverAuto,
            Apoapsis,
            Periapsis,
            AscendingNode,
            DescendingNode,
            LaunchRendevous,
            Closest,
            SOIChange,
            SOIChangeAuto,
            Transfer,
            TransferModelled,
            Distance,
            Crew,
            EarthTime,
            Contract,
            ContractAuto,
            ScienceLab
        }

        // The mod's AlarmActionEnum, by value
        internal enum AlarmActionEnum
        {
            DoNothingDeleteWhenPassed,
            DoNothing,
            MessageOnly,
            KillWarpOnly,
            KillWarp,
            PauseGame,
            Custom,
            Converted
        }

        static Func<object> instance;
        static Func<bool> apiReady;
        static Func<IList> alarms;
        static Func<object, AlarmTypeEnum, string, double, string> createAlarm;
        static Func<object, string, bool> deleteAlarm;

        // The members of KACAlarm
        internal static class Alarm
        {
            public static Func<object, string> ID;
            public static Field<object, string> Name;
            public static Field<object, string> Notes;
            public static Func<object, AlarmTypeEnum> Type;
            public static Field<object, double> Time;
            public static Field<object, double> Margin;
            public static Field<object, AlarmActionEnum> Action;
            public static Field<object, string> VesselID;
            public static Field<object, string> XferOriginBodyName;
            public static Field<object, string> XferTargetBodyName;
            public static Field<object, bool> Repeat;
            public static Field<object, double> RepeatPeriod;
            public static Func<object, bool> SupportsRepeat;
            public static Func<object, bool> SupportsRepeatPeriod;
            public static Field<object, bool> Enabled;
            public static Func<object, bool> Triggered;
            // The alarm's own PlaySound field is persisted but unused. The mod reads the one in Actions.
            public static Func<object, object> Actions;
            public static Field<object, bool> PlaySound;
        }

        protected override void Bind (ModBinder mod)
        {
            var kac = mod.Type ("KerbalAlarmClock.KerbalAlarmClock");
            instance = kac.StaticGetter<object> ("APIInstance");
            apiReady = kac.StaticGetter<bool> ("APIReady");
            alarms = kac.StaticGetter<IList> ("alarms");
            createAlarm = kac.Method<Func<object, AlarmTypeEnum, string, double, string>> ("CreateAlarm");
            deleteAlarm = kac.Method<Func<object, string, bool>> ("DeleteAlarm");

            var alarm = mod.Type ("KerbalAlarmClock.KACAlarm");
            Alarm.ID = alarm.Getter<object, string> ("ID");
            Alarm.Name = alarm.Field<object, string> ("Name");
            Alarm.Notes = alarm.Field<object, string> ("Notes");
            Alarm.Type = alarm.Getter<object, AlarmTypeEnum> ("TypeOfAlarm");
            Alarm.Time = alarm.Field<object, double> ("AlarmTimeUT");
            Alarm.Margin = alarm.Field<object, double> ("AlarmMarginSecs");
            Alarm.Action = alarm.Field<object, AlarmActionEnum> ("AlarmActionConvert");
            Alarm.VesselID = alarm.Field<object, string> ("VesselID");
            Alarm.XferOriginBodyName = alarm.Field<object, string> ("XferOriginBodyName");
            Alarm.XferTargetBodyName = alarm.Field<object, string> ("XferTargetBodyName");
            Alarm.Repeat = alarm.Field<object, bool> ("RepeatAlarm");
            Alarm.RepeatPeriod = alarm.Field<object, double> ("RepeatAlarmPeriodUT");
            Alarm.SupportsRepeat = alarm.Getter<object, bool> ("SupportsRepeat");
            Alarm.SupportsRepeatPeriod = alarm.Getter<object, bool> ("SupportsRepeatPeriod");
            Alarm.Enabled = alarm.Field<object, bool> ("Enabled");
            Alarm.Triggered = alarm.Getter<object, bool> ("Triggered");
            Alarm.Actions = alarm.Getter<object, object> ("Actions");
            Alarm.PlaySound = mod.Type ("KerbalAlarmClock.AlarmActions").Field<object, bool> ("PlaySound");
        }

        /// <summary>
        /// Why the mod cannot serve a call, or null when it can. The mod creates its instance in
        /// each scene it runs in, then loads the save's alarms.
        /// </summary>
        internal static string WhyNotReady ()
        {
            if (instance () == null)
                return "not running in this scene";
            if (!apiReady ())
                return "loading its alarms";
            return null;
        }

        protected override string NotReady ()
        {
            return WhyNotReady ();
        }

        /// <summary>
        /// The mod's alarms, as KACAlarm objects.
        /// </summary>
        public static IList Alarms {
            get { return alarms (); }
        }

        /// <summary>
        /// The alarm with the given id, or null if there is none.
        /// </summary>
        public static object FindAlarm (string id)
        {
            var list = Alarms;
            for (var i = 0; i < list.Count; i++) {
                if (Alarm.ID (list [i]) == id)
                    return list [i];
            }
            return null;
        }

        /// <summary>
        /// Create an alarm, and return its id.
        /// </summary>
        public static string CreateAlarm (AlarmTypeEnum type, string name, double ut)
        {
            return createAlarm (instance (), type, name, ut);
        }

        /// <summary>
        /// Delete the alarm with the given id.
        /// </summary>
        public static void DeleteAlarm (string id)
        {
            deleteAlarm (instance (), id);
        }
    }
}
