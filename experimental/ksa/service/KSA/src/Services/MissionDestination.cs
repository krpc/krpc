using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Where a mission goes. See <see cref="Mission.Destination" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum MissionDestination
    {
        /// <summary>
        /// No destination.
        /// </summary>
        None,
        /// <summary>
        /// A low orbit around the home body.
        /// </summary>
        LowOrbit,
        /// <summary>
        /// Another body, given by <see cref="Mission.Body" />.
        /// </summary>
        Body
    }
}
