using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.KerbalAlarmClock.ExtensionMethods;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.KerbalAlarmClock
{
    /// <summary>
    /// Provides functionality to interact with Kerbal Alarm Clock.
    /// </summary>
    [KRPCService (Id = ServiceInfo.Id, GameScene = GameScene.All & ~GameScene.MissionBuilder)]
    public static class KerbalAlarmClock
    {
        static void CheckAPI ()
        {
            ModAPI.Get<API> ().CheckAvailable ();
        }

        /// <summary>
        /// Whether Kerbal Alarm Clock is available.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.All)]
        public static bool Available {
            get { return ModAPI.Get<API> ().IsAvailable; }
        }

        /// <summary>
        /// A list of all the alarms.
        /// </summary>
        [KRPCProperty]
        public static IList<Alarm> Alarms {
            get {
                CheckAPI ();
                return API.Alarms.Cast<object> ().Select (x => new Alarm (x)).ToList ();
            }
        }

        /// <summary>
        /// Get the alarm with the given <paramref name="name"/>, or <c>null</c>
        /// if no alarms have that name. If more than one alarm has the name,
        /// only returns one of them.
        /// </summary>
        /// <param name="name">Name of the alarm to search for.</param>
        [KRPCProcedure (Nullable = true)]
        public static Alarm AlarmWithName (string name)
        {
            CheckAPI ();
            var alarm = API.Alarms.Cast<object> ().FirstOrDefault (x => API.Alarm.Name.Get (x) == name);
            return alarm != null ? new Alarm (alarm) : null;
        }

        /// <summary>
        /// Get a list of alarms of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">Type of alarm to return.</param>
        [KRPCProcedure]
        public static IList<Alarm> AlarmsWithType (AlarmType type)
        {
            CheckAPI ();
            return API.Alarms.Cast<object> ()
                .Where (x => API.Alarm.Type (x) == type.FromAlarmType ())
                .Select (x => new Alarm (x))
                .ToList ();
        }

        /// <summary>
        /// Create a new alarm and return it.
        /// </summary>
        /// <param name="type">Type of the new alarm.</param>
        /// <param name="name">Name of the new alarm.</param>
        /// <param name="ut">Time at which the new alarm should trigger.</param>
        [KRPCProcedure]
        public static Alarm CreateAlarm (AlarmType type, string name, double ut)
        {
            CheckAPI ();
            var id = API.CreateAlarm (type.FromAlarmType (), name, ut);
            return new Alarm (API.FindAlarm (id));
        }
    }
}
