using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The setting of a power switch. See <see cref="PowerConsumer.Mode" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum PowerMode
    {
        /// <summary>
        /// Switched off.
        /// </summary>
        Off,
        /// <summary>
        /// On standby while another control module flies the vehicle, and on when none can.
        /// </summary>
        Auto,
        /// <summary>
        /// Switched on.
        /// </summary>
        On
    }
}
