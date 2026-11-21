using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The power status of a part with a power switch. See <see cref="PowerConsumer.Status" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum PowerStatus
    {
        /// <summary>
        /// Switched off.
        /// </summary>
        Off,
        /// <summary>
        /// On standby, while another control module flies the vehicle or a radiation storm is under way.
        /// </summary>
        Standby,
        /// <summary>
        /// Rebooting after a radiation upset.
        /// </summary>
        Rebooting,
        /// <summary>
        /// On, with power.
        /// </summary>
        On,
        /// <summary>
        /// On, with power for less than five minutes.
        /// </summary>
        Low,
        /// <summary>
        /// Out of power.
        /// </summary>
        Out,
        /// <summary>
        /// Failed from its radiation dose.
        /// </summary>
        Failed
    }
}
