using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A crew member's roster status.
    /// See <see cref="CrewMember.RosterStatus"/>.
    /// </summary>
    [Serializable]
    [SuppressMessage ("Gendarme.Rules.Naming", "UseSingularNameInEnumsUnlessAreFlagsRule")]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum RosterStatus
    {
        /// <summary>
        /// Available.
        /// </summary>
        Available = ProtoCrewMember.RosterStatus.Available,
        /// <summary>
        /// Assigned.
        /// </summary>
        Assigned = ProtoCrewMember.RosterStatus.Assigned,
        /// <summary>
        /// Dead.
        /// </summary>
        Dead = ProtoCrewMember.RosterStatus.Dead,
        /// <summary>
        /// Missing.
        /// </summary>
        Missing = ProtoCrewMember.RosterStatus.Missing,
    }
}
