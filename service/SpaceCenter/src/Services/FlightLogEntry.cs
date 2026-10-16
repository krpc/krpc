using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// An entry in a crew member's career flight log.
    /// </summary>
    /// <remarks>
    /// See <see cref="CrewMember.CareerLog"/>.
    /// </remarks>
    [KRPCStruct (Service = "SpaceCenter")]
    public struct FlightLogEntry
    {
        internal FlightLogEntry (FlightLog.Entry entry) : this ()
        {
            Flight = entry.flight;
            Type = entry.type;
            Target = entry.target;
        }

        /// <summary>
        /// The flight identifier of the entry.
        /// </summary>
        [KRPCProperty]
        public int Flight { get; set; }

        /// <summary>
        /// The type of the entry, e.g. <c>Orbit</c> or <c>Land</c>.
        /// </summary>
        [KRPCProperty]
        public string Type { get; set; }

        /// <summary>
        /// The name of the body the entry is for.
        /// </summary>
        [KRPCProperty]
        public string Target { get; set; }
    }
}
