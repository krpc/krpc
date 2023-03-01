using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A crew member's suit type.
    /// See <see cref="CrewMember.SuitType"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum SuitType
    {
        /// <summary>
        /// Default.
        /// </summary>
        Default = ProtoCrewMember.KerbalSuit.Default,
        /// <summary>
        /// Vintage.
        /// </summary>
        Vintage = ProtoCrewMember.KerbalSuit.Vintage,
        /// <summary>
        /// Future.
        /// </summary>
        Future = ProtoCrewMember.KerbalSuit.Future,
        /// <summary>
        /// Slim.
        /// </summary>
        Slim = ProtoCrewMember.KerbalSuit.Slim,
    }
}
