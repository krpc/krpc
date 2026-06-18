using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Alarm manager.
    /// Obtained by calling <see cref="SpaceCenter.AlarmManager"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class AlarmManager : Equatable<AlarmManager>
    {
        internal AlarmManager()
        {
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(AlarmManager other)
        {
            return !ReferenceEquals(other, null);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return 0;
        }

        /// <summary>
        /// A list of all alarms.
        /// </summary>
        [KRPCProperty]
        public IList<Alarm> Alarms
        {
            get
            {
                return AlarmClockScenario.Instance.alarms.Values.Select(alarm => new Alarm(alarm)).ToList();
            }
        }

        /// <summary>
        /// A list of all alarms of the given type.
        /// </summary>
        /// <param name="type">The type of alarm to return.</param>
        [KRPCMethod]
        public IList<Alarm> AlarmsWithType(AlarmType type)
        {
            return AlarmClockScenario.Instance.alarms.Values
                .Where(alarm => alarm.ToAlarmType() == type)
                .Select(alarm => new Alarm(alarm))
                .ToList();
        }

        /// <summary>
        /// Returns the first alarm with the given title, or <c>null</c> if no such
        /// alarm exists. Alarm titles are not guaranteed to be unique; if more than
        /// one alarm shares the given title, the first one found is returned.
        /// </summary>
        /// <param name="name">The title of the alarm to return.</param>
        [KRPCMethod(Nullable = true)]
        public Alarm AlarmWithName(string name)
        {
            var alarm = AlarmClockScenario.Instance.alarms.Values
                .FirstOrDefault(a => a.title == name);
            return alarm != null ? new Alarm(alarm) : null;
        }

        private static AlarmTypeRaw AddRawAlarm(double time, string title, string description)
        {
            AlarmTypeRaw alarm = new AlarmTypeRaw
            {
                title = title,
                description = description,
                ut = Planetarium.GetUniversalTime() + time,
                actions =
                {
                    warp = AlarmActions.WarpEnum.KillWarp,
                    message = AlarmActions.MessageEnum.Yes
                }
            };
            return alarm;
        }

        /// <summary>
        /// Create an alarm.
        /// </summary>
        /// <param name="time">Number of seconds from now that the alarm should trigger.</param>
        /// <param name="title">Title for the alarm.</param>
        /// <param name="description">Description for the alarm.</param>
        [KRPCMethod]
        public static Alarm AddAlarm(double time, string title="Alarm", string description = "")
        {
            var alarm = AddRawAlarm(time, title, description);
            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }

        /// <summary>
        /// Create an alarm linked to a vessel.
        /// </summary>
        /// <param name="time">Number of seconds from now that the alarm should trigger.</param>
        /// <param name="vessel">Vessel to link the alarm to.</param>
        /// <param name="title">Title for the alarm.</param>
        /// <param name="description">Description for the alarm.</param>
        [KRPCMethod]
        public static Alarm AddVesselAlarm(double time, Vessel vessel, string title="Vessel Alarm", string description="")
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            var alarm = AddRawAlarm(time, title, description);
            alarm.vesselId = vessel.InternalVessel.persistentId;
            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }


        /// <summary>
        /// Create an alarm for the given vessel's next apoapsis.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        /// <param name="offset">Time in seconds to offset the alarm by.</param>
        /// <param name="title">Title for the alarm.</param>
        /// <param name="description">Description for the alarm.</param>
        [KRPCMethod]
        public static Alarm AddApoapsisAlarm(Vessel vessel, double offset = 60, string title="Apoapsis Alarm", string description="")
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            AlarmTypeApoapsis alarm = new AlarmTypeApoapsis
            {
                title = title,
                description = description,
                eventOffset = offset,
                actions =
                {
                    warp = AlarmActions.WarpEnum.KillWarp,
                    message = AlarmActions.MessageEnum.Yes
                },
                vesselId = vessel.InternalVessel.persistentId
            };
            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }

        /// <summary>
        /// Create an alarm for the given vessel's next periapsis.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        /// <param name="offset">Time in seconds to offset the alarm by.</param>
        /// <param name="title">Title for the alarm.</param>
        /// <param name="description">Description for the alarm.</param>
        [KRPCMethod]
        public static Alarm AddPeriapsisAlarm(Vessel vessel, double offset = 60, string title="Periapsis Alarm", string description = "")
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            AlarmTypePeriapsis alarm = new AlarmTypePeriapsis
            {
                title = title,
                description = description,
                actions =
                {
                    warp = AlarmActions.WarpEnum.KillWarp,
                    message = AlarmActions.MessageEnum.Yes
                },
                vesselId = vessel.InternalVessel.persistentId
            };
            alarm.eventOffset = offset;
            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }

        /// <summary>
        /// Create an alarm for the given vessel and maneuver node.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        /// <param name="node">The maneuver node.</param>
        /// <param name="offset">Time in seconds to offset the alarm by.</param>
        /// <param name="addBurnTime">Whether the node's burn time should be included in the alarm.</param>
        /// <param name="title">Title for the alarm.</param>
        /// <param name="description">Description for the alarm.</param>
        [KRPCMethod]
        public static Alarm AddManeuverNodeAlarm(Vessel vessel, Node node, double offset = 60, bool addBurnTime = true, string title="Maneuver Node Alarm", string description="" )
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            if (ReferenceEquals (node, null))
                throw new ArgumentNullException (nameof (node));
            AlarmTypeManeuver alarm = new AlarmTypeManeuver
            {
                title = title,
                description = description,
                eventOffset = offset,
                useBurnTimeMargin = addBurnTime,
                actions =
                {
                    warp = AlarmActions.WarpEnum.KillWarp,
                    message = AlarmActions.MessageEnum.Yes
                },
                vesselId = vessel.InternalVessel.persistentId,
            Maneuver = node.InternalNode
            };

            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }

        /// <summary>
        /// Create an alarm for the given vessel's next sphere of influence change.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        /// <param name="offset">Time in seconds to offset the alarm by.</param>
        /// <param name="title">Title for the alarm.</param>
        /// <param name="description">Description for the alarm.</param>
        [KRPCMethod]
        public static Alarm AddSOIAlarm(Vessel vessel, double offset = 60, string title="SOI Change Alarm", string description="" )
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            AlarmTypeSOI alarm = new AlarmTypeSOI
            {
                title = title,
                description = description,
                eventOffset = offset,
                actions =
                {
                    warp = AlarmActions.WarpEnum.KillWarp,
                    message = AlarmActions.MessageEnum.Yes
                },
                vesselId = vessel.InternalVessel.persistentId
            };
            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }

        /// <summary>
        /// Create an alarm for the next planetary transfer window from the vessel's
        /// current parent body to the target body.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        /// <param name="target">The target body.</param>
        /// <param name="title">Title for the alarm.</param>
        /// <param name="description">Description for the alarm.</param>
        /// <remarks>
        /// This relies on KSP's stock transfer-window alarm logic. If KSP cannot
        /// compute a transfer from the vessel's current parent body to the target,
        /// the resulting alarm may not fire at a useful time; in that case the
        /// properties on the returned alarm can still be used to inspect or adjust it.
        /// </remarks>
        [KRPCMethod]
        public static Alarm AddTransferWindowAlarm(Vessel vessel, CelestialBody target, string title="Transfer Window Alarm", string description="")
        {
            if (ReferenceEquals(vessel, null))
                throw new ArgumentNullException(nameof(vessel));
            if (ReferenceEquals(target, null))
                throw new ArgumentNullException(nameof(target));
            AlarmTypeTransferWindow alarm = new AlarmTypeTransferWindow
            {
                title = title,
                description = description,
                actions =
                {
                    warp = AlarmActions.WarpEnum.KillWarp,
                    message = AlarmActions.MessageEnum.Yes
                },
                vesselId = vessel.InternalVessel.persistentId,
                source = vessel.InternalVessel.orbit.referenceBody,
                dest = target.InternalBody
            };
            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }
    }
}
