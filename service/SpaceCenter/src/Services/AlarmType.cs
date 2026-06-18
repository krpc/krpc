using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The type of an alarm. See <see cref="Alarm.Type"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum AlarmType
    {
        /// <summary>
        /// An alarm for a specific date/time or a specific period in the future.
        /// </summary>
        Raw,
        /// <summary>
        /// An alarm for the next apoapsis of a vessel.
        /// </summary>
        Apoapsis,
        /// <summary>
        /// An alarm for the next periapsis of a vessel.
        /// </summary>
        Periapsis,
        /// <summary>
        /// An alarm based on a maneuver node on the vessel's flight path.
        /// </summary>
        Maneuver,
        /// <summary>
        /// An alarm for the next sphere of influence change on the vessel's flight path.
        /// </summary>
        SOIChange,
        /// <summary>
        /// An alarm for the next planetary transfer window from the vessel's current
        /// orbit to a target body.
        /// </summary>
        TransferWindow,
        /// <summary>
        /// The alarm is of a type not recognised by kRPC. Typically this is a type
        /// introduced by a mod.
        /// </summary>
        Unknown
    }
}
