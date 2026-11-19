using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The health of avionics. See <see cref="Avionics.Health" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum AvionicsHealth
    {
        /// <summary>
        /// Working.
        /// </summary>
        Ok,
        /// <summary>
        /// Working, with a dose of at least four fifths of the tolerance.
        /// </summary>
        Worn,
        /// <summary>
        /// Failed for good, at the tolerance.
        /// </summary>
        Failed
    }
}
