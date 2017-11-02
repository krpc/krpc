using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The type of a crew member.
    /// See <see cref="CrewMember.Type"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum CrewMemberType
    {
        /// <summary>
        /// An applicant for crew.
        /// </summary>
        Applicant,
        /// <summary>
        /// Rocket crew.
        /// </summary>
        Crew,
        /// <summary>
        /// A tourist.
        /// </summary>
        Tourist,
        /// <summary>
        /// An unowned crew member.
        /// </summary>
        Unowned
    }
}
