using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The type of a communication link.
    /// See <see cref="CommLink.Type"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum CommLinkType
    {
        /// <summary>
        /// Link is to a base station on Kerbin.
        /// </summary>
        Home,
        /// <summary>
        /// Link is to a control source, for example a manned spacecraft.
        /// </summary>
        Control,
        /// <summary>
        /// Link is to a relay satellite.
        /// </summary>
        Relay
    }
}
