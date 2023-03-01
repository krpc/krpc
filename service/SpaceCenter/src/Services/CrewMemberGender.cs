using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A crew member's gender.
    /// See <see cref="CrewMember.Gender"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum CrewMemberGender
    {
        /// <summary>
        /// Male.
        /// </summary>
        Male = ProtoCrewMember.Gender.Male,
        /// <summary>
        /// Female.
        /// </summary>
        Female = ProtoCrewMember.Gender.Female,
    }
}
