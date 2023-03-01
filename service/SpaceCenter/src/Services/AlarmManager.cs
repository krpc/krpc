using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
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
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public IList<Alarm> Alarms
        {
            get
            {
                return AlarmClockScenario.Instance.alarms.Values.Select(alarm => new Alarm(alarm)).ToList();
            }
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
        public static Alarm AddAlarm(double time, string title="Raw Alarm", string description = "")
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
        public static Alarm AddVesselAlarm(double time, Vessel vessel, string title="Raw Alarm", string description="")
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
        public static Alarm AddApoapsisAlarm(Vessel vessel, double offset = 60, string title="APA Alarm", string description="")
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
        public static Alarm AddPeriapsisAlarm(Vessel vessel, double offset = 60, string title="PEA Alarm", string description = "")
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public static Alarm AddManeuverNodeAlarm(Vessel vessel, Node node, double offset = 60, bool addBurnTime = true, string title="Maneuver Alarm", string description="" )
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
        public static Alarm AddSOIAlarm(Vessel vessel, double offset = 60, string title="SOI Change", string description="" )
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

        // FIXME: Not working
        // /// <summary>
        // /// Create an alarm for the given vessel's next transfer window.
        // /// </summary>
        // [KRPCMethod]
        // public Alarm AddTransferWindowAlarm(Vessel vessel, CelestialBody Target, string title="Transfer Window", string description="")
        // {
        //     AlarmTypeTransferWindow alarm = new AlarmTypeTransferWindow
        //     {
        //         title = title,
        //         description = description,
        //         actions =
        //         {
        //             warp = AlarmActions.WarpEnum.KillWarp,
        //             message = AlarmActions.MessageEnum.Yes
        //         },
        //         vesselId = vessel.InternalVessel.persistentId,
        //         dest = Target.InternalBody,
        //         source = vessel.InternalVessel.orbit.referenceBody
        //     };
        //     AlarmClockScenario.AddAlarm(alarm);
        //     return new Alarm(alarm);
        // }
    }
}
