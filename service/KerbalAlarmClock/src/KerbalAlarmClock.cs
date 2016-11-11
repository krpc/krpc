using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.KerbalAlarmClock.ExtensionMethods;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPC.KerbalAlarmClock
{
    /// <summary>
    /// This service provides functionality to interact with
    /// <a href="http://forum.kerbalspaceprogram.com/index.php?/topic/22809-12x-kerbal-alarm-clock-v3800-october-12/">Kerbal Alarm Clock</a>.
    /// </summary>
    [KRPCService (GameScene = GameScene.All)]
    public static class KerbalAlarmClock
    {
        static void CheckAPI ()
        {
            if (!KACWrapper.APIReady)
                throw new InvalidOperationException ("Kerbal Alarm Clock is not available");
        }

        /// <summary>
        /// Whether Kerbal Alarm Clock is available.
        /// </summary>
        [KRPCProperty]
        public static bool Available {
            get { return KACWrapper.APIReady; }
        }

        /// <summary>
        /// A list of all the alarms.
        /// </summary>
        [KRPCProperty]
        public static IList<Alarm> Alarms {
            get {
                CheckAPI ();
                return KACWrapper.KAC.Alarms.Select (x => new Alarm (x)).ToList ();
            }
        }

        /// <summary>
        /// Get the alarm with the given <paramref name="name"/>, or <c>null</c>
        /// if no alarms have that name. If more than one alarm has the name,
        /// only returns one of them.
        /// </summary>
        /// <param name="name">Name of the alarm to search for.</param>
        [KRPCProcedure]
        public static Alarm AlarmWithName (string name)
        {
            CheckAPI ();
            var alarm = KACWrapper.KAC.Alarms.FirstOrDefault (x => x.Name == name);
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
            return KACWrapper.KAC.Alarms
                .Where (x => x.AlarmType == type.FromAlarmType ())
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
            var id = KACWrapper.KAC.CreateAlarm (type.FromAlarmType (), name, ut);
            return new Alarm (KACWrapper.KAC.Alarms.First (x => x.ID == id));
        }
    }
}
