using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// How long the crew's supplies last. See <see cref="LifeSupport.Level" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum LifeSupportLevel
    {
        /// <summary>
        /// More than a day of supplies, or no crew.
        /// </summary>
        Ok,
        /// <summary>
        /// A day of supplies or less.
        /// </summary>
        Low,
        /// <summary>
        /// An hour of supplies or less.
        /// </summary>
        Critical,
        /// <summary>
        /// A supply has run out.
        /// </summary>
        Out
    }
}
