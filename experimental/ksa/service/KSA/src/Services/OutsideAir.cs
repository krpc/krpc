using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Whether a kitten can breathe the air outside a vehicle. See
    /// <see cref="Flight.OutsideAir" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum OutsideAir
    {
        /// <summary>
        /// Outside the atmosphere, or at a body with none.
        /// </summary>
        NoAtmosphere,
        /// <summary>
        /// An atmosphere with no oxygen.
        /// </summary>
        NoOxygen,
        /// <summary>
        /// An atmosphere whose oxygen pressure is too low to breathe.
        /// </summary>
        TooThin,
        /// <summary>
        /// Breathable air.
        /// </summary>
        Breathable
    }
}
