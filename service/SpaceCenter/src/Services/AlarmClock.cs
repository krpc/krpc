using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Contracts manager.
    /// Obtained by calling <see cref="SpaceCenter.ContractManager"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class AlarmClock : Equatable<AlarmClock>
    {
        internal AlarmClock()
        {
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(AlarmClock other)
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
        /// Make a Simple Alarm
        /// Parameter 'time' is the number of seconds from now that the alarm should trigger.
        /// </summary>
        [KRPCMethod]
        public Alarm MakeRawAlarm(double time, string title="Raw Alarm", string description = "")
        {
            AlarmTypeRaw alarm = new AlarmTypeRaw
            {
                title = title,
                description = description,
                actions =
                {
                    warp = AlarmActions.WarpEnum.KillWarp,
                    message = AlarmActions.MessageEnum.Yes
                },
                ut = Planetarium.GetUniversalTime() + time
        };
            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }

        /// <summary>
        /// Make a Simple Alarm linked to a Vessel
        /// Parameter 'time' is the number of seconds from now that the alarm should trigger.
        /// </summary>
        [KRPCMethod]
        public Alarm MakeRawAlarmVessel(double time, Vessel V, string title="Raw Alarm", string description="")
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
                },
                vesselId = V.InternalVessel.persistentId
        };
            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }


        /// <summary>
        /// Create an alarm for the given vessel's next Apoapsis
        /// </summary>
        [KRPCMethod]
        public Alarm MakeApaAlarm(Vessel V, double offset = 60, string title="APA Alarm", string description="")
        {
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
                vesselId = V.InternalVessel.persistentId
            };
            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }
        
                /// <summary>
        /// Create an alarm for the given vessel's next Periapsis
        /// </summary>
        [KRPCMethod]
        public Alarm MakePeaAlarm(Vessel V, double offset = 60, string title="PEA Alarm", string description = "")
        {
            AlarmTypePeriapsis alarm = new AlarmTypePeriapsis
            {
                title = title,
                description = description,
                actions =
                {
                    warp = AlarmActions.WarpEnum.KillWarp,
                    message = AlarmActions.MessageEnum.Yes
                },
                vesselId = V.InternalVessel.persistentId
            };
            alarm.eventOffset = offset;
            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }

        /// <summary>
        /// Create an alarm for the given vessel and maneuver node
        /// </summary>
        [KRPCMethod]
        public Alarm MakeManeuverAlarm(Vessel V, Node Man, double offset = 60, bool AddBurnTime = true, string title="Maneuver Alarm", string description="" )
        {
            AlarmTypeManeuver alarm = new AlarmTypeManeuver
            {
                title = title,
                description = description,
                eventOffset = offset,
                useBurnTimeMargin = AddBurnTime,
                actions =
                {
                    warp = AlarmActions.WarpEnum.KillWarp,
                    message = AlarmActions.MessageEnum.Yes
                },
                vesselId = V.InternalVessel.persistentId,
            Maneuver = Man.InternalNode
            };

            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }

        /// <summary>
        /// Create an alarm for the given vessel's next SOI change
        /// </summary>
        [KRPCMethod]
        public Alarm MakeSOIAlarm(Vessel V, double offset = 60, string title="SOI Change", string description="" )
        {
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
                vesselId = V.InternalVessel.persistentId
            };
            AlarmClockScenario.AddAlarm(alarm);
            return new Alarm(alarm);
        }

        //Not working - to be dealt with later
        ///// <summary>
        ///// Create an alarm for the given vessel's next transfer window
        ///// </summary>
        //[KRPCMethod]
        //public Alarm MakeWindowAlarm(Vessel V, CelestialBody Target, string title="Transfer Window", string description="")
        //{
        //    AlarmTypeTransferWindow alarm = new AlarmTypeTransferWindow
        //    {
        //        title = title,
        //        description = description,
        //        actions =
        //        {
        //            warp = AlarmActions.WarpEnum.KillWarp,
        //            message = AlarmActions.MessageEnum.Yes
        //        },
        //        vesselId = V.InternalVessel.persistentId,
        //        dest = Target.InternalBody,
        //        source = V.InternalVessel.orbit.referenceBody
        //};

        //    AlarmClockScenario.AddAlarm(alarm);
        //    return new Alarm(alarm);
        //}

        /// <summary>
        /// Returns a list of all alarms
        /// </summary>
        [KRPCMethod]
        public IList<Alarm> GetAlarms()
        {
            IList<Alarm> Alarms = new List<Alarm>();
            for (int i = 0; i < AlarmClockScenario.Instance.alarms.Count; i++)
            {
                AlarmTypeBase a = AlarmClockScenario.Instance.alarms.At(i);
                Alarm A = new Alarm(a);
                Alarms.Add(A);

            }
            return Alarms;
        }
    }
}
