using System;
using KRPC.Service.Attributes;

namespace KRPC.KerbalAlarmClock
{
    /// <summary>
    /// The type of an alarm.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KerbalAlarmClock")]
    public enum AlarmType
    {
        /// <summary>
        /// An alarm for a specific date/time or a specific period in the future.
        /// </summary>
        Raw,
        /// <summary>
        /// An alarm based on the next maneuver node on the current ships flight path.
        /// This node will be stored and can be restored when you come back to the ship.
        /// </summary>
        Maneuver,
        /// <summary>
        /// See <see cref="Maneuver"/>.
        /// </summary>
        ManeuverAuto,
        /// <summary>
        /// An alarm for furthest part of the orbit from the planet.
        /// </summary>
        Apoapsis,
        /// <summary>
        /// An alarm for nearest part of the orbit from the planet.
        /// </summary>
        Periapsis,
        /// <summary>
        /// Ascending node for the targeted object, or equatorial ascending node.
        /// </summary>
        AscendingNode,
        /// <summary>
        /// Descending node for the targeted object, or equatorial descending node.
        /// </summary>
        DescendingNode,
        /// <summary>
        /// An alarm based on the closest approach of this vessel to the targeted
        /// vessel, some number of orbits into the future.
        /// </summary>
        Closest,
        /// <summary>
        /// An alarm based on the expiry or deadline of contracts in career modes.
        /// </summary>
        Contract,
        /// <summary>
        /// See <see cref="Contract"/>.
        /// </summary>
        ContractAuto,
        /// <summary>
        /// An alarm that is attached to a crew member.
        /// </summary>
        Crew,
        /// <summary>
        /// An alarm that is triggered when a selected target comes within a chosen distance.
        /// </summary>
        Distance,
        /// <summary>
        /// An alarm based on the time in the "Earth" alternative Universe (aka the Real World).
        /// </summary>
        EarthTime,
        /// <summary>
        /// An alarm that fires as your landed craft passes under the orbit of your target.
        /// </summary>
        LaunchRendevous,
        /// <summary>
        /// An alarm manually based on when the next SOI point is on the flight path
        /// or set to continually monitor the active flight path and add alarms as it
        /// detects SOI changes.
        /// </summary>
        SOIChange,
        /// <summary>
        /// See <see cref="SOIChange"/>.
        /// </summary>
        SOIChangeAuto,
        /// <summary>
        /// An alarm based on Interplanetary Transfer Phase Angles, i.e. when should
        /// I launch to planet X? Based on Kosmo Not's post and used in Olex's
        /// Calculator.
        /// </summary>
        Transfer,
        /// <summary>
        /// See <see cref="Transfer"/>.
        /// </summary>
        TransferModelled
    }
}
